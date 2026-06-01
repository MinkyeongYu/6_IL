using UnityEngine;

namespace IL6
{
    /// <summary>
    /// Resources/Sprites/ 아래 PNG 를 이름으로 로드하는 유틸.
    /// ColorFallback 보다 먼저 sr.sprite 에 할당하면 ColorFallback 이 폴백을 건너뜀.
    /// </summary>
    public static class SpriteBank
    {
        public static Sprite Load(string path)
        {
            var s = Resources.Load<Sprite>($"Sprites/{path}");
            if (s == null) Debug.LogWarning($"[SpriteBank] 스프라이트 없음: Sprites/{path}");
            return s;
        }

        // ── 동물 ─────────────────────────────────────────────────────────────────
        public static Sprite Deer()         => Load("Animals/deer");
        public static Sprite Wolf()         => Load("Animals/wolf");
        public static Sprite Rabbit()       => Load("Animals/rabbit");
        public static Sprite Boar()         => Load("Animals/boar");
        public static Sprite Mammoth()      => Load("Animals/mammoth");
        public static Sprite Bear()         => Load("Animals/bear");

        // ── 적 ──────────────────────────────────────────────────────────────────
        public static Sprite Zombie()       => Load("Enemies/zombie");
        public static Sprite BossFrostZombie()  => Load("Enemies/boss_frost_zombie");
        public static Sprite BossWinterKnight() => Load("Enemies/boss_winter_knight");
        public static Sprite BossIronGiant()    => Load("Enemies/boss_iron_giant");
        public static Sprite BossFrostLich()    => Load("Enemies/boss_frost_lich");

        // ── 동료 ─────────────────────────────────────────────────────────────────
        public static Sprite CompanionUncle()  => Load("Companions/uncle");
        public static Sprite CompanionAunt()   => Load("Companions/aunt");
        public static Sprite CompanionChild()  => Load("Companions/child");

        // ── 플레이어 ──────────────────────────────────────────────────────────────
        public static Sprite Player()       => Load("Player/player");

        // ── 역할명 → 동료 스프라이트 ─────────────────────────────────────────────
        public static Sprite CompanionByRole(string role) => role switch
        {
            "사냥꾼" => CompanionUncle(),
            "전사"   => CompanionUncle(),
            "농부"   => CompanionAunt(),
            "아이"   => CompanionChild(),
            "노인"   => CompanionAunt(),
            _        => CompanionUncle(),
        };

        // ── 프랍 (나무 / 바위 / 장식물) ──────────────────────────────────────────
        public static Sprite PineTree()       => Load("Props/pine_tree");
        public static Sprite BareTree()       => Load("Props/bare_tree");
        public static Sprite SnowRocks()      => Load("Props/snow_rocks");
        public static Sprite SmallRocks()     => Load("Props/small_rocks");
        public static Sprite Stump()          => Load("Props/stump");
        public static Sprite SnowBush()       => Load("Props/snow_bush");
        public static Sprite Logs()           => Load("Props/logs");

        // ── 건물 ─────────────────────────────────────────────────────────────────
        public static Sprite Campfire()       => Load("Props/campfire");
        public static Sprite Cabin()          => Load("Props/cabin");
        public static Sprite Watchtower()     => Load("Props/watchtower");
        public static Sprite FenceVertical()  => Load("Props/fence_vertical");
        public static Sprite SnowFenceH()     => Load("Props/snow_fence_h");
        public static Sprite WoodBarricade()  => Load("Props/wood_barricade");
        public static Sprite StoneWall()      => Load("Props/stone_wall");
        public static Sprite SpikeBarricade() => Load("Props/spike_barricade");

        // ── BuildingKind → 스프라이트 ─────────────────────────────────────────────
        public static Sprite BuildingByKind(BuildingKind k) => k switch
        {
            BuildingKind.Campfire    => Campfire(),
            BuildingKind.Fence       => FenceVertical(),
            BuildingKind.Barricade   => WoodBarricade(),
            BuildingKind.Watchtower  => Watchtower(),
            BuildingKind.House       => Cabin(),
            _                        => null,
        };

        // ── AnimalArchetype 이름 → 스프라이트 ────────────────────────────────────
        public static Sprite AnimalByName(string procName) => procName switch
        {
            "Deer_proc"      => Deer(),
            "Wolf_proc"      => Wolf(),
            "Rabbit_proc"    => Rabbit(),
            "SnowHare_proc"  => Rabbit(),
            "Boar_proc"      => Boar(),
            "Mammoth_proc"   => Mammoth(),
            "Bear_proc"      => Bear(),
            "Fox_proc"       => Bear(),   // 여우 스프라이트 없음 — 곰으로 폴백
            _                => null,
        };
    }
}
