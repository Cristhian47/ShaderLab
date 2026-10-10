using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShaderLab.EditorTools
{
    /// <summary>
    /// Construye TODO el museo con un clic:  menu  Shader Lab > Construir museo.
    ///   1. Configura URP (Depth/Opaque texture + Full Screen Pass Renderer Feature).
    ///   2. Genera texturas y materiales.
    ///   3. Crea la escena: lobby, Sala de Objetos (9 exhibiciones) y Sala de Camara (12 efectos).
    /// Se puede ejecutar de nuevo cuando quieras: reconstruye la escena desde cero.
    /// </summary>
    public static partial class ShaderLabMuseumBuilder
    {
        const string Root = "Assets/ShaderLab";
        const string ScenePath = Root + "/Scenes/ShaderLabMuseum.unity";
        const string FeatureName = "ShaderLab Screen Effect";

        // ---- Geometria del museo (metros) ----
        const float LobbyHalf = 10f;          // lobby: 20 x 20
        const float WallHeight = 7f;
        const float ObjHallLength = 70f;      // x desde -10 hasta -80
        const float ObjHallHalfWidth = 14f;
        const float CamHallLength = 48f;      // x desde 10 hasta 58
        const float CamHallHalfWidth = 18f;
        const float DoorHalfWidth = 3f;
        const float DoorHeight = 4.6f;

        static Transform museumRoot;
        static Font labFont;
        static List<FullScreenPassRendererFeature> features;
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        static Shader shLit, shUnlit;

        // =================================================================================
        //  PUNTO DE ENTRADA
        // =================================================================================
        [MenuItem("Shader Lab/Construir museo (escena completa)", priority = 0)]
        public static void BuildMuseum()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                Progress("Preparando carpetas...", 0.02f);
                EnsureFolder(Root + "/Scenes");
                EnsureFolder(Root + "/Textures");
                EnsureFolder(Root + "/Meshes");
                EnsureFolder(Root + "/Materials/Objects");
                EnsureFolder(Root + "/Materials/Screen");
                EnsureFolder(Root + "/Materials/Museum");

                // Escena vacia primero: asi podemos recrear materiales sin dejar referencias rotas.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                matCache.Clear();

                Progress("Configurando URP (Depth Texture + Full Screen Pass)...", 0.08f);
                features = SetupUrp();

                Progress("Cargando shaders...", 0.14f);
                LoadShaders();

                Progress("Generando texturas y mallas...", 0.2f);
                CreateTextures();
                CreateWaterMesh();
                CreateObjectMaterials();

                labFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                Progress("Construyendo lobby...", 0.3f);
                museumRoot = new GameObject("Shader Lab Museum").transform;
                BuildEnvironment();
                Transform player = BuildPlayer();
                BuildLobby();

                Progress("Construyendo Sala de Objetos...", 0.45f);
                BuildObjectHall();

                Progress("Construyendo Sala de Camara...", 0.75f);
                BuildCameraHall();

                Progress("Conectando sistemas...", 0.92f);
                BuildSystems(player);

                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene, ScenePath);
                AddSceneToBuildSettings(ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Shader Lab",
                    "Museo construido.\n\nEscena: " + ScenePath +
                    "\n\nPulsa Play y camina con WASD. Lee Assets/ShaderLab/Docs/GUIA_DE_CLASE.md para la guia de la clase.",
                    "Listo");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Shader Lab - Error",
                    "No se pudo construir el museo:\n\n" + e.Message + "\n\nRevisa la consola para ver los detalles.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Shader Lab/Abrir escena del museo", priority = 1)]
        public static void OpenMuseumScene()
        {
            if (!File.Exists(Path.Combine(ProjectRoot(), ScenePath)))
            {
                EditorUtility.DisplayDialog("Shader Lab", "Todavia no existe la escena. Usa primero 'Construir museo'.", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        // =================================================================================
        //  UTILIDADES DE ARCHIVOS
        // =================================================================================
        static void Progress(string msg, float t) => EditorUtility.DisplayProgressBar("Shader Lab", msg, t);

        static string ProjectRoot() => Directory.GetParent(Application.dataPath).FullName;

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void AddSceneToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list)
                if (s.path == path) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static Shader FindShader(string name)
        {
            Shader s = Shader.Find(name);
            if (s == null)
                throw new Exception("No se encontró el shader '" + name + "'. Revisa la consola: probablemente algún archivo .shader tiene un error de compilación.");
            return s;
        }

        // =================================================================================
        //  CONFIGURACION DE URP
        // =================================================================================
        static List<FullScreenPassRendererFeature> SetupUrp()
        {
            var result = new List<FullScreenPassRendererFeature>();

            // 1) Cada Universal Renderer del proyecto recibe un Full Screen Pass Renderer Feature.
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) continue;

                FullScreenPassRendererFeature feature = null;
                foreach (ScriptableRendererFeature f in data.rendererFeatures)
                {
                    if (f is FullScreenPassRendererFeature fs && fs.name == FeatureName) { feature = fs; break; }
                }
                if (feature == null) feature = AddFeature(data);

                feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
                feature.fetchColorBuffer = true;                       // el shader necesita leer la imagen de la camara
                feature.requirements = ScriptableRenderPassInput.Depth; // y la profundidad (Depth Fog)
                feature.passMaterial = null;                           // sin efecto hasta que el jugador elija uno
                feature.SetActive(true);
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(data);
                result.Add(feature);
            }

            // 2) Los URP Assets necesitan Depth Texture (escudo, agua) y Opaque Texture.
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                var so = new SerializedObject(asset);
                SerializedProperty depth = so.FindProperty("m_RequireDepthTexture");
                SerializedProperty opaque = so.FindProperty("m_RequireOpaqueTexture");
                if (depth != null) depth.boolValue = true; else Debug.LogWarning("No se encontró m_RequireDepthTexture en " + asset.name + ": activa 'Depth Texture' a mano.");
                if (opaque != null) opaque.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();

            if (result.Count == 0)
                throw new Exception("No se encontró ningún Universal Renderer Data en el proyecto.");
            return result;
        }

        static FullScreenPassRendererFeature AddFeature(UniversalRendererData data)
        {
            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;

            AssetDatabase.AddObjectToAsset(feature, data);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();      // para que el feature reciba su "local file id"

            var so = new SerializedObject(data);
            SerializedProperty list = so.FindProperty("m_RendererFeatures");
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;

            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string guid, out long localId))
            {
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return feature;
        }

        // =================================================================================
        //  TEXTURAS Y MALLAS GENERADAS POR CODIGO
        // =================================================================================
        static Texture2D gridTexture, testPatternTexture;

        static void CreateTextures()
        {
            gridTexture = WriteTexture("GridFloor", 128, 128, true, (x, y) =>
            {
                bool border = x < 2 || y < 2;
                bool mid = x == 64 || y == 64;
                if (border) return new Color32(70, 92, 130, 255);
                if (mid) return new Color32(34, 42, 60, 255);
                return new Color32(20, 24, 36, 255);
            });

            // Carta de ajuste: barras de color + rampa de grises + arcoiris + cuadricula.
            Color32[] bars =
            {
                new Color32(191,191,191,255), new Color32(191,191,0,255), new Color32(0,191,191,255),
                new Color32(0,191,0,255), new Color32(191,0,191,255), new Color32(191,0,0,255), new Color32(0,0,191,255)
            };
            testPatternTexture = WriteTexture("TestPattern", 512, 256, false, (x, y) =>
            {
                float v = y / 255f;                    // 0 abajo, 1 arriba
                if (v > 0.38f) return bars[Mathf.Min(6, x * 7 / 512)];
                if (v > 0.28f)
                {
                    int i = Mathf.Min(6, x * 7 / 512);
                    return (i % 2 == 0) ? new Color32(0, 0, 191, 255) : new Color32(0, 0, 0, 255);
                }
                if (v > 0.14f)
                {
                    if (x < 256) { byte g = (byte)(x * 255 / 255); return new Color32(g, g, g, 255); }
                    Color c = Color.HSVToRGB((x - 256) / 256f, 0.9f, 0.95f);
                    return (Color32)c;
                }
                bool checker = ((x / 16) + (y / 16)) % 2 == 0;
                return checker ? new Color32(235, 235, 235, 255) : new Color32(35, 35, 35, 255);
            });
        }

        static Texture2D WriteTexture(string name, int w, int h, bool repeat, Func<int, int, Color32> pixel)
        {
            string rel = Root + "/Textures/" + name + ".png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x, y);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(ProjectRoot(), rel), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(rel);
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = repeat;
            importer.anisoLevel = repeat ? 8 : 1;
            importer.filterMode = repeat ? FilterMode.Bilinear : FilterMode.Point;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
        }

        static Mesh waterMesh;

        /// <summary>Malla plana de 1x1 con 96x96 cuadros: hacen falta muchos vertices para ver las olas.</summary>
        static void CreateWaterMesh()
        {
            const int n = 96;
            var verts = new Vector3[(n + 1) * (n + 1)];
            var uvs = new Vector2[verts.Length];
            var normals = new Vector3[verts.Length];
            var tris = new int[n * n * 6];

            for (int z = 0; z <= n; z++)
            {
                for (int x = 0; x <= n; x++)
                {
                    int i = z * (n + 1) + x;
                    verts[i] = new Vector3((float)x / n - 0.5f, 0f, (float)z / n - 0.5f);
                    uvs[i] = new Vector2((float)x / n, (float)z / n);
                    normals[i] = Vector3.up;
                }
            }
            int t = 0;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int a = z * (n + 1) + x;
                    int b = a + 1;
                    int c = a + (n + 1);
                    int d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            }

            string path = Root + "/Meshes/WaterGrid.asset";
            AssetDatabase.DeleteAsset(path);
            var mesh = new Mesh { name = "WaterGrid" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1.2f, 1f, 1.2f));   // margen para el desplazamiento de las olas
            AssetDatabase.CreateAsset(mesh, path);
            waterMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        // =================================================================================
        //  MATERIALES
        // =================================================================================
        static Material NewMaterialAsset(string folder, string name, Shader shader)
        {
            string path = Root + "/Materials/" + folder + "/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);        // siempre desde cero, valores por defecto del shader
            var mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>Copia de un material (para variantes de color) guardada como asset.</summary>
        static Material Variant(Material source, string folder, string name, Action<Material> tweak)
        {
            string path = Root + "/Materials/" + folder + "/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            var mat = new Material(source) { name = name };
            tweak?.Invoke(mat);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>Material URP/Lit sencillo, con cache por nombre.</summary>
        static Material Lit(string name, Color color, float smoothness = 0.25f, float metallic = 0f, Color? emission = null)
        {
            if (matCache.TryGetValue(name, out Material cached) && cached != null) return cached;
            var mat = NewMaterialAsset("Museum", name, shLit);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            matCache[name] = mat;
            return mat;
        }

        static Material Glow(string name, Color color, float intensity = 2.2f) =>
            Lit(name, color * 0.25f, 0.2f, 0f, color * intensity);

        static Material FloorMat(float w, float d)
        {
            string name = "Floor_" + Mathf.RoundToInt(w) + "x" + Mathf.RoundToInt(d);
            if (matCache.TryGetValue(name, out Material cached) && cached != null) return cached;
            var mat = NewMaterialAsset("Museum", name, shLit);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetTexture("_BaseMap", gridTexture);
            mat.SetTextureScale("_BaseMap", new Vector2(w / 2f, d / 2f));       // 1 cuadro = 2 metros
            matCache[name] = mat;
            return mat;
        }

        // Materiales de shaders propios (los usan varias exhibiciones)
        static Shader shLiquid, shToon, shOutline, shShield, shHolo, shDissolve, shLava, shSlime, shWater;
        static Material mLiquidRed, mToon, mOutline, mShield, mHolo, mDissolve, mLava, mSlime, mWater;

        static void LoadShaders()
        {
            shLit = FindShader("Universal Render Pipeline/Lit");
            shUnlit = FindShader("Universal Render Pipeline/Unlit");
            shLiquid = FindShader("ShaderLab/Objects/00 Liquid Fill");
            shToon = FindShader("ShaderLab/Objects/Toon");
            shOutline = FindShader("ShaderLab/Objects/Outline");
            shShield = FindShader("ShaderLab/Objects/Plasma Shield");
            shHolo = FindShader("ShaderLab/Objects/Hologram");
            shDissolve = FindShader("ShaderLab/Objects/Dissolve");
            shLava = FindShader("ShaderLab/Objects/Lava");
            shSlime = FindShader("ShaderLab/Objects/Slime");
            shWater = FindShader("ShaderLab/Objects/Water");
        }

        static void CreateObjectMaterials()
        {
            mLiquidRed = NewMaterialAsset("Objects", "Liquid_Red", shLiquid);
            mToon = NewMaterialAsset("Objects", "Toon_Blue", shToon);
            mOutline = NewMaterialAsset("Objects", "Outline_Orange", shOutline);
            mShield = NewMaterialAsset("Objects", "PlasmaShield", shShield);
            mHolo = NewMaterialAsset("Objects", "Hologram_Cyan", shHolo);
            mDissolve = NewMaterialAsset("Objects", "Dissolve_Stone", shDissolve);
            mLava = NewMaterialAsset("Objects", "Lava", shLava);
            mSlime = NewMaterialAsset("Objects", "Slime_Green", shSlime);
            mWater = NewMaterialAsset("Objects", "Water", shWater);
        }

        // =================================================================================
        //  HELPERS DE ESCENA
        // =================================================================================
        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
            Material mat, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }
            return go;
        }

        static Vector3 V(float u) => new Vector3(u, u, u);

        static GameObject Group(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        static T Add<T>(GameObject go) where T : Component => go.AddComponent<T>();

        static Light PointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>
        /// Texto 3D. 'toViewer' es la direccion (en mundo) hacia donde mira la cara legible del texto.
        /// </summary>
        static TextMesh Text(Transform parent, string text, Vector3 worldPos, Vector3 toViewer, float charSize,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.LookRotation(-toViewer.normalized, Vector3.up);
            var tm = go.AddComponent<TextMesh>();
            tm.font = labFont;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            go.GetComponent<MeshRenderer>().sharedMaterial = labFont.material;
            return tm;
        }

        /// <summary>Muro: un cubo con material oscuro.</summary>
        static GameObject Wall(string name, Transform parent, Vector3 center, Vector3 size)
        {
            return Prim(PrimitiveType.Cube, name, parent, center, size, Lit("Wall", new Color(0.075f, 0.085f, 0.12f), 0.15f));
        }

        static GameObject Floor(string name, Transform parent, Vector3 center, float w, float d)
        {
            return Prim(PrimitiveType.Cube, name, parent, new Vector3(center.x, -0.1f, center.z), new Vector3(w, 0.2f, d), FloorMat(w, d));
        }

        // =================================================================================
        //  ENTORNO, JUGADOR Y SISTEMAS
        // =================================================================================
        static void BuildEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.23f, 0.32f);
            RenderSettings.fog = false;

            var sun = new GameObject("Luz Direccional (luna)");
            sun.transform.SetParent(museumRoot, false);
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(0.78f, 0.84f, 1f);
            l.intensity = 1.15f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.9f;
        }

        static Transform BuildPlayer()
        {
            var player = new GameObject("Jugador");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0.05f, -7f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.04f);
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            camGo.AddComponent<AudioListener>();

            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.requiresDepthOption = CameraOverrideOption.On;
            urpCam.requiresColorOption = CameraOverrideOption.On;

            var p = player.AddComponent<ShaderLabPlayer>();
            p.playerCamera = cam;
            return player.transform;
        }

        static void BuildSystems(Transform player)
        {
            var systems = new GameObject("Sistemas del museo");
            systems.transform.SetParent(museumRoot, false);

            var director = systems.AddComponent<MuseumDirector>();
            director.player = player;
            director.playerCamera = player.GetComponentInChildren<Camera>();

            var hud = systems.AddComponent<MuseumHUD>();
            hud.director = director;

            var fx = systems.AddComponent<ScreenEffectManager>();
            fx.features = new List<FullScreenPassRendererFeature>(features);
            fx.effects = BuildEffectList();

            // Conectar cada terminal con su efecto (por indice) ya esta hecho al crearlas.
        }
    }
}
