using UnityEngine;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// Snowfield 신의 루트 컨트롤러. 플레이어 + 채집 + 청크 매니저 + 시야 조립.
    /// 모든 페이즈를 한 신에서 — 저녁/밤은 NightController 가 처리.
    /// </summary>
    public sealed class SnowfieldController : MonoBehaviour
    {
        [Header("Refs")]
        public PlayerController Player;
        public GatherController Gather;
        public InputReader Input;
        public ChunkManager Chunks;
        public VisionMask Vision;
        public Camera MainCamera;

        private System.Action _unsubPlayerDied;
        private GameSession _session;

        private void Start()
        {
            _session = GameSession.Instance;
            if (_session == null)
            {
                Debug.LogError("[Snowfield] GameSession not found. Add it to BootScene.");
                return;
            }

            Vector3 villageCenter = new Vector3(GameConstants.VillageCenterX, GameConstants.VillageCenterY, 0);
            if (Player != null) Player.transform.position = villageCenter;
            if (Gather != null)
            {
                Gather.Player = Player;
                Gather.Input = Input;
                Gather.Store = _session.Resources;
            }
            if (Chunks != null) Chunks.Player = Player != null ? Player.transform : null;
            if (Vision != null) Vision.Target = Player != null ? Player.transform : null;
            if (MainCamera != null && Player != null)
            {
                var f = MainCamera.gameObject.AddComponent<CameraFollow>();
                f.Target = Player.transform;
                if (MainCamera.GetComponent<SnowEmitter>() == null)
                    MainCamera.gameObject.AddComponent<SnowEmitter>();
            }

            // 마을 자리에 모닥불 + 울타리 링 자동 스폰 (이미 있으면 스킵)
            VillageStarter.SpawnStarterVillage(villageCenter);

            // 맨 첫 타일(마을 영역) 정리 — 동료/NPC/동물 모두 제거.
            // 영입은 외곽에서 만나는 RecruitableNpc 로만, 동물은 외곽 청크에서만.
            const float ClearRadius = 14f;
            var preComps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in preComps)
                if (c != null) Destroy(c.gameObject);

            var preNpcs = Object.FindObjectsByType<RecruitableNpc>(FindObjectsSortMode.None);
            foreach (var n in preNpcs)
                if (n != null && Vector2.Distance(n.transform.position, villageCenter) < ClearRadius)
                    Destroy(n.gameObject);

            var preDeer = Object.FindObjectsByType<DeerAi>(FindObjectsSortMode.None);
            foreach (var d in preDeer)
                if (d != null && Vector2.Distance(d.transform.position, villageCenter) < ClearRadius)
                    Destroy(d.gameObject);

            var preWolf = Object.FindObjectsByType<WolfAi>(FindObjectsSortMode.None);
            foreach (var w in preWolf)
                if (w != null && Vector2.Distance(w.transform.position, villageCenter) < ClearRadius)
                    Destroy(w.gameObject);
            // PlayerDied 이벤트는 SimpleHud 의 Death overlay 가 처리.
        }

        private bool _diedEmitted;

        private void Update()
        {
            // GameSession.Instance 가 HardReset 등으로 사라졌다 다시 생기는 경우 대비해 매번 다시 조회
            if (_session == null || GameSession.Instance != _session) _session = GameSession.Instance;
            if (_session == null || _session.Cycle == null) return;
            _session.Cycle.Update(Time.deltaTime);

            if (Player != null && Player.IsDead && !_diedEmitted)
            {
                EventBus.Instance.Emit(new PlayerDiedPayload(_session.Cycle.Day));
                _diedEmitted = true;
            }
        }

        private void OnDestroy()
        {
            _unsubPlayerDied?.Invoke();
        }
    }

    /// <summary>카메라 부드러운 추종 + 흔들림(Trauma).
    /// CameraFollow.Shake(amount, duration) 으로 외부에서 트리거.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Smooth = 0.15f;
        private Vector3 _vel;

        private static CameraFollow _instance;
        private float _trauma;
        private float _traumaDecay = 1.6f;
        private float _maxOffset = 0.6f;

        public static void Shake(float amount, float duration = 0.3f)
        {
            if (_instance == null) return;
            _instance._trauma = Mathf.Min(1f, _instance._trauma + amount);
            // duration 은 decay 속도에 반영 (긴 흔들림 = 느린 감쇠)
            _instance._traumaDecay = 1f / Mathf.Max(0.05f, duration);
        }

        private void OnEnable() { _instance = this; }
        private void OnDisable() { if (_instance == this) _instance = null; }

        private void LateUpdate()
        {
            if (Target == null) return;
            var goal = new Vector3(Target.position.x, Target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _vel, Smooth);

            if (_trauma > 0f)
            {
                float t2 = _trauma * _trauma;
                float ox = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * _maxOffset * t2;
                float oy = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * _maxOffset * t2;
                transform.position += new Vector3(ox, oy, 0f);
                _trauma = Mathf.Max(0f, _trauma - _traumaDecay * Time.deltaTime);
            }
        }
    }
}
