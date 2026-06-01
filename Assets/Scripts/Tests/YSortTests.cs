#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using IL6;

namespace IL6.Tests
{
    public class YSortTests
    {
        [Test]
        public void ComputeOrder_YZero_Returns_Zero()
        {
            Assert.AreEqual(0, YSort.ComputeOrder(0f));
        }

        [Test]
        public void ComputeOrder_YPositive_Returns_Negative()
        {
            Assert.AreEqual(-500, YSort.ComputeOrder(5f));
        }

        [Test]
        public void ComputeOrder_YNegative_Returns_Positive()
        {
            Assert.AreEqual(300, YSort.ComputeOrder(-3f));
        }

        [Test]
        public void ComputeOrder_RoundsToNearestInt()
        {
            Assert.AreEqual(-123, YSort.ComputeOrder(1.234f));
        }
    }
}
#endif
