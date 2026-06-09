#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using IL6;

namespace IL6.Tests
{
    public class CompanionFollowPriorityTests
    {
        [Test]
        public void PriorityFollowDistance_PlayerMoving_IsShorterThanIdle()
        {
            float idle = Companion.PriorityFollowDistance(9f, 1.7f, playerMoving: false, playerAttacking: false);
            float moving = Companion.PriorityFollowDistance(9f, 1.7f, playerMoving: true, playerAttacking: false);

            Assert.Less(moving, idle);
        }

        [Test]
        public void PriorityFollowDistance_PlayerAttacking_IsLongerThanIdle()
        {
            float idle = Companion.PriorityFollowDistance(9f, 1.7f, playerMoving: false, playerAttacking: false);
            float attacking = Companion.PriorityFollowDistance(9f, 1.7f, playerMoving: false, playerAttacking: true);

            Assert.Greater(attacking, idle);
        }

        [Test]
        public void PriorityFollowDistance_AttackingWinsOverMoving()
        {
            float moving = Companion.PriorityFollowDistance(9f, 1.7f, playerMoving: true, playerAttacking: false);
            float attackingAndMoving = Companion.PriorityFollowDistance(9f, 1.7f, playerMoving: true, playerAttacking: true);

            Assert.Greater(attackingAndMoving, moving);
        }
    }
}
#endif
