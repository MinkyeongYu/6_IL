using UnityEngine;
using UnityEngine.SceneManagement;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// 씬 간 공유 상태(자원·사이클·세이브 등). DontDestroyOnLoad 싱글톤.
    /// SnowfieldController/VillageController가 Instance를 통해 접근.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public ResourceStore Resources { get; private set; }
        public DayNightController Cycle { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (GetComponent<AchievementManager>() == null)
                gameObject.AddComponent<AchievementManager>();
            if (GetComponent<SettlementGoalManager>() == null)
                gameObject.AddComponent<SettlementGoalManager>();

            Resources = new ResourceStore();
            Cycle = new DayNightController(BalanceConfig.Instance);

            // 시작 자원 (세이브가 없을 때)
            var b = BalanceConfig.Instance;
            Resources.Add(ResourceKind.Wood, b.StartingWood);
            Resources.Add(ResourceKind.Stone, b.StartingStone);
            Resources.Add(ResourceKind.Meat, b.StartingMeat);
            Resources.Add(ResourceKind.Food, b.StartingFood);
            Resources.Add(ResourceKind.Frostbloom, b.StartingFrostbloom);

            // 세이브 있으면 덮어씌움
            var save = SaveLoad.Load();
            if (save != null)
            {
                Resources.Restore(save.resources);
                Cycle.Restore(new DayNightSnapshot { day = save.currentDay, phase = Phase.Day, elapsedInPhase = 0 });
                PregnancyActive = save.pregnancyActive;
                PregnancyStartDay = save.pregnancyStartDay;
                PregnancyDueDay = save.pregnancyDueDay;
                LastPregnancyParents = save.pregnancyParents;
                if (PregnancyActive && PregnancyDueDay <= 0)
                    PregnancyDueDay = save.currentDay + PregnancyDurationDays;
            }
        }

        public int LastFoodEaten { get; private set; }
        public int LastFoodNeeded { get; private set; }
        public int LastFoodShortage { get; private set; }
        public int ConsecutiveFedDays { get; private set; }
        public int LastBirthChancePercent { get; private set; }
        public bool LastChildBorn { get; private set; }
        public bool PregnancyActive { get; private set; }
        public int PregnancyStartDay { get; private set; }
        public int PregnancyDueDay { get; private set; }
        public bool LastPregnancyStarted { get; private set; }
        public string LastPregnancyParents { get; private set; }
        public const int PregnancyDurationDays = 7;
        public int TotalKills { get; private set; }
        public int CompanionsLost { get; private set; }
        public int MaxCompanionsAtOnce { get; private set; }
        public int PlayerDeathDay { get; private set; }

        public int Score => Mathf.Max(0,
            (Cycle != null ? (Cycle.Day - 1) * 10 : 0)
            + TotalKills * 3
            + MaxCompanionsAtOnce * 5
            - CompanionsLost * 4);

        public void OnZombieKilled()
        {
            TotalKills++;
            var night = Object.FindFirstObjectByType<NightController>();
            if (night != null) night.OnNightKill();

            // 가장 가까운 살아있는 동료에게 +1 XP — 위치 기반은 NightController 의 좀비 위치를
            // 알 수 없어 여기서는 플레이어 기준으로 가까운 동료를 픽함
            var p = GameObject.FindWithTag("Player");
            if (p == null) return;
            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            Companion best = null; float bestDist = float.MaxValue;
            foreach (var c in comps)
            {
                if (c == null || c.IsDead) continue;
                float d = Vector2.Distance(c.transform.position, p.transform.position);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            if (best != null) best.GrantXp(1);
        }
        public void OnCompanionLost() { CompanionsLost++; }

        public void MarkPlayerDied(int day)
        {
            if (PlayerDeathDay > 0) return;
            PlayerDeathDay = Mathf.Max(1, day);
        }

        private System.Action _unsubDay;

        private void Start()
        {
            _unsubDay = EventBus.Instance.Subscribe<DayStartedPayload>(p => OnDayStarted(p.Day));
        }

        private void OnDestroy()
        {
            _unsubDay?.Invoke();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // 동시 동료 최대치 추적
            int n = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None).Length;
            if (n > MaxCompanionsAtOnce) MaxCompanionsAtOnce = n;
        }

        private void OnDayStarted(int day)
        {
            LastChildBorn = false;
            LastPregnancyStarted = false;
            TryCompletePregnancy(day);

            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int needed = FoodNeededForCompanions(comps);
            int have = Resources.Get(ResourceKind.Food);
            int eat = Mathf.Min(have, needed);
            if (eat > 0) Resources.Spend(ResourceKind.Food, eat);
            int hungry = needed - eat;
            LastFoodNeeded = needed;
            LastFoodEaten = eat;
            LastFoodShortage = hungry;
            ConsecutiveFedDays = hungry > 0 ? 0 : ConsecutiveFedDays + 1;
            if (hungry > 0 && comps != null)
            {
                for (int i = 0; i < hungry && i < comps.Length; i++)
                {
                    var c = comps[i];
                    if (c == null) continue;
                    int moraleLoss = BuildingUpgradeRules.ReduceMoraleLoss(15, unchecked((uint)day * 2654435761u + (uint)i * 97u));
                    c.Morale -= moraleLoss;
                    if (c.Morale <= 0)
                    {
                        OnCompanionLost();
                        Destroy(c.gameObject);
                    }
                }
            }

            TryStartPregnancy(day, comps);

            // 새벽 회복: Player + 모든 살아있는 동료 풀 HP
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.Heal(pc.MaxHp);
            }
            if (comps != null)
            {
                foreach (var c in comps)
                {
                    if (c != null) c.Heal(c.MaxHp);
                }
            }

            // 자동 저장 (영속: 자원 + 일자)
            SaveNow();
            LastAutoSaveAt = Time.time;
        }

        public static int FoodNeededForPopulation(int population)
        {
            if (population <= 0) return 0;

            if (population <= 10) return population;
            if (population <= 18) return Mathf.CeilToInt(population * 1.25f);
            if (population <= 30) return Mathf.CeilToInt(population * 1.5f);
            return population * 2;
        }

        public static int FoodNeededForCompanions(Companion[] companions)
        {
            if (companions == null || companions.Length == 0) return 0;

            int foodUnitsX2 = 0;
            int living = 0;
            foreach (var c in companions)
            {
                if (c == null || c.IsDead) continue;
                living++;
                var growth = c.GetComponent<VillageChildGrowth>();
                int units = growth != null ? growth.FoodUnitsX2 : 2;
                units = Mathf.Max(1, Mathf.RoundToInt(units * CompanionTrait.FoodMultiplierFor(c)));
                foodUnitsX2 += units;
            }

            int baseNeed = Mathf.CeilToInt(foodUnitsX2 / 2f);
            if (living <= 10) return baseNeed;
            if (living <= 18) return Mathf.CeilToInt(baseNeed * 1.25f);
            if (living <= 30) return Mathf.CeilToInt(baseNeed * 1.5f);
            return baseNeed * 2;
        }

        public int PregnancyDaysRemaining(int currentDay)
        {
            if (!PregnancyActive) return 0;
            return Mathf.Max(0, PregnancyDueDay - currentDay);
        }

        private void TryCompletePregnancy(int day)
        {
            if (!PregnancyActive || day < PregnancyDueDay) return;
            SpawnBornChild(day);
            PregnancyActive = false;
            LastChildBorn = true;
        }

        private void TryStartPregnancy(int day, Companion[] companions)
        {
            LastBirthChancePercent = 0;
            if (PregnancyActive) return;
            if (Resources.Get(ResourceKind.Food) < 20) return;
            if (LastFoodShortage > 0 || ConsecutiveFedDays < 2) return;

            EnsureFamilyProfiles(companions);
            PairEligibleAdults();

            int living = 0;
            if (companions != null)
            {
                foreach (var c in companions)
                {
                    if (c == null || c.IsDead) continue;
                    living++;
                }
            }

            int cap = RecruitableNpc.VillageCapacity();
            if (cap - living < 2) return;

            var couples = FindPregnancyEligibleCouples();
            if (couples.Count == 0) return;

            int chance = 5;
            int houses = CountBuildings(BuildingKind.House);
            chance += houses * 2;
            if (Resources.Get(ResourceKind.Food) >= 40) chance += 3;
            if (CountBuildings(BuildingKind.Infirmary) > 0) chance += 5;
            chance = Mathf.Clamp(chance, 0, 25);
            LastBirthChancePercent = chance;

            uint rollSeed = unchecked((uint)day * 1103515245u + (uint)living * 97u + (uint)TotalKills * 13u);
            var rng = new SeededRng(rollSeed);
            if (rng.IntRange(1, 100) > chance) return;

            var chosen = couples[rng.IntRange(0, couples.Count - 1)];
            PregnancyActive = true;
            PregnancyStartDay = day;
            PregnancyDueDay = day + PregnancyDurationDays;
            LastPregnancyStarted = true;
            LastPregnancyParents = $"{chosen.Male.gameObject.name} + {chosen.Female.gameObject.name}";
        }

        private struct PregnancyCouple
        {
            public readonly CompanionFamily Male;
            public readonly CompanionFamily Female;

            public PregnancyCouple(CompanionFamily male, CompanionFamily female)
            {
                Male = male;
                Female = female;
            }
        }

        private static void EnsureFamilyProfiles(Companion[] companions)
        {
            if (companions == null) return;
            foreach (var c in companions)
            {
                if (c == null || c.IsDead) continue;
                var family = c.GetComponent<CompanionFamily>();
                if (family == null) family = c.gameObject.AddComponent<CompanionFamily>();
                if (family.BiologicalSex != CompanionFamily.Sex.Unknown) continue;

                var growth = c.GetComponent<VillageChildGrowth>();
                if (growth != null)
                {
                    family.IsChild = true;
                    continue;
                }

                string name = c.gameObject.name;
                bool looksFemale = name.Contains("농부") || name.Contains("노인") || name.Contains("Aunt") || name.Contains("aunt");
                family.BiologicalSex = looksFemale ? CompanionFamily.Sex.Female : CompanionFamily.Sex.Male;
            }
        }

        private static void PairEligibleAdults()
        {
            var families = Object.FindObjectsByType<CompanionFamily>(FindObjectsSortMode.None);
            foreach (var female in families)
            {
                if (female == null || female.BiologicalSex != CompanionFamily.Sex.Female || female.EverPartnered || !female.IsLivingAdult) continue;
                foreach (var male in families)
                {
                    if (male == null || male.BiologicalSex != CompanionFamily.Sex.Male || male.EverPartnered || !male.IsLivingAdult) continue;
                    female.PairWith(male);
                    break;
                }
            }
        }

        private static System.Collections.Generic.List<PregnancyCouple> FindPregnancyEligibleCouples()
        {
            var result = new System.Collections.Generic.List<PregnancyCouple>();
            var families = Object.FindObjectsByType<CompanionFamily>(FindObjectsSortMode.None);
            foreach (var female in families)
            {
                if (female == null || female.BiologicalSex != CompanionFamily.Sex.Female || !female.IsLivingAdult) continue;
                var partner = female.FindPartner();
                if (partner == null || partner.BiologicalSex != CompanionFamily.Sex.Male || !partner.IsLivingAdult) continue;
                result.Add(new PregnancyCouple(partner, female));
            }
            return result;
        }

        private static int CountBuildings(BuildingKind kind)
        {
            int count = 0;
            var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in buildings)
            {
                if (b == null || b.CurrentHp <= 0 || b.Kind != kind) continue;
                count++;
            }
            return count;
        }

        private void SpawnBornChild(int day)
        {
            Vector2 jitter = Random.insideUnitCircle * 1.2f;
            uint nameSeed = unchecked((uint)day * 747796405u + (uint)TotalKills * 2891336453u + (uint)RecruitableNpc.CurrentCompanionCount());
            string childName = CompanionNameGenerator.GenerateForRole("아이", new SeededRng(nameSeed));
            var go = new GameObject($"{childName}(Born)");
            go.transform.position = new Vector3(GameConstants.VillageCenterX + jitter.x, GameConstants.VillageCenterY + jitter.y, 0f);
            go.transform.localScale = Vector3.one * 0.7f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;
            var spr = SpriteBank.CompanionChild();
            if (spr != null) sr.sprite = spr;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.95f, 0.85f, 0.75f);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 64;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.1f, 0.1f, 0.15f, 1f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.28f;

            var child = go.AddComponent<Companion>();
            var player = GameObject.FindWithTag("Player");
            if (player != null) child.Player = player.transform;
            child.IsCombat = false;
            child.MoveSpeed = 3.2f;
            child.AttackRange = 1.0f;
            child.Damage = 1;
            child.AttackCooldown = 2.5f;
            child.SetMaxHp(25, true);

            var growth = go.AddComponent<VillageChildGrowth>();
            growth.BirthDay = day;
            growth.AgeDays = 0;

            var family = go.AddComponent<CompanionFamily>();
            family.IsChild = true;
            family.BiologicalSex = CompanionFamily.Sex.Unknown;
            CompanionTrait.AssignRandom(go, "?꾩씠", new SeededRng(nameSeed ^ 0xa531c6f1u));

            EventBus.Instance.Emit(new CompanionRecruitedPayload(childName, "아이", "새 생명이 마을에 태어났어요."));
        }

        public float LastAutoSaveAt { get; private set; } = -10f;

        public void SaveNow()
        {
            SaveLoad.Save(new SaveFileV1
            {
                version = 1,
                currentDay = Cycle.Day,
                resources = Resources.Snapshot(),
                weatherRng = 42,
                pregnancyActive = PregnancyActive,
                pregnancyStartDay = PregnancyStartDay,
                pregnancyDueDay = PregnancyDueDay,
                pregnancyParents = LastPregnancyParents,
            });
        }

        public void HardReset()
        {
            SaveLoad.Clear();
            Instance = null;
            Destroy(gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
