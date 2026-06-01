#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using IL6;

namespace IL6.Tests
{
    public class DayNightLightCalcTests
    {
        [Test]
        public void DayTarget_IsBrightWarmWhite()
        {
            var (intensity, color) = DayNightLightCalc.Target(Phase.Day);
            Assert.AreEqual(1.0f, intensity, 0.001f);
            Assert.Greater(color.r, 0.9f);
            Assert.Greater(color.g, 0.9f);
        }

        [Test]
        public void NightTarget_IsDarkCoolBlue()
        {
            var (intensity, color) = DayNightLightCalc.Target(Phase.Night);
            Assert.AreEqual(0.25f, intensity, 0.001f);
            Assert.Less(color.r, 0.5f);
            Assert.Greater(color.b, color.r);
        }

        [Test]
        public void Lerp_HalfwayBetweenDayAndNight_IsMidpoint()
        {
            var (i, _) = DayNightLightCalc.Lerp(Phase.Day, Phase.Night, 0.5f);
            Assert.AreEqual((1.0f + 0.25f) / 2f, i, 0.001f);
        }
    }
}
#endif
