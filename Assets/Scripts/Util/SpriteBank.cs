using UnityEngine;

namespace IL6
{
    public static class SpriteBank
    {
        public static Sprite Load(string path)
        {
            var s = Resources.Load<Sprite>($"Sprites/{path}");
            if (s == null) Debug.LogWarning($"[SpriteBank] Missing sprite: Sprites/{path}");
            return s;
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

        public static Sprite PineTree() => Load("Props/pine_tree");
        public static Sprite PineTree02() => Load("Props/pine_tree_02");
        public static Sprite PineTree03() => Load("Props/pine_tree_03");
        public static Sprite PineTree04() => Load("Props/pine_tree_04");
        public static Sprite BareTree() => Load("Props/bare_tree");
        public static Sprite BareTree02() => Load("Props/bare_tree_02");
        public static Sprite BareTree03() => Load("Props/bare_tree_03");
        public static Sprite SnowTree01() => Load("Props/snow_tree_01");
        public static Sprite SnowTree02() => Load("Props/snow_tree_02");

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

        public static Sprite SnowRocks() => Load("Props/snow_rocks");
        public static Sprite SmallRocks() => Load("Props/small_rocks");
        public static Sprite Stump() => Load("Props/stump");
        public static Sprite SnowBush() => Load("Props/snow_bush");
        public static Sprite Logs() => Load("Props/logs");

        public static Sprite Campfire() => Load("Props/campfire");
        public static Sprite Cabin() => Load("Props/cabin");
        public static Sprite Watchtower() => Load("Props/watchtower");
        public static Sprite FenceVertical() => Load("Props/fence_vertical");
        public static Sprite SnowFenceH() => Load("Props/snow_fence_h");
        public static Sprite WoodBarricade() => Load("Props/wood_barricade");
        public static Sprite StoneWall() => Load("Props/stone_wall");
        public static Sprite SpikeBarricade() => Load("Props/spike_barricade");

        public static Sprite BuildingByKind(BuildingKind k) => k switch
        {
            BuildingKind.Campfire => Campfire(),
            BuildingKind.Brazier => Campfire(),
            BuildingKind.Blacksmith => Cabin(),
            BuildingKind.SeedStorage => Logs(),
            BuildingKind.Carpenter => Logs(),
            BuildingKind.TrainingCamp => WoodBarricade(),
            BuildingKind.FoodStorage => Logs(),
            BuildingKind.LookoutPost => Watchtower(),
            BuildingKind.Sawmill => Logs(),
            BuildingKind.Church => Cabin(),
            BuildingKind.Fence => FenceVertical(),
            BuildingKind.Barricade => WoodBarricade(),
            BuildingKind.Watchtower => Watchtower(),
            BuildingKind.House => Cabin(),
            _ => null,
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
