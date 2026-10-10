using System.Collections.Generic;
using UnityEngine;

namespace ShaderLab
{
    /// <summary>
    /// Una terminal de la Sala de Camara. El jugador se acerca y pulsa E para activar el efecto.
    /// La pantalla de la terminal cambia de color cuando su efecto esta activo.
    /// </summary>
    public class ScreenEffectTerminal : MonoBehaviour
    {
        public int effectIndex;
        public Renderer screen;
        public Color idleColor = new Color(0.1f, 0.35f, 0.5f, 1f);
        public Color activeColor = new Color(1f, 0.8f, 0.1f, 1f);

        public static readonly List<ScreenEffectTerminal> All = new List<ScreenEffectTerminal>();

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;
        int lastState = -1;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            if (screen == null) return;
            ScreenEffectManager mgr = ScreenEffectManager.Instance;
            int state = (mgr != null && mgr.ActiveIndex == effectIndex) ? 1 : 0;
            if (state == lastState) return;
            lastState = state;

            if (block == null) block = new MaterialPropertyBlock();
            screen.GetPropertyBlock(block);
            block.SetColor(BaseColorId, state == 1 ? activeColor : idleColor);
            screen.SetPropertyBlock(block);
        }
    }
}
