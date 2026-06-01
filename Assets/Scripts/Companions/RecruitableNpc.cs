using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 영입 가능한 NPC. SimpleHud 가 근처 NPC 를 감지해 하단 대화 패널을 띄우고,
    /// 플레이어가 "영입" 버튼 (또는 F 키) 을 누르면 Recruit() 으로 Companion 전환.
    /// </summary>
    public sealed class RecruitableNpc : MonoBehaviour
    {
        public Transform Player;
        public float RecruitRange = 1.8f;
        public KeyCode RecruitKey = KeyCode.F;

        [Header("Profile")]
        public string DisplayName = "Stranger";
        public string Role = "Hunter";
        [TextArea(1, 3)] public string DialogText = "함께 가고 싶습니다.";
        [Range(0, 5)] public int CombatRating = 3;
        [Range(0, 5)] public int FarmRating = 3;

        [Header("Recruited Companion config")]
        public bool IsCombat = true;
        public float FollowStopDistance = 0.25f;
        public float MoveSpeed = 4.5f;
        public float AttackRange = 5f;
        public int AttackDamage = 6;
        public float AttackCooldown = 1.6f;

        public bool IsPlayerInRange { get; private set; }
        public string DisplayNamePublic => DisplayName;

        private Vector3 _baseScale;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            // 영입 전에는 Kinematic — 좀비/플레이어 충돌에 밀려나지 않고 제자리에 고정.
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
                col.radius = 0.35f;
            }
        }

        private void Start()
        {
            _baseScale = transform.localScale;
            if (Player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) Player = p.transform;
            }
        }

        private void Update()
        {
            if (Player == null) { IsPlayerInRange = false; return; }
            float d = Vector2.Distance(transform.position, Player.position);
            IsPlayerInRange = d <= RecruitRange;

            if (IsPlayerInRange)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                transform.localScale = _baseScale * pulse;
                if (Input.GetKeyDown(RecruitKey)) Recruit();
            }
            else
            {
                transform.localScale = _baseScale;
            }
        }

        /// <summary>초반 그레이스 — 집이 0채여도 항상 허용되는 최저 수용 인원.</summary>
        public const int FreeCapacity = 12;
        /// <summary>집(House) 1채당 추가 인구.</summary>
        public const int CapacityPerHouse = 4;

        /// <summary>현재 마을이 수용 가능한 동료 수 — FreeCapacity + 집 수 × CapacityPerHouse.</summary>
        public static int VillageCapacity()
        {
            int houses = 0;
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0) continue;
                if (b.Kind == BuildingKind.House) houses++;
            }
            return FreeCapacity + houses * CapacityPerHouse;
        }

        public static int CurrentCompanionCount()
        {
            int n = 0;
            var cs = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in cs) if (c != null && !c.IsDead) n++;
            return n;
        }

        // 일일 영입 한도는 제거 — MaxAliveNpcs (월드 동시 NPC 캡) 으로 자연스럽게 빈도 제한.
        public bool CanRecruit() => CurrentCompanionCount() < VillageCapacity();

        // 호환용 stub — 외부 호출자가 있어서 유지 (no-op).
        public static void ResetDailyRecruits(int day) { }

        public void Recruit()
        {
            // 마을 수용 한도만 — 일일 한도 없음 (스폰 빈도로 자연 제한)
            if (!CanRecruit()) return;

            // 영입되면 Dynamic 으로 전환 — Companion 이 velocity 로 이동해야 하고 다른 유닛과 물리적 상호작용도 가능.
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

            var comp = gameObject.AddComponent<Companion>();
            comp.Player = Player;
            comp.IsCombat = IsCombat;
            comp.FollowStopDistance = FollowStopDistance;
            comp.MoveSpeed = MoveSpeed;
            comp.AttackRange = AttackRange;
            comp.Damage = AttackDamage;
            comp.AttackCooldown = AttackCooldown;
            comp.GatherReach = 0.7f;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.Lerp(sr.color, new Color(0.55f, 0.85f, 0.5f), 0.45f);
            }
            transform.localScale = _baseScale;
            gameObject.name = $"{DisplayName}(Recruited)";
            Destroy(this);
        }
    }
}
