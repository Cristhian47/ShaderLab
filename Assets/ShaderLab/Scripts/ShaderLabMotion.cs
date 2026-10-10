using UnityEngine;

namespace ShaderLab
{
    /// <summary>
    /// Movimiento decorativo sencillo para los dioramas: girar, flotar, saltar u orbitar.
    /// (Los shaders hacen su magia solos; esto solo mueve los objetos para que se vea vivo.)
    /// </summary>
    public class ShaderLabMotion : MonoBehaviour
    {
        [Header("Giro (grados por segundo, en ejes locales)")]
        public Vector3 spin;

        [Header("Flotar (amplitud en metros, ejes locales)")]
        public Vector3 bobAmplitude;
        public float bobSpeed = 1f;
        public float phase;

        [Header("Saltar (como un slime): altura maxima y velocidad")]
        public float hopHeight;
        public float hopSpeed = 2f;

        [Header("Orbitar alrededor de un punto local del padre")]
        public bool orbit;
        public Vector3 orbitCenter;
        public float orbitRadius = 2f;
        public float orbitRadiusPulse;      // cuanto varia el radio
        public float orbitPulseSpeed = 1f;
        public float orbitSpeed = 1f;       // radianes por segundo
        public float orbitHeightAmplitude;
        public float orbitHeightSpeed = 1.5f;

        Vector3 startLocalPosition;

        void Start()
        {
            startLocalPosition = transform.localPosition;
        }

        void Update()
        {
            float t = Time.time;

            if (spin != Vector3.zero)
                transform.Rotate(spin * Time.deltaTime, Space.Self);

            if (orbit)
            {
                float angle = t * orbitSpeed + phase;
                float r = orbitRadius + Mathf.Sin(t * orbitPulseSpeed + phase) * orbitRadiusPulse;
                float h = Mathf.Sin(t * orbitHeightSpeed + phase) * orbitHeightAmplitude;
                transform.localPosition = orbitCenter + new Vector3(Mathf.Cos(angle) * r, h, Mathf.Sin(angle) * r);
                return;
            }

            Vector3 offset = bobAmplitude * Mathf.Sin(t * bobSpeed + phase);
            if (hopHeight > 0f)
                offset.y += Mathf.Abs(Mathf.Sin(t * hopSpeed + phase)) * hopHeight;
            if (bobAmplitude != Vector3.zero || hopHeight > 0f)
                transform.localPosition = startLocalPosition + offset;
        }
    }
}
