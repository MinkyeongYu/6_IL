using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IL6
{
    /// <summary>
    /// Light 2D intensity 를 sin wave 로 진동 (모닥불 플리커).
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class LightFlicker : MonoBehaviour
    {
        public float BaseIntensity = 1.5f;
        public float Amplitude = 0.2f;
        public float Frequency = 3f; // Hz

        private Light2D _light;

        public static float ComputeIntensity(float baseIntensity, float time, float amp, float freq)
        {
            float raw = baseIntensity + Mathf.Sin(time * freq * 2f * Mathf.PI) * amp;
            return Mathf.Max(0f, raw);
        }

        private void Awake() { _light = GetComponent<Light2D>(); }

        private void Update()
        {
            _light.intensity = ComputeIntensity(BaseIntensity, Time.time, Amplitude, Frequency);
        }
    }
}
