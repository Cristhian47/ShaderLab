using System.Collections.Generic;
using UnityEngine;

namespace ShaderLab
{
    /// <summary>
    /// Una "exhibicion" del museo: la esfera con el shader + el diorama de ejemplo en un juego.
    /// Guarda el texto educativo y administra copias (instancias) de los materiales para que
    /// los sliders de la interfaz NO modifiquen los assets del proyecto.
    /// </summary>
    public class Exhibit : MonoBehaviour
    {
        public int number;
        public string title;
        [TextArea] public string concept;
        [TextArea] public string whatYouLearn;
        [TextArea] public string challenge;
        [Tooltip("Ruta del archivo .shader dentro del proyecto")]
        public string shaderFile;

        public Shader shader;
        [Header("Version Shader Graph")]
        [Tooltip("Shader Graph equivalente (se usa cuando ShaderLabMode.UseGraphs es verdadero)")]
        public Shader graphShader;
        [Tooltip("Solo Outline: el segundo material (casco invertido) que dibuja el contorno")]
        public Shader graphHullShader;
        public string graphFile;
        [Tooltip("Todos los renderers de esta exhibicion (esfera + diorama)")]
        public Renderer[] renderers;
        [Tooltip("El renderer de la esfera de muestra (su material es el que se edita con los sliders)")]
        public Renderer primaryRenderer;
        public float activationRadius = 9f;

        public static readonly List<Exhibit> All = new List<Exhibit>();

        /// <summary>Material (instancia) que se edita con los sliders.</summary>
        public Material PrimaryMaterial { get; private set; }
        /// <summary>Todos los materiales (instancias) de esta exhibicion que usan el shader.</summary>
        public readonly List<Material> TweakMaterials = new List<Material>();

        /// <summary>Archivo que se esta mostrando segun el modo (grafo o HLSL).</summary>
        public string ActiveFile { get { return ShaderLabMode.UseGraphs && graphShader != null ? graphFile : shaderFile; } }

        Material hull;
        static readonly string[] HullProps = { "_OutlineColor", "_OutlineWidth", "_SmoothNormals" };

        readonly Dictionary<Material, Material> instanceToSource = new Dictionary<Material, Material>();

        void Awake()
        {
            All.Add(this);
            InstanceMaterials();
            ShaderLabMode.Changed += ApplyMode;
            ApplyMode();
        }

        void Update()
        {
            // Outline en Shader Graph: el contorno es OTRO material; le copiamos los parametros del principal.
            if (hull == null || PrimaryMaterial == null) return;
            foreach (string p in HullProps)
            {
                if (!PrimaryMaterial.HasProperty(p) || !hull.HasProperty(p)) continue;
                if (p == "_OutlineColor") hull.SetColor(p, PrimaryMaterial.GetColor(p));
                else hull.SetFloat(p, PrimaryMaterial.GetFloat(p));
            }
        }

        /// <summary>Cambia entre la version HLSL y la de Shader Graph (los valores de las propiedades se conservan).</summary>
        void ApplyMode()
        {
            bool g = ShaderLabMode.UseGraphs && graphShader != null;
            foreach (Material m in TweakMaterials)
                if (m != null) m.shader = g ? graphShader : shader;
            if (graphHullShader != null) SetHull(g);
        }

        void SetHull(bool on)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                var mats = new List<Material>(r.sharedMaterials);
                bool owns = false;
                foreach (Material m in mats) if (m != null && TweakMaterials.Contains(m)) owns = true;
                if (!owns) continue;

                bool has = hull != null && mats.Contains(hull);
                if (on && !has)
                {
                    if (hull == null) hull = new Material(graphHullShader) { name = "Contorno (instancia)" };
                    mats.Add(hull);
                    r.sharedMaterials = mats.ToArray();
                }
                else if (!on && has)
                {
                    mats.Remove(hull);
                    r.sharedMaterials = mats.ToArray();
                }
            }
        }

        void OnDestroy()
        {
            All.Remove(this);
            ShaderLabMode.Changed -= ApplyMode;
            if (hull != null) Destroy(hull);
            foreach (Material m in TweakMaterials)
                if (m != null) Destroy(m);
        }

        void InstanceMaterials()
        {
            var sourceToInstance = new Dictionary<Material, Material>();

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material source = mats[i];
                    if (source == null || source.shader != shader) continue;

                    if (!sourceToInstance.TryGetValue(source, out Material inst))
                    {
                        inst = new Material(source) { name = source.name + " (instancia)" };
                        sourceToInstance[source] = inst;
                        instanceToSource[inst] = source;
                        TweakMaterials.Add(inst);
                    }
                    mats[i] = inst;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }

            if (primaryRenderer != null && primaryRenderer.sharedMaterial != null
                && instanceToSource.ContainsKey(primaryRenderer.sharedMaterial))
                PrimaryMaterial = primaryRenderer.sharedMaterial;
            else if (TweakMaterials.Count > 0)
                PrimaryMaterial = TweakMaterials[0];
        }

        /// <summary>Restaura todos los parametros a los valores originales del material.</summary>
        public void ResetParameters()
        {
            foreach (var pair in instanceToSource)
                pair.Key.CopyPropertiesFromMaterial(pair.Value);
            ApplyMode();
        }
    }
}
