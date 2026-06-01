#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using IL6;

namespace IL6.Tests
{
    public class LightFlickerTests
    {
        [Test]
        public void ComputeIntensity_AtTimeZero_Returns_BaseIntensity()
        {
            float result = LightFlicker.ComputeIntensity(1.5f, 0f, 0.2f, 3f);
            Assert.AreEqual(1.5f, result, 0.0001f);
        }

        [Test]
        public void ComputeIntensity_OscillatesWithinBand()
        {
            // 0.25 주기의 1/4 = Sin(pi/2) = 1 → intensity = base + amp
            float result = LightFlicker.ComputeIntensity(1.0f, 0.25f, 0.2f, 1f);
            Assert.AreEqual(1.2f, result, 0.01f);
        }

        [Test]
        public void ComputeIntensity_NeverNegative()
        {
            float result = LightFlicker.ComputeIntensity(0.1f, 0.75f, 0.5f, 1f);
            Assert.GreaterOrEqual(result, 0f);
        }
    }
}
#endif
