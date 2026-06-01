using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 페이즈별 Global Light 2D 타겟 값 계산 (pure).
    /// </summary>
    public static class DayNightLightCalc
    {
        public static (float intensity, Color color) Target(Phase phase) => phase switch
        {
            Phase.Day     => (1.0f, new Color(0.96f, 0.96f, 0.94f)), // #f5f5f0
            Phase.Evening => (0.65f, new Color(0.85f, 0.65f, 0.55f)), // 주황빛 석양
            Phase.Night   => (0.25f, new Color(0.23f, 0.29f, 0.48f)), // #3a4a7a
            Phase.Dawn    => (0.65f, new Color(0.85f, 0.70f, 0.70f)), // 연분홍 새벽
            _             => (1.0f, Color.white),
        };

        public static (float intensity, Color color) Lerp(Phase from, Phase to, float t)
        {
            var a = Target(from); var b = Target(to);
            return (
                Mathf.Lerp(a.intensity, b.intensity, t),
                Color.Lerp(a.color, b.color, t)
            );
        }
    }
}
