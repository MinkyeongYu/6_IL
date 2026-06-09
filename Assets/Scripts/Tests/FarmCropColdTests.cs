#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using IL6;

namespace IL6.Tests
{
    public class FarmCropColdTests
    {
        [Test]
        public void SurvivalChance_AtVeryLowTemperature_FavorsTurnipOverPotatoOverWheat()
        {
            float temp = -32f;

            float turnip = FarmBuilding.CropColdSurvivalChance(FarmBuilding.CropKind.Turnip, temp);
            float potato = FarmBuilding.CropColdSurvivalChance(FarmBuilding.CropKind.Potato, temp);
            float wheat = FarmBuilding.CropColdSurvivalChance(FarmBuilding.CropKind.Wheat, temp);

            Assert.Greater(turnip, potato);
            Assert.Greater(potato, wheat);
        }

        [Test]
        public void YieldMultiplier_AtLowTemperature_FavorsColdHardyCrops()
        {
            float temp = -24f;

            float turnip = FarmBuilding.CropColdYieldMultiplier(FarmBuilding.CropKind.Turnip, temp);
            float potato = FarmBuilding.CropColdYieldMultiplier(FarmBuilding.CropKind.Potato, temp);
            float wheat = FarmBuilding.CropColdYieldMultiplier(FarmBuilding.CropKind.Wheat, temp);

            Assert.Greater(turnip, potato);
            Assert.Greater(potato, wheat);
        }

        [Test]
        public void YieldMultiplier_AtMildTemperature_DoesNotPenalizeAnyCrop()
        {
            float temp = -6f;

            Assert.AreEqual(1f, FarmBuilding.CropColdYieldMultiplier(FarmBuilding.CropKind.Turnip, temp), 0.001f);
            Assert.AreEqual(1f, FarmBuilding.CropColdYieldMultiplier(FarmBuilding.CropKind.Potato, temp), 0.001f);
            Assert.AreEqual(1f, FarmBuilding.CropColdYieldMultiplier(FarmBuilding.CropKind.Wheat, temp), 0.001f);
        }
    }
}
#endif
