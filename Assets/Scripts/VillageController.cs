using UnityEngine;
using UnityEngine.SceneManagement;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// Village 신 루트. 그리드, 배치 컨트롤러, 좀비 웨이브 스폰, 새벽 시 Snowfield로.
    /// </summary>
    public sealed class VillageController : MonoBehaviour
    {
        [Header("Refs")]
        public PlayerController Player;
        public PlacementController Placement;
        public GameObject ZombiePrefab;
        public Camera MainCamera;
        public string SnowfieldSceneName = "SnowfieldScene";

        private GameSession _session;
        private VillageGrid _grid;
        private System.Action _unsubNight, _unsubDawn, _unsubBuild;
        private bool _waveSpawned;

        private void Start()
        {
            _session = GameSession.Instance;
            if (_session == null) { Debug.LogError("[Village] GameSession not found"); return; }

            _grid = new VillageGrid(GameConstants.VillageGridSize, GameConstants.VillageGridSize, GameConstants.TileSize);

            if (Placement != null)
            {
                Placement.Grid = _grid;
                Placement.Store = _session.Resources;
                Placement.MainCamera = MainCamera != null ? MainCamera : Camera.main;
            }

            if (Player != null)
                Player.transform.position = new Vector3(GameConstants.VillageCenterX, GameConstants.VillageCenterY, 0);

            var cam = MainCamera != null ? MainCamera : Camera.main;
            if (cam != null && Player != null && cam.GetComponent<CameraFollow>() == null)
            {
                var f = cam.gameObject.AddComponent<CameraFollow>();
                f.Target = Player.transform;
            }
            if (cam != null && cam.GetComponent<SnowEmitter>() == null)
                cam.gameObject.AddComponent<SnowEmitter>();

            _unsubNight = EventBus.Instance.Subscribe<NightStartedPayload>(p => SpawnWave(p.Day));
            _unsubDawn = EventBus.Instance.Subscribe<DawnStartedPayload>(_ =>
            {
                _session.SaveNow();
                SceneManager.LoadScene(SnowfieldSceneName);
            });
            _unsubBuild = EventBus.Instance.Subscribe<BuildRequestPayload>(p =>
            {
                if (Placement == null) return;
                if (p.Kind == "campfire") Placement.Begin(BuildingKind.Campfire);
                else if (p.Kind == "barricade") Placement.Begin(BuildingKind.Barricade);
            });
        }

        private void Update()
        {
            if (_session == null) return;
            _session.Cycle.Update(Time.deltaTime);
        }

        private void SpawnWave(int day)
        {
            if (_waveSpawned || ZombiePrefab == null) return;
            _waveSpawned = true;
            var rng = new SeededRng((uint)(day * 1000));
            var spawner = new WaveSpawner(rng, BalanceConfig.Instance);
            var plan = spawner.PlanWave(day);
            for (int i = 0; i < plan.EnemyCount; i++)
            {
                var pt = plan.SpawnPoints[i % plan.SpawnPoints.Count];
                Instantiate(ZombiePrefab, new Vector3(pt.x + Random.Range(-20f, 20f), pt.y + Random.Range(-20f, 20f), 0), Quaternion.identity);
            }
        }

        private void OnDestroy()
        {
            _unsubNight?.Invoke();
            _unsubDawn?.Invoke();
            _unsubBuild?.Invoke();
        }
    }
}
