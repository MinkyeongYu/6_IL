using UnityEngine;
using UnityEngine.SceneManagement;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// Snowfield 신의 루트 컨트롤러. 플레이어 + 채집 + 청크 매니저 + 시야 조립.
    /// 저녁 시작 시 Village 씬으로 이동.
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
        public string VillageSceneName = "VillageScene";

        private System.Action _unsubEvening;
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

            if (Player != null) Player.transform.position = new Vector3(GameConstants.VillageCenterX, GameConstants.VillageCenterY, 0);
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
            }

            _unsubEvening = EventBus.Instance.Subscribe<EveningStartedPayload>(_ =>
            {
                SceneManager.LoadScene(VillageSceneName);
            });
            _unsubPlayerDied = EventBus.Instance.Subscribe<PlayerDiedPayload>(_ =>
            {
                if (_session != null) _session.HardReset();
            });
        }

        private void Update()
        {
            if (_session == null) return;
            _session.Cycle.Update(Time.deltaTime);

            if (Player != null && Player.IsDead)
            {
                EventBus.Instance.Emit(new PlayerDiedPayload(_session.Cycle.Day));
            }
        }

        private void OnDestroy()
        {
            _unsubEvening?.Invoke();
            _unsubPlayerDied?.Invoke();
        }
    }

    /// <summary>카메라 부드러운 추종.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Smooth = 0.15f;
        private Vector3 _vel;

        private void LateUpdate()
        {
            if (Target == null) return;
            var goal = new Vector3(Target.position.x, Target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _vel, Smooth);
        }
    }
}
