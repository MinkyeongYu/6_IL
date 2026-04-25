using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    public enum BuildingKind { Campfire, Barricade }

    /// <summary>
    /// 모든 건물 공통. HP, 파괴 처리, 안에 숨은 비전투 동료 노출.
    /// </summary>
    public sealed class Building : MonoBehaviour
    {
        public BuildingKind Kind;
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public int Eid { get; set; }
        public VillageGrid Grid { get; set; }

        public readonly List<Companion> HostedCompanions = new();

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
                ExposeHosted();
                EventBus.Instance.Emit(new BuildingDestroyedPayload(Eid, Kind.ToString().ToLower()));
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            ExposeHosted();
        }

        private void ExposeHosted()
        {
            foreach (var c in HostedCompanions)
            {
                if (c != null) c.ExposeAndFlee();
            }
            HostedCompanions.Clear();
        }
    }
}
