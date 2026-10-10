using UnityEngine;
using UnityEngine.InputSystem;

namespace ShaderLab
{
    /// <summary>
    /// Controlador en primera persona para recorrer el museo (usa el Input System nuevo).
    ///   WASD = mover | Shift = correr | Espacio = saltar | Mouse = mirar
    ///   Tab = alternar "modo cursor" (para usar los sliders de parametros) | Esc = modo cursor
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ShaderLabPlayer : MonoBehaviour
    {
        public Camera playerCamera;
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float jumpHeight = 1.1f;
        public float gravity = -20f;
        [Tooltip("Sensibilidad del mouse (grados por pixel)")]
        public float mouseSensitivity = 0.1f;

        /// <summary>True cuando el cursor esta libre (se muestran los sliders del shader).</summary>
        public static bool UiMode { get; private set; }

        CharacterController controller;
        float yaw;
        float pitch;
        float verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            yaw = transform.eulerAngles.y;
        }

        void Start()
        {
            SetUiMode(false);
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UiMode = false;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !UiMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public static void SetUiMode(bool ui)
        {
            UiMode = ui;
            Cursor.lockState = ui ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = ui;
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (kb.tabKey.wasPressedThisFrame) SetUiMode(!UiMode);
            if (kb.escapeKey.wasPressedThisFrame) SetUiMode(true);

            // Con el cursor libre, el jugador se queda quieto (estamos usando la interfaz).
            if (UiMode)
            {
                ApplyGravityOnly();
                return;
            }

            // ---- Mirar ----
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -85f, 85f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // ---- Mover ----
            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            Vector3 move = transform.right * x + transform.forward * z;
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;

            // ---- Gravedad y salto ----
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (controller.isGrounded && kb.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        void ApplyGravityOnly()
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }
    }
}
