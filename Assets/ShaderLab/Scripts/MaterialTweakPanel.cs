using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderLab
{
    /// <summary>
    /// Dibuja (con IMGUI) sliders para TODAS las propiedades de un material: lee la lista de
    /// propiedades directamente del shader. Sirve para ver en vivo, en clase, que hace cada parametro.
    /// </summary>
    public static class MaterialTweakPanel
    {
        public static void Draw(Material primary, IList<Material> group, ref Vector2 scroll, float width, float height)
        {
            if (primary == null) return;
            Shader sh = primary.shader;

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(width), GUILayout.Height(height));

            int count = sh.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if ((sh.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) != 0) continue;

                ShaderPropertyType type = sh.GetPropertyType(i);
                string pname = sh.GetPropertyName(i);
                string label = sh.GetPropertyDescription(i);
                string[] attrs = sh.GetPropertyAttributes(i);
                bool isToggle = HasAttribute(attrs, "Toggle");
                bool isInt = HasAttribute(attrs, "IntRange");
                bool isHdr = HasAttribute(attrs, "HDR");

                if (type == ShaderPropertyType.Range || type == ShaderPropertyType.Float)
                    DrawFloat(primary, group, pname, label, type, sh, i, isToggle, isInt, width);
                else if (type == ShaderPropertyType.Color)
                    DrawColor(primary, pname, label, isHdr, width);
            }

            GUILayout.EndScrollView();
        }

        static bool HasAttribute(string[] attrs, string name)
        {
            if (attrs == null) return false;
            foreach (string a in attrs)
                if (a != null && a.StartsWith(name)) return true;
            return false;
        }

        static void DrawFloat(Material primary, IList<Material> group, string pname, string label,
            ShaderPropertyType type, Shader sh, int index, bool isToggle, bool isInt, float width)
        {
            float current = primary.GetFloat(pname);
            float next = current;

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(width * 0.44f));

            if (isToggle)
            {
                next = GUILayout.Toggle(current > 0.5f, " activado") ? 1f : 0f;
            }
            else
            {
                float min = 0f, max = Mathf.Max(1f, Mathf.Abs(current) * 3f);
                if (type == ShaderPropertyType.Range)
                {
                    Vector2 limits = sh.GetPropertyRangeLimits(index);
                    min = limits.x;
                    max = limits.y;
                }
                next = GUILayout.HorizontalSlider(current, min, max, GUILayout.Width(width * 0.34f));
                if (isInt) next = Mathf.Round(next);
                GUILayout.Label(next.ToString("0.##"), GUILayout.Width(width * 0.1f));
            }
            GUILayout.EndHorizontal();

            if (!Mathf.Approximately(next, current))
            {
                // Aplicamos el cambio a todos los materiales "enlazados": los que tenian el mismo
                // valor que el principal. Asi una variante con un valor distinto a proposito se respeta.
                foreach (Material m in group)
                {
                    if (m == null) continue;
                    if (m == primary || Mathf.Approximately(m.GetFloat(pname), current))
                        m.SetFloat(pname, next);
                }
                primary.SetFloat(pname, next);
            }
        }

        static void DrawColor(Material primary, string pname, string label, bool isHdr, float width)
        {
            Color current = primary.GetColor(pname);
            Color next = current;
            float max = isHdr ? 4f : 1f;
            float sliderWidth = width * 0.11f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(width * 0.36f));

            Rect swatch = GUILayoutUtility.GetRect(22, 18, GUILayout.Width(22));
            Color prev = GUI.color;
            GUI.color = new Color(Mathf.Clamp01(current.r), Mathf.Clamp01(current.g), Mathf.Clamp01(current.b), 1f);
            GUI.DrawTexture(swatch, Texture2D.whiteTexture);
            GUI.color = prev;

            next.r = GUILayout.HorizontalSlider(current.r, 0f, max, GUILayout.Width(sliderWidth));
            next.g = GUILayout.HorizontalSlider(current.g, 0f, max, GUILayout.Width(sliderWidth));
            next.b = GUILayout.HorizontalSlider(current.b, 0f, max, GUILayout.Width(sliderWidth));
            GUILayout.EndHorizontal();

            if (next != current)
                primary.SetColor(pname, next);
        }
    }
}
