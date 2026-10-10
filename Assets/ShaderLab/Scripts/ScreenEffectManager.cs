using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace ShaderLab
{
    /// <summary>Datos de un efecto de camara (shader fullscreen) y su texto educativo.</summary>
    [Serializable]
    public class ScreenEffectInfo
    {
        public string name;
        public Material material;
        [TextArea] public string concept;
        [TextArea] public string whatYouLearn;
        [TextArea] public string challenge;
        public string shaderFile;
        [Header("Version Shader Graph")]
        public Shader graphShader;
        public string graphFile;

        public string ActiveFile { get { return ShaderLabMode.UseGraphs && graphShader != null ? graphFile : shaderFile; } }
    }

    /// <summary>
    /// Activa y desactiva los efectos de camara del museo.
    ///
    /// COMO FUNCIONA: en el Universal Renderer hay un "Full Screen Pass Renderer Feature" que
    /// dibuja un material sobre toda la pantalla. Aqui solo le cambiamos el material
    /// (feature.passMaterial) cuando el jugador elige un efecto, o se lo quitamos (null) para
    /// ver la imagen normal.
    ///
    /// Teclas: X o Backspace = quitar efecto | 1..9, 0, -, = = efecto rapido.
    /// </summary>
    public class ScreenEffectManager : MonoBehaviour
    {
        public static ScreenEffectManager Instance { get; private set; }

        public List<ScreenEffectInfo> effects = new List<ScreenEffectInfo>();
        [Tooltip("Los Full Screen Pass Renderer Features (uno por cada Universal Renderer del proyecto)")]
        public List<FullScreenPassRendererFeature> features = new List<FullScreenPassRendererFeature>();

        public int ActiveIndex { get; private set; } = -1;
        public Material ActiveMaterial => ActiveIndex >= 0 && ActiveIndex < runtimeMaterials.Length ? runtimeMaterials[ActiveIndex] : null;
        public ScreenEffectInfo ActiveInfo => ActiveIndex >= 0 && ActiveIndex < effects.Count ? effects[ActiveIndex] : null;

        Material[] runtimeMaterials = new Material[0];

        static readonly Key[] QuickKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6,
            Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Minus, Key.Equals
        };

        void Awake()
        {
            Instance = this;
            // Trabajamos con COPIAS de los materiales para que los sliders no modifiquen los assets.
            runtimeMaterials = new Material[effects.Count];
            for (int i = 0; i < effects.Count; i++)
            {
                Material src = effects[i].material;
                runtimeMaterials[i] = src != null ? new Material(src) { name = src.name + " (instancia)" } : null;
            }
            ShaderLabMode.Changed += ApplyMode;
            ApplyMode();
            Clear();
        }

        /// <summary>Cambia cada material entre su shader HLSL y su Shader Graph (conserva los valores).</summary>
        void ApplyMode()
        {
            for (int i = 0; i < effects.Count; i++)
            {
                Material m = runtimeMaterials[i];
                ScreenEffectInfo info = effects[i];
                if (m == null || info.material == null) continue;
                m.shader = ShaderLabMode.UseGraphs && info.graphShader != null ? info.graphShader : info.material.shader;
            }
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || ShaderLabPlayer.UiMode) return;

            if (kb.xKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
                Clear();

            for (int i = 0; i < QuickKeys.Length && i < effects.Count; i++)
            {
                if (kb[QuickKeys[i]].wasPressedThisFrame)
                    Apply(i);
            }
        }

        public void Apply(int index)
        {
            if (index < 0 || index >= effects.Count || runtimeMaterials[index] == null) return;
            ActiveIndex = index;
            foreach (FullScreenPassRendererFeature f in features)
            {
                if (f == null) continue;
                f.passMaterial = runtimeMaterials[index];
                f.SetActive(true);
            }
        }

        public void Toggle(int index)
        {
            if (ActiveIndex == index) Clear();
            else Apply(index);
        }

        /// <summary>Quita el efecto: sin material, el feature no dibuja nada.</summary>
        public void Clear()
        {
            ActiveIndex = -1;
            foreach (FullScreenPassRendererFeature f in features)
            {
                if (f == null) continue;
                f.passMaterial = null;
            }
        }

        /// <summary>Restaura los parametros del efecto activo a sus valores originales.</summary>
        public void ResetActiveParameters()
        {
            if (ActiveIndex < 0) return;
            Material src = effects[ActiveIndex].material;
            if (src != null && runtimeMaterials[ActiveIndex] != null)
            {
                runtimeMaterials[ActiveIndex].CopyPropertiesFromMaterial(src);
                ApplyMode();
            }
        }

        void OnDestroy()
        {
            ShaderLabMode.Changed -= ApplyMode;
            Clear();    // dejamos el Renderer del proyecto limpio al salir de Play
            foreach (Material m in runtimeMaterials)
                if (m != null) Destroy(m);
            if (Instance == this) Instance = null;
        }
    }
}
