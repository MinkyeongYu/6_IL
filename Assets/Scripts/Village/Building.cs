using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    public enum BuildingKind { Campfire, Barricade, Fence }

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

        private System.Action _unsubDawn;

        private void Awake()
        {
            var b = BalanceConfig.Instance;
            MaxHp = Kind switch
            {
                BuildingKind.Campfire => b.CampfireHp,
                BuildingKind.Barricade => b.BarricadeHp,
                BuildingKind.Fence => b.FenceHp,
                _ => b.BarricadeHp,
            };
            CurrentHp = MaxHp;
        }

        private void Start()
        {
            _unsubDawn = EventBus.Instance.Subscribe<DawnStartedPayload>(_ => DawnRepair());
        }

        public float LastDamagedAt { get; private set; } = -100f;

        public void TakeDamage(int amount)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            LastDamagedAt = Time.time;
            GameFeel.HitFlash(this, GetComponent<SpriteRenderer>());
            if (CurrentHp <= 0)
            {
                Grid?.Remove(Eid);
                ExposeHosted();
                EventBus.Instance.Emit(new BuildingDestroyedPayload(Eid, Kind.ToString().ToLower()));
                Destroy(gameObject);
            }
        }

        private void DawnRepair()
        {
            if (CurrentHp <= 0) return;
            int heal = Mathf.Max(1, MaxHp / 4);
            int before = CurrentHp;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + heal);
            int delta = CurrentHp - before;
            if (delta > 0) GameFeel.FloatText(transform.position, $"+{delta} HP", new Color(0.6f, 1f, 0.7f));
        }

        private void OnDestroy()
        {
            _unsubDawn?.Invoke();
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
