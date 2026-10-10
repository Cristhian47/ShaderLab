using System.Collections.Generic;
using UnityEngine;

namespace ShaderLab
{
    /// <summary>
    /// Interfaz del museo dibujada con IMGUI (OnGUI): no necesita Canvas ni assets.
    ///   - Esquina superior izquierda: sala actual.
    ///   - Esquina superior derecha: efecto de camara activo.
    ///   - Panel inferior derecho: ficha educativa de la exhibicion/efecto cercano.
    ///   - Tab: modo cursor con sliders para cambiar los parametros del shader en vivo.
    /// </summary>
    public class MuseumHUD : MonoBehaviour
    {
        public MuseumDirector director;

        GUIStyle titleStyle, bodyStyle, sectionStyle, fileStyle, hintStyle, bigStyle;
        Vector2 tweakScroll;

        static readonly Color Panel = new Color(0.04f, 0.05f, 0.09f, 0.86f);
        static readonly Color Accent = new Color(0.30f, 0.85f, 1f, 1f);

        void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, wordWrap = true };
            titleStyle.normal.textColor = Color.white;

            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            bodyStyle.normal.textColor = new Color(0.9f, 0.92f, 0.96f);

            sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            sectionStyle.normal.textColor = Accent;

            fileStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            fileStyle.normal.textColor = new Color(1f, 0.8f, 0.35f);

            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            hintStyle.normal.textColor = new Color(0.85f, 0.88f, 0.95f);

            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bigStyle.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            if (director == null) return;
            EnsureStyles();

            // Dibujamos en un "lienzo virtual" de 1080 px de alto y lo escalamos a la pantalla real.
            float s = Mathf.Max(0.6f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s;
            float H = Screen.height / s;

            ScreenEffectManager fx = ScreenEffectManager.Instance;

            DrawZone(director.Zone);
            DrawActiveEffect(W, fx);
            DrawControls(H);

            if (!ShaderLabPlayer.UiMode)
            {
                // Mira (crosshair)
                Box(new Rect(W * 0.5f - 3, H * 0.5f - 3, 6, 6), new Color(1, 1, 1, 0.75f));
                DrawTerminalPrompt(W, H, fx);
            }

            // ---- Ficha educativa ----
            ScreenEffectTerminal term = director.CurrentTerminal;
            Exhibit ex = director.CurrentExhibit;
            if (term != null && fx != null && term.effectIndex < fx.effects.Count)
            {
                ScreenEffectInfo info = fx.effects[term.effectIndex];
                DrawInfoPanel(W, H, (term.effectIndex + 1) + ".  " + info.name, info.ActiveFile,
                    info.concept, info.whatYouLearn, info.challenge);
            }
            else if (ex != null)
            {
                DrawInfoPanel(W, H, ex.number + ".  " + ex.title, ex.ActiveFile,
                    ex.concept, ex.whatYouLearn, ex.challenge);
            }

            if (ShaderLabPlayer.UiMode)
                DrawTweakPanel(W, H, fx, ex);
        }

        // -------------------------------------------------------------------------------
        void DrawZone(MuseumZone zone)
        {
            string text = zone == MuseumZone.Lobby ? "LOBBY"
                        : zone == MuseumZone.ObjectHall ? "SALA DE SHADERS DE OBJETO"
                        : "SALA DE SHADERS DE CAMARA";
            text += "   ·   " + ShaderLabMode.Label;
            Rect r = new Rect(24, 20, 700, 52);
            Box(r, Panel);
            GUI.Label(new Rect(r.x + 16, r.y + 6, r.width - 24, r.height), text, sectionStyle);
        }

        void DrawActiveEffect(float W, ScreenEffectManager fx)
        {
            if (fx == null) return;
            string text = fx.ActiveIndex >= 0
                ? "Efecto de camara: <b>" + fx.ActiveInfo.name + "</b>    [X] quitar"
                : "Efecto de camara: ninguno";
            Rect r = new Rect(W - 604, 20, 580, 52);
            Box(r, Panel);
            var st = new GUIStyle(hintStyle) { richText = true, alignment = TextAnchor.MiddleRight, fontSize = 19 };
            GUI.Label(new Rect(r.x, r.y, r.width - 16, r.height), text, st);
        }

        void DrawControls(float H)
        {
            string txt = ShaderLabPlayer.UiMode
                ? "MODO CURSOR   |   [Tab] volver a caminar"
                : "WASD mover  ·  Shift correr  ·  Espacio saltar  ·  [E] terminal  ·  [X] quitar efecto  ·  [1-0] efectos  ·  [Tab] sliders  ·  [G] HLSL / Shader Graph  ·  [O] abrir grafo (Editor)";
            Rect r = new Rect(24, H - 60, 1500, 40);
            Box(r, Panel);
            GUI.Label(new Rect(r.x + 14, r.y + 6, r.width - 20, r.height), txt, hintStyle);
        }

        void DrawTerminalPrompt(float W, float H, ScreenEffectManager fx)
        {
            ScreenEffectTerminal t = director.CurrentTerminal;
            if (t == null || fx == null || t.effectIndex >= fx.effects.Count) return;

            bool active = fx.ActiveIndex == t.effectIndex;
            string text = (active ? "[E] Quitar efecto:  " : "[E] Activar efecto:  ") + fx.effects[t.effectIndex].name;
            Rect r = new Rect(W * 0.5f - 300, H - 190, 600, 60);
            Box(r, Panel);
            GUI.Label(r, text, bigStyle);
        }

        void DrawInfoPanel(float W, float H, string heading, string file, string concept, string learn, string challenge)
        {
            float w = 640f, pad = 20f, inner = w - pad * 2f;
            var segs = new List<KeyValuePair<GUIStyle, string>>
            {
                new KeyValuePair<GUIStyle, string>(titleStyle, heading),
                new KeyValuePair<GUIStyle, string>(fileStyle, "Archivo:  " + file),
                new KeyValuePair<GUIStyle, string>(sectionStyle, "QUE HACE"),
                new KeyValuePair<GUIStyle, string>(bodyStyle, concept),
                new KeyValuePair<GUIStyle, string>(sectionStyle, "QUE APRENDES"),
                new KeyValuePair<GUIStyle, string>(bodyStyle, learn),
                new KeyValuePair<GUIStyle, string>(sectionStyle, "RETO PARA CLASE"),
                new KeyValuePair<GUIStyle, string>(bodyStyle, challenge),
            };

            float total = pad * 2f;
            foreach (var seg in segs)
                total += seg.Key.CalcHeight(new GUIContent(seg.Value), inner) + 4f;

            float x = W - w - 24f;
            float y = Mathf.Max(90f, H - total - 80f);
            Box(new Rect(x, y, w, total), Panel);
            Box(new Rect(x, y, 5, total), Accent);

            float cy = y + pad;
            foreach (var seg in segs)
            {
                float h = seg.Key.CalcHeight(new GUIContent(seg.Value), inner);
                GUI.Label(new Rect(x + pad, cy, inner, h), seg.Value, seg.Key);
                cy += h + 4f;
            }
        }

        void DrawTweakPanel(float W, float H, ScreenEffectManager fx, Exhibit ex)
        {
            // Que material editamos: el del efecto de camara activo (si estas en la sala de camara)
            // o el de la exhibicion cercana.
            Material primary = null;
            IList<Material> group = null;
            string title = "";
            System.Action reset = null;

            bool preferEffect = director.Zone == MuseumZone.CameraHall || ex == null;
            if (preferEffect && fx != null && fx.ActiveMaterial != null)
            {
                primary = fx.ActiveMaterial;
                group = new List<Material> { primary };
                title = "Efecto: " + fx.ActiveInfo.name;
                reset = fx.ResetActiveParameters;
            }
            else if (ex != null && ex.PrimaryMaterial != null)
            {
                primary = ex.PrimaryMaterial;
                group = ex.TweakMaterials;
                title = ex.number + ". " + ex.title;
                reset = ex.ResetParameters;
            }

            float w = 600f;
            float h = H - 220f;
            Rect r = new Rect(24, 90, w, h);
            Box(r, Panel);
            Box(new Rect(r.x, r.y, 5, r.height), Accent);

            GUI.Label(new Rect(r.x + 18, r.y + 10, w - 36, 36), "PARAMETROS EN VIVO", sectionStyle);

            if (primary == null)
            {
                GUI.Label(new Rect(r.x + 18, r.y + 56, w - 36, 200),
                    "Acercate a una exhibicion de la Sala de Objetos, o activa un efecto en la Sala de Camara, y vuelve a pulsar Tab.",
                    bodyStyle);
                return;
            }

            GUI.Label(new Rect(r.x + 18, r.y + 42, w - 220, 36), title, titleStyle);
            if (GUI.Button(new Rect(r.xMax - 170, r.y + 14, 150, 34), "Restablecer"))
                reset?.Invoke();

            GUILayout.BeginArea(new Rect(r.x + 18, r.y + 96, w - 36, h - 110));
            MaterialTweakPanel.Draw(primary, group, ref tweakScroll, w - 50, h - 120);
            GUILayout.EndArea();
        }

        static void Box(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
