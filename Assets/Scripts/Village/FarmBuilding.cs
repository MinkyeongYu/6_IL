using System;
using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// 밭. 밤이 NightsToRipe 만큼 지나면 HarvestReady=true.
    /// 동료를 배치하면 수확량이 증가하고 동료는 Farming 모드로 묶임.
    /// 수확하면 식량 적립, 동료 해제, 카운트 리셋.
    /// </summary>
    public sealed class FarmBuilding : MonoBehaviour
    {
        public enum CropKind { Potato, Turnip, Wheat }

        public CropKind CurrentCrop = CropKind.Potato;
        public int BaseMaxWorkers = 2;

        public int NightsToRipe => EffectiveGrowthNights(CurrentCrop);
        public int BaseYield => CropBaseYield(CurrentCrop);
        public int PerWorkerBonus => CropWorkerBonus(CurrentCrop);
        public int MaxWorkers => BaseMaxWorkers + (FarmLevel >= 3 ? 1 : 0) + (FarmLevel >= 5 ? 1 : 0);

        /// <summary>최대 농장 수 = 1(기본) + 창고 수. 창고를 지을 때마다 농장 한도 +1.</summary>
        public static int MaxFarmsAllowed()
        {
            int storages = 0;
            int seedLevels = 0;
            var bs = UnityEngine.Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0) continue;
                if (b.Kind == BuildingKind.Storage) storages++;
                if (b.Kind == BuildingKind.SeedStorage) seedLevels += Mathf.Max(1, b.Level);
            }
            return 1 + storages + seedLevels;
        }

        public static int CurrentFarmCount()
        {
            return UnityEngine.Object.FindObjectsByType<FarmBuilding>(FindObjectsSortMode.None).Length;
        }

        public bool HarvestReady { get; private set; }
        public int NightsPassed { get; private set; }
        public readonly List<Companion> Workers = new();

        private Action _unsub;
        private float _coldYieldMultiplier = 1f;
        private int FarmLevel
        {
            get
            {
                var building = GetComponent<Building>();
                return building != null ? Mathf.Max(1, building.Level) : 1;
            }
        }

        private void Start()
        {
            _unsub = EventBus.Instance.Subscribe<NightStartedPayload>(_ => OnNight());
        }

        private void OnDestroy()
        {
            _unsub?.Invoke();
            foreach (var w in Workers) if (w != null) w.ReleaseFarm();
        }

        private void OnNight()
        {
            if (HarvestReady) return;
            float coldTempC = CurrentFarmTemperatureCelsius();
            float protectedTempC = FarmProtectedTemperature(coldTempC);
            if (!SurvivesColdNight(protectedTempC))
            {
                NightsPassed = 0;
                _coldYieldMultiplier = 1f;
                foreach (var w in Workers) if (w != null) w.ReleaseFarm();
                Workers.Clear();
                GameFeel.FloatText(transform.position, $"{CropDisplayName(CurrentCrop)} withered", new Color(0.95f, 0.55f, 0.45f));
                return;
            }

            _coldYieldMultiplier = Mathf.Min(_coldYieldMultiplier, CropColdYieldMultiplier(CurrentCrop, protectedTempC));

            if (IsBlizzardStalled())
            {
                GameFeel.FloatText(transform.position, $"{CropDisplayName(CurrentCrop)} stalled", new Color(0.55f, 0.85f, 1f));
                return;
            }

            NightsPassed++;
            if (NightsPassed >= NightsToRipe) HarvestReady = true;
        }

        public bool TryAssignWorker(Companion c)
        {
            if (c == null || Workers.Contains(c)) return false;
            if (Workers.Count >= MaxWorkers) return false;
            Workers.Add(c);
            c.AssignFarm(transform);
            return true;
        }

        public int Harvest()
        {
            if (!HarvestReady) return 0;
            int baseYield = BaseYield + Workers.Count * PerWorkerBonus;
            float farmLevelBonus = 1f + 0.15f * (FarmLevel - 1);
            int yield = Mathf.Max(1, Mathf.RoundToInt(baseYield * BuildingUpgradeRules.CropYieldMultiplier() * farmLevelBonus * _coldYieldMultiplier));
            var session = GameSession.Instance;
            if (session != null) session.Resources.Add(ResourceKind.Food, yield);
            GameFeel.FloatText(transform.position, $"+{yield} Food", new Color(0.7f, 0.95f, 0.5f));
            foreach (var w in Workers) if (w != null) w.ReleaseFarm();
            Workers.Clear();
            NightsPassed = 0;
            HarvestReady = false;
            _coldYieldMultiplier = 1f;
            return yield;
        }

        public int EstimatedYield()
        {
            int baseYield = BaseYield + Workers.Count * PerWorkerBonus;
            float farmLevelBonus = 1f + 0.15f * (FarmLevel - 1);
            return Mathf.Max(1, Mathf.RoundToInt(baseYield * BuildingUpgradeRules.CropYieldMultiplier() * farmLevelBonus * _coldYieldMultiplier));
        }

        public bool CanChangeCrop()
        {
            return !HarvestReady && NightsPassed == 0 && Workers.Count == 0 && UnlockedCrops().Count > 1;
        }

        public bool CycleCrop()
        {
            if (!CanChangeCrop()) return false;
            var crops = UnlockedCrops();
            if (crops.Count <= 1) return false;
            int current = crops.IndexOf(CurrentCrop);
            CurrentCrop = crops[(current + 1 + crops.Count) % crops.Count];
            _coldYieldMultiplier = 1f;
            GameFeel.FloatText(transform.position, CropDisplayName(CurrentCrop), new Color(0.95f, 0.9f, 0.45f));
            return true;
        }

        public string CropLabel()
        {
            return $"{CropDisplayName(CurrentCrop)} {NightsPassed}/{NightsToRipe}";
        }

        public static string CropDisplayName(CropKind crop) => crop switch
        {
            CropKind.Turnip => "Turnip",
            CropKind.Wheat => "Wheat",
            _ => "Potato",
        };

        public static int CropBaseYield(CropKind crop) => crop switch
        {
            CropKind.Turnip => 6,
            CropKind.Wheat => 22,
            _ => 10,
        };

        public static int CropWorkerBonus(CropKind crop) => crop switch
        {
            CropKind.Turnip => 3,
            CropKind.Wheat => 8,
            _ => 6,
        };

        public static int CropGrowthNights(CropKind crop) => crop switch
        {
            CropKind.Turnip => 1,
            CropKind.Wheat => 3,
            _ => 2,
        };

        public static float CropColdSurvivalChance(CropKind crop, float temperatureCelsius)
        {
            float hardyLimit = crop switch
            {
                CropKind.Turnip => -34f,
                CropKind.Potato => -26f,
                CropKind.Wheat => -18f,
                _ => -24f,
            };

            float pressure = Mathf.Max(0f, hardyLimit - temperatureCelsius);
            float lossPerDegree = crop switch
            {
                CropKind.Turnip => 0.018f,
                CropKind.Potato => 0.035f,
                CropKind.Wheat => 0.055f,
                _ => 0.04f,
            };
            return Mathf.Clamp(1f - pressure * lossPerDegree, 0.1f, 1f);
        }

        public static float CropColdYieldMultiplier(CropKind crop, float temperatureCelsius)
        {
            float yieldLimit = crop switch
            {
                CropKind.Turnip => -28f,
                CropKind.Potato => -18f,
                CropKind.Wheat => -10f,
                _ => -16f,
            };

            float pressure = Mathf.Max(0f, yieldLimit - temperatureCelsius);
            float lossPerDegree = crop switch
            {
                CropKind.Turnip => 0.018f,
                CropKind.Potato => 0.028f,
                CropKind.Wheat => 0.042f,
                _ => 0.03f,
            };
            return Mathf.Clamp(1f - pressure * lossPerDegree, 0.25f, 1f);
        }

        public static int RequiredSeedStorageLevel(CropKind crop) => crop switch
        {
            CropKind.Turnip => 2,
            CropKind.Wheat => 3,
            _ => 0,
        };

        public static bool IsCropUnlocked(CropKind crop)
        {
            return BuildingUpgradeRules.HighestLevel(BuildingKind.SeedStorage) >= RequiredSeedStorageLevel(crop);
        }

        public static List<CropKind> UnlockedCrops()
        {
            var result = new List<CropKind> { CropKind.Potato };
            if (IsCropUnlocked(CropKind.Turnip)) result.Add(CropKind.Turnip);
            if (IsCropUnlocked(CropKind.Wheat)) result.Add(CropKind.Wheat);
            return result;
        }

        private int EffectiveGrowthNights(CropKind crop)
        {
            int nights = CropGrowthNights(crop);
            if (FarmLevel >= 4) nights = Mathf.Max(1, nights - 1);
            return nights;
        }

        private bool IsBlizzardStalled()
        {
            if (CurrentCrop == CropKind.Turnip) return false;
            if (FarmLevel >= 4) return false;
            var nights = UnityEngine.Object.FindObjectsByType<NightController>(FindObjectsSortMode.None);
            return nights != null && nights.Length > 0 && nights[0] != null && nights[0].IsBlizzard;
        }

        private bool SurvivesColdNight(float temperatureCelsius)
        {
            float chance = CropColdSurvivalChance(CurrentCrop, temperatureCelsius);
            return UnityEngine.Random.value <= chance;
        }

        private float FarmProtectedTemperature(float temperatureCelsius)
        {
            return temperatureCelsius + Mathf.Max(0, FarmLevel - 1) * 2f;
        }

        private static float CurrentFarmTemperatureCelsius()
        {
            var session = GameSession.Instance;
            if (session == null || session.Cycle == null) return -8f;

            float baseTemp = session.Cycle.Phase switch
            {
                Phase.Day => -8f,
                Phase.Evening => -18f,
                Phase.Night => -28f,
                Phase.Dawn => -14f,
                _ => -8f
            };
            float progress = session.Cycle.PhaseDurationSec > 0f
                ? session.Cycle.ElapsedInPhase / session.Cycle.PhaseDurationSec
                : 0f;
            float nightDrop = session.Cycle.Phase == Phase.Night ? -8f * progress : 0f;

            var night = UnityEngine.Object.FindFirstObjectByType<NightController>();
            float blizzardDrop = night != null && night.IsBlizzard ? -8f : 0f;
            return baseTemp + nightDrop + blizzardDrop;
        }
    }
}
