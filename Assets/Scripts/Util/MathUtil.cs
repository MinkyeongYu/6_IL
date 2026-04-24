using UnityEngine;

namespace IL6
{
    public static class MathUtil
    {
        public static float Clamp(float v, float min, float max) =>
            v < min ? min : v > max ? max : v;

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static float Dist2(Vector2 a, Vector2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        public static float Distance(Vector2 a, Vector2 b) => Mathf.Sqrt(Dist2(a, b));

        public static Vector2 SafeNormalize(Vector2 v)
        {
            float len = v.magnitude;
            if (len <= 1e-6f) return Vector2.zero;
            return v / len;
        }
    }
}
