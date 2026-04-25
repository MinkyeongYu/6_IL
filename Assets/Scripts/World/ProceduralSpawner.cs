using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 플레이어 주변 청크(그리드 셀)를 스캔해 확률적으로 나무/사슴을 런타임 생성.
    /// 외부 prefab 없이 SpriteRenderer + ColorFallback + Gatherable (+ DeerAi) 를 코드로 조립.
    /// 멀어진 청크는 해당 콘텐츠 제거.
    /// 초기 스타터 존(3x3 청크) 은 스폰 스킵 — 씬에 배치된 오브젝트와 겹치지 않게.
    /// </summary>
    public sealed class ProceduralSpawner : MonoBehaviour
    {
        [Header("Grid")]
        public Transform Player;
        public int ChunkSize = 10;
        public int LoadRadius = 2;
        public int UnloadRadius = 4;

        [Header("Spawn probability (per slot)")]
        [Range(0, 1)] public float TreeChance = 0.5f;
        [Range(0, 1)] public float RockChance = 0.15f;
        [Range(0, 1)] public float DeerChance = 0.08f;
        [Range(0, 1)] public float NpcChance = 0.04f;
        public int SlotsPerChunk = 6;

        [Header("Starter zone to skip (centered at chunk (Cx, Cy))")]
        public int StarterCx = 1;
        public int StarterCy = 1;
        public int StarterRadius = 1;

        [Header("Deterministic")]
        public uint Seed = 20260425u;

        private class ChunkData { public List<GameObject> Spawned = new(); }
        private readonly Dictionary<(int, int), ChunkData> _loaded = new();

        private void Start()
        {
            if (Player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) Player = p.transform;
            }
        }

        private void Update()
        {
            if (Player == null) return;
            int pcx = Mathf.FloorToInt(Player.position.x / ChunkSize);
            int pcy = Mathf.FloorToInt(Player.position.y / ChunkSize);

            EnsureLoaded(pcx, pcy);
            UnloadFar(pcx, pcy);
        }

        private void EnsureLoaded(int pcx, int pcy)
        {
            for (int dy = -LoadRadius; dy <= LoadRadius; dy++)
            {
                for (int dx = -LoadRadius; dx <= LoadRadius; dx++)
                {
                    var key = (pcx + dx, pcy + dy);
                    if (!_loaded.ContainsKey(key)) LoadChunk(key);
                }
            }
        }

        private void UnloadFar(int pcx, int pcy)
        {
            var toRemove = new List<(int, int)>();
            foreach (var kv in _loaded)
            {
                int ddx = Mathf.Abs(kv.Key.Item1 - pcx);
                int ddy = Mathf.Abs(kv.Key.Item2 - pcy);
                if (Mathf.Max(ddx, ddy) > UnloadRadius)
                {
                    foreach (var go in kv.Value.Spawned) if (go != null) Destroy(go);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove) _loaded.Remove(k);
        }

        private void LoadChunk((int, int) key)
        {
            var data = new ChunkData();
            _loaded[key] = data;

            if (IsInStarterZone(key.Item1, key.Item2)) return;

            uint seed = Seed ^ unchecked((uint)((key.Item1 * 73856093) ^ (key.Item2 * 19349663)));
            var rng = new SeededRng(seed);

            float baseX = key.Item1 * ChunkSize;
            float baseY = key.Item2 * ChunkSize;

            for (int i = 0; i < SlotsPerChunk; i++)
            {
                float x = baseX + rng.Next() * ChunkSize;
                float y = baseY + rng.Next() * ChunkSize;
                float roll = rng.Next();
                float cumTree = TreeChance;
                float cumRock = cumTree + RockChance;
                float cumDeer = cumRock + DeerChance;
                float cumNpc = cumDeer + NpcChance;
                if (roll < cumTree) data.Spawned.Add(CreateTree(x, y));
                else if (roll < cumRock) data.Spawned.Add(CreateRock(x, y));
                else if (roll < cumDeer) SpawnAnimalAt(x, y, rng, data.Spawned);
                else if (roll < cumNpc) data.Spawned.Add(CreateNpc(x, y, rng));
            }
        }

        private static GameObject CreateRock(float x, float y)
        {
            var go = new GameObject("Rock_proc");
            go.transform.position = new Vector3(x, y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            var gat = go.AddComponent<Gatherable>();
            gat.YieldKind = ResourceKind.Stone;
            gat.YieldAmount = 3;
            gat.DurationSec = 12f;
            gat.DestroyOnGather = true;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 0.55f, 0.6f);
            cf.Shape = FallbackShape.Rounded;
            cf.Circle = false;
            cf.PixelSize = 64;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            return go;
        }

        private bool IsInStarterZone(int cx, int cy)
        {
            return Mathf.Abs(cx - StarterCx) <= StarterRadius
                && Mathf.Abs(cy - StarterCy) <= StarterRadius;
        }

        private static GameObject CreateTree(float x, float y)
        {
            var go = new GameObject("Tree_proc");
            go.transform.position = new Vector3(x, y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            var gat = go.AddComponent<Gatherable>();
            gat.YieldKind = ResourceKind.Wood;
            gat.YieldAmount = 4;
            gat.DurationSec = 9f;
            gat.DestroyOnGather = true;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.2f, 0.55f, 0.25f);
            cf.Shape = FallbackShape.Triangle;
            cf.Circle = true; // (Shape overrides to Triangle)
            cf.PixelSize = 64;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.05f, 0.15f, 0.05f, 1f);
            return go;
        }

        private struct AnimalArchetype
        {
            public string Name;
            public int MeatMin, MeatMax;  // 고기 yield 범위 (rng 으로 결정)
            public float MeatDropChance;  // 0~1: 고기 드랍 확률 (0 이면 드랍 X)
            public float DurationSec;
            public int Hp;
            public float FleeRadius;
            public float FleeSpeed;
            public float Scale;
            public float ColliderRadius;
            public Color Tint;
            public FallbackShape Shape;
            public Color Outline;
            public float Weight;
            public bool IsPredator;       // true → WolfAi, false → DeerAi
            public int PredatorDamage;    // IsPredator 인 경우만
            public int PackMin, PackMax;  // 무리 사이즈 (1=단독)
            public bool DropsFrostbloom;
        }

        // 가중치 기반 동물 풀.
        private static readonly AnimalArchetype[] _animals =
        {
            // 토끼 (가장 흔함)
            new AnimalArchetype {
                Name = "Rabbit_proc", MeatMin = 1, MeatMax = 1, MeatDropChance = 1f,
                DurationSec = 1.5f, Hp = 4,
                FleeRadius = 5.5f, FleeSpeed = 5.5f,
                Scale = 0.5f, ColliderRadius = 0.25f,
                Tint = new Color(0.92f, 0.88f, 0.82f),
                Shape = FallbackShape.Circle,
                Outline = new Color(0.4f, 0.3f, 0.2f, 1f),
                Weight = 0.34f, PackMin = 1, PackMax = 1,
            },
            // 여우
            new AnimalArchetype {
                Name = "Fox_proc", MeatMin = 2, MeatMax = 2, MeatDropChance = 1f,
                DurationSec = 2.5f, Hp = 8,
                FleeRadius = 4.5f, FleeSpeed = 4.5f,
                Scale = 0.7f, ColliderRadius = 0.3f,
                Tint = new Color(0.85f, 0.45f, 0.18f),
                Shape = FallbackShape.Triangle,
                Outline = new Color(0.4f, 0.18f, 0.05f, 1f),
                Weight = 0.20f, PackMin = 1, PackMax = 1,
            },
            // 멧돼지
            new AnimalArchetype {
                Name = "Boar_proc", MeatMin = 3, MeatMax = 5, MeatDropChance = 1f,
                DurationSec = 4.5f, Hp = 28,
                FleeRadius = 2.5f, FleeSpeed = 2.2f,
                Scale = 1.25f, ColliderRadius = 0.5f,
                Tint = new Color(0.35f, 0.25f, 0.18f),
                Shape = FallbackShape.Rounded,
                Outline = new Color(0.1f, 0.05f, 0.02f, 1f),
                Weight = 0.13f, PackMin = 1, PackMax = 1,
            },
            // 늑대 (포식자, 무리, 25%만 고기)
            new AnimalArchetype {
                Name = "Wolf_proc", MeatMin = 1, MeatMax = 1, MeatDropChance = 0.25f,
                DurationSec = 1.5f, Hp = 16,
                FleeRadius = 0f, FleeSpeed = 0f, // 안 도망감
                Scale = 0.85f, ColliderRadius = 0.35f,
                Tint = new Color(0.45f, 0.45f, 0.5f),
                Shape = FallbackShape.Triangle,
                Outline = new Color(0.15f, 0.15f, 0.18f, 1f),
                Weight = 0.13f, IsPredator = true, PredatorDamage = 6,
                PackMin = 2, PackMax = 4,
            },
            // 사슴 (낮춘 빈도)
            new AnimalArchetype {
                Name = "Deer_proc", MeatMin = 2, MeatMax = 2, MeatDropChance = 1f,
                DurationSec = 3f, Hp = 12,
                FleeRadius = 3.5f, FleeSpeed = 3f,
                Scale = 1f, ColliderRadius = 0.4f,
                Tint = new Color(0.55f, 0.4f, 0.25f),
                Shape = FallbackShape.Circle,
                Outline = new Color(0.2f, 0.12f, 0.05f, 1f),
                Weight = 0.10f, PackMin = 1, PackMax = 1,
            },
            // 흰토끼 (희귀, Frostbloom 보너스)
            new AnimalArchetype {
                Name = "SnowHare_proc", MeatMin = 1, MeatMax = 1, MeatDropChance = 1f,
                DurationSec = 1.8f, Hp = 5,
                FleeRadius = 6f, FleeSpeed = 6.5f,
                Scale = 0.45f, ColliderRadius = 0.25f,
                Tint = new Color(0.95f, 0.97f, 1f),
                Shape = FallbackShape.Circle,
                Outline = new Color(0.45f, 0.6f, 0.85f, 1f),
                Weight = 0.06f, PackMin = 1, PackMax = 1, DropsFrostbloom = true,
            },
            // 맘모스 (희귀, 매우 단단함 200 HP, 고기 30~50)
            new AnimalArchetype {
                Name = "Mammoth_proc", MeatMin = 30, MeatMax = 50, MeatDropChance = 1f,
                DurationSec = 12f, Hp = 200,
                FleeRadius = 1.5f, FleeSpeed = 1.0f,
                Scale = 2.2f, ColliderRadius = 0.85f,
                Tint = new Color(0.45f, 0.32f, 0.22f),
                Shape = FallbackShape.Rounded,
                Outline = new Color(0.15f, 0.08f, 0.02f, 1f),
                Weight = 0.04f, PackMin = 1, PackMax = 1,
            },
        };

        private static void SpawnAnimalAt(float x, float y, SeededRng rng, System.Collections.Generic.List<GameObject> sink)
        {
            // 가중치 추첨
            float total = 0f;
            for (int i = 0; i < _animals.Length; i++) total += _animals[i].Weight;
            float roll = rng.Next() * total;
            AnimalArchetype a = _animals[0];
            float acc = 0f;
            for (int i = 0; i < _animals.Length; i++)
            {
                acc += _animals[i].Weight;
                if (roll <= acc) { a = _animals[i]; break; }
            }

            int packSize = rng.IntRange(a.PackMin, Mathf.Max(a.PackMin, a.PackMax));
            for (int p = 0; p < packSize; p++)
            {
                float ox = p == 0 ? 0f : (rng.Next() - 0.5f) * 2f;
                float oy = p == 0 ? 0f : (rng.Next() - 0.5f) * 2f;
                // 개체별로 yield + drop 결정 (각 개체 독립)
                int yield = a.MeatMin == a.MeatMax ? a.MeatMin : rng.IntRange(a.MeatMin, a.MeatMax);
                if (a.MeatDropChance < 1f && rng.Next() > a.MeatDropChance) yield = 0;
                sink.Add(CreateOneAnimal(a, x + ox, y + oy, yield));
            }
        }

        private static GameObject CreateOneAnimal(AnimalArchetype a, float x, float y, int yieldOverride)
        {
            var go = new GameObject(a.Name);
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = Vector3.one * a.Scale;
            go.tag = "Untagged";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;
            sr.color = a.Tint;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = a.ColliderRadius;

            var gat = go.AddComponent<Gatherable>();
            gat.YieldKind = ResourceKind.Meat;
            gat.YieldAmount = yieldOverride;
            gat.DurationSec = a.DurationSec;
            gat.DestroyOnGather = true;

            if (a.IsPredator)
            {
                var ai = go.AddComponent<WolfAi>();
                ai.MaxHp = a.Hp;
                ai.InitHp(a.Hp);
                ai.Damage = a.PredatorDamage;
            }
            else
            {
                var ai = go.AddComponent<DeerAi>();
                ai.FleeRadius = a.FleeRadius;
                ai.FleeSpeed = a.FleeSpeed;
                ai.InitHp(a.Hp);
            }

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = a.Tint;
            cf.Shape = a.Shape;
            cf.Circle = a.Shape == FallbackShape.Circle;
            cf.PixelSize = 64;
            cf.OutlineWidth = 2;
            cf.OutlineColor = a.Outline;

            if (a.DropsFrostbloom)
            {
                var bonus = go.AddComponent<BonusYieldOnGather>();
                bonus.Kind = ResourceKind.Frostbloom;
                bonus.Amount = 1;
            }

            // 큰 동물 (맘모스 등) 은 HP 바도 띄움
            if (a.Hp >= 8)
            {
                var hpBar = go.AddComponent<HpBarUi>();
                if (a.IsPredator) hpBar.WolfRef = go.GetComponent<WolfAi>();
                else hpBar.DeerRef = go.GetComponent<DeerAi>();
                hpBar.Offset = new Vector2(0f, 0.55f);
                hpBar.Size = new Vector2(0.9f, 0.1f);
                hpBar.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
                hpBar.FillColor = new Color(0.85f, 0.4f, 0.4f);
            }
            return go;
        }

        private static readonly string[] _npcNames =
        {
            "려화", "도윤", "서원", "은규", "혜인", "지운", "한결", "무영", "선유", "여린"
        };

        private struct NpcArchetype
        {
            public string Role;
            public string Dialog;
            public bool IsCombat;
            public int CombatRating;
            public int FarmRating;
            public float MoveSpeed;
            public float AttackRange;
            public int AttackDamage;
            public float AttackCooldown;
            public Color Tint;
            public FallbackShape Shape;
        }

        private static readonly NpcArchetype[] _npcArchetypes =
        {
            new NpcArchetype
            {
                Role = "사냥꾼",
                Dialog = "활을 들고 합류하지요. 멀리서 돕겠습니다.",
                IsCombat = true,
                CombatRating = 4, FarmRating = 2,
                MoveSpeed = 4.6f, AttackRange = 6.5f, AttackDamage = 5, AttackCooldown = 1.4f,
                Tint = new Color(0.55f, 0.7f, 0.45f), Shape = FallbackShape.Triangle,
            },
            new NpcArchetype
            {
                Role = "전사",
                Dialog = "검 한 자루로 살아남아 왔습니다. 데려가 주십시오.",
                IsCombat = true,
                CombatRating = 5, FarmRating = 1,
                MoveSpeed = 4.2f, AttackRange = 1.6f, AttackDamage = 9, AttackCooldown = 1.1f,
                Tint = new Color(0.7f, 0.45f, 0.4f), Shape = FallbackShape.Rounded,
            },
            new NpcArchetype
            {
                Role = "치유사",
                Dialog = "약초를 다룹니다. 부상자를 돌볼 수 있습니다.",
                IsCombat = true,
                CombatRating = 2, FarmRating = 4,
                MoveSpeed = 4.4f, AttackRange = 4.5f, AttackDamage = 3, AttackCooldown = 1.8f,
                Tint = new Color(0.85f, 0.85f, 0.95f), Shape = FallbackShape.Circle,
            },
            new NpcArchetype
            {
                Role = "농부",
                Dialog = "씨를 뿌리고 거두는 일이라면 자신 있어요.",
                IsCombat = false,
                CombatRating = 1, FarmRating = 5,
                MoveSpeed = 4.0f, AttackRange = 1.5f, AttackDamage = 2, AttackCooldown = 2.0f,
                Tint = new Color(0.9f, 0.75f, 0.4f), Shape = FallbackShape.Rounded,
            },
            new NpcArchetype
            {
                Role = "방랑객",
                Dialog = "갈 곳이 없습니다. 같이 있어도 될까요.",
                IsCombat = true,
                CombatRating = 3, FarmRating = 3,
                MoveSpeed = 4.5f, AttackRange = 2.0f, AttackDamage = 5, AttackCooldown = 1.5f,
                Tint = new Color(0.6f, 0.55f, 0.7f), Shape = FallbackShape.Circle,
            },
        };

        private static GameObject CreateNpc(float x, float y, SeededRng rng)
        {
            var arch = _npcArchetypes[rng.IntRange(0, _npcArchetypes.Length - 1)];
            string nm = _npcNames[rng.IntRange(0, _npcNames.Length - 1)];

            var go = new GameObject($"Npc_{arch.Role}_{nm}");
            go.transform.position = new Vector3(x, y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;

            var npc = go.AddComponent<RecruitableNpc>();
            npc.DisplayName = nm;
            npc.Role = arch.Role;
            npc.DialogText = arch.Dialog;
            npc.CombatRating = arch.CombatRating;
            npc.FarmRating = arch.FarmRating;
            npc.IsCombat = arch.IsCombat;
            npc.MoveSpeed = arch.MoveSpeed;
            npc.AttackRange = arch.AttackRange;
            npc.AttackDamage = arch.AttackDamage;
            npc.AttackCooldown = arch.AttackCooldown;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = arch.Tint;
            cf.Shape = arch.Shape;
            cf.Circle = arch.Shape == FallbackShape.Circle;
            cf.PixelSize = 64;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            return go;
        }
    }
}
