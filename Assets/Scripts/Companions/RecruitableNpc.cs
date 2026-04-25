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
            rb.bodyType = RigidbodyType2D.Dynamic;

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

        public void Recruit()
        {
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
