using UnityEngine;

namespace IL6
{
    /// <summary>
    /// Central gameplay balance asset. Values can be edited from Resources/BalanceConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "6IL/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Day/Night Cycle (seconds)")]
        public float DayDurationSec = 540f;
        public float NightDurationSec = 360f;
        public float EveningTransitionSec = 30f;
        public float DawnTransitionSec = 30f;

        [Header("Vision (tiles)")]
        public int DayRadiusTiles = 10;
        public int NightRadiusTiles = 6;
        public int MegaBlizzardRadiusTiles = 3;

        [Header("Resources (starting)")]
        public int StartingWood = 15;
        public int StartingStone = 5;
        public int StartingMeat = 0;
        public int StartingFood = 5;
        public int StartingFrostbloom = 0;

        [Header("Gather (seconds + yield)")]
        public float TreeDurationSec = 4f;
        public float RockDurationSec = 6f;
        public float DeerDurationSec = 3f;
        public int TreeWoodYield = 3;
        public int RockStoneYield = 2;
        public int DeerMeatYield = 2;

        [Header("Player")]
        public int PlayerMaxHp = 100;
        public float PlayerMoveSpeed = 180f;
        public float PlayerRespawnSec = 3f;
        public int PlayerArmor = 0;

        [Header("Zombie")]
        public int ZombieMaxHp = 20;
        public float ZombieMoveSpeed = 60f;
        public int ZombieAttackDamage = 8;
        public float ZombieAttackCooldownSec = 1f;
        public float ZombieAttackRange = 36f;
        public int ZombieArmor = 0;

        [Header("Wave")]
        public int WaveBaseCount = 8;
        public int WavePerDay = 4;
        public int WaveMaxCount = 300;

        [Header("Building HP")]
        public int CampfireHp = 280;
        public int BrazierHp = 520;
        public int BlacksmithHp = 360;
        public int SeedStorageHp = 180;
        public int CarpenterHp = 220;
        public int TrainingCampHp = 240;
        public int FoodStorageHp = 220;
        public int LookoutPostHp = 180;
        public int SawmillHp = 260;
        public int BarricadeHp = 280;
        public int FenceHp = 14;

        [Header("Building Cost (wood)")]
        public int CampfireCost = 5;
        public int BarricadeCost = 5;
        public int BrazierCost = 12;
        public int BlacksmithCost = 10;
        public int SeedStorageCost = 8;
        public int CarpenterCost = 12;
        public int TrainingCampCost = 12;
        public int FoodStorageCost = 10;
        public int LookoutPostCost = 9;
        public int SawmillCost = 14;

        [Header("Campfire Aura")]
        public float BonfireDamagePerSec = 5f;
        public float BonfireRadius = 128f;
        public float BonfireAttackBuff = 0.15f;
        public float CampfireHpDrainPerSec = 0.5f;

        private static BalanceConfig _instance;
        public static BalanceConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BalanceConfig>("BalanceConfig");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[BalanceConfig] Resources/BalanceConfig.asset not found, using defaults.");
                        _instance = ScriptableObject.CreateInstance<BalanceConfig>();
                    }
                }
                return _instance;
            }
        }
    }
}
