using UnityEngine;

namespace IL6
{
    public readonly struct ResourceCost
    {
        public readonly int Wood;
        public readonly int Stone;
        public readonly int Food;
        public readonly int Frostbloom;

        public ResourceCost(int wood, int stone = 0, int food = 0, int frostbloom = 0)
        {
            Wood = wood;
            Stone = stone;
            Food = food;
            Frostbloom = frostbloom;
        }

        public bool CanPay(ResourceStore store)
        {
            if (store == null) return false;
            return store.Get(ResourceKind.Wood) >= Wood
                && store.Get(ResourceKind.Stone) >= Stone
                && store.Get(ResourceKind.Food) >= Food
                && store.Get(ResourceKind.Frostbloom) >= Frostbloom;
        }

        public bool Pay(ResourceStore store)
        {
            if (!CanPay(store)) return false;
            if (Wood > 0 && !store.Spend(ResourceKind.Wood, Wood)) return false;
            if (Stone > 0 && !store.Spend(ResourceKind.Stone, Stone)) return false;
            if (Food > 0 && !store.Spend(ResourceKind.Food, Food)) return false;
            if (Frostbloom > 0 && !store.Spend(ResourceKind.Frostbloom, Frostbloom)) return false;
            return true;
        }

        public override string ToString()
        {
            string s = "";
            Append(ref s, Wood, "W");
            Append(ref s, Stone, "S");
            Append(ref s, Food, "F");
            Append(ref s, Frostbloom, "Fb");
            return string.IsNullOrEmpty(s) ? "0" : s;
        }

        private static void Append(ref string s, int amount, string suffix)
        {
            if (amount <= 0) return;
            if (!string.IsNullOrEmpty(s)) s += " + ";
            s += $"{amount}{suffix}";
        }
    }

    public static class BuildingUpgradeRules
    {
        public const int MaxLevel = 5;

        public static int BaseWoodCost(BuildingKind kind) => kind switch
        {
            BuildingKind.Campfire => 5,
            BuildingKind.House => 6,
            BuildingKind.Fence => 1,
            BuildingKind.Storage => 8,
            BuildingKind.Farm => 6,
            BuildingKind.Watchtower => 8,
            BuildingKind.Infirmary => 7,
            BuildingKind.HuntersHut => 8,
            BuildingKind.Brazier => 12,
            BuildingKind.Blacksmith => 10,
            BuildingKind.SeedStorage => 8,
            BuildingKind.Carpenter => 12,
            BuildingKind.TrainingCamp => 12,
            BuildingKind.FoodStorage => 10,
            BuildingKind.LookoutPost => 9,
            BuildingKind.Sawmill => 14,
            _ => 5,
        };

        public static int BaseStoneCost(BuildingKind kind) => kind switch
        {
            BuildingKind.Watchtower => 4,
            BuildingKind.Brazier => 4,
            BuildingKind.Blacksmith => 8,
            BuildingKind.SeedStorage => 2,
            BuildingKind.Carpenter => 6,
            BuildingKind.TrainingCamp => 5,
            BuildingKind.FoodStorage => 3,
            BuildingKind.LookoutPost => 6,
            BuildingKind.Sawmill => 8,
            _ => 0,
        };

        public static ResourceCost BuildCost(BuildingKind kind, float duplicateMultiplier)
        {
            return new ResourceCost(
                Mathf.RoundToInt(BaseWoodCost(kind) * duplicateMultiplier),
                Mathf.RoundToInt(BaseStoneCost(kind) * duplicateMultiplier)
            );
        }

        public static ResourceCost UpgradeCost(BuildingKind kind, int currentLevel)
        {
            int next = Mathf.Clamp(currentLevel + 1, 2, MaxLevel);
            float mul = next switch
            {
                2 => 1.0f,
                3 => 1.7f,
                4 => 2.6f,
                _ => 3.8f,
            };

            int wood = Mathf.Max(1, Mathf.RoundToInt(BaseWoodCost(kind) * mul));
            int stone = Mathf.RoundToInt((BaseStoneCost(kind) + StoneUpgradeBias(kind)) * mul);
            int food = kind == BuildingKind.SeedStorage && next >= 3 ? next : 0;
            int frost = kind == BuildingKind.Brazier && next >= 4 ? 1 : 0;
            return new ResourceCost(wood, stone, food, frost);
        }

        private static int StoneUpgradeBias(BuildingKind kind) => kind switch
        {
            BuildingKind.Fence => 1,
            BuildingKind.Barricade => 1,
            BuildingKind.Campfire => 1,
            BuildingKind.Farm => 1,
            BuildingKind.FoodStorage => 1,
            BuildingKind.Sawmill => 2,
            _ => 0,
        };

        public static int BaseHp(BuildingKind kind, BalanceConfig b) => kind switch
        {
            BuildingKind.Campfire => b.CampfireHp,
            BuildingKind.Brazier => b.BrazierHp,
            BuildingKind.Blacksmith => b.BlacksmithHp,
            BuildingKind.SeedStorage => b.SeedStorageHp,
            BuildingKind.Carpenter => b.CarpenterHp,
            BuildingKind.TrainingCamp => b.TrainingCampHp,
            BuildingKind.FoodStorage => b.FoodStorageHp,
            BuildingKind.LookoutPost => b.LookoutPostHp,
            BuildingKind.Sawmill => b.SawmillHp,
            BuildingKind.Barricade => b.BarricadeHp,
            BuildingKind.Fence => b.FenceHp,
            BuildingKind.House => 140,
            BuildingKind.Storage => 200,
            BuildingKind.Farm => 90,
            BuildingKind.Watchtower => 200,
            BuildingKind.Infirmary => 180,
            BuildingKind.HuntersHut => 160,
            _ => b.BarricadeHp,
        };

        public static int HighestLevel(BuildingKind kind)
        {
            int best = 0;
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0 || b.Kind != kind) continue;
                best = Mathf.Max(best, b.Level);
            }
            return best;
        }

        public static float FenceHpMultiplier()
        {
            int carpenter = HighestLevel(BuildingKind.Carpenter);
            return 1f + 0.5f * carpenter;
        }

        public static float CropYieldMultiplier()
        {
            int seedStorage = HighestLevel(BuildingKind.SeedStorage);
            return 1f + 0.15f * seedStorage;
        }

        public const int FoodStorageCapPerLevel = 40;

        public static float TrainingDamageMultiplier()
        {
            int training = HighestLevel(BuildingKind.TrainingCamp);
            return 1f + 0.10f * training;
        }

        public static int FoodStorageCapBonus()
        {
            int total = 0;
            var all = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in all)
            {
                if (b == null || b.CurrentHp <= 0 || b.Kind != BuildingKind.FoodStorage) continue;
                total += Mathf.Max(1, b.Level) * FoodStorageCapPerLevel;
            }
            return total;
        }

        public static float LookoutVisionRadius()
        {
            int lookout = HighestLevel(BuildingKind.LookoutPost);
            return lookout <= 0 ? 0f : 4.5f + 0.6f * (lookout - 1);
        }

        public static float SawmillWoodYieldMultiplier()
        {
            int sawmill = HighestLevel(BuildingKind.Sawmill);
            return 1f + 0.15f * sawmill;
        }

        public static string UpgradeSummary(BuildingKind kind, int nextLevel) => kind switch
        {
            BuildingKind.SeedStorage => $"작물 해금/수확 +{nextLevel * 15}%",
            BuildingKind.Carpenter => $"울타리 HP +{nextLevel * 50}%",
            BuildingKind.Brazier => "열/시야/화염 피해 증가",
            BuildingKind.Blacksmith => "열원 강화 + 장비 테크 기반",
            BuildingKind.TrainingCamp => $"동료 공격력 +{nextLevel * 10}%",
            BuildingKind.FoodStorage => $"+{FoodStorageCapPerLevel} Food 보관",
            BuildingKind.LookoutPost => "밤 시야 반경 증가",
            BuildingKind.Sawmill => $"목재 채집 +{nextLevel * 15}%",
            BuildingKind.Farm => "수확량 증가",
            BuildingKind.Fence => "내구도 증가",
            BuildingKind.Campfire => "열/시야/연료량 증가",
            _ => "HP와 기능 효율 증가",
        };
    }
}
