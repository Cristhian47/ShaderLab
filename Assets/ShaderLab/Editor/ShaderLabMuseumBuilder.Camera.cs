using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderLab.EditorTools
{
    /// <summary>La Sala de Camara: 12 terminales (una por efecto fullscreen) y una arena de pruebas.</summary>
    public static partial class ShaderLabMuseumBuilder
    {
        class EffectDef
        {
            public string label;        // texto de la terminal (ASCII)
            public string name;         // nombre mostrado en el HUD
            public string shaderName;   // nombre dentro de Shader.Find
            public string file;
            public string concept, learn, challenge;
        }

        static readonly EffectDef[] EffectDefs =
        {
            new EffectDef { label = "GRAYSCALE", name = "Escala de grises", shaderName = "ShaderLab/Screen/Grayscale", file = "S01_Grayscale",
                concept = "Convierte la imagen a blanco y negro. La intensidad mezcla el original con el resultado.",
                learn = "Leer el color de la pantalla y devolver otro. Luminancia ponderada (el ojo ve más el verde). lerp(a, b, t).",
                challenge = "Deja a color solo lo que sea muy rojo (pista: compara el canal r con g y b)." },
            new EffectDef { label = "INVERT", name = "Invertir colores", shaderName = "ShaderLab/Screen/Invert", file = "S02_Invert",
                concept = "Muestra el negativo de la imagen: cada canal pasa a ser 1 - valor.",
                learn = "Operaciones matemáticas sobre un color. Un color es un vector de 3 números entre 0 y 1.",
                challenge = "Invierte solo los pixeles que sean más brillantes que 0.5." },
            new EffectDef { label = "SEPIA", name = "Sepia", shaderName = "ShaderLab/Screen/Sepia", file = "S03_Sepia",
                concept = "Efecto de foto antigua: una matriz de color tiñe la imagen de marrón y una viñeta suave oscurece los bordes.",
                learn = "Transformar un color con una matriz 3x3 (pesos distintos por canal). Combinar efectos pequeños.",
                challenge = "Cambia los pesos de la matriz para crear un efecto de 'noche azul'." },
            new EffectDef { label = "VIGNETTE", name = "Viñeta", shaderName = "ShaderLab/Screen/Vignette", file = "S04_Vignette",
                concept = "Oscurece (o tiñe) los bordes de la pantalla. Con color rojo sirve para indicar daño.",
                learn = "Coordenadas uv de pantalla. Distancia al centro con length(). smoothstep. Corregir el aspect ratio.",
                challenge = "Anima la intensidad desde un script cuando el jugador recibe daño." },
            new EffectDef { label = "PIXELATE", name = "Pixelado", shaderName = "ShaderLab/Screen/Pixelate", file = "S05_Pixelate",
                concept = "Reduce la resolución aparente con pixeles gigantes, como un juego retro.",
                learn = "floor() sobre las uv para crear una cuadrícula. Muestrear con filtro Point en vez de Linear.",
                challenge = "Haz que el tamaño del pixel dependa de la distancia al centro de la pantalla." },
            new EffectDef { label = "RETRO PALETTE", name = "Paleta retro (Game Boy)", shaderName = "ShaderLab/Screen/Retro Palette", file = "S06_RetroPalette",
                concept = "Reduce la imagen a 4 colores como una Game Boy, con dithering ordenado para simular degradados.",
                learn = "Cuantizar con floor(). Usar un índice para elegir un color de una paleta. Matriz de Bayer para el dithering.",
                challenge = "Cambia la paleta por los 4 colores de tu juego favorito (Tab y sliders)." },
            new EffectDef { label = "CHROMATIC", name = "Aberración cromática", shaderName = "ShaderLab/Screen/Chromatic Aberration", file = "S07_ChromaticAberration",
                concept = "Separa los canales rojo y azul hacia los bordes, como una lente imperfecta. Se usa mucho en impactos y glitches.",
                learn = "Leer la misma textura varias veces con uv distintas. Dirección radial desde el centro. Animar con _Time.",
                challenge = "Haz que el efecto crezca con el cuadrado de la distancia al centro." },
            new EffectDef { label = "UNDERWATER", name = "Bajo el agua", shaderName = "ShaderLab/Screen/Underwater", file = "S08_Underwater",
                concept = "Deforma la imagen con ondas y la tiñe de azul, más oscuro hacia los bordes.",
                learn = "Distorsión de UV con funciones seno. Tinte de color. Animación con el tiempo.",
                challenge = "Añade cáusticas: un patrón de ruido brillante que se mueva sobre la imagen." },
            new EffectDef { label = "BLUR", name = "Desenfoque", shaderName = "ShaderLab/Screen/Blur", file = "S09_Blur",
                concept = "Desenfoque gaussiano: cada pixel se promedia con sus vecinos, dando más peso al centro.",
                learn = "Kernels de convolución y pesos. El tamaño de un texel es 1 / resolución. Bucles dentro de un shader.",
                challenge = "Desenfoca solo lo lejano usando la Depth Texture (como una profundidad de campo)." },
            new EffectDef { label = "EDGE DETECT", name = "Detección de bordes", shaderName = "ShaderLab/Screen/Edge Detect", file = "S10_EdgeDetect",
                concept = "Encuentra los bordes con el operador Sobel y los dibuja encima de la imagen (o solos).",
                learn = "El operador Sobel (gradiente horizontal y vertical). Leer 8 vecinos. Umbral con smoothstep.",
                challenge = "Detecta bordes usando la profundidad en lugar de la luminosidad: quedan mucho más limpios." },
            new EffectDef { label = "CRT", name = "Monitor CRT", shaderName = "ShaderLab/Screen/CRT", file = "S11_CRT",
                concept = "Simula un monitor de tubo: pantalla curva, scanlines, máscara RGB, viñeta y parpadeo.",
                learn = "Distorsión de barril. Scanlines con seno. Máscara RGB por columnas. Componer varios efectos en uno.",
                challenge = "Añade ruido estático animado usando una función hash dependiente de _Time." },
            new EffectDef { label = "DEPTH FOG", name = "Niebla por profundidad", shaderName = "ShaderLab/Screen/Depth Fog", file = "S12_DepthFog",
                concept = "Niebla que depende de la distancia real a la cámara. Activa 'Mostrar solo la profundidad' (Tab) para ver el depth buffer.",
                learn = "La Depth Texture. LinearEyeDepth para convertir a metros. Reconstruir un efecto 3D con datos de pantalla.",
                challenge = "Haz niebla por altura: más densa cerca del suelo (necesitas reconstruir la posición en el mundo)." },
        };

        const string GraphRoot = "Assets/ShaderLab/ShaderGraphs/";

        /// <summary>"Assets/.../Objects/03_PlasmaShield.shader" -> "Assets/ShaderLab/ShaderGraphs/Objects/SG_03_PlasmaShield.shadergraph"</summary>
        static string GraphPathForObject(string hlslFile)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(hlslFile);
            if (name.Contains("02_Outline")) name = "02_Outline_Objeto";
            return GraphRoot + "Objects/SG_" + name + ".shadergraph";
        }

        static Shader LoadGraphShader(string path)
        {
            Shader s = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (s == null) Debug.LogWarning("No se encontro el Shader Graph: " + path + " (se usara solo la version HLSL)");
            return s;
        }

        static List<ScreenEffectInfo> BuildEffectList()
        {
            var list = new List<ScreenEffectInfo>();
            for (int i = 0; i < EffectDefs.Length; i++)
            {
                EffectDef e = EffectDefs[i];
                Shader sh = FindShader(e.shaderName);
                Material mat = NewMaterialAsset("Screen", "Screen_" + e.file, sh);
                list.Add(new ScreenEffectInfo
                {
                    name = e.name,
                    material = mat,
                    concept = e.concept,
                    whatYouLearn = e.learn,
                    challenge = e.challenge,
                    shaderFile = "Assets/ShaderLab/Shaders/Screen/" + e.file + ".shader",
                    graphFile = GraphRoot + "Screen/SG_" + e.file + ".shadergraph",
                    graphShader = LoadGraphShader(GraphRoot + "Screen/SG_" + e.file + ".shadergraph"),
                });
            }
            return list;
        }

        // =================================================================================
        //  SALA DE CAMARA (hacia +X)
        // =================================================================================
        static void BuildCameraHall()
        {
            Transform hall = Group("Sala de Camara", museumRoot, Vector3.zero).transform;
            float cx = LobbyHalf + CamHallLength / 2f;
            float endX = LobbyHalf + CamHallLength;
            float w = CamHallHalfWidth;
            float h = WallHeight;

            Floor("Piso Sala Camara", hall, new Vector3(cx, 0, 0), CamHallLength, w * 2f);
            Wall("Muro Norte", hall, new Vector3(cx, h / 2, w + 0.2f), new Vector3(CamHallLength + 0.8f, h, 0.4f));
            Wall("Muro Sur", hall, new Vector3(cx, h / 2, -w - 0.2f), new Vector3(CamHallLength + 0.8f, h, 0.4f));
            Wall("Muro Fondo", hall, new Vector3(endX + 0.2f, h / 2, 0), new Vector3(0.4f, h, w * 2f + 0.8f));

            float extra = w - LobbyHalf;
            Wall("Muro Entrada N", hall, new Vector3(LobbyHalf + 0.2f, h / 2, LobbyHalf + extra / 2), new Vector3(0.4f, h, extra));
            Wall("Muro Entrada S", hall, new Vector3(LobbyHalf + 0.2f, h / 2, -LobbyHalf - extra / 2), new Vector3(0.4f, h, extra));

            // Carta de ajuste en el muro del fondo: ideal para ver el efecto de cada shader.
            Material pattern = NewMaterialAsset("Museum", "TestPattern", shUnlit);
            pattern.SetTexture("_BaseMap", testPatternTexture);
            var quad = Prim(PrimitiveType.Quad, "Carta de ajuste", hall, new Vector3(endX - 0.02f, 4.2f, 0), new Vector3(26f, 9.5f, 1f), pattern, false);
            quad.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);   // la cara visible mira hacia -X

            Text(hall, "SALA DE SHADERS DE CAMARA", new Vector3(LobbyHalf + 3f, 5.6f, 0), Vector3.left, 0.11f, Magenta);
            Text(hall, "Acercate a una terminal y pulsa E.   X quita el efecto.   Tab = sliders",
                new Vector3(LobbyHalf + 3f, 4.9f, 0), Vector3.left, 0.05f, Color.white);

            BuildArena(hall, new Vector3(34f, 0, 0));

            // Terminales: 6 en el muro sur (efectos 1-6) y 6 en el muro norte (efectos 7-12)
            Material terminalScreen = NewMaterialAsset("Museum", "TerminalScreen", shUnlit);
            terminalScreen.SetColor("_BaseColor", new Color(0.1f, 0.35f, 0.5f));
            for (int k = 0; k < 6; k++)
            {
                float x = 16f + 7.4f * k;
                BuildTerminal(hall, k, new Vector3(x, 0, -w + 1.0f), Vector3.forward, terminalScreen);
                BuildTerminal(hall, 6 + k, new Vector3(x, 0, w - 1.0f), Vector3.back, terminalScreen);
            }

            // Iluminacion de color
            PointLight(hall, "Luz 1", new Vector3(20f, 5.5f, 0), new Color(1f, 0.95f, 0.85f), 2.5f, 16f);
            PointLight(hall, "Luz 2", new Vector3(34f, 6f, 0), new Color(1f, 1f, 1f), 3.0f, 20f);
            PointLight(hall, "Luz 3", new Vector3(48f, 5.5f, 0), new Color(0.5f, 0.85f, 1f), 2.5f, 16f);
            PointLight(hall, "Luz magenta", new Vector3(34f, 3f, -11f), Magenta, 2.2f, 12f);
            PointLight(hall, "Luz ambar", new Vector3(34f, 3f, 11f), Amber, 2.2f, 12f);
            PointLight(hall, "Luz cian", new Vector3(26f, 3f, 11f), Cyan, 1.8f, 10f);
        }

        static void BuildTerminal(Transform hall, int index, Vector3 pos, Vector3 toRoom, Material screenMat)
        {
            EffectDef def = EffectDefs[index];
            Transform t = Group("Terminal " + (index + 1) + " - " + def.label, hall, pos).transform;
            t.rotation = Quaternion.LookRotation(toRoom, Vector3.up);     // +Z local = hacia la sala

            Prim(CUBE, "Base", t, new Vector3(0, 0.5f, 0), new Vector3(1.7f, 1.0f, 0.8f), Lit("DarkMetal", new Color(0.12f, 0.13f, 0.17f), 0.6f, 0.7f));
            Prim(CUBE, "Franja", t, new Vector3(0, 1.02f, 0.3f), new Vector3(1.72f, 0.04f, 0.18f), Glow("GlowMagenta", Magenta), false);

            var screen = Prim(PrimitiveType.Quad, "Pantalla", t, new Vector3(0, 1.45f, 0.1f), new Vector3(1.5f, 0.85f, 1f), screenMat, false);
            screen.transform.localRotation = Quaternion.Euler(20, 180, 0);       // mira hacia +Z local, inclinada
            Prim(CUBE, "Soporte", t, new Vector3(0, 1.15f, -0.05f), new Vector3(0.12f, 0.3f, 0.12f), Lit("DarkMetal", new Color(0.12f, 0.13f, 0.17f), 0.6f, 0.7f), false);

            Vector3 toViewer = toRoom;
            Text(t, (index + 1) + ". " + def.label, t.TransformPoint(new Vector3(0, 2.35f, 0.2f)), toViewer, 0.075f, Color.white);

            var term = t.gameObject.AddComponent<ScreenEffectTerminal>();
            term.effectIndex = index;
            term.screen = screen.GetComponent<Renderer>();
        }

        /// <summary>Escenario de pruebas: colores saturados, formas y movimiento para que cualquier efecto se note.</summary>
        static void BuildArena(Transform hall, Vector3 c)
        {
            Transform arena = Group("Arena de pruebas", hall, c).transform;

            // Tótem central: cubos de colores girando en sentidos distintos
            Color[] totemColors =
            {
                new Color(0.9f, 0.15f, 0.15f), new Color(1f, 0.75f, 0.1f),
                new Color(0.15f, 0.75f, 0.3f), new Color(0.2f, 0.45f, 0.95f)
            };
            for (int i = 0; i < 4; i++)
            {
                GameObject cube = Prim(CUBE, "Totem " + (i + 1), arena, new Vector3(0, 0.9f + i * 1.25f, 0),
                    V(1.9f - i * 0.3f), Lit("Totem" + i, totemColors[i], 0.4f));
                var mo = Add<ShaderLabMotion>(cube);
                mo.spin = new Vector3(0, (i % 2 == 0 ? 35f : -50f), 0);
            }
            Prim(SPHERE, "Corona", arena, new Vector3(0, 5.6f, 0), V(1.0f), Glow("GlowAmber", Amber, 2.5f), false);

            // Anillo de esferas de colores sobre pedestales
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 6f, 0, Mathf.Sin(a) * 6f);
                Color col = Color.HSVToRGB(i / 8f, 0.85f, 0.95f);
                Prim(CYL, "Pedestal " + i, arena, p + new Vector3(0, 0.4f, 0), new Vector3(1.0f, 0.4f, 1.0f), Lit("PedestalPlain", new Color(0.8f, 0.8f, 0.85f), 0.3f));
                GameObject s = Prim(SPHERE, "Esfera de color " + i, arena, p + new Vector3(0, 1.5f, 0), V(1.3f), Lit("Ball" + i, col, 0.6f), false);
                var mo = Add<ShaderLabMotion>(s);
                mo.bobAmplitude = new Vector3(0, 0.25f, 0);
                mo.bobSpeed = 1.2f;
                mo.phase = i * 0.8f;
            }

            // Satelites en orbita (movimiento rapido: se nota el blur, el pixelado, etc.)
            for (int i = 0; i < 3; i++)
            {
                GameObject s = Prim(SPHERE, "Satelite " + i, arena, new Vector3(0, 3f, 0), V(0.5f), Glow("GlowCyan", Cyan, 2.5f), false);
                var mo = Add<ShaderLabMotion>(s);
                mo.orbit = true;
                mo.orbitCenter = new Vector3(0, 3.2f, 0);
                mo.orbitRadius = 3.2f;
                mo.orbitSpeed = 1.6f;
                mo.phase = i * 2.09f;
                mo.orbitHeightAmplitude = 0.8f;
            }

            // Columnas hacia el fondo de la sala: dan perspectiva (y se ve la niebla por profundidad)
            Material pillar = Lit("Pillar", new Color(0.82f, 0.84f, 0.9f), 0.3f);
            for (int i = 0; i < 6; i++)
            {
                float x = -16f + i * 6.5f - 1f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = side * (i % 2 == 0 ? 11.5f : 13.5f);
                    Prim(CYL, "Columna", arena, new Vector3(x, 3f, z), new Vector3(1.0f, 3f, 1.0f), pillar);
                    Color cap = Color.HSVToRGB((i * 0.17f + (side > 0 ? 0.5f : 0f)) % 1f, 0.9f, 1f);
                    Prim(CYL, "Capitel", arena, new Vector3(x, 6.05f, z), new Vector3(1.15f, 0.06f, 1.15f), Glow("Cap" + i + (side > 0 ? "N" : "S"), cap), false);
                }
            }
        }
    }
}
