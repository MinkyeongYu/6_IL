using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 게임 밸런스 단일 소스. ScriptableObject로 만들어 Inspector에서 수정 가능.
    /// 메뉴: Assets > Create > 6IL > Balance Config
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "6IL/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Day/Night Cycle (seconds)")]
        public float DayDurationSec = 180f;
        public float NightDurationSec = 180f;
        public float EveningTransitionSec = 5f;
        public float DawnTransitionSec = 5f;

        [Header("Vision (tiles)")]
        public int DayRadiusTiles = 10;
        public int NightRadiusTiles = 6;
        public int MegaBlizzardRadiusTiles = 3;

        [Header("Resources (starting)")]
        public int StartingWood = 15;
        public int StartingMeat = 0;
        public int StartingFood = 5;
        public int StartingFrostbloom = 0;

        [Header("Gather (seconds + yield)")]
        public float TreeDurationSec = 4f;
        public float DeerDurationSec = 2f;
        public int TreeWoodYield = 3;
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
        public int CampfireHp = 400;
        public int BarricadeHp = 200;

        [Header("Building Cost (wood)")]
        public int CampfireCost = 5;
        public int BarricadeCost = 5;

        // 싱글톤 접근 (Resources에서 자동 로드)
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
