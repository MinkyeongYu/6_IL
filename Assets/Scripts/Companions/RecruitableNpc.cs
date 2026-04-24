using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 영입 가능한 NPC. 플레이어가 RecruitRange 안으로 들어와 F 키를 누르면
    /// 자신에게 Companion 컴포넌트를 추가하고 자신은 제거되어 동료가 됨.
    /// 가까이 있을 때 살짝 펄스 애니메이션으로 영입 가능 상태 표시.
    /// </summary>
    public sealed class RecruitableNpc : MonoBehaviour
    {
        public Transform Player;
        public float RecruitRange = 1.6f;
        public KeyCode RecruitKey = KeyCode.F;
        public string DisplayName = "Stranger";

        [Header("Recruited Companion config")]
        public float FollowStopDistance = 1.4f;
        public float MoveSpeed = 4.5f;

        public bool IsPlayerInRange { get; private set; }
        public string DisplayNamePublic => DisplayName;

        private Vector3 _baseScale;

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

        private void Recruit()
        {
            var comp = gameObject.AddComponent<Companion>();
            comp.Player = Player;
            comp.FollowDistance = 1.8f;
            comp.FollowStopDistance = FollowStopDistance;
            comp.MoveSpeed = MoveSpeed;
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
