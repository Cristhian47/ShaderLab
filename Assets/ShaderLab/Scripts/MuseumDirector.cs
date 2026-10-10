using UnityEngine;
using UnityEngine.InputSystem;

namespace ShaderLab
{
    public enum MuseumZone { Lobby, ObjectHall, CameraHall }

    /// <summary>
    /// Cerebro del museo: detecta en que sala esta el jugador, cual es la exhibicion o la
    /// terminal mas cercana, resalta lo que miras (HoverOutline) y atiende la tecla E.
    /// </summary>
    public class MuseumDirector : MonoBehaviour
    {
        public static MuseumDirector Instance { get; private set; }

        public Transform player;
        public Camera playerCamera;
        [Tooltip("Si |x| del jugador supera este valor esta dentro de una sala (no en el lobby)")]
        public float hallBoundary = 10f;
        public float terminalRange = 3.4f;
        public float lookRange = 9f;

        public MuseumZone Zone { get; private set; }
        public Exhibit CurrentExhibit { get; private set; }
        public ScreenEffectTerminal CurrentTerminal { get; private set; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (player == null) return;
            Vector3 p = player.position;

            Zone = p.x < -hallBoundary ? MuseumZone.ObjectHall
                 : p.x > hallBoundary ? MuseumZone.CameraHall
                 : MuseumZone.Lobby;

            // ---- exhibicion mas cercana (distancia en el plano horizontal) ----
            CurrentExhibit = null;
            float best = float.MaxValue;
            foreach (Exhibit e in Exhibit.All)
            {
                float d = Flat(e.transform.position - p).sqrMagnitude;
                if (d < e.activationRadius * e.activationRadius && d < best)
                {
                    best = d;
                    CurrentExhibit = e;
                }
            }

            // ---- terminal mas cercana ----
            CurrentTerminal = null;
            best = terminalRange * terminalRange;
            foreach (ScreenEffectTerminal t in ScreenEffectTerminal.All)
            {
                float d = Flat(t.transform.position - p).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    CurrentTerminal = t;
                }
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && !ShaderLabPlayer.UiMode && CurrentTerminal != null
                && kb.eKey.wasPressedThisFrame && ScreenEffectManager.Instance != null)
            {
                ScreenEffectManager.Instance.Toggle(CurrentTerminal.effectIndex);
            }

            if (kb != null && !ShaderLabPlayer.UiMode)
            {
                if (kb.gKey.wasPressedThisFrame) ShaderLabMode.Toggle();   // HLSL <-> Shader Graph
                if (kb.oKey.wasPressedThisFrame) OpenCurrentGraph();
            }

            UpdateLookTarget();
        }

        /// <summary>Solo en el Editor: abre en la ventana de Shader Graph el grafo de la exhibicion o terminal cercana.</summary>
        void OpenCurrentGraph()
        {
#if UNITY_EDITOR
            string file = null;
            ScreenEffectManager fx = ScreenEffectManager.Instance;
            if (CurrentTerminal != null && fx != null && CurrentTerminal.effectIndex < fx.effects.Count)
                file = fx.effects[CurrentTerminal.effectIndex].graphFile;
            else if (CurrentExhibit != null)
                file = CurrentExhibit.graphFile;
            if (string.IsNullOrEmpty(file)) return;

            Object asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(file);
            if (asset != null) UnityEditor.AssetDatabase.OpenAsset(asset);
#endif
        }

        /// <summary>Raycast desde el centro de la pantalla: resalta el objeto con HoverOutlineTarget.</summary>
        void UpdateLookTarget()
        {
            HoverOutlineTarget.Current = null;
            if (playerCamera == null || ShaderLabPlayer.UiMode) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, lookRange, ~0, QueryTriggerInteraction.Ignore))
                HoverOutlineTarget.Current = hit.collider.GetComponentInParent<HoverOutlineTarget>();
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
