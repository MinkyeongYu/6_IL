using UnityEngine;

namespace IL6
{
    public static class SpriteBank
    {
        private static Sprite LoadQuiet(string path)
        {
            return Resources.Load<Sprite>($"Sprites/{path}");
        }

        public static Sprite Load(string path)
        {
            var s = Resources.Load<Sprite>($"Sprites/{path}");
            if (s == null) Debug.LogWarning($"[SpriteBank] Missing sprite: Sprites/{path}");
            return s;
        }

        public static Sprite LoadSubSprite(string path, string spriteName)
        {
            var sprites = Resources.LoadAll<Sprite>($"Sprites/{path}");
            foreach (var s in sprites)
            {
                if (s != null && s.name == spriteName) return s;
            }

            Debug.LogWarning($"[SpriteBank] Missing sub sprite: Sprites/{path}/{spriteName}");
            return null;
        }

        public static Sprite LoadUiSprite(string path)
        {
            var s = Resources.Load<Sprite>($"UI/{path}");
            if (s == null) Debug.LogWarning($"[SpriteBank] Missing UI sprite: UI/{path}");
            return s;
        }

        private static Sprite LoadSpriteOrUi(string spritePath, string uiPath)
        {
            var s = LoadQuiet(spritePath);
            if (s != null) return s;

            s = Resources.Load<Sprite>($"UI/{uiPath}");
            if (s != null) return s;

            Debug.LogWarning($"[SpriteBank] Missing sprite fallback: Sprites/{spritePath} or UI/{uiPath}");
            return null;
        }

        public static Sprite Deer() => Load("Animals/deer");
        public static Sprite Wolf() => Load("Animals/wolf");
        public static Sprite Rabbit() => Load("Animals/rabbit");
        public static Sprite Boar() => Load("Animals/boar");
        public static Sprite Mammoth() => Load("Animals/mammoth");
        public static Sprite Bear() => Load("Animals/bear");

        public static Sprite Zombie() => Load("Enemies/zombie");
        public static Sprite BossFrostZombie() => Load("Enemies/boss_frost_zombie");
        public static Sprite BossWinterKnight() => Load("Enemies/boss_winter_knight");
        public static Sprite BossIronGiant() => Load("Enemies/boss_iron_giant");
        public static Sprite BossFrostLich() => Load("Enemies/boss_frost_lich");

        public static Sprite CompanionUncle() => Load("Companions/uncle");
        public static Sprite CompanionAunt() => Load("Companions/aunt");
        public static Sprite CompanionChild() => Load("Companions/child");

        public static Sprite Player() => Load("Player/player");

        public static Sprite CompanionByRole(string role) => CompanionSpriteForRole(role);

        public static Sprite CompanionSpriteForRole(string role)
        {
            if (IsChildRole(role)) return CompanionChild();
            if (IsFemaleRole(role)) return CompanionAunt();
            return CompanionUncle();
        }

        public static Vector3 CompanionScaleForRole(string role)
        {
            if (IsChildRole(role)) return Vector3.one * 0.82f;
            if (IsFemaleRole(role)) return Vector3.one * 0.96f;
            return Vector3.one;
        }

        public static bool IsChildRole(string role)
        {
            string r = role ?? "";
            return r.Contains("Child") || r.Contains("child") || r.Contains("\uC544\uC774") || r.Contains("?꾩씠");
        }

        public static bool IsFemaleRole(string role)
        {
            string r = role ?? "";
            return r.Contains("Aunt") || r.Contains("aunt")
                || r.Contains("Farmer") || r.Contains("Elder") || r.Contains("Cook")
                || r.Contains("\uB18D") || r.Contains("\uB178\uC778")
                || r.Contains("?띾?") || r.Contains("?몄씤");
        }

        public static Sprite PineTree() => LoadSpriteOrUi("Props/pine_tree", "hud/hud-wood");
        public static Sprite PineTree02() => LoadSpriteOrUi("Props/pine_tree_02", "hud/hud-wood");
        public static Sprite PineTree03() => LoadSpriteOrUi("Props/pine_tree_03", "hud/hud-wood");
        public static Sprite PineTree04() => LoadSpriteOrUi("Props/pine_tree_04", "hud/hud-wood");
        public static Sprite BareTree() => LoadSpriteOrUi("Props/bare_tree", "hud/hud-wood");
        public static Sprite BareTree02() => LoadSpriteOrUi("Props/bare_tree_02", "hud/hud-wood");
        public static Sprite BareTree03() => LoadSpriteOrUi("Props/bare_tree_03", "hud/hud-wood");
        public static Sprite SnowTree01() => LoadSpriteOrUi("Props/snow_tree_01", "hud/hud-wood");
        public static Sprite SnowTree02() => LoadSpriteOrUi("Props/snow_tree_02", "hud/hud-wood");

        public static Sprite TreeVariant(int variant)
        {
            switch (Mathf.Abs(variant) % 9)
            {
                case 0: return PineTree();
                case 1: return PineTree02();
                case 2: return PineTree03();
                case 3: return PineTree04();
                case 4: return BareTree();
                case 5: return BareTree02();
                case 6: return BareTree03();
                case 7: return SnowTree01();
                default: return SnowTree02();
            }
        }

        public static Sprite SnowRocks() => LoadSpriteOrUi("Props/snow_rocks", "hud/hud-stone");
        public static Sprite SmallRocks() => LoadSpriteOrUi("Props/small_rocks", "hud/hud-stone");
        public static Sprite Stump() => LoadSpriteOrUi("Props/stump", "hud/hud-wood");
        public static Sprite SnowBush() => LoadSpriteOrUi("Props/snow_bush", "hud/hud-wood");
        public static Sprite Logs() => LoadSpriteOrUi("Props/logs", "hud/hud-wood");

        public static Sprite CropPotatoIcon() => LoadUiSprite("hud/hud-crop-potato");
        public static Sprite CropTurnipIcon() => LoadUiSprite("hud/hud-crop-turnip");
        public static Sprite CropWheatIcon() => LoadUiSprite("hud/hud-crop-wheat");
        public static Sprite CropHarvestIcon() => LoadUiSprite("hud/hud-crop-harvest");
        public static Sprite FoodIcon() => LoadUiSprite("hud/hud-food");
        public static Sprite WoodIcon() => LoadUiSprite("hud/hud-wood");
        public static Sprite StoneIcon() => LoadUiSprite("hud/hud-stone");
        public static Sprite MeatIcon() => LoadUiSprite("hud/hud-meat");
        public static Sprite HomeIcon() => LoadUiSprite("hud/hud-home");
        public static Sprite TempIcon() => LoadUiSprite("hud/hud-temp");
        public static Sprite HpIcon() => LoadUiSprite("hud/hud-hp");
        public static Sprite ThreatIcon() => LoadUiSprite("hud/hud-threat");
        public static Sprite PopulationIcon() => LoadUiSprite("hud/hud-population");
        public static Sprite UpgradeIcon() => LoadUiSprite("hud/hud-upgrade");

        public static Sprite Campfire() => LoadSpriteOrUi("Props/campfire", "hud/hud-temp");
        public static Sprite Cabin() => LoadSpriteOrUi("Props/cabin", "hud/hud-home");
        public static Sprite Watchtower() => LoadSpriteOrUi("Props/watchtower", "hud/hud-threat");
        public static Sprite FenceVertical() => LoadQuiet("Props/fence_vertical") ?? SnowFenceCenter();
        public static Sprite SnowFenceH() => LoadQuiet("Props/snow_fence_h") ?? SnowFenceCenter();
        public static Sprite SnowFenceLeft() => LoadSubSprite("Props/Fence", "wooden_fence_Left");
        public static Sprite SnowFenceCenter() => LoadSubSprite("Props/Fence", "wooden_fence_Center");
        public static Sprite SnowFenceRight() => LoadSubSprite("Props/Fence", "wooden_fence_Right");
        public static Sprite WoodBarricade() => LoadQuiet("Props/wood_barricade") ?? SnowFenceCenter() ?? WoodIcon();
        public static Sprite StoneWall() => LoadSpriteOrUi("Props/stone_wall", "hud/hud-stone");
        public static Sprite SpikeBarricade() => LoadQuiet("Props/spike_barricade") ?? SnowFenceCenter() ?? ThreatIcon();

        public static Sprite BuildingByKind(BuildingKind k) => k switch
        {
            BuildingKind.Campfire => Campfire(),
            BuildingKind.Brazier => Campfire(),
            BuildingKind.Blacksmith => TempIcon(),
            BuildingKind.SeedStorage => CropPotatoIcon(),
            BuildingKind.Carpenter => WoodIcon(),
            BuildingKind.TrainingCamp => WoodBarricade(),
            BuildingKind.FoodStorage => FoodIcon(),
            BuildingKind.LookoutPost => Watchtower(),
            BuildingKind.Sawmill => WoodIcon(),
            BuildingKind.Church => HomeIcon(),
            BuildingKind.Fence => SnowFenceCenter(),
            BuildingKind.Barricade => WoodBarricade(),
            BuildingKind.Watchtower => Watchtower(),
            BuildingKind.House => Cabin(),
            BuildingKind.Storage => Logs(),
            BuildingKind.Farm => CropPotatoIcon(),
            BuildingKind.Infirmary => HpIcon(),
            BuildingKind.HuntersHut => MeatIcon(),
            _ => null,
        };

        public static Sprite DiscoveryByReward(ResourceKind kind) => kind switch
        {
            ResourceKind.Wood => WoodIcon(),
            ResourceKind.Stone => StoneIcon(),
            ResourceKind.Meat => MeatIcon(),
            ResourceKind.Food => FoodIcon(),
            ResourceKind.Frostbloom => UpgradeIcon(),
            _ => UpgradeIcon(),
        };

        public static Sprite AnimalByName(string procName) => procName switch
        {
            "Deer_proc" => Deer(),
            "Wolf_proc" => Wolf(),
            "Rabbit_proc" => Rabbit(),
            "SnowHare_proc" => Rabbit(),
            "Boar_proc" => Boar(),
            "Mammoth_proc" => Mammoth(),
            "Bear_proc" => Bear(),
            "Fox_proc" => Bear(),
            _ => null,
        };
    }
}
