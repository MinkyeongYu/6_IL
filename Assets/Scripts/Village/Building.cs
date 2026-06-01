using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    public enum BuildingKind { Campfire, Barricade, Fence, House, Storage, Farm, Watchtower, Infirmary, HuntersHut }

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
            int baseHp = Kind switch
            {
                BuildingKind.Campfire => b.CampfireHp,
                BuildingKind.Barricade => b.BarricadeHp,
                BuildingKind.Fence => b.FenceHp,
                BuildingKind.House => 140,    // 200→140 — 작은 거주지
                BuildingKind.Storage => 200,  // 250→200 — 자원함
                BuildingKind.Farm => 90,      // 150→90 — 가장 약한 핵심 건물 (지속 식량 위해 보호 필요)
                BuildingKind.Watchtower => 200, // 220→200
                BuildingKind.Infirmary => 180,  // 회복 건물
                BuildingKind.HuntersHut => 160, // 사냥꾼 오두막 — 야외 동물 스폰 부스트
                _ => b.BarricadeHp,
            };
            // 망루는 펜스 HP 50% 부스트 (스택)
            if (Kind == BuildingKind.Fence)
            {
                int towers = 0;
                var existing = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
                foreach (var bb in existing)
                {
                    if (bb == null || bb == this) continue;
                    if (bb.Kind == BuildingKind.Watchtower) towers++;
                }
                baseHp = Mathf.RoundToInt(baseHp * (1f + 0.5f * towers));
            }
            // 마을 성장도 — 펜스 제외 건물 수에 따라 HP 가산.
            // 기존 building 1개당 +12%, 최대 +200%.
            int existingCore = 0;
            var all = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var bb in all)
            {
                if (bb == null || bb == this) continue;
                if (bb.Kind == BuildingKind.Fence) continue;
                existingCore++;
            }
            float growth = 1f + Mathf.Min(existingCore * 0.12f, 2f);
            MaxHp = Mathf.RoundToInt(baseHp * growth);
            CurrentHp = MaxHp;
        }

        private void Start()
        {
            // 펜스/게이트는 수십 개라 HP 바 제외 — 나머지만 부착 (풀체력에서 자동 숨김)
            if (Kind != BuildingKind.Fence && GetComponent<HpBarUi>() == null)
            {
                var hp = gameObject.AddComponent<HpBarUi>();
                hp.Building = this;
                hp.Offset = new Vector2(0f, 0.6f);
                hp.Size = new Vector2(0.9f, 0.10f);
                hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
                hp.FillColor = Kind switch
                {
                    BuildingKind.Campfire  => new Color(1f, 0.55f, 0.2f),
                    BuildingKind.Barricade => new Color(0.6f, 0.4f, 0.2f),
                    _ => new Color(0.5f, 0.85f, 0.5f),
                };
            }
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

        /// <summary>외부 (HUD 수리 버튼) 에서 즉시 HP 회복.</summary>
        public void RepairHp(int amount)
        {
            if (CurrentHp <= 0) return;
            int before = CurrentHp;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            int delta = CurrentHp - before;
            if (delta > 0) GameFeel.FloatText(transform.position, $"+{delta} HP", new Color(0.6f, 1f, 0.7f));
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
