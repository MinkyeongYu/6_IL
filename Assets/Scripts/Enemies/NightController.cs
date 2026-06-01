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
        public int BaseWaveCount = 8;
        public int PerDayIncrement = 6;
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
            _unsubDay = EventBus.Instance.Subscribe<DayStartedPayload>(_ => { CurrentPhase = Phase.Day; EndNight(); });
        }

        private void OnDestroy()
        {
            _unsubEvening?.Invoke();
            _unsubNight?.Invoke();
            _unsubDawn?.Invoke();
            _unsubDay?.Invoke();
        }

        public float BossWarningRemaining { get; private set; }
        public bool IsBossNight { get; private set; }

        public int KillsThisNight { get; private set; }
        public int BossKillThreshold = 25;
        private bool _bossSpawnedThisNight;

        public void StartNight(int day)
        {
            CurrentPhase = Phase.Night;
            IsBlizzard = _rng.Next() < BlizzardChance;
            int basePending = BaseWaveCount + (day - 1) * PerDayIncrement;
            _pendingSpawns = IsBlizzard ? basePending * 2 : basePending;
            _spawnTimer = 0f;
            IsBossNight = day > 0 && day % 5 == 0;
            KillsThisNight = 0;
            _bossSpawnedThisNight = false;
            Sfx.NightHowl();
            CameraFollow.Shake(0.35f, 0.6f);
            if (IsBossNight) { Sfx.Boss(); CameraFollow.Shake(0.7f, 1.2f); _bossSpawnedThisNight = true; StartCoroutine(BossWarningThenSpawn(day)); }
        }

        /// <summary>좀비가 죽을 때 GameSession.OnZombieKilled 와 함께 호출됨.</summary>
        public void OnNightKill()
        {
            if (CurrentPhase != Phase.Night) return;
            KillsThisNight++;
            // 일정 수 처치 시 — 보스가 아직 안 나왔다면 보스 강제 소환
            if (!_bossSpawnedThisNight && KillsThisNight >= BossKillThreshold)
            {
                _bossSpawnedThisNight = true;
                int day = GameSession.Instance != null ? GameSession.Instance.Cycle.Day : 1;
                Sfx.Boss(); CameraFollow.Shake(0.7f, 1.2f);
                StartCoroutine(BossWarningThenSpawn(day));
            }
        }

        private System.Collections.IEnumerator BossWarningThenSpawn(int day)
        {
            BossWarningRemaining = 3f;
            while (BossWarningRemaining > 0f)
            {
                BossWarningRemaining -= Time.deltaTime;
                yield return null;
            }
            BossWarningRemaining = 0f;
            SpawnBoss(day);
        }

        private void SpawnBoss(int day)
        {
            if (Player == null) return;
            Vector3 spawnPos = PickOutsideVillageSpawnPos(rngBased: false);

            var go = new GameObject($"Boss_d{day}");
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * 1.7f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;

            var zombie = go.AddComponent<Zombie>();
            zombie.InitHp(100 + day * 100);
            zombie.VariantDamageBonus = 18 + (day - 1) * 2;
            zombie.MoveSpeedMul = 1f + Mathf.Min((day - 1) * 0.02f, 0.5f);
            _tracked.Add(zombie);

            // 보스는 일자에 따라 다른 스프라이트
            var bossSpr = day switch
            {
                1 => SpriteBank.BossFrostZombie(),
                2 => SpriteBank.BossWinterKnight(),
                3 => SpriteBank.BossIronGiant(),
                _ => SpriteBank.BossFrostLich(),
            };
            if (bossSpr != null) { sr.sprite = bossSpr; sr.color = Color.white; }

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

            // 안전망: 추적되지 않은 좀비도 모두 정리 (낮에는 절대 좀비 없음)
            var stragglers = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
            foreach (var z in stragglers) if (z != null) Destroy(z.gameObject);
        }

        private float _blizzardDmgAccum;

        private void Update()
        {
            _tracked.RemoveAll(z => z == null);
            _activeZombies = _tracked.Count;

            UpdateCameraBg();
            ApplyBlizzardDamage();

            // 안전망: Cycle.Phase 가 Night 인데 자체 CurrentPhase 가 못 따라온 경우 (이벤트 누락 등) 강제 동기화
            var session = GameSession.Instance;
            if (session != null && session.Cycle != null)
            {
                if (session.Cycle.Phase == Phase.Night && CurrentPhase != Phase.Night)
                {
                    StartNight(session.Cycle.Day);
                }
                else if (session.Cycle.Phase != Phase.Night && CurrentPhase == Phase.Night)
                {
                    CurrentPhase = session.Cycle.Phase;
                    EndNight();
                }
            }

            if (CurrentPhase != Phase.Night) return;
            if (Player == null) return;

            // 무한 웨이브 — 활성 좀비 0 이고 대기열도 0 이면 새 웨이브 보충 (점점 커짐)
            if (_activeZombies == 0 && _pendingSpawns <= 0)
            {
                int day = GameSession.Instance != null ? GameSession.Instance.Cycle.Day : 1;
                int refill = BaseWaveCount + (day - 1) * PerDayIncrement + KillsThisNight / 5;
                _pendingSpawns = IsBlizzard ? refill * 2 : refill;
            }

            if (_pendingSpawns <= 0) return;
            if (_activeZombies >= MaxActive) return;

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

        private enum Variant { Normal, Fast, Tank, Archer }

        private Variant PickVariant()
        {
            int day = GameSession.Instance != null ? GameSession.Instance.Cycle.Day : 1;
            float r = _rng.Next();
            if (day >= 7 && r < 0.18f) return Variant.Tank;
            if (day >= 4 && r < 0.20f) return Variant.Archer; // 원거리 좀비 — Day 4+
            if (day >= 3 && r < 0.30f) return Variant.Fast;
            return Variant.Normal;
        }

        // 마을 펜스 사각형 — VillageStarter 의 halfSize 와 일치
        private static readonly Vector2 _villageCenter = new Vector2(GameConstants.VillageCenterX, GameConstants.VillageCenterY);
        private static float _villageHalfSize => VillageStarter.CurrentHalfSize;
        private const float _villageMargin = 1.0f; // 펜스에서 추가로 1u 더 밖

        /// <summary>
        /// 좀비 스폰 지점을 마을 펜스 밖에 보장.
        /// 플레이어 주변 랜덤 각도/거리로 시도 → 마을 안이면 마을 밖으로 강제 이동.
        /// </summary>
        private Vector3 PickOutsideVillageSpawnPos(bool rngBased)
        {
            float angle = rngBased ? _rng.Next() * Mathf.PI * 2f : Random.Range(0f, Mathf.PI * 2f);
            float dist = SpawnDistance + (rngBased ? (_rng.Next() * 2f - 1f) : Random.Range(-1f, 1f)) * SpawnJitter;
            float x = Player.position.x + Mathf.Cos(angle) * dist;
            float y = Player.position.y + Mathf.Sin(angle) * dist;

            // 마을 사각형 내부면 가장 가까운 외곽으로 밀어냄
            Vector2 d = new Vector2(x - _villageCenter.x, y - _villageCenter.y);
            float halfWithMargin = _villageHalfSize + _villageMargin;
            if (Mathf.Abs(d.x) < halfWithMargin && Mathf.Abs(d.y) < halfWithMargin)
            {
                // 안에 있음 — 더 멀리 떨어진 축으로 밀어냄
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                {
                    x = _villageCenter.x + Mathf.Sign(d.x == 0 ? 1 : d.x) * halfWithMargin;
                }
                else
                {
                    y = _villageCenter.y + Mathf.Sign(d.y == 0 ? 1 : d.y) * halfWithMargin;
                }
            }
            return new Vector3(x, y, 0);
        }

        private void SpawnOne()
        {
            Vector3 spawnPos = PickOutsideVillageSpawnPos(rngBased: true);

            var go = new GameObject("Zombie_wave");
            go.transform.position = spawnPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            var zombie = go.AddComponent<Zombie>();
            _tracked.Add(zombie);

            // 일자별 강화 — Day 1 기준, 매 밤마다 +12% HP, +1 dmg, +2% speed (속도 +50% 캡)
            int day = GameSession.Instance != null ? GameSession.Instance.Cycle.Day : 1;
            float dayHpMul = 1f + (day - 1) * 0.12f;
            int dayDmgBonus = day - 1;
            float daySpeedMul = 1f + Mathf.Min((day - 1) * 0.02f, 0.5f);

            int dayBaseHp = Mathf.RoundToInt(zombie.MaxHp * dayHpMul);

            var variant = PickVariant();
            Color tint;
            float scale = 1f;
            switch (variant)
            {
                case Variant.Fast:
                    tint = new Color(0.85f, 0.5f, 0.25f);  // 주황
                    scale = 0.8f;
                    zombie.MoveSpeedMul = 1.6f * daySpeedMul;
                    zombie.InitHp(Mathf.RoundToInt(dayBaseHp * 0.6f));
                    zombie.VariantDamageBonus = dayDmgBonus;
                    break;
                case Variant.Tank:
                    tint = new Color(0.35f, 0.18f, 0.4f);  // 짙은 보라
                    scale = 1.35f;
                    zombie.MoveSpeedMul = 0.6f * daySpeedMul;
                    zombie.InitHp(Mathf.RoundToInt(dayBaseHp * 2.2f));
                    zombie.VariantDamageBonus = 4 + dayDmgBonus;
                    break;
                case Variant.Archer:
                    tint = new Color(0.4f, 0.6f, 0.4f); // 어두운 녹색
                    scale = 0.95f;
                    zombie.MoveSpeedMul = 0.85f * daySpeedMul;
                    zombie.InitHp(Mathf.RoundToInt(dayBaseHp * 0.8f));
                    zombie.VariantDamageBonus = dayDmgBonus;
                    zombie.IsRanged = true;
                    zombie.RangedRange = 7f;
                    zombie.RangedDamage = 6 + dayDmgBonus;
                    break;
                default:
                    tint = new Color(0.6f, 0.2f, 0.22f);
                    zombie.MoveSpeedMul = daySpeedMul;
                    zombie.InitHp(dayBaseHp);
                    zombie.VariantDamageBonus = dayDmgBonus;
                    break;
            }
            go.transform.localScale = Vector3.one * scale;

            var zombieSpr = SpriteBank.Zombie();
            if (zombieSpr != null) { sr.sprite = zombieSpr; sr.color = tint; }

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = tint;
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
