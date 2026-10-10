using UnityEngine;

namespace ShaderLab
{
    /// <summary>
    /// Ejemplo de uso del shader Outline en un juego: el objeto al que apuntas con la mira
    /// se resalta (color amarillo y contorno mas grueso). Usa un MaterialPropertyBlock para
    /// cambiar propiedades de UN solo renderer sin tocar el material compartido.
    /// </summary>
    public class HoverOutlineTarget : MonoBehaviour
    {
        public Renderer[] targets;
        public Color hoverColor = new Color(1f, 0.85f, 0.15f, 1f);
        public float hoverWidthMultiplier = 2f;

        /// <summary>Lo asigna el MuseumDirector cada frame segun lo que mira el jugador.</summary>
        public static HoverOutlineTarget Current;

        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        MaterialPropertyBlock block;
        bool wasHovered;

        void Awake()
        {
            if (targets == null || targets.Length == 0) targets = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            bool hovered = Current == this;
            if (hovered)
            {
                foreach (Renderer r in targets)
                {
                    if (r == null) continue;
                    Material m = r.sharedMaterial;
                    float baseWidth = m != null && m.HasProperty(OutlineWidthId) ? m.GetFloat(OutlineWidthId) : 0.03f;
                    r.GetPropertyBlock(block);
                    block.SetColor(OutlineColorId, hoverColor);
                    block.SetFloat(OutlineWidthId, baseWidth * hoverWidthMultiplier);
                    r.SetPropertyBlock(block);
                }
            }
            else if (wasHovered)
            {
                foreach (Renderer r in targets)
                    if (r != null) r.SetPropertyBlock(null);   // volver a las propiedades normales del material
            }
            wasHovered = hovered;
        }
    }
}
