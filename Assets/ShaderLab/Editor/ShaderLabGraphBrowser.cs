using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShaderLab.EditorTools
{
    /// <summary>
    /// Ventana "Shader Lab > Grafos (Shader Graph)": lista todos los grafos del laboratorio en el
    /// ORDEN DE CLASE (de lo mas simple a lo mas complejo) y permite abrirlos con un clic.
    /// Los datos salen de Assets/ShaderLab/ShaderGraphs/graphs.json.
    /// </summary>
    public class ShaderLabGraphBrowser : EditorWindow
    {
        const string Root = "Assets/ShaderLab/ShaderGraphs/";

        [Serializable] class Entry
        {
            public string file, title, kind, key, shader, hlsl, pairedWith;
            public int level;
            public bool hidden;
            public string[] concepts;
        }
        [Serializable] class Manifest { public Entry[] items; }

        Manifest manifest;
        Vector2 scroll;
        string filter = "";
        int tab;
        static readonly string[] Tabs = { "Todos", "Fundamentos", "Objetos", "Camara" };
        static readonly string[] Kinds = { null, "fundamento", "objeto", "camara" };

        [MenuItem("Shader Lab/Grafos (Shader Graph)")]
        public static void Open()
        {
            var w = GetWindow<ShaderLabGraphBrowser>("Grafos Shader Lab");
            w.minSize = new Vector2(520, 420);
            w.Show();
        }

        void OnEnable() { Load(); }

        void Load()
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "graphs.json");
            manifest = ta != null ? JsonUtility.FromJson<Manifest>(ta.text) : null;
        }

        void OnGUI()
        {
            if (manifest == null || manifest.items == null)
            {
                EditorGUILayout.HelpBox("No se encontro " + Root + "graphs.json", MessageType.Warning);
                if (GUILayout.Button("Reintentar")) Load();
                return;
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Grafos de Shader Graph del laboratorio", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Orden sugerido para la clase: Fundamentos -> Objetos (de arriba a abajo) -> Camara. " +
                "Cada grafo esta dividido en GRUPOS numerados con una nota que explica ese paso.",
                MessageType.Info);

            tab = GUILayout.Toolbar(tab, Tabs);
            filter = EditorGUILayout.TextField("Buscar", filter);
            GUILayout.Space(4);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            string last = null;
            foreach (Entry e in Sorted())
            {
                if (e.hidden) continue;
                if (Kinds[tab] != null && e.kind != Kinds[tab]) continue;
                if (!string.IsNullOrEmpty(filter) && (e.title + " " + e.key + " " + string.Join(" ", e.concepts ?? new string[0]))
                        .IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                if (e.kind != last)
                {
                    last = e.kind;
                    GUILayout.Space(8);
                    EditorGUILayout.LabelField(KindTitle(e.kind), EditorStyles.boldLabel);
                }
                DrawEntry(e);
            }
            EditorGUILayout.EndScrollView();
        }

        IEnumerable<Entry> Sorted()
        {
            var list = new List<Entry>(manifest.items);
            string[] order = { "fundamento", "objeto", "camara" };
            list.Sort((a, b) =>
            {
                int ka = Array.IndexOf(order, a.kind), kb = Array.IndexOf(order, b.kind);
                if (ka != kb) return ka.CompareTo(kb);
                if (a.level != b.level) return a.level.CompareTo(b.level);
                return string.CompareOrdinal(a.file, b.file);
            });
            return list;
        }

        static string KindTitle(string kind)
        {
            return kind == "fundamento" ? "FUNDAMENTOS  (nodos basicos)"
                 : kind == "objeto" ? "SHADERS DE OBJETO"
                 : "SHADERS DE CAMARA  (Fullscreen)";
        }

        void DrawEntry(Entry e)
        {
            string path = Root + e.file + ".shadergraph";
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Nivel " + e.level + "  ·  " + e.title, EditorStyles.boldLabel);
            if (GUILayout.Button("Abrir grafo", GUILayout.Width(100)))
                OpenGraph(path);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(Path.GetFileName(path) + "   (" + e.shader + ")", EditorStyles.miniLabel);
            if (e.concepts != null && e.concepts.Length > 0)
                EditorGUILayout.LabelField("Conceptos: " + string.Join(" · ", e.concepts), EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ver en Project", GUILayout.Width(110)))
            {
                UnityEngine.Object o = AssetDatabase.LoadMainAssetAtPath(path);
                if (o != null) { Selection.activeObject = o; EditorGUIUtility.PingObject(o); }
            }
            if (e.kind != "camara" && GUILayout.Button("Crear material de prueba", GUILayout.Width(170)))
                MakeMaterial(path, Path.GetFileNameWithoutExtension(path));
            if (!string.IsNullOrEmpty(e.pairedWith) && e.key == "Outline" && GUILayout.Button("Abrir el contorno (2/2)", GUILayout.Width(170)))
                OpenGraph(Root + e.pairedWith + ".shadergraph");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        static void OpenGraph(string path)
        {
            UnityEngine.Object o = AssetDatabase.LoadMainAssetAtPath(path);
            if (o == null) { Debug.LogWarning("No existe " + path); return; }
            AssetDatabase.OpenAsset(o);
        }

        static void MakeMaterial(string graphPath, string name)
        {
            Shader sh = AssetDatabase.LoadAssetAtPath<Shader>(graphPath);
            if (sh == null) return;
            string dir = "Assets/ShaderLab/Materials/Graphs";
            if (!AssetDatabase.IsValidFolder("Assets/ShaderLab/Materials")) AssetDatabase.CreateFolder("Assets/ShaderLab", "Materials");
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/ShaderLab/Materials", "Graphs");
            string matPath = dir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            Selection.activeObject = mat;
            EditorGUIUtility.PingObject(mat);
        }
    }
}
