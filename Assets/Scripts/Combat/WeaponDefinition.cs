using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 무기 데이터. Inspector에서 자유로운 변형 가능.
    /// 메뉴: Assets > Create > 6IL > Weapon Definition
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon", menuName = "6IL/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string Id = "longsword";
        public string DisplayName = "Longsword";
        public int BaseDamage = 12;
        public float Range = 48f;
        public float CooldownSec = 0.75f;
        public float CritChance = 0.08f;
        public float CritMultiplier = 2f;
        public float HitRadius = 36f;
    }
}
