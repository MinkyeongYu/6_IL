using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// 밤 페이즈가 시작되면 플레이어 주변 4방위에서 좀비 웨이브를 분산 스폰.
    /// 낮/저녁/새벽에는 비활성. 남은 좀비 카운트 공개 (HUD 표시용).
    /// 카메라 배경색도 페이즈에 맞춰 Lerp (URP Light 없이 낮/밤 체감 구현).
    /// </summary>
    public sealed class NightController : MonoBehaviour
    {
        public Transform Player;
        public Camera MainCamera;
        public int BaseWaveCount = 6;
        public int PerDayIncrement = 3;
        public int MaxActive = 40;
        public float SpawnDistance = 6f;
        public float SpawnJitter = 2f;
        public float BetweenSpawnsSec = 0.6f;

        public Color DayColor = new Color(0.88f, 0.93f, 0.98f);
        public Color NightColor = new Color(0.1f, 0.15f, 0.28f);
        public Color BlizzardColor = new Color(0.55f, 0.7f, 0.85f);
        public Color EveningColor = new Color(0.55f, 0.4f, 0.35f);
        public Color DawnColor = new Color(0.75f, 0.7f, 0.65f);

        [Header("Blizzard")]
        [Range(0, 1)] public float BlizzardChance = 0.2f;
        public float BlizzardDpsOutsideFire = 2f;

        public bool IsBlizzard { get; private set; }
        public int ActiveZombies => _activeZombies;
        public int WavePending => _pendingSpawns;
        public Phase CurrentPhase { get; private set; } = Phase.Day;

        private readonly List<Zombie> _tracked = new();
        private int _activeZombies;
        private int _pendingSpawns;
        private float _spawnTimer;
        private SeededRng _rng = new SeededRng(20260425u);

        private System.Action _unsubEvening;
        private System.Action _unsubNight;
        private System.Action _unsubDawn;
        private System.Action _unsubDay;

        private void Start()
        {
            if (Player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) Player = p.transform;
            }
            _unsubEvening = EventBus.Instance.Subscribe<EveningStartedPayload>(_ => { CurrentPhase = Phase.Evening; });
            _unsubNight = EventBus.Instance.Subscribe<NightStartedPayload>(p => StartNight(p.Day));
            _unsubDawn = EventBus.Instance.Subscribe<DawnStartedPayload>(_ => { CurrentPhase = Phase.Dawn; EndNight(); });
            _unsubDay = EventBus.Instance.Subscribe<DayStartedPayload>(_ => { CurrentPhase = Phase.Day; });
        }

        private void OnDestroy()
        {
            _unsubEvening?.Invoke();
            _unsubNight?.Invoke();
            _unsubDawn?.Invoke();
            _unsubDay?.Invoke();
        }

        public void StartNight(int day)
        {
            CurrentPhase = Phase.Night;
            IsBlizzard = _rng.Next() < BlizzardChance;
            int basePending = BaseWaveCount + (day - 1) * PerDayIncrement;
            _pendingSpawns = IsBlizzard ? basePending * 2 : basePending;
            _spawnTimer = 0f;
            if (day > 0 && day % 5 == 0) SpawnBoss(day);
        }

        private void SpawnBoss(int day)
        {
            if (Player == null) return;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = SpawnDistance + 1f;
            float x = Player.position.x + Mathf.Cos(angle) * dist;
            float y = Player.position.y + Mathf.Sin(angle) * dist;

            var go = new GameObject($"Boss_d{day}");
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = Vector3.one * 1.7f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;

            var zombie = go.AddComponent<Zombie>();
            zombie.InitHp(60 + day * 8);
            _tracked.Add(zombie);

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.4f, 0.1f, 0.5f);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 64;
            cf.OutlineWidth = 3;
            cf.OutlineColor = new Color(0.1f, 0.0f, 0.15f, 1f);

            var hp = go.AddComponent<HpBarUi>();
            hp.Zombie = zombie;
            hp.Offset = new Vector2(0f, 0.7f);
            hp.Size = new Vector2(1.1f, 0.14f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            hp.FillColor = new Color(0.95f, 0.2f, 0.55f);
        }

        public void EndNight()
        {
            _pendingSpawns = 0;
            IsBlizzard = false;
            foreach (var z in _tracked)
            {
                if (z != null) Destroy(z.gameObject);
            }
            _tracked.Clear();
            _activeZombies = 0;
        }

        private float _blizzardDmgAccum;

        private void Update()
        {
            _tracked.RemoveAll(z => z == null);
            _activeZombies = _tracked.Count;

            UpdateCameraBg();
            ApplyBlizzardDamage();

            if (CurrentPhase != Phase.Night) return;
            if (_pendingSpawns <= 0) return;
            if (_activeZombies >= MaxActive) return;
            if (Player == null) return;

            float interval = IsBlizzard ? BetweenSpawnsSec * 0.6f : BetweenSpawnsSec;
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < interval) return;
            _spawnTimer = 0f;
            SpawnOne();
            _pendingSpawns--;
        }

        private void ApplyBlizzardDamage()
        {
            if (!IsBlizzard || CurrentPhase != Phase.Night || Player == null) return;
            _blizzardDmgAccum += Time.deltaTime * BlizzardDpsOutsideFire;
            if (_blizzardDmgAccum < 1f) return;
            int dmg = Mathf.FloorToInt(_blizzardDmgAccum);
            _blizzardDmgAccum -= dmg;

            // 모닥불 반경 안이면 보호
            var auras = Object.FindObjectsByType<CampfireAura>(FindObjectsSortMode.None);
            foreach (var a in auras)
            {
                if (a == null) continue;
                if (Vector2.Distance(Player.position, a.transform.position) < a.Radius) return;
            }
            var pc = Player.GetComponent<PlayerController>();
            if (pc != null) pc.TakeDamage(dmg);
        }

        /// <summary>디버그: 페이즈/카운트 무관하게 1마리 즉시 스폰.</summary>
        public void SpawnDebugZombie()
        {
            if (Player == null) return;
            SpawnOne();
        }

        private void SpawnOne()
        {
            float angle = _rng.Next() * Mathf.PI * 2f;
            float dist = SpawnDistance + (_rng.Next() * 2f - 1f) * SpawnJitter;
            float x = Player.position.x + Mathf.Cos(angle) * dist;
            float y = Player.position.y + Mathf.Sin(angle) * dist;

            var go = new GameObject("Zombie_wave");
            go.transform.position = new Vector3(x, y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            var zombie = go.AddComponent<Zombie>();
            _tracked.Add(zombie);

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.6f, 0.2f, 0.22f);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 64;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.15f, 0.05f, 0.05f, 1f);

            var hp = go.AddComponent<HpBarUi>();
            hp.Zombie = zombie;
            hp.Offset = new Vector2(0f, 0.6f);
            hp.Size = new Vector2(0.7f, 0.1f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            hp.FillColor = new Color(0.85f, 0.25f, 0.25f);
        }

        private void UpdateCameraBg()
        {
            if (MainCamera == null) return;
            Color target = CurrentPhase switch
            {
                Phase.Day => DayColor,
                Phase.Evening => EveningColor,
                Phase.Night => IsBlizzard ? BlizzardColor : NightColor,
                Phase.Dawn => DawnColor,
                _ => DayColor,
            };
            MainCamera.backgroundColor = Color.Lerp(MainCamera.backgroundColor, target, Time.deltaTime * 1.5f);
        }
    }
}
