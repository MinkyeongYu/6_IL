using UnityEngine;
using IL6.Events;

namespace IL6
{
    public enum BuildingKind { Campfire, Barricade }

    /// <summary>
    /// 모든 건물 공통. HP, 파괴 처리, 그리드 셀 정리.
    /// </summary>
    public sealed class Building : MonoBehaviour
    {
        public BuildingKind Kind;
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public int Eid { get; set; } // VillageGrid에 등록된 ID
        public VillageGrid Grid { get; set; }

        private void Awake()
        {
            var b = BalanceConfig.Instance;
            MaxHp = Kind == BuildingKind.Campfire ? b.CampfireHp : b.BarricadeHp;
            CurrentHp = MaxHp;
        }

        public void TakeDamage(int amount)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp <= 0)
            {
                Grid?.Remove(Eid);
                EventBus.Instance.Emit(new BuildingDestroyedPayload(Eid, Kind.ToString().ToLower()));
                Destroy(gameObject);
            }
        }
    }
}
