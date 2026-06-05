using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    public enum BuildingKind
    {
        Campfire,
        Barricade,
        Fence,
        House,
        Storage,
        Farm,
        Watchtower,
        Infirmary,
        HuntersHut,
        Brazier,
        Blacksmith,
        SeedStorage,
        Carpenter,
        TrainingCamp,
        FoodStorage,
        LookoutPost,
        Sawmill,
        Church
    }

    public sealed class Building : MonoBehaviour
    {
        public BuildingKind Kind;
        public int Level { get; private set; } = 1;
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public int Eid { get; set; }
        public VillageGrid Grid { get; set; }

        public readonly List<Companion> HostedCompanions = new();

        private System.Action _unsubDawn;
        private bool _started;

        private void Start()
        {
            _started = true;
            if (MaxHp <= 0) RecalculateStats(true);
            EnsureHpBar();
            _unsubDawn = EventBus.Instance.Subscribe<DawnStartedPayload>(_ => DawnRepair());
        }

        public void Initialize(BuildingKind kind)
        {
            Kind = kind;
            Level = Mathf.Max(1, Level);
            RecalculateStats(true);
            ApplyLevelEffects();
            if (_started) EnsureHpBar();
        }

        public ResourceCost NextUpgradeCost()
        {
            return BuildingUpgradeRules.UpgradeCost(Kind, Level);
        }

        public bool CanUpgrade(ResourceStore store)
        {
            return Level < BuildingUpgradeRules.MaxLevel && NextUpgradeCost().CanPay(store);
        }

        public bool TryUpgrade(ResourceStore store)
        {
            if (Level >= BuildingUpgradeRules.MaxLevel) return false;
            var cost = NextUpgradeCost();
            if (!cost.Pay(store)) return false;

            Level++;
            RecalculateStats(false);
            ApplyLevelEffects();
            ApplyUpgradeSideEffects(store);
            RecalculateAllBuildings();
            GameFeel.FloatText(transform.position, $"{Kind} Lv.{Level}", new Color(1f, 0.86f, 0.45f));
            return true;
        }

        public void RecalculateStats(bool fullHeal)
        {
            int baseHp = BuildingUpgradeRules.BaseHp(Kind, BalanceConfig.Instance);
            if (Kind == BuildingKind.Fence)
            {
                baseHp = Mathf.RoundToInt(baseHp * BuildingUpgradeRules.FenceHpMultiplier());
            }

            int existingCore = 0;
            var all = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var bb in all)
            {
                if (bb == null || bb == this) continue;
                if (bb.Kind == BuildingKind.Fence) continue;
                existingCore++;
            }

            float growth = 1f + Mathf.Min(existingCore * 0.12f, 2f);
            float levelMul = 1f + 0.35f * (Level - 1);
            int oldMax = MaxHp;
            MaxHp = Mathf.RoundToInt(baseHp * growth * levelMul);
            CurrentHp = fullHeal ? MaxHp : Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, MaxHp - oldMax));
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

        private void EnsureHpBar()
        {
            if (Kind == BuildingKind.Fence || GetComponent<HpBarUi>() != null) return;

            var hp = gameObject.AddComponent<HpBarUi>();
            hp.Building = this;
            hp.Offset = new Vector2(0f, 0.6f);
            hp.Size = new Vector2(0.9f, 0.10f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            hp.FillColor = Kind switch
            {
                BuildingKind.Campfire => new Color(1f, 0.55f, 0.2f),
                BuildingKind.Brazier => new Color(1f, 0.72f, 0.22f),
                BuildingKind.Blacksmith => new Color(1f, 0.35f, 0.18f),
                BuildingKind.TrainingCamp => new Color(0.9f, 0.42f, 0.22f),
                BuildingKind.FoodStorage => new Color(0.95f, 0.78f, 0.4f),
                BuildingKind.LookoutPost => new Color(0.55f, 0.8f, 1f),
                BuildingKind.Sawmill => new Color(0.55f, 0.36f, 0.18f),
                BuildingKind.Church => new Color(0.78f, 0.78f, 0.95f),
                BuildingKind.Barricade => new Color(0.6f, 0.4f, 0.2f),
                _ => new Color(0.5f, 0.85f, 0.5f),
            };
        }

        private void ApplyLevelEffects()
        {
            var aura = GetComponent<CampfireAura>();
            if (aura != null) aura.ApplyBuildingLevel(Kind, Level);
        }

        private void ApplyUpgradeSideEffects(ResourceStore store)
        {
            if (store == null) return;
            if (Kind == BuildingKind.FoodStorage)
            {
                store.IncreaseCap(ResourceKind.Food, BuildingUpgradeRules.FoodStorageCapPerLevel);
            }
        }

        private static void RecalculateAllBuildings()
        {
            var all = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in all)
            {
                if (b == null || b.CurrentHp <= 0) continue;
                b.RecalculateStats(false);
            }
        }
    }
}
