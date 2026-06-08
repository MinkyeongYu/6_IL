using UnityEngine;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// IMGUI 기반 HUD. UiTheme 헬퍼로 패널/바/아이콘/버튼 일관 스타일 적용.
    /// 좌측: 플레이어 + 무기 + 빌드. 우측: 자원 + 사이클. 월드: 채집/수확/배치/영입.
    /// </summary>
    public sealed class SimpleHud : MonoBehaviour
    {
        public PlayerController Player;
        public GatherController Gather;
        public PlayerAttackController Attacker;
        public NightController Night;
        public PlayerProgression Progression;

        private System.Collections.Generic.List<RuneKind> _runeOffer;
        private readonly System.Collections.Generic.Dictionary<RuneKind, Texture2D> _runeIconCache = new();
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> _portraitCache = new();
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> _hudIconCache = new();

        private GUIStyle _label, _labelSubtle, _title, _section, _weapon, _bigDeath, _btn, _smallBtn;
        private string _recruitCutName = "";
        private string _recruitCutRole = "";
        private string _recruitCutDialog = "";
        private Texture2D _recruitCutPortrait;
        private float _recruitCutLeft;
        private float _recruitCutPrevTimeScale = 1f;
        private bool _recruitCutPaused;

        // 장비 모드: 근접 / 원거리 / 건축. BuildHotbar 는 건축에서만, 무기는 모드 따라 자동 전환.
        public enum HudMode { Melee, Ranged, Build }
        private HudMode _hudMode = HudMode.Melee;

        private readonly struct ContextAction
        {
            public readonly int Priority;
            public readonly string Label;
            public readonly bool Enabled;
            public readonly System.Action Callback;

            public ContextAction(int priority, string label, bool enabled, System.Action callback)
            {
                Priority = priority;
                Label = label;
                Enabled = enabled;
                Callback = callback;
            }
        }

        // 빌드 배치 모드 — 핫바에서 건물 선택 후 _pendingBuildKind 설정, 다음 월드 클릭에 ConstructionSite 스폰.
        private BuildingKind? _pendingBuildKind;

        private void SetHudMode(HudMode m)
        {
            _hudMode = m;
            if (Attacker != null)
            {
                if (m == HudMode.Melee) Attacker.SwitchToWeapon(0);
                else if (m == HudMode.Ranged) Attacker.SwitchToWeapon(1);
                // Build 모드는 무기 변경 안 함 — 직전 무기 유지
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawStatCard();
            DrawResourceBar();
            DrawWaveStanceBar();
            if (_hudMode == HudMode.Build) DrawBuildHotbar();
            DrawContextActionPanel();
            DrawRecruitDialog();
            DrawRecruitCutscene();
            DrawRuneModal();
            DrawPhaseBanner();
            DrawBossWarning();
            DrawAutoSaveToast();
            DrawAchievementToast();
            DrawHomeCompass();
            DrawThreatCompass();   // 공격받는 건물/동료 방향 화살표
            DrawPhaseClock();      // 상단 중앙
            // ControlsHint 제거 — 모드 탭과 튜토리얼이 키 안내 대신함
            DrawTutorialOverlay();
            DrawPauseMenu();
            DrawDeathOverlay();
            DrawDamageFlash();
            DrawNightIntroFade();
            DrawDawnFlare();
            DrawPlacementPreview();
            HandlePlacementInput();
        }

        // ====================================================================
        // 배치 모드 — 핫바에서 건물 고른 후 마우스로 위치 지정
        // ====================================================================
        private void DrawPlacementPreview()
        {
            if (_pendingBuildKind == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 wp = MouseWorldPos(cam);

            // 화면 가운데 안내 텍스트
            var bannerStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 15, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UiTheme.TextGold }
            };
            string label = $"📍 {_pendingBuildKind} — 땅을 클릭해 배치 (우클릭/ESC 취소)";
            UiTheme.Rect(new Rect(0, 100, Screen.width, 36), new Color(0.05f, 0.07f, 0.12f, 0.85f));
            GUI.Label(new Rect(0, 105, Screen.width, 26), label, bannerStyle);

            // 마우스 위치에 작은 골드 + (커서)
            Vector3 sp = cam.WorldToScreenPoint(wp);
            float gx = sp.x, gy = Screen.height - sp.y;
            UiTheme.Rect(new Rect(gx - 1, gy - 12, 2, 24), UiTheme.TextGold);
            UiTheme.Rect(new Rect(gx - 12, gy - 1, 24, 2), UiTheme.TextGold);
        }

        private void HandlePlacementInput()
        {
            if (_pendingBuildKind == null) return;
            var ev = Event.current;
            if (ev == null) return;
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
            {
                _pendingBuildKind = null; ev.Use(); return;
            }
            if (ev.type == EventType.MouseDown && ev.button == 1) // 우클릭 취소
            {
                _pendingBuildKind = null; ev.Use(); return;
            }
            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                ConfirmPlacement();
                ev.Use();
            }
        }

        private static Vector3 MouseWorldPos(Camera cam)
        {
            Vector3 mp = Input.mousePosition;
            mp.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(mp);
        }

        private void ConfirmPlacement()
        {
            if (_pendingBuildKind == null) return;
            var session = GameSession.Instance;
            if (session == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 wp = MouseWorldPos(cam);
            wp.z = 0f;

            BuildingKind kind = _pendingBuildKind.Value;
            ResourceCost cost = BuildingUpgradeRules.BuildCost(kind, CostMul(kind));

            if (!cost.CanPay(session.Resources)) return;

            if (!cost.Pay(session.Resources)) return;

            // ConstructionSite 스폰
            SpawnConstructionSite(kind, wp);
            _pendingBuildKind = null;
            Sfx.Build();
        }

        private static int BaseWoodCost(BuildingKind k) => BuildingUpgradeRules.BaseWoodCost(k);
        private static int BaseStoneCost(BuildingKind k) => BuildingUpgradeRules.BaseStoneCost(k);

        private void SpawnConstructionSite(BuildingKind kind, Vector3 pos)
        {
            var go = new GameObject($"Construction_{kind}");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.9f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            sr.color = new Color(0.7f, 0.78f, 0.85f, 0.5f);
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.7f, 0.78f, 0.85f, 0.5f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.95f, 0.78f, 0.35f, 1f);

            var site = go.AddComponent<ConstructionSite>();
            site.Kind = kind;
            site.TotalTime = 8f;
            site.OnComplete = (k, p) =>
            {
                switch (k)
                {
                    case BuildingKind.Campfire: SpawnCampfire(p); break;
                    case BuildingKind.Brazier: SpawnBrazier(p); break;
                    case BuildingKind.Blacksmith: SpawnBlacksmith(p); break;
                    case BuildingKind.SeedStorage: SpawnSeedStorage(p); break;
                    case BuildingKind.Carpenter: SpawnCarpenter(p); break;
                    case BuildingKind.TrainingCamp: SpawnTrainingCamp(p); break;
                    case BuildingKind.FoodStorage:
                        SpawnFoodStorage(p);
                        var foodSes = GameSession.Instance;
                        if (foodSes != null) foodSes.Resources.IncreaseCap(ResourceKind.Food, BuildingUpgradeRules.FoodStorageCapPerLevel);
                        break;
                    case BuildingKind.LookoutPost: SpawnLookoutPost(p); break;
                    case BuildingKind.Sawmill: SpawnSawmill(p); break;
                    case BuildingKind.Church: SpawnChurch(p); break;
                    case BuildingKind.House: SpawnHouse(p); break;
                    case BuildingKind.Fence: VillageStarter.SpawnFence(p, 0f); break;
                    case BuildingKind.Storage:
                        SpawnStorage(p);
                        var ses = GameSession.Instance;
                        if (ses != null) ses.Resources.IncreaseCap(50);
                        break;
                    case BuildingKind.Farm: SpawnFarm(p); break;
                    case BuildingKind.Watchtower: SpawnWatchtower(p); break;
                    case BuildingKind.Barricade: SpawnBarricade(p); break;
                    case BuildingKind.Infirmary: SpawnInfirmary(p); break;
                    case BuildingKind.HuntersHut: SpawnHuntersHut(p); break;
                }
                // 비-펜스 건물 추가 시 마을 펜스 자동 확장
                if (k != BuildingKind.Fence)
                {
                    var s = GameSession.Instance;
                    Vector3 center = new Vector3(GameConstants.VillageCenterX, GameConstants.VillageCenterY, 0f);
                    VillageStarter.OnBuildingAdded(center);
                }
            };
        }

        // ====================================================================
        // 아침 연출: Dawn/Day 시작 → 골드빛 오버레이가 밝아지다 사라짐 + "☀ 아침"
        // ====================================================================
        private float _dawnAlpha;          // 0 = 비활성, >0 = 진행
        private float _dawnLifetime = 2.6f;
        private GUIStyle _dawnStyle;

        private void DrawDawnFlare()
        {
            if (_dawnAlpha <= 0.001f) return;
            _dawnAlpha -= Time.unscaledDeltaTime / _dawnLifetime;

            // 0~0.4 구간: 페이드 인 (alpha → 0.55), 0.4~1 구간: 페이드 아웃 (0.55 → 0)
            float k = Mathf.Clamp01(_dawnAlpha);
            float intensity = k > 0.6f ? Mathf.InverseLerp(1f, 0.6f, k) * 0.55f : k / 0.6f * 0.55f;

            // 위쪽이 더 밝은 그라데이션 — 5분할 사각으로 흉내
            int bands = 6;
            for (int i = 0; i < bands; i++)
            {
                float t = i / (float)(bands - 1);
                float bandAlpha = intensity * Mathf.Lerp(1f, 0.4f, t);
                Color c = Color.Lerp(new Color(1f, 0.85f, 0.55f), new Color(1f, 0.65f, 0.30f), t);
                c.a = bandAlpha;
                float yTop = (Screen.height / (float)bands) * i;
                float h = Screen.height / (float)bands + 1;
                UiTheme.Rect(new Rect(0, yTop, Screen.width, h), c);
            }

            // 중앙 텍스트
            if (_dawnStyle == null)
            {
                _dawnStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 24, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.95f, 0.7f) }
                };
            }
            float textA = intensity / 0.55f;
            var oldC = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.95f, 0.7f, textA);
            GUI.Label(new Rect(0, Screen.height / 2 - 24, Screen.width, 48), "New dawn", _dawnStyle);
            GUI.contentColor = oldC;
        }

        // ====================================================================
        // 밤 인트로: Evening 시작 → 화면 암전 → 플레이어 마을로 이동 → 암전 해제 → Night
        // ====================================================================
        private float _fadeAlpha;          // 0 (투명) ~ 1 (완전 암전)
        private float _fadeTarget;         // 목표 alpha
        private float _fadeSpeed = 1.6f;   // alpha/sec
        private bool _teleportPending;
        private GUIStyle _fadeStyle;

        private void DrawNightIntroFade()
        {
            // 부드러운 이행
            if (Mathf.Abs(_fadeAlpha - _fadeTarget) > 0.001f)
            {
                float dir = Mathf.Sign(_fadeTarget - _fadeAlpha);
                _fadeAlpha = Mathf.Clamp01(_fadeAlpha + dir * _fadeSpeed * Time.unscaledDeltaTime);
                // 암전 절정에서 마을로 이동
                if (_teleportPending && _fadeAlpha > 0.92f)
                {
                    _teleportPending = false;
                    BringEveryoneToVillage();
                }
            }
            if (_fadeAlpha <= 0.001f) return;

            UiTheme.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, _fadeAlpha));
            // 중앙 텍스트
            if (_fadeStyle == null)
            {
                _fadeStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 18, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.86f, 0.45f, 1f) }
                };
            }
            var oldC = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.86f, 0.45f, _fadeAlpha);
            GUI.Label(new Rect(0, Screen.height / 2 - 20, Screen.width, 40), "마을로 돌아가는 중...", _fadeStyle);
            GUI.contentColor = oldC;
        }

        /// <summary>플레이어 + 모든 살아있는 동료/펫 을 마을 중심으로 즉시 이동.</summary>
        private void BringEveryoneToVillage()
        {
            Vector3 villageCenter = new Vector3(GameConstants.VillageCenterX, GameConstants.VillageCenterY, 0f);
            if (Player != null)
            {
                Player.transform.position = villageCenter;
                var prb = Player.GetComponent<Rigidbody2D>();
                if (prb != null) prb.velocity = Vector2.zero;
            }
            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in comps)
            {
                if (c == null || c.IsDead) continue;
                Vector2 jitter = Random.insideUnitCircle * 1.5f;
                c.transform.position = villageCenter + (Vector3)jitter;
                var rb = c.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
            }
            // 펫(개/매)도 같이
            var pets = Object.FindObjectsByType<Pet>(FindObjectsSortMode.None);
            foreach (var p in pets)
            {
                if (p == null) continue;
                Vector2 jitter = Random.insideUnitCircle * 1.0f;
                p.transform.position = villageCenter + (Vector3)jitter;
                var rb = p.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
            }
        }

        // OnEnable에서 추가 페이즈 핸들러 등록 (기존 unsubE/N/D/A 옆에 한 쌍 더)
        private System.Action _unsubFadeIn, _unsubFadeOut, _unsubDawnFlare;
        private void HookFadeEvents()
        {
            if (_unsubFadeIn != null) return;
            _unsubFadeIn = EventBus.Instance.Subscribe<EveningStartedPayload>(_ =>
            {
                _fadeTarget = 1f;
                _teleportPending = true;
            });
            _unsubFadeOut = EventBus.Instance.Subscribe<NightStartedPayload>(_ =>
            {
                _fadeTarget = 0f;
                // Evening 페이즈가 스킵된 경우(디버그 강제 밤 등) 안전망 — 즉시 마을 집결
                BringEveryoneToVillage();
            });
            _unsubDawnFlare = EventBus.Instance.Subscribe<DawnStartedPayload>(_ => _dawnAlpha = 1f);
        }

        private void UnhookFadeEvents()
        {
            _unsubFadeIn?.Invoke(); _unsubFadeIn = null;
            _unsubFadeOut?.Invoke(); _unsubFadeOut = null;
            _unsubDawnFlare?.Invoke(); _unsubDawnFlare = null;
        }

        private AchievementManager.Entry? _achToast;
        private float _achToastLeft;
        private GUIStyle _achTitle, _achDetail;

        private void DrawAchievementToast()
        {
            var am = AchievementManager.Instance;
            if (am == null) return;
            if (_achToastLeft <= 0f && am.NewlyUnlocked.Count > 0)
            {
                _achToast = am.NewlyUnlocked.Dequeue();
                _achToastLeft = 4.0f;
            }
            if (_achToastLeft <= 0f || _achToast == null) return;
            _achToastLeft -= Time.unscaledDeltaTime;

            if (_achTitle == null)
            {
                _achTitle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold,
                    normal = { textColor = UiTheme.TextGold } };
                _achDetail = new GUIStyle(GUI.skin.label) { fontSize = 12,
                    normal = { textColor = UiTheme.TextCream }, wordWrap = true };
            }

            float a = Mathf.Clamp01(_achToastLeft) > 0.6f ? 1f : Mathf.Clamp01(_achToastLeft / 0.6f);
            int W = 360, H = 64;
            // 상단 중앙 — PhaseClock 아래 (Clock 64h + 6 gap)
            var r = new Rect(Screen.width / 2 - W / 2, 88, W, H);
            UiTheme.Rect(r, new Color(0.07f, 0.09f, 0.14f, 0.92f * a));
            UiTheme.Rect(new Rect(r.x, r.y, r.width, 2), new Color(0.95f, 0.78f, 0.35f, a));
            UiTheme.Rect(new Rect(r.x, r.yMax - 2, r.width, 2), new Color(0.95f, 0.78f, 0.35f, a));

            var oldC = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.86f, 0.45f, a);
            GUI.Label(new Rect(r.x + 12, r.y + 6, r.width - 16, 20), $"🏆  {_achToast.Value.Title}", _achTitle);
            GUI.contentColor = new Color(1f, 1f, 0.92f, a);
            GUI.Label(new Rect(r.x + 12, r.y + 28, r.width - 16, 28), _achToast.Value.Detail, _achDetail);
            GUI.contentColor = oldC;
        }

        private string _phaseBanner = "";
        private float _phaseBannerLeft;
        private System.Action _unsubE, _unsubN, _unsubD, _unsubA;
        private System.Action _unsubRecruit;

        private void OnEnable()
        {
            _unsubE = EventBus.Instance.Subscribe<EveningStartedPayload>(p =>
                { ShowBanner($"Day {p.Day}  Evening"); Music.PlayForPhase(Phase.Evening); });
            _unsubN = EventBus.Instance.Subscribe<NightStartedPayload>(p =>
                { ShowBanner($"Day {p.Day}  Night"); Music.PlayForPhase(Phase.Night); });
            _unsubD = EventBus.Instance.Subscribe<DawnStartedPayload>(p =>
                { ShowBanner($"Day {p.Day}  🌄  새벽"); Music.PlayForPhase(Phase.Dawn); });
            _unsubA = EventBus.Instance.Subscribe<DayStartedPayload>(p =>
                { ShowBanner($"Day {p.Day}  Day"); Music.PlayForPhase(Phase.Day); });
            _unsubRecruit = EventBus.Instance.Subscribe<CompanionRecruitedPayload>(p =>
                ShowRecruitCutscene(p.DisplayName, p.Role, p.DialogText));
            HookFadeEvents();
        }

        private void OnDisable()
        {
            _unsubE?.Invoke(); _unsubN?.Invoke(); _unsubD?.Invoke(); _unsubA?.Invoke();
            _unsubRecruit?.Invoke(); _unsubRecruit = null;
            UnhookFadeEvents();
        }

        private bool _initialBannerShown;

        private void TryShowInitialBanner()
        {
            if (_initialBannerShown) return;
            var s = GameSession.Instance;
            if (s == null || s.Cycle == null) return; // 다음 프레임에 다시 시도
            _initialBannerShown = true;
            string txt = s.Cycle.Phase switch
            {
                Phase.Day => $"Day {s.Cycle.Day}  Day",
                Phase.Evening => $"Day {s.Cycle.Day}  Evening",
                Phase.Night => $"Day {s.Cycle.Day}  Night",
                Phase.Dawn => $"Day {s.Cycle.Day}  🌄  새벽",
                _ => "",
            };
            ShowBanner(txt);
            Music.PlayForPhase(s.Cycle.Phase);
        }

        private void ShowBanner(string text)
        {
            _phaseBanner = text;
            _phaseBannerLeft = 2.6f;
        }

        private GUIStyle _bannerStyle;
        private void DrawPhaseBanner()
        {
            if (_phaseBannerLeft <= 0f) return;
            _phaseBannerLeft -= Time.unscaledDeltaTime;
            if (_bannerStyle == null)
            {
                _bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UiTheme.TextGold },
                };
            }
            float a = Mathf.Clamp01(_phaseBannerLeft / 1.5f);
            int y = Screen.height / 2 - 100;
            UiTheme.Rect(new Rect(0, y, Screen.width, 56), new Color(0.05f, 0.07f, 0.12f, 0.7f * a));
            UiTheme.Rect(new Rect(0, y, Screen.width, 1), new Color(0.78f, 0.62f, 0.30f, a));
            UiTheme.Rect(new Rect(0, y + 55, Screen.width, 1), new Color(0.78f, 0.62f, 0.30f, a));
            var oldC = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.86f, 0.45f, a);
            GUI.Label(new Rect(0, y + 8, Screen.width, 44), _phaseBanner, _bannerStyle);
            GUI.contentColor = oldC;
        }

        private GUIStyle _clockBig, _clockSmall;

        private static float GetTemperatureCelsius()
        {
            var s = GameSession.Instance;
            if (s?.Cycle == null) return -10f;
            float baseTemp = s.Cycle.Phase switch
            {
                Phase.Day     => -8f,
                Phase.Evening => -18f,
                Phase.Night   => -28f,
                Phase.Dawn    => -14f,
                _             => -10f,
            };
            float progress = s.Cycle.PhaseDurationSec > 0
                ? s.Cycle.ElapsedInPhase / s.Cycle.PhaseDurationSec : 0f;
            // 밤은 시간이 지날수록 더 추워짐
            float nightDrop = s.Cycle.Phase == Phase.Night ? -8f * progress : 0f;
            return baseTemp + nightDrop;
        }

        private void DrawPhaseClock()
        {
            var s = GameSession.Instance;
            if (s == null || s.Cycle == null) return;

            if (_clockBig == null)
            {
                _clockBig = new GUIStyle(GUI.skin.label) {
                    fontSize = 16, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UiTheme.TextGold } };
                _clockSmall = new GUIStyle(GUI.skin.label) {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UiTheme.TextCream } };
            }

            string phaseName = s.Cycle.Phase switch
            {
                Phase.Day => "Day",
                Phase.Evening => "Evening",
                Phase.Night => "Night",
                Phase.Dawn => "새벽",
                _ => "",
            };
            string phaseIcon = s.Cycle.Phase switch
            {
                Phase.Day => "Day",
                Phase.Evening => "Evening",
                Phase.Night => "Night",
                Phase.Dawn => "🌄",
                _ => "·",
            };
            float dur = s.Cycle.PhaseDurationSec;
            float rem = Mathf.Max(0f, dur - s.Cycle.ElapsedInPhase);
            int mm = Mathf.FloorToInt(rem / 60f);
            int ss = Mathf.FloorToInt(rem % 60f);
            string clock = $"{mm:00}:{ss:00}";

            int W = 130, H = 62;
            var r = new Rect(Screen.width - W - 12, 12, W, H);

            // 배경 패널 (페이즈 색조)
            Color tint = s.Cycle.Phase switch
            {
                Phase.Day     => new Color(0.65f, 0.78f, 0.95f, 0.95f),
                Phase.Evening => new Color(0.85f, 0.55f, 0.45f, 0.95f),
                Phase.Night   => new Color(0.18f, 0.22f, 0.45f, 0.97f),
                Phase.Dawn    => new Color(0.95f, 0.78f, 0.55f, 0.95f),
                _ => new Color(0.1f, 0.1f, 0.15f, 0.95f),
            };
            UiTheme.Rect(r, new Color(0.05f, 0.07f, 0.12f, 0.38f));
            // 진행 바 (하단)
            float progress = dur > 0 ? Mathf.Clamp01(1f - rem / dur) : 0f;
            UiTheme.Rect(new Rect(r.x, r.yMax - 3, r.width * progress, 3), new Color(tint.r, tint.g, tint.b, 0.72f));

            // 시계 + 페이즈 아이콘
            GUI.Label(new Rect(r.x, r.y + 3, r.width, 20), $"{phaseIcon} {phaseName} {clock}", _clockBig);
            // Day
            GUI.Label(new Rect(r.x, r.y + 22, r.width, 16), $"Day {s.Cycle.Day}", _clockSmall);
            // 기온
            float temp = GetTemperatureCelsius();
            Color tempColor = temp < -25f ? new Color(0.55f, 0.85f, 1f) : UiTheme.TextSubtle;
            var oldTempC = GUI.contentColor;
            GUI.contentColor = tempColor;
            DrawHudIcon(new Rect(r.x + 44, r.y + 37, 14, 14), "temp");
            GUI.Label(new Rect(r.x + 60, r.y + 38, r.width - 60, 16), $"{temp:F0}°C", _clockSmall);
            GUI.contentColor = oldTempC;
        }

        private GUIStyle _compassDist;

        /// <summary>최근 공격받은 건물/동료가 화면 밖에 있으면 화면 가장자리에 빨간 방향 화살표.</summary>
        private void DrawThreatCompass()
        {
            var cam = Camera.main;
            if (cam == null) return;
            const float RecentSec = 4f;
            const int ArrowSize = 22;
            int margin = 40;

            void DrawArrow(Vector3 worldPos, Color color, string label)
            {
                Vector3 sp = cam.WorldToScreenPoint(worldPos);
                bool onScreen = sp.z > 0 && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
                if (onScreen) return; // 화면 안이면 HpBar 가 충분히 표시

                Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 target = new Vector2(sp.x, Screen.height - sp.y);
                if (sp.z < 0) target = center * 2f - target; // 카메라 뒤
                Vector2 dir = (target - center).normalized;

                // 화면 가장자리에서 안쪽으로 margin 만큼 위치
                float halfW = Screen.width * 0.5f - margin;
                float halfH = Screen.height * 0.5f - margin;
                float t = Mathf.Min(halfW / Mathf.Abs(dir.x + 0.0001f), halfH / Mathf.Abs(dir.y + 0.0001f));
                Vector2 edgePos = center + dir * t;

                float angDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                var matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(angDeg, edgePos);
                // 삼각형 화살표
                UiTheme.Rect(new Rect(edgePos.x - ArrowSize, edgePos.y - 4, ArrowSize, 8), color);
                UiTheme.Rect(new Rect(edgePos.x, edgePos.y - 8, 6, 16), color);
                UiTheme.Rect(new Rect(edgePos.x + 4, edgePos.y - 6, 4, 12), color);
                UiTheme.Rect(new Rect(edgePos.x + 6, edgePos.y - 4, 4, 8), color);
                UiTheme.Rect(new Rect(edgePos.x + 8, edgePos.y - 2, 4, 4), color);
                GUI.matrix = matrix;

                if (_threatLabel == null)
                {
                    _threatLabel = new GUIStyle(GUI.skin.label) {
                        fontSize = 12, fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = color }
                    };
                }
                GUI.Label(new Rect(edgePos.x - 40, edgePos.y + 14, 80, 16), label, _threatLabel);
            }

            float now = Time.time;
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0) continue;
                if (now - b.LastDamagedAt > RecentSec) continue;
                DrawArrow(b.transform.position, new Color(0.95f, 0.4f, 0.4f), "⚠ 건물 피격");
            }
            var cs = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in cs)
            {
                if (c == null || c.IsDead) continue;
                if (now - c.LastDamagedAt > RecentSec) continue;
                DrawArrow(c.transform.position, new Color(0.95f, 0.6f, 0.4f), "⚠ 동료 피격");
            }
        }
        private GUIStyle _threatLabel;

        private void DrawHomeCompass()
        {
            if (Player == null) return;
            // 가장 가까운 모닥불·창고·바리게이트 방향 표시. 없으면 (0,0).
            Vector2 home = Vector2.zero;
            float bestDist = float.MaxValue;
            var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in buildings)
            {
                if (b == null) continue;
                float d = Vector2.Distance(Player.transform.position, b.transform.position);
                if (d < bestDist) { bestDist = d; home = b.transform.position; }
            }
            float dist = bestDist == float.MaxValue
                ? Vector2.Distance(Player.transform.position, Vector2.zero)
                : bestDist;
            if (dist < 12f) return; // 가까우면 안 그림

            Vector2 dir = (home - (Vector2)Player.transform.position).normalized;
            float angDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            int W = 130, H = 40;
            // 상단 중앙 ResourceBar 왼쪽으로 — PhaseClock 우측 빈 공간
            var r = new Rect(Screen.width / 2 + 160, 16, W, H);
            UiTheme.Rect(r, new Color(0.07f, 0.09f, 0.14f, 0.85f));
            UiTheme.Rect(new Rect(r.x, r.y, r.width, 1), new Color(0.78f, 0.62f, 0.30f, 0.7f));
            UiTheme.Rect(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(0.78f, 0.62f, 0.30f, 0.7f));

            // 화살표는 IMGUI 회전으로
            var arrowCenter = new Vector2(r.x + 18, r.y + H * 0.5f);
            var pivot = arrowCenter;
            var matrix = GUI.matrix;
            // 화면좌표는 y가 아래로 + 인 점을 보정 (월드 angDeg 의 -y 반전)
            GUIUtility.RotateAroundPivot(-angDeg, pivot);
            UiTheme.Rect(new Rect(arrowCenter.x - 12, arrowCenter.y - 2, 18, 4), UiTheme.TextGold);
            UiTheme.Rect(new Rect(arrowCenter.x + 4, arrowCenter.y - 6, 4, 12), UiTheme.TextGold);
            UiTheme.Rect(new Rect(arrowCenter.x + 6, arrowCenter.y - 4, 4, 8), UiTheme.TextGold);
            UiTheme.Rect(new Rect(arrowCenter.x + 8, arrowCenter.y - 2, 4, 4), UiTheme.TextGold);
            GUI.matrix = matrix;

            if (_compassDist == null)
            {
                _compassDist = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter, normal = { textColor = UiTheme.TextCream } };
            }
            GUI.Label(new Rect(r.x + 36, r.y, r.width - 36, H), $"{dist:F0}u to home", _compassDist);
        }

        private const string TutorialPrefKey = "il6_tutorial_seen_v2"; // v2 = 세계관/목표 페이지 추가
        private bool _tutorialDismissed;
        private int _tutPage; // 0: 세계관 / 1: 목표 / 2: 조작
        private GUIStyle _tutTitle, _tutBody, _tutLore;

        private void DrawTutorialOverlay()
        {
            if (_tutorialDismissed) return;
            if (PlayerPrefs.GetInt(TutorialPrefKey, 0) == 1) { _tutorialDismissed = true; return; }

            int W = 620, H = 380;
            var r = new Rect(Screen.width / 2 - W / 2, Screen.height / 2 - H / 2, W, H);
            UiTheme.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.7f));
            UiTheme.Panel(r);

            string pageTitle = _tutPage switch
            {
                0 => "  ❄  끝없는 겨울이 찾아왔다  ",
                1 => "  🏹  목표  ",
                _ => "  🎮  조작과 시스템  ",
            };
            UiTheme.TitleBar(r, pageTitle, _title);

            if (_tutTitle == null)
            {
                _tutTitle = new GUIStyle(GUI.skin.label) {
                    fontSize = 19, fontStyle = FontStyle.Bold,
                    normal = { textColor = UiTheme.TextGold }, wordWrap = true };
                _tutBody = new GUIStyle(GUI.skin.label) {
                    fontSize = 16, normal = { textColor = UiTheme.TextCream }, wordWrap = true };
                _tutLore = new GUIStyle(GUI.skin.label) {
                    fontSize = 17, fontStyle = FontStyle.Normal,
                    normal = { textColor = UiTheme.TextCream }, wordWrap = true };
            }

            int x = (int)r.x + 28, y = (int)r.y + 56, w = (int)r.width - 56;

            if (_tutPage == 0)
            {
                GUI.Label(new Rect(x, y, w, 28), "한때 푸르렀던 이 땅에 끝없는 겨울이 내려앉았다.", _tutLore); y += 30;
                GUI.Label(new Rect(x, y, w, 28), "사람들은 떠났고, 남은 자들은 얼어죽거나 — 더 끔찍한 운명을 맞았다.", _tutLore); y += 30;
                GUI.Label(new Rect(x, y, w, 28), "밤이 되면 죽은 자들이 마을의 모닥불을 향해 일어선다.", _tutLore); y += 36;
                GUI.Label(new Rect(x, y, w, 24), "Evening: gather resources and return to the village.", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 28), "눈보라 너머에는 두려움도, 희망도 함께 살고 있다.", _tutLore); y += 30;
            }
            else if (_tutPage == 1)
            {
                GUI.Label(new Rect(x, y, w, 24), "Evening: gather resources and return to the village.", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 22), "·  나무·돌을 캐고 동물을 사냥해 식량 확보", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  문 밖에서 떠도는 방랑자(NPC)를 만나 영입", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  마을에 모닥불·울타리·망루를 지어 방어 강화", _tutBody); y += 30;

                GUI.Label(new Rect(x, y, w, 24), "Evening: gather resources and return to the village.", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 22), "·  좀비 웨이브가 사방에서 몰려온다", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  5일·10일·15일째 밤에는 보스가 출현", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  레벨이 오를수록 낮과 밤이 길어진다 — 더 많은 시간, 더 큰 위협", _tutBody); y += 30;
            }
            else // page 2
            {
                GUI.Label(new Rect(x, y, w, 24), "Evening: gather resources and return to the village.", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 22), "·  WASD / 방향키 — 이동 (W = 위)", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  E — 근처 자원 채집  ·  F — 방랑자 영입", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  공격은 자동 — 사거리 안 적을 즉시 공격", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "- ESC pauses / saves / restarts", _tutBody); y += 30;

                GUI.Label(new Rect(x, y, w, 24), "Evening: gather resources and return to the village.", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 22), "·  좀비 처치 → XP → 레벨업 시 룬 3종 중 1개 선택 (최대 3중첩)", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  같은 원소 룬 마스터 시 시너지 발동 (독·얼음·번개)", _tutBody); y += 22;
            }

            // 페이지 인디케이터 (점 3개)
            int dotY = (int)r.yMax - 60;
            int dotX = (int)(r.x + r.width / 2 - 18);
            for (int i = 0; i < 3; i++)
            {
                Color dot = i == _tutPage ? UiTheme.TextGold : UiTheme.PanelBorderDim;
                UiTheme.Rect(new Rect(dotX + i * 14, dotY, 8, 8), dot);
            }

            // 버튼
            int btnW = 120;
            int btnY = (int)r.yMax - 38;
            if (_tutPage > 0)
            {
                if (UiTheme.Button(new Rect(r.x + 28, btnY, btnW, 28), "◀ 이전", _smallBtn)) _tutPage--;
            }
            string nextLabel = _tutPage < 2 ? "Next" : "Start";
            if (UiTheme.Button(new Rect(r.xMax - btnW - 28, btnY, btnW, 28), nextLabel, _btn))
            {
                if (_tutPage < 2) _tutPage++;
                else DismissTutorial();
            }
        }

        private void DismissTutorial()
        {
            _tutorialDismissed = true;
            PlayerPrefs.SetInt(TutorialPrefKey, 1);
            PlayerPrefs.Save();
        }

        private void DrawControlsHint()
        {
            const string hint = "WASD/방향키 이동 · E 채집 · F 영입 · ESC 일시정지 · 자동 공격";
            const int W = 480, H = 24;
            var r = new Rect(Screen.width / 2 - W / 2, Screen.height - H - 8, W, H);
            UiTheme.Rect(r, new Color(0.05f, 0.07f, 0.12f, 0.55f));
            UiTheme.Rect(new Rect(r.x, r.y, r.width, 1), new Color(0.78f, 0.62f, 0.30f, 0.4f));
            UiTheme.Rect(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(0.78f, 0.62f, 0.30f, 0.4f));
            var oldC = GUI.contentColor;
            GUI.contentColor = UiTheme.TextSubtle;
            GUI.Label(r, hint, _section);
            GUI.contentColor = oldC;
        }

        private GUIStyle _bossWarnStyle;
        private void DrawBossWarning()
        {
            if (Night == null || Night.BossWarningRemaining <= 0f) return;
            if (_bossWarnStyle == null)
            {
                _bossWarnStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.3f, 0.6f) },
                };
            }
            float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
            UiTheme.Rect(new Rect(0, Screen.height / 2 - 60, Screen.width, 120),
                new Color(0.5f, 0.05f, 0.15f, 0.35f * pulse));
            GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 60),
                $"⚠ 보스 출현  {Mathf.CeilToInt(Night.BossWarningRemaining)}", _bossWarnStyle);
        }

        private void DrawAutoSaveToast()
        {
            var s = GameSession.Instance;
            if (s == null) return;
            float elapsed = Time.time - s.LastAutoSaveAt;
            if (elapsed > 2.5f) return;
            float a = Mathf.Clamp01(1f - elapsed / 2.5f);
            int W = 180, H = 32;
            // StatCard(420 wide @ x=12) 아래로 — 겹침 방지
            var r = new Rect(12, 250, W, H);
            UiTheme.Rect(r, new Color(0.07f, 0.09f, 0.14f, 0.85f * a));
            UiTheme.Rect(new Rect(r.x, r.y, r.width, 1), new Color(0.78f, 0.62f, 0.30f, a));
            UiTheme.Rect(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(0.78f, 0.62f, 0.30f, a));
            var oldC = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.86f, 0.45f, a);
            GUI.Label(r, "💾 자동 저장됨", _section);
            GUI.contentColor = oldC;
        }

        private int _lastPlayerHp = -1;
        private float _damageFlashAmount;

        private void Update()
        {
            TryShowInitialBanner();

            // 모드 단축키 1/2/3
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetHudMode(HudMode.Melee);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetHudMode(HudMode.Ranged);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetHudMode(HudMode.Build);

            if (Player == null) return;
            if (_lastPlayerHp == -1) _lastPlayerHp = Player.CurrentHp;
            if (Player.CurrentHp < _lastPlayerHp) _damageFlashAmount = 0.55f;
            _lastPlayerHp = Player.CurrentHp;
            if (_damageFlashAmount > 0f) _damageFlashAmount -= Time.unscaledDeltaTime;
        }

        private void DrawDamageFlash()
        {
            if (_damageFlashAmount <= 0f) return;
            float a = Mathf.Clamp01(_damageFlashAmount) * 0.55f;
            // 외곽 빨간 비네트 (4개 직사각형)
            int t = 60;
            UiTheme.Rect(new Rect(0, 0, Screen.width, t), new Color(0.9f, 0.05f, 0.05f, a));
            UiTheme.Rect(new Rect(0, Screen.height - t, Screen.width, t), new Color(0.9f, 0.05f, 0.05f, a));
            UiTheme.Rect(new Rect(0, 0, t, Screen.height), new Color(0.9f, 0.05f, 0.05f, a));
            UiTheme.Rect(new Rect(Screen.width - t, 0, t, Screen.height), new Color(0.9f, 0.05f, 0.05f, a));
        }

        // ====================================================================
        // STAT CARD (top-left): HP / XP — 얇은 바 2줄
        // ====================================================================
        private void DrawStatCard()
        {
            var panel = HudLayout.TopLeftStatus();
            int x = (int)panel.x + 10;
            int y = (int)panel.y + 10;
            int W = (int)panel.width - 20;

            if (Player != null)
            {
                float hpPct = Player.MaxHp > 0 ? (float)Player.CurrentHp / Player.MaxHp : 0f;
                Color hpFill = Color.Lerp(new Color(0.85f, 0.2f, 0.18f), new Color(0.4f, 0.85f, 0.4f), hpPct);
                DrawHudIcon(new Rect(x, y - 4, 18, 18), "hp");
                int barX = x + 22;
                int barW = W - 22;
                UiTheme.Rect(new Rect(barX - 1, y - 1, barW + 2, 14), UiTheme.PanelBorderDim);
                UiTheme.Bar(new Rect(barX, y, barW, 12), hpPct, hpFill);
                var hpStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect(barX, y, barW, 12), $"HP {Player.CurrentHp}/{Player.MaxHp}", hpStyle);
                y += 14;
            }

            if (Progression != null)
            {
                float xpPct = Progression.XpToNext > 0 ? (float)Progression.Xp / Progression.XpToNext : 0f;
                DrawHudIcon(new Rect(x + 2, y - 5, 14, 14), "xp");
                int barX = x + 22;
                int barW = W - 22;
                UiTheme.Rect(new Rect(barX - 1, y - 1, barW + 2, 9), UiTheme.PanelBorderDim);
                UiTheme.Bar(new Rect(barX, y, barW, 7), xpPct, UiTheme.BarXpFill);
                y += 9;
            }

            if (Gather != null && Gather.IsActive)
            {
                UiTheme.Rect(new Rect(x - 1, y + 1, W + 2, 9), UiTheme.PanelBorderDim);
                UiTheme.Bar(new Rect(x, y + 2, W, 7), Gather.Progress, new Color(0.6f, 0.85f, 0.4f));
                var gStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 9,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UiTheme.TextCream }
                };
                GUI.Label(new Rect(x, y + 2, W, 7), $"\uCC44\uC9D1 {(Gather.Progress * 100):F0}%", gStyle);
            }

            int tabY = (int)panel.y + 14 + 9 + 24;
            int tabW = (W - 8) / 3;
            DrawModeTab(new Rect(x, tabY, tabW, 22), HudMode.Melee, "\uB3C4\uB07C [1]");
            DrawModeTab(new Rect(x + tabW + 4, tabY, tabW, 22), HudMode.Ranged, "\uD65C [2]");
            DrawModeTab(new Rect(x + (tabW + 4) * 2, tabY, tabW, 22), HudMode.Build, "\uAC74\uC124 [3]");
        }

        private void DrawModeTab(Rect r, HudMode mode, string label)
        {
            bool active = _hudMode == mode;
            // 활성 탭은 골드 보더 + 밝은 배경
            Color bg = active ? new Color(0.18f, 0.20f, 0.28f, 0.72f) : new Color(0.10f, 0.12f, 0.16f, 0.38f);
            UiTheme.Rect(r, bg);
            var oldC = GUI.contentColor;
            GUI.contentColor = active ? UiTheme.TextGold : UiTheme.TextSubtle;
            var s = new GUIStyle(_label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(r, label, s);
            GUI.contentColor = oldC;
            if (GUI.Button(r, "", GUIStyle.none)) SetHudMode(mode);
        }

        // ====================================================================
        // (REMOVED) InfoCard — 점수/킬/손실 제거 요청. 사망 화면에는 여전히 점수 표시됨.
        // ====================================================================
        private void DrawInfoCard_DEPRECATED()
        {
            const int W = 280, H = 152;
            var panel = new Rect(Screen.width - W - 12, 12, W, H);
            UiTheme.Panel(panel);

            var session = GameSession.Instance;
            if (session == null) return;
            int innerX = (int)panel.x + 12;
            int innerW = W - 24;
            int y = (int)panel.y + 10;

            // 점수 (큰 골드)
            var scoreStyle = new GUIStyle(_title) {
                fontSize = 28, alignment = TextAnchor.MiddleLeft,
                normal = { textColor = UiTheme.TextGold }
            };
            GUI.Label(new Rect(innerX, y, innerW, 32), $"점수  {session.Score}", scoreStyle);
            y += 32;

            // 킬 / 손실
            GUI.Label(new Rect(innerX, y, innerW, 18),
                $"킬 {session.TotalKills}  ·  동료 손실 {session.CompanionsLost}  ·  최대 {session.MaxCompanionsAtOnce}",
                _labelSubtle);
            y += 22;

            // 웨이브 + 블리자드
            if (Night != null && Night.CurrentPhase == Phase.Night)
            {
                var nightOldC = GUI.contentColor;
                GUI.contentColor = new Color(0.95f, 0.5f, 0.5f);
                GUI.Label(new Rect(innerX, y, innerW, 20),
                    $"🧟 활성 {Night.ActiveZombies}  ·  대기 {Night.WavePending}", _section);
                GUI.contentColor = nightOldC;
                y += 22;
                if (Night.IsBlizzard)
                {
                    GUI.contentColor = new Color(0.55f, 0.85f, 1f);
                    GUI.Label(new Rect(innerX, y, innerW, 20), "❄ 눈보라!", _section);
                    GUI.contentColor = nightOldC;
                    y += 22;
                }
            }
            else if (session.LastFoodShortage > 0)
            {
                var oldC = GUI.contentColor;
                GUI.contentColor = UiTheme.TextDanger;
                GUI.Label(new Rect(innerX, y, innerW, 20),
                    $"⚠ 식량 부족 {session.LastFoodShortage}", _section);
                GUI.contentColor = oldC;
                y += 22;
            }

            // 동료 스탠스 (밑쪽 줄, 컴팩트)
            var allComps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int liveCount = 0;
            Companion.Stance majority = Companion.Stance.Follow;
            foreach (var c in allComps)
            {
                if (c == null || c.IsDead || c.CurrentMode == Companion.Mode.Hiding) continue;
                liveCount++;
                majority = c.CurrentStance;
            }
            string sLabel = majority switch
            {
                Companion.Stance.Follow => "Follow",
                Companion.Stance.Hold => "🛡 사수",
                Companion.Stance.Aggressive => "⚔ 공세",
                _ => "",
            };
            int btnY = (int)panel.yMax - 32;
            if (UiTheme.Button(new Rect(innerX, btnY, innerW, 26), $"동료 {sLabel}  ({liveCount})", _smallBtn, liveCount > 0))
            {
                var next = majority switch
                {
                    Companion.Stance.Follow => Companion.Stance.Hold,
                    Companion.Stance.Hold => Companion.Stance.Aggressive,
                    _ => Companion.Stance.Follow,
                };
                foreach (var c in allComps) if (c != null) c.SetStance(next);
            }
        }

        // ====================================================================
        // RESOURCE BAR (좌상단 수평 스트립): Wood / Stone / Meat / Food
        // ====================================================================
        private GUIStyle _resStyle;

        private void DrawResourceBar()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            if (_resStyle == null)
                _resStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = UiTheme.TextCream }
                };

            ResourceKind[] kinds =
            {
                ResourceKind.Wood,
                ResourceKind.Stone,
                ResourceKind.Meat,
                ResourceKind.Food,
                ResourceKind.Frostbloom
            };

            var panel = HudLayout.TopRightResources(kinds.Length);

            float x = panel.x + HudStyleConfig.PanelPadding;
            float y = panel.y + HudStyleConfig.PanelPadding;
            float rowH = HudStyleConfig.ResourceCellHeight;
            float textX = x + HudStyleConfig.IconMedium + 7f;
            float textW = panel.width - HudStyleConfig.PanelPadding * 2f - HudStyleConfig.IconMedium - 7f;

            for (int i = 0; i < kinds.Length; i++)
            {
                var k = kinds[i];
                int cur = session.Resources.Get(k);
                int cap = session.Resources.GetCap(k);
                float rowY = y + i * rowH;

                DrawHudIcon(new Rect(x, rowY + 4f, HudStyleConfig.IconMedium, HudStyleConfig.IconMedium), ResourceIconKey(k));

                var oldC = GUI.contentColor;
                GUI.contentColor = cur >= cap ? UiTheme.TextDanger : UiTheme.TextCream;
                GUI.Label(new Rect(textX, rowY + 5f, textW, rowH - 4f), $"{cur}/{cap}", _resStyle);
                GUI.contentColor = oldC;

            }
        }

        // ====================================================================
        // WAVE / STANCE BAR: 웨이브 정보 + 동료 스탠스 (좌상단, 자원 아래)
        // ====================================================================
        private void DrawSettlementGoals()
        {
            var gm = SettlementGoalManager.Instance;
            if (gm == null || gm.Goals == null || gm.Goals.Count == 0) return;

            const int W = 330;
            const int H = 122;
            var panel = new Rect(Screen.width - W - 12, 91, W, H);

            var titleStyle = new GUIStyle(_section)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = UiTheme.TextGold }
            };
            var lineStyle = new GUIStyle(_labelSubtle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
            };

            GUI.Label(new Rect(panel.x + 10, panel.y + 6, panel.width - 20, 18), "Mid Goals", titleStyle);

            int shown = 0;
            float y = panel.y + 28;
            foreach (var g in gm.Goals)
            {
                if (g.Completed) continue;
                GUI.Label(new Rect(panel.x + 12, y, panel.width - 24, 16), g.Title, _label);
                GUI.Label(new Rect(panel.x + 12, y + 16, panel.width - 24, 14), g.Progress, lineStyle);
                y += 32;
                shown++;
                if (shown >= 3) break;
            }

            if (shown == 0)
            {
                GUI.Label(new Rect(panel.x + 12, panel.y + 42, panel.width - 24, 22), "All mid goals complete", _labelSubtle);
            }
        }

        private void DrawWaveStanceBar()
        {
            DrawSettlementGoals();
            var session = GameSession.Instance;
            if (session == null) return;

            const int W = 280, H = 108;
            // ResourceBar(63+24+4=91) 아래
            var panel = new Rect(12, 91, W, H);

            int innerX = (int)panel.x + 10;
            int innerW = W - 20;
            int y = (int)panel.y + 6;

            // 페이즈별 컨텍스트 라인
            if (Night != null && Night.CurrentPhase == Phase.Night)
            {
                var oldC = GUI.contentColor;
                GUI.contentColor = new Color(0.95f, 0.5f, 0.5f);
                DrawHudIcon(new Rect(innerX, y + 1, 18, 18), "wave");
                GUI.Label(new Rect(innerX + 24, y, innerW - 24, 22),
                    $"활성 {Night.ActiveZombies}  ·  대기 {Night.WavePending}", _section);
                GUI.contentColor = oldC;
                y += 24;
                if (Night.IsBlizzard)
                {
                    GUI.contentColor = new Color(0.55f, 0.85f, 1f);
                    DrawHudIcon(new Rect(innerX, y + 1, 18, 18), "blizzard");
                    GUI.Label(new Rect(innerX + 24, y, innerW - 24, 22), "Blizzard", _section);
                    GUI.contentColor = oldC;
                    y += 24;
                }
            }
            else if (session.LastFoodShortage > 0)
            {
                var oldC = GUI.contentColor;
                GUI.contentColor = UiTheme.TextDanger;
                GUI.Label(new Rect(innerX, y, innerW, 22),
                    $"⚠ 식량 부족 {session.LastFoodShortage}", _section);
                GUI.contentColor = oldC;
                y += 24;
            }
            else
            {
                DrawHudIcon(new Rect(innerX, y + 1, 18, 18), "home");
                GUI.Label(new Rect(innerX + 24, y, innerW - 24, 22), "Village stable", _labelSubtle);
                y += 24;
            }

            // 인구 표시 (현재 동료 수 / 최대 수용)
            {
                int have = RecruitableNpc.CurrentCompanionCount();
                int cap  = RecruitableNpc.VillageCapacity();
                var oldC = GUI.contentColor;
                GUI.contentColor = (have >= cap) ? UiTheme.TextDanger : UiTheme.TextCream;
                DrawHudIcon(new Rect(innerX, y, 18, 18), "population");
                GUI.Label(new Rect(innerX + 24, y, innerW - 24, 20), $"{have}/{cap}", _section);
                GUI.contentColor = oldC;
                y += 22;
            }

            {
                var compsForFood = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
                int foodNeed = GameSession.FoodNeededForCompanions(compsForFood);
                DrawHudIcon(new Rect(innerX + 1, y, 16, 16), "daily-food");
                GUI.Label(new Rect(innerX + 24, y, innerW - 24, 18), $"일일 식량 -{foodNeed}", _labelSubtle);
                y += 20;
            }

            if (session.PregnancyActive)
            {
                int currentDay = session.Cycle != null ? session.Cycle.Day : 0;
                int left = session.PregnancyDaysRemaining(currentDay);
                string parents = string.IsNullOrEmpty(session.LastPregnancyParents) ? "" : $" · {session.LastPregnancyParents}";
                DrawHudIcon(new Rect(innerX + 1, y, 16, 16), "pregnancy");
                GUI.Label(new Rect(innerX + 24, y, innerW - 24, 18), $"출산까지 {left}일{parents}", _labelSubtle);
                y += 20;
            }
            else if (session.LastPregnancyStarted)
            {
                DrawHudIcon(new Rect(innerX + 1, y, 16, 16), "pregnancy");
                GUI.Label(new Rect(innerX + 24, y, innerW - 24, 18), "임신 소식", _labelSubtle);
                y += 20;
            }

            // 동료 스탠스 토글 (하단 줄)
            var allComps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int liveCount = 0;
            Companion.Stance majority = Companion.Stance.Follow;
            foreach (var c in allComps)
            {
                if (c == null || c.IsDead || c.CurrentMode == Companion.Mode.Hiding) continue;
                liveCount++;
                majority = c.CurrentStance;
            }
            string sLabel = majority switch
            {
                Companion.Stance.Follow => "Follow",
                Companion.Stance.Hold => "🛡 사수",
                Companion.Stance.Aggressive => "⚔ 공세",
                _ => "",
            };
            int btnY = (int)panel.yMax - 22;
            var stanceRect = new Rect(innerX, btnY, innerW, 18);
            if (UiTheme.Button(stanceRect, $"동료 {sLabel} ({liveCount})", _smallBtn, liveCount > 0))
            {
                var next = majority switch
                {
                    Companion.Stance.Follow => Companion.Stance.Hold,
                    Companion.Stance.Hold => Companion.Stance.Aggressive,
                    _ => Companion.Stance.Follow,
                };
                foreach (var c in allComps) if (c != null) c.SetStance(next);
            }
            DrawHudIcon(new Rect(innerX + 6, btnY + 1, 16, 16), StanceIconKey(majority));
        }

        // ====================================================================
        // BUILD HOTBAR (bottom-center): 6개 빌드 아이콘 버튼
        // ====================================================================
        private struct BuildSlot
        {
            public string Icon, Name;
            public int CostWood, CostStone;
            public Color Color;
            public BuildingKind Kind;
            public bool Available;
        }

        /// <summary>건물 종류별로 이미 지은 개수 — 비용 점증에 사용.</summary>
        private static int CountBuiltOfKind(BuildingKind k)
        {
            int n = 0;
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bs) if (b != null && b.Kind == k && b.CurrentHp > 0) n++;
            return n;
        }

        private static float CostMul(BuildingKind k) => 1f + 0.35f * CountBuiltOfKind(k);
        private static int Inflate(int baseCost, BuildingKind k)
            => Mathf.RoundToInt(baseCost * CostMul(k));
        private static ResourceCost BuildCost(BuildingKind k)
            => BuildingUpgradeRules.BuildCost(k, CostMul(k));

        private void DrawBuildHotbar()
        {
            var session = GameSession.Instance;
            if (session == null || Player == null) return;

            int wood = session.Resources.Get(ResourceKind.Wood);
            int stone = session.Resources.Get(ResourceKind.Stone);

            bool farmAllowed = FarmBuilding.CurrentFarmCount() < FarmBuilding.MaxFarmsAllowed();

            BuildSlot[] slots = {
                new BuildSlot { Icon = "F", Name = "Campfire",
                    CostWood = BuildCost(BuildingKind.Campfire).Wood,
                    CostStone = BuildCost(BuildingKind.Campfire).Stone,
                    Kind = BuildingKind.Campfire, Available = true,
                    Color = new Color(1f, 0.55f, 0.2f) },
                new BuildSlot { Icon = "B", Name = "Brazier",
                    CostWood = BuildCost(BuildingKind.Brazier).Wood,
                    CostStone = BuildCost(BuildingKind.Brazier).Stone,
                    Kind = BuildingKind.Brazier, Available = true,
                    Color = new Color(1f, 0.72f, 0.22f) },
                new BuildSlot { Icon = "H", Name = "House(+4)",
                    CostWood = BuildCost(BuildingKind.House).Wood,
                    CostStone = BuildCost(BuildingKind.House).Stone,
                    Kind = BuildingKind.House, Available = true,
                    Color = new Color(0.85f, 0.6f, 0.4f) },
                new BuildSlot { Icon = "W", Name = "Fence",
                    CostWood = BuildCost(BuildingKind.Fence).Wood,
                    CostStone = BuildCost(BuildingKind.Fence).Stone,
                    Kind = BuildingKind.Fence, Available = true,
                    Color = new Color(0.78f, 0.62f, 0.30f) },
                new BuildSlot { Icon = "S", Name = "Storage",
                    CostWood = BuildCost(BuildingKind.Storage).Wood,
                    CostStone = BuildCost(BuildingKind.Storage).Stone,
                    Kind = BuildingKind.Storage, Available = true,
                    Color = new Color(0.55f, 0.45f, 0.3f) },
                new BuildSlot { Icon = "G", Name = "Seed Store",
                    CostWood = BuildCost(BuildingKind.SeedStorage).Wood,
                    CostStone = BuildCost(BuildingKind.SeedStorage).Stone,
                    Kind = BuildingKind.SeedStorage, Available = true,
                    Color = new Color(0.62f, 0.52f, 0.28f) },
                new BuildSlot { Icon = farmAllowed ? "P" : "!",
                    Name = farmAllowed ? "Farm" : "Need Seed Store",
                    CostWood = BuildCost(BuildingKind.Farm).Wood,
                    CostStone = BuildCost(BuildingKind.Farm).Stone,
                    Kind = BuildingKind.Farm, Available = farmAllowed,
                    Color = new Color(0.5f, 0.85f, 0.35f) },
                new BuildSlot { Icon = "T", Name = "Watchtower",
                    CostWood = BuildCost(BuildingKind.Watchtower).Wood,
                    CostStone = BuildCost(BuildingKind.Watchtower).Stone,
                    Kind = BuildingKind.Watchtower, Available = true,
                    Color = new Color(0.6f, 0.85f, 0.55f) },
                new BuildSlot { Icon = "+", Name = "Infirmary",
                    CostWood = BuildCost(BuildingKind.Infirmary).Wood,
                    CostStone = BuildCost(BuildingKind.Infirmary).Stone,
                    Kind = BuildingKind.Infirmary, Available = true,
                    Color = new Color(0.9f, 0.95f, 0.95f) },
                new BuildSlot { Icon = "M", Name = "Hunter Hut",
                    CostWood = BuildCost(BuildingKind.HuntersHut).Wood,
                    CostStone = BuildCost(BuildingKind.HuntersHut).Stone,
                    Kind = BuildingKind.HuntersHut, Available = true,
                    Color = new Color(0.55f, 0.4f, 0.25f) },
                new BuildSlot { Icon = "C", Name = "Carpenter",
                    CostWood = BuildCost(BuildingKind.Carpenter).Wood,
                    CostStone = BuildCost(BuildingKind.Carpenter).Stone,
                    Kind = BuildingKind.Carpenter, Available = true,
                    Color = new Color(0.58f, 0.38f, 0.18f) },
                new BuildSlot { Icon = "A", Name = "Blacksmith",
                    CostWood = BuildCost(BuildingKind.Blacksmith).Wood,
                    CostStone = BuildCost(BuildingKind.Blacksmith).Stone,
                    Kind = BuildingKind.Blacksmith, Available = true,
                    Color = new Color(0.9f, 0.3f, 0.15f) },
                new BuildSlot { Icon = "R", Name = "Training",
                    CostWood = BuildCost(BuildingKind.TrainingCamp).Wood,
                    CostStone = BuildCost(BuildingKind.TrainingCamp).Stone,
                    Kind = BuildingKind.TrainingCamp, Available = true,
                    Color = new Color(0.9f, 0.42f, 0.22f) },
                new BuildSlot { Icon = "O", Name = "Food Store",
                    CostWood = BuildCost(BuildingKind.FoodStorage).Wood,
                    CostStone = BuildCost(BuildingKind.FoodStorage).Stone,
                    Kind = BuildingKind.FoodStorage, Available = true,
                    Color = new Color(0.95f, 0.78f, 0.4f) },
                new BuildSlot { Icon = "L", Name = "Lookout",
                    CostWood = BuildCost(BuildingKind.LookoutPost).Wood,
                    CostStone = BuildCost(BuildingKind.LookoutPost).Stone,
                    Kind = BuildingKind.LookoutPost, Available = true,
                    Color = new Color(0.55f, 0.8f, 1f) },
                new BuildSlot { Icon = "Y", Name = "Sawmill",
                    CostWood = BuildCost(BuildingKind.Sawmill).Wood,
                    CostStone = BuildCost(BuildingKind.Sawmill).Stone,
                    Kind = BuildingKind.Sawmill, Available = true,
                    Color = new Color(0.55f, 0.36f, 0.18f) },
                new BuildSlot { Icon = "U", Name = "Church",
                    CostWood = BuildCost(BuildingKind.Church).Wood,
                    CostStone = BuildCost(BuildingKind.Church).Stone,
                    Kind = BuildingKind.Church, Available = true,
                    Color = new Color(0.78f, 0.78f, 0.95f) },
            };

            const int CellH = 72, Gap = 4;
            int maxPerRow = Mathf.Clamp((Screen.width - 24 + Gap) / (58 + Gap), 1, slots.Length);
            int columns = Mathf.Min(slots.Length, maxPerRow);
            int cellW = Mathf.Clamp((Screen.width - 24 - Gap * (columns - 1)) / columns, 58, 72);
            int rows = Mathf.CeilToInt(slots.Length / (float)columns);
            int y0 = Screen.height - rows * CellH - (rows - 1) * Gap - 12;

            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                int row = i / columns;
                int col = i % columns;
                int countInRow = Mathf.Min(columns, slots.Length - row * columns);
                int totalW = cellW * countInRow + Gap * (countInRow - 1);
                int startX = Screen.width / 2 - totalW / 2;
                int cx = startX + col * (cellW + Gap);
                int y = y0 + row * (CellH + Gap);
                var r = new Rect(cx, y, cellW, CellH);
                bool ok = s.Available && wood >= s.CostWood && stone >= s.CostStone;

                // 셀 배경
                UiTheme.Rect(r, ok ? new Color(0.07f, 0.09f, 0.14f, 0.58f) : new Color(0.08f, 0.08f, 0.1f, 0.42f));
                // 컬러 띠 상단
                UiTheme.Rect(new Rect(r.x, r.y, r.width, 3), new Color(s.Color.r, s.Color.g, s.Color.b, 0.72f));

                // 아이콘 (큰 글자)
                var iconStyle = new GUIStyle(_title) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                var oldC = GUI.contentColor;
                GUI.contentColor = ok ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(r.x, r.y + 10, r.width, 36), s.Icon, iconStyle);

                // 이름
                GUI.contentColor = ok ? UiTheme.TextCream : new Color(1f, 1f, 1f, 0.4f);
                var nameStyle = new GUIStyle(_label) {
                    fontSize = 11, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                GUI.Label(new Rect(r.x, r.y + 46, r.width, 18), s.Name, nameStyle);

                // 비용
                string cost = s.CostStone > 0 ? $"{s.CostWood}W + {s.CostStone}S" : $"{s.CostWood}W";
                GUI.contentColor = ok ? UiTheme.TextSubtle : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                var costStyle = new GUIStyle(_labelSubtle) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(r.x, r.y + 58, r.width, 14), cost, costStyle);
                GUI.contentColor = oldC;

                // 클릭 영역 — 즉시 스폰 대신 배치 모드 진입 (땅 클릭으로 확정)
                if (ok && GUI.Button(r, "", GUIStyle.none))
                {
                    _pendingBuildKind = s.Kind;
                    Sfx.Click();
                }
            }
        }

        // ====================================================================
        // DEBUG CORNER (bottom-right): 디버그 + SFX
        // ====================================================================
        private void DrawDebugCorner()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            const int W = 220, H = 100;
            var panel = new Rect(Screen.width - W - 12, Screen.height - H - 12, W, H);
            UiTheme.Panel(panel);

            int innerX = (int)panel.x + 10;
            int innerW = W - 20;
            int y = (int)panel.y + 10;

            GUI.Label(new Rect(innerX, y, innerW, 16), "Debug", _labelSubtle);
            y += 18;

            int half = (innerW - 6) / 2;
            if (UiTheme.Button(new Rect(innerX, y, half, 24), "▶ 페이즈+1", _smallBtn))
                session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
            if (UiTheme.Button(new Rect(innerX + half + 6, y, half, 24), "Force Night", _smallBtn))
                if (Night != null) Night.StartNight(session.Cycle.Day);
            y += 28;

            if (UiTheme.Button(new Rect(innerX, y, half, 24), "🧟 좀비+1", _smallBtn))
                if (Night != null) Night.SpawnDebugZombie();
            if (UiTheme.Button(new Rect(innerX + half + 6, y, half, 24), "To Day", _smallBtn))
                for (int i = 0; i < 4 && session.Cycle.Phase != Phase.Day; i++)
                    session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
            y += 28;

            // SFX 슬라이더
            GUI.Label(new Rect(innerX, y, 50, 18), "🔊", _label);
            float vol = Sfx.Volume;
            float newVol = GUI.HorizontalSlider(new Rect(innerX + 40, y + 5, innerW - 90, 12), vol, 0f, 1f);
            if (Mathf.Abs(newVol - vol) > 0.005f) Sfx.Volume = newVol;
            GUI.Label(new Rect(innerX + innerW - 40, y, 40, 18), $"{(newVol * 100):F0}%", _labelSubtle);
        }

        // ====================================================================
        // 우측: 자원 / 사이클 / 디버그
        // ====================================================================
        private void DrawRightPanel()
        {
            const int W = 270;
            var panel = new Rect(Screen.width - W - 12, 90, W, 440);
            UiTheme.Panel(panel);
            UiTheme.TitleBar(panel, "  자원  ", _title);

            int y = (int)panel.y + 36;
            int innerX = (int)panel.x + 14;
            int innerW = (int)panel.width - 28;

            var session = GameSession.Instance;
            if (session == null)
            {
                GUI.Label(new Rect(innerX, y, innerW, 22), "GameSession NOT FOUND", _label);
                return;
            }

            void DrawRes(ResourceKind k, string name)
            {
                UiTheme.Icon(new Rect(innerX, y + 4, 16, 16), UiTheme.ResColor(k));
                GUI.Label(new Rect(innerX + 24, y, 110, 22), name, _label);
                int cur = session.Resources.Get(k);
                int cap = session.Resources.GetCap(k);
                var color = cur >= cap ? UiTheme.TextDanger : UiTheme.TextCream;
                var oldC = GUI.contentColor;
                GUI.contentColor = color;
                GUI.Label(new Rect(innerX + 134, y, innerW - 134, 22), $"{cur} / {cap}", _label);
                GUI.contentColor = oldC;
                y += 22;
            }

            DrawRes(ResourceKind.Wood, "Wood");
            DrawRes(ResourceKind.Stone, "Stone");
            DrawRes(ResourceKind.Meat, "Meat");
            DrawRes(ResourceKind.Food, "Food");

            UiTheme.Separator(new Rect(innerX, y + 4, innerW, 1));
            y += 10;

            // 라이브 점수 / 처치 (페이즈 정보는 상단 시계 위젯에서 표시)
            var scoreOldC = GUI.contentColor;
            GUI.contentColor = UiTheme.TextGold;
            GUI.Label(new Rect(innerX, y, 110, 22), $"점수 {session.Score}", _section);
            GUI.contentColor = UiTheme.TextSubtle;
            GUI.Label(new Rect(innerX + 120, y + 2, innerW - 120, 20), $"킬 {session.TotalKills}  ·  손실 {session.CompanionsLost}", _labelSubtle);
            GUI.contentColor = scoreOldC;
            y += 26;

            if (session.LastFoodShortage > 0)
            {
                var oldC = GUI.contentColor;
                GUI.contentColor = UiTheme.TextDanger;
                GUI.Label(new Rect(innerX, y, innerW, 20), $"⚠ 식량 부족 {session.LastFoodShortage}", _section);
                GUI.contentColor = oldC;
                y += 22;
            }

            if (Night != null)
            {
                GUI.Label(new Rect(innerX, y, innerW, 18),
                    $"좀비 {Night.ActiveZombies}  대기 {Night.WavePending}", _labelSubtle);
                y += 20;
                if (Night.IsBlizzard)
                {
                    var oldC = GUI.contentColor;
                    GUI.contentColor = new Color(0.55f, 0.85f, 1f);
                    GUI.Label(new Rect(innerX, y, innerW, 20), "❄ BLIZZARD", _section);
                    GUI.contentColor = oldC;
                    y += 22;
                }
            }

            // ──── 디버그 버튼 ────
            UiTheme.Separator(new Rect(innerX, y + 4, innerW, 1));
            y += 10;
            GUI.Label(new Rect(innerX, y, innerW, 18), "Debug", _labelSubtle);
            y += 18;

            int half = (innerW - 8) / 2;
            if (UiTheme.Button(new Rect(innerX, y, half, 26), "▶ 페이즈 +1", _smallBtn))
            {
                session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
            }
            if (UiTheme.Button(new Rect(innerX + half + 8, y, half, 26), "Force Night", _smallBtn))
            {
                if (Night != null) Night.StartNight(session.Cycle.Day);
            }
            y += 30;

            if (UiTheme.Button(new Rect(innerX, y, half, 26), "🧟 좀비 +1", _smallBtn))
            {
                if (Night != null) Night.SpawnDebugZombie();
            }
            if (UiTheme.Button(new Rect(innerX + half + 8, y, half, 26), "To Day", _smallBtn))
            {
                for (int i = 0; i < 4 && session.Cycle.Phase != Phase.Day; i++)
                {
                    session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
                }
            }
            y += 30;

            // SFX 볼륨 슬라이더
            GUI.Label(new Rect(innerX, y, 70, 20), $"🔊 SFX", _label);
            float vol = Sfx.Volume;
            float newVol = GUI.HorizontalSlider(new Rect(innerX + 70, y + 6, innerW - 110, 12), vol, 0f, 1f);
            if (Mathf.Abs(newVol - vol) > 0.005f) Sfx.Volume = newVol;
            GUI.Label(new Rect(innerX + innerW - 36, y, 36, 20), $"{(newVol * 100):F0}%", _labelSubtle);
        }

        // ====================================================================
        // 월드 공간 버튼들
        // ====================================================================
        // 손상된 건물 근처에 수리 버튼 — 클릭당 wood 1 소모, MaxHp 의 20% 회복.
        // 플레이어 또는 동료가 3.5u 안에 있어야 표시.
        private void DrawContextActionPanel()
        {
            if (_hudMode == HudMode.Build) return;
            if (Player == null) return;
            var session = GameSession.Instance;
            if (session == null) return;

            var actions = new System.Collections.Generic.List<ContextAction>();
            AddRepairAction(actions, session);
            AddRefuelAction(actions, session);
            AddUpgradeAction(actions, session);
            AddFenceActions(actions, session);
            AddFarmActions(actions);
            AddGatherAction(actions, ResourceKind.Wood, "Chop wood");
            AddGatherAction(actions, ResourceKind.Stone, "Mine stone");
            DrawContextActions(actions);
        }

        private void DrawContextActions(System.Collections.Generic.List<ContextAction> actions)
        {
            if (actions == null || actions.Count == 0) return;
            actions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            int count = Mathf.Min(actions.Count, HudStyleConfig.ContextActionLimit);
            var panel = HudLayout.BottomCenterContext(count);

            float x = panel.x + HudStyleConfig.PanelPadding;
            float y = panel.y + HudStyleConfig.PanelPadding;
            float w = panel.width - HudStyleConfig.PanelPadding * 2f;
            for (int i = 0; i < count; i++)
            {
                var action = actions[i];
                var rect = new Rect(x, y, w, HudStyleConfig.ContextButtonHeight);
                if (UiTheme.Button(rect, action.Label, _smallBtn, action.Enabled))
                {
                    action.Callback?.Invoke();
                }
                y += HudStyleConfig.ContextButtonHeight + HudStyleConfig.PanelGap;
            }
        }

        private void AddRepairAction(System.Collections.Generic.List<ContextAction> actions, GameSession session)
        {
            Building best = FindNearbyDamagedBuilding(3.5f);
            if (best == null) return;
            const int Cost = 1;
            int healAmount = Mathf.Max(1, best.MaxHp / 5);
            bool canAfford = session.Resources.Get(ResourceKind.Wood) >= Cost;
            string label = canAfford ? $"Repair +{healAmount} HP ({Cost} Wood)" : "Need Wood to repair";
            actions.Add(new ContextAction(20, label, canAfford, () =>
            {
                if (session.Resources.Spend(ResourceKind.Wood, Cost))
                {
                    best.RepairHp(healAmount);
                    Sfx.Build();
                }
            }));
        }

        private void AddUpgradeAction(System.Collections.Generic.List<ContextAction> actions, GameSession session)
        {
            Building best = FindNearbyUpgradeableBuilding(3.5f);
            if (best == null) return;
            ResourceCost cost = best.NextUpgradeCost();
            bool ok = cost.CanPay(session.Resources);
            string effect = BuildingUpgradeRules.UpgradeSummary(best.Kind, best.Level + 1);
            string label = ok ? $"Upgrade Lv.{best.Level + 1} {effect} ({cost})" : $"Need resources for Lv.{best.Level + 1} ({cost})";
            actions.Add(new ContextAction(30, label, ok, () =>
            {
                if (best.TryUpgrade(session.Resources)) Sfx.Build();
            }));
        }

        private void AddRefuelAction(System.Collections.Generic.List<ContextAction> actions, GameSession session)
        {
            CampfireAura best = FindNearbyCampfireNeedingFuel(3.5f);
            if (best == null) return;
            const int Cost = 1;
            const float FuelAdd = 30f;
            bool ok = session.Resources.Get(ResourceKind.Wood) >= Cost;
            int fuelPct = Mathf.RoundToInt(best.Fuel / best.MaxFuel * 100f);
            string label = ok ? $"Refuel +{(int)FuelAdd} ({Cost} Wood) {fuelPct}%" : $"Need Wood to refuel {fuelPct}%";
            actions.Add(new ContextAction(20, label, ok, () =>
            {
                if (session.Resources.Spend(ResourceKind.Wood, Cost))
                {
                    best.AddFuel(FuelAdd);
                    Sfx.Build();
                }
            }));
        }

        private void AddFenceActions(System.Collections.Generic.List<ContextAction> actions, GameSession session)
        {
            if (session.Cycle == null || session.Cycle.Phase != Phase.Day) return;
            Vector3 center = new Vector3(GameConstants.VillageCenterX, GameConstants.VillageCenterY, 0f);
            if (Vector2.Distance(Player.transform.position, center) > VillageStarter.CurrentHalfSize + 2.5f) return;
            if (FenceWorkCrew.IsActive)
            {
                actions.Add(new ContextAction(40, "Fence crew working", false, null));
                return;
            }

            int damaged = VillageStarter.CountDamagedFences();
            int missing = VillageStarter.CountMissingOuterFences(center);
            int wood = session.Resources.Get(ResourceKind.Wood);
            int healAmount = Mathf.Max(10, BuildingUpgradeRules.BaseHp(BuildingKind.Fence, BalanceConfig.Instance) * 3);

            if (damaged > 0)
            {
                int repairCost = Mathf.Max(4, Mathf.CeilToInt(damaged * 0.35f));
                bool ok = wood >= repairCost;
                actions.Add(new ContextAction(40, ok ? $"Repair fences x{damaged} ({repairCost} Wood)" : $"Need {repairCost} Wood for fences", ok, () =>
                {
                    if (!session.Resources.Spend(ResourceKind.Wood, repairCost)) return;
                    int crew = FenceWorkCrew.Begin(FenceWorkCrew.JobKind.Repair, center, healAmount);
                    if (crew <= 0)
                    {
                        int repaired = VillageStarter.RepairAllFences(healAmount);
                        GameFeel.FloatText(center, $"Fences repaired x{repaired}", new Color(0.65f, 1f, 0.65f));
                        Sfx.Build();
                    }
                }));
            }

            if (missing > 0)
            {
                int rebuildCost = Mathf.Max(6, missing);
                bool ok = wood >= rebuildCost;
                actions.Add(new ContextAction(40, ok ? $"Rebuild fence x{missing} ({rebuildCost} Wood)" : $"Need {rebuildCost} Wood to rebuild", ok, () =>
                {
                    if (!session.Resources.Spend(ResourceKind.Wood, rebuildCost)) return;
                    int crew = FenceWorkCrew.Begin(FenceWorkCrew.JobKind.Rebuild, center, healAmount);
                    if (crew <= 0)
                    {
                        int built = VillageStarter.RebuildMissingOuterFences(center);
                        GameFeel.FloatText(center, $"Fences rebuilt x{built}", new Color(0.8f, 0.95f, 1f));
                        Sfx.Build();
                    }
                }));
            }
        }

        private void AddFarmActions(System.Collections.Generic.List<ContextAction> actions)
        {
            var farms = Object.FindObjectsByType<FarmBuilding>(FindObjectsSortMode.None);
            FarmBuilding best = null;
            float bestDist = 3f;
            foreach (var farm in farms)
            {
                if (farm == null || Player == null) continue;
                float d = Vector2.Distance(Player.transform.position, farm.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = farm;
                }
            }
            if (best == null) return;

            if (best.CanChangeCrop())
            {
                actions.Add(new ContextAction(48, $"Crop: {best.CropLabel()}", true, () => best.CycleCrop()));
            }

            if (best.HarvestReady)
            {
                int yield = best.EstimatedYield();
                actions.Add(new ContextAction(50, $"Harvest +{yield}", true, () => best.Harvest()));
            }
            else if (best.Workers.Count < best.MaxWorkers)
            {
                actions.Add(new ContextAction(50, $"{best.CropLabel()} - Farmer {best.Workers.Count}/{best.MaxWorkers}", true, () =>
                {
                    var c = FindNearestFreeCompanion(best.transform.position);
                    if (c != null) best.TryAssignWorker(c);
                }));
            }
        }

        private void AddGatherAction(System.Collections.Generic.List<ContextAction> actions, ResourceKind kind, string label)
        {
            if (Player == null) return;
            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            var node = FindGatherableWithUnitsNearby(kind, 3.5f, companions);
            if (node == null) return;
            float playerDist = Vector2.Distance(Player.transform.position, node.transform.position);
            bool playerNear = playerDist <= 3.5f;
            var nearbyCompanions = new System.Collections.Generic.List<Companion>();
            foreach (var c in companions)
            {
                if (c == null || c.CurrentMode == Companion.Mode.Hiding || c.CurrentMode == Companion.Mode.Farming) continue;
                if (Vector2.Distance(c.transform.position, node.transform.position) <= 3.5f) nearbyCompanions.Add(c);
            }
            int totalWorkers = (playerNear ? 1 : 0) + nearbyCompanions.Count;
            if (totalWorkers == 0) return;
            actions.Add(new ContextAction(60, $"{label} ({totalWorkers})", true, () => StartGatherContext(node, playerNear, nearbyCompanions)));
        }

        private void StartGatherContext(Gatherable node, bool playerNear, System.Collections.Generic.List<Companion> nearbyCompanions)
        {
            nearbyCompanions.Sort((a, b) =>
                Vector2.Distance(a.transform.position, node.transform.position)
                .CompareTo(Vector2.Distance(b.transform.position, node.transform.position)));
            int compCap = Mathf.Min(nearbyCompanions.Count, 2);
            if (playerNear && Gather != null) Gather.StartGathering(node);
            for (int i = 0; i < compCap; i++) nearbyCompanions[i].AssignGather(node);
        }

        private Building FindNearbyDamagedBuilding(float range)
        {
            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            Building best = null;
            float bestDist = float.MaxValue;
            Vector3 ppos = Player.transform.position;
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0 || b.CurrentHp >= b.MaxHp) continue;
                if (!IsAnyUnitNear(b.transform.position, range, companions)) continue;
                float d = Vector2.Distance(ppos, b.transform.position);
                if (d < bestDist) { bestDist = d; best = b; }
            }
            return best;
        }

        private Building FindNearbyUpgradeableBuilding(float range)
        {
            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            Building best = null;
            float bestDist = float.MaxValue;
            Vector3 ppos = Player.transform.position;
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0 || b.Level >= BuildingUpgradeRules.MaxLevel) continue;
                if (!IsAnyUnitNear(b.transform.position, range, companions)) continue;
                float d = Vector2.Distance(ppos, b.transform.position);
                if (d < bestDist) { bestDist = d; best = b; }
            }
            return best;
        }

        private CampfireAura FindNearbyCampfireNeedingFuel(float range)
        {
            var auras = Object.FindObjectsByType<CampfireAura>(FindObjectsSortMode.None);
            CampfireAura best = null;
            float bestDist = range;
            Vector3 ppos = Player.transform.position;
            foreach (var a in auras)
            {
                if (a == null || a.Fuel >= a.MaxFuel - 0.5f) continue;
                float d = Vector2.Distance(ppos, a.transform.position);
                if (d < bestDist) { bestDist = d; best = a; }
            }
            return best;
        }

        private bool IsAnyUnitNear(Vector3 pos, float range, Companion[] companions)
        {
            if (Player != null && Vector2.Distance(Player.transform.position, pos) <= range) return true;
            foreach (var c in companions)
            {
                if (c == null || c.IsDead) continue;
                if (Vector2.Distance(c.transform.position, pos) <= range) return true;
            }
            return false;
        }

        private void DrawWorldRepairButton()
        {
            if (Player == null) return;
            var session = GameSession.Instance;
            if (session == null) return;

            const float Range = 3.5f;
            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);

            // 플레이어 또는 동료 근처에 손상된 건물이 있으면 가장 가까운 것 선택
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            Building best = null;
            float bestDist = float.MaxValue;
            Vector3 ppos = Player.transform.position;
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0) continue;
                if (b.CurrentHp >= b.MaxHp) continue; // 풀체력은 스킵
                bool anyNear = Vector2.Distance(ppos, b.transform.position) <= Range;
                if (!anyNear)
                {
                    foreach (var c in companions)
                    {
                        if (c == null || c.IsDead) continue;
                        if (Vector2.Distance(c.transform.position, b.transform.position) <= Range)
                        { anyNear = true; break; }
                    }
                }
                if (!anyNear) continue;
                float d = Vector2.Distance(ppos, b.transform.position);
                if (d < bestDist) { bestDist = d; best = b; }
            }
            if (best == null) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(best.transform.position + new Vector3(0f, 1.0f, 0f));
            if (sp.z < 0) return;
            float guiY = Screen.height - sp.y;

            int wood = session.Resources.Get(ResourceKind.Wood);
            const int Cost = 1;
            bool canAfford = wood >= Cost;
            int healAmount = Mathf.Max(1, best.MaxHp / 5);

            string label = canAfford
                ? $"🔨 수리  +{healAmount} HP  ({Cost} Wood)"
                : "Need Wood to repair";
            var rect = new Rect(sp.x - 80, guiY - 14, 160, 28);
            var bigBtn = new GUIStyle(_btn) { fontSize = 12, fontStyle = FontStyle.Bold };
            if (UiTheme.Button(rect, label, bigBtn, canAfford))
            {
                if (session.Resources.Spend(ResourceKind.Wood, Cost))
                {
                    best.RepairHp(healAmount);
                    Sfx.Build();
                }
            }
        }

        private void DrawVillageFenceMaintenance(GameSession session)
        {
            if (Player == null || session == null || session.Resources == null || session.Cycle == null) return;
            if (session.Cycle.Phase != Phase.Day) return;

            Vector3 center = new Vector3(GameConstants.VillageCenterX, GameConstants.VillageCenterY, 0f);
            float dist = Vector2.Distance(Player.transform.position, center);
            if (dist > VillageStarter.CurrentHalfSize + 2.5f) return;

            int damaged = VillageStarter.CountDamagedFences();
            int missing = VillageStarter.CountMissingOuterFences(center);
            if (damaged <= 0 && missing <= 0) return;

            int wood = session.Resources.Get(ResourceKind.Wood);
            int repairCost = Mathf.Max(4, Mathf.CeilToInt(damaged * 0.35f));
            int rebuildCost = Mathf.Max(6, missing);
            int healAmount = Mathf.Max(10, BuildingUpgradeRules.BaseHp(BuildingKind.Fence, BalanceConfig.Instance) * 3);

            const int W = 300;
            int H = 30 + (damaged > 0 ? 26 : 0) + (missing > 0 ? 26 : 0);
            var panel = new Rect(Screen.width / 2f - W / 2f, 112, W, H);
            UiTheme.Panel(panel);

            var titleStyle = new GUIStyle(_section) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(panel.x + 8, panel.y + 6, panel.width - 16, 18), "Fence Works", titleStyle);

            if (FenceWorkCrew.IsActive)
            {
                GUI.Label(new Rect(panel.x + 10, panel.y + 30, panel.width - 20, 18), "Crew is repairing the fence", _labelSubtle);
                return;
            }

            int y = (int)panel.y + 28;
            if (damaged > 0)
            {
                bool ok = wood >= repairCost;
                string label = ok
                    ? $"Bulk repair {damaged} fences ({repairCost} Wood)"
                    : $"Need {repairCost} Wood for bulk repair";
                if (UiTheme.Button(new Rect(panel.x + 10, y, panel.width - 20, 22), label, _smallBtn, ok))
                {
                    if (session.Resources.Spend(ResourceKind.Wood, repairCost))
                    {
                        int crew = FenceWorkCrew.Begin(FenceWorkCrew.JobKind.Repair, center, healAmount);
                        if (crew > 0)
                        {
                            GameFeel.FloatText(center, $"Fence crew x{crew}", new Color(0.75f, 1f, 0.75f));
                        }
                        else
                        {
                            int repaired = VillageStarter.RepairAllFences(healAmount);
                            GameFeel.FloatText(center, $"Fences repaired x{repaired}", new Color(0.65f, 1f, 0.65f));
                            Sfx.Build();
                        }
                    }
                }
                y += 26;
            }

            if (missing > 0)
            {
                bool ok = wood >= rebuildCost;
                string label = ok
                    ? $"Rebuild outer fence x{missing} ({rebuildCost} Wood)"
                    : $"Need {rebuildCost} Wood to rebuild fence";
                if (UiTheme.Button(new Rect(panel.x + 10, y, panel.width - 20, 22), label, _smallBtn, ok))
                {
                    if (session.Resources.Spend(ResourceKind.Wood, rebuildCost))
                    {
                        int crew = FenceWorkCrew.Begin(FenceWorkCrew.JobKind.Rebuild, center, healAmount);
                        if (crew > 0)
                        {
                            GameFeel.FloatText(center, $"Fence crew x{crew}", new Color(0.8f, 0.95f, 1f));
                        }
                        else
                        {
                            int built = VillageStarter.RebuildMissingOuterFences(center);
                            GameFeel.FloatText(center, $"Fences rebuilt x{built}", new Color(0.8f, 0.95f, 1f));
                            Sfx.Build();
                        }
                    }
                }
            }
        }

        private void DrawWorldUpgradeButton()
        {
            if (Player == null) return;
            var session = GameSession.Instance;
            if (session == null) return;
            DrawVillageFenceMaintenance(session);

            const float Range = 3.5f;
            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            var bs = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            Building best = null;
            float bestDist = float.MaxValue;
            Vector3 ppos = Player.transform.position;

            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0) continue;
                if (b.Level >= BuildingUpgradeRules.MaxLevel) continue;

                bool anyNear = Vector2.Distance(ppos, b.transform.position) <= Range;
                if (!anyNear)
                {
                    foreach (var c in companions)
                    {
                        if (c == null || c.IsDead) continue;
                        if (Vector2.Distance(c.transform.position, b.transform.position) <= Range)
                        { anyNear = true; break; }
                    }
                }
                if (!anyNear) continue;

                float d = Vector2.Distance(ppos, b.transform.position);
                if (d < bestDist) { bestDist = d; best = b; }
            }
            if (best == null) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(best.transform.position + new Vector3(0f, 1.35f, 0f));
            if (sp.z < 0) return;
            float guiY = Screen.height - sp.y;

            ResourceCost cost = best.NextUpgradeCost();
            bool ok = cost.CanPay(session.Resources);
            string effect = BuildingUpgradeRules.UpgradeSummary(best.Kind, best.Level + 1);
            string label = ok
                ? $"⬆ Lv.{best.Level + 1} {effect} ({cost})"
                : $"⬆ Lv.{best.Level + 1} 자원 부족 ({cost})";
            var rect = new Rect(sp.x - 120, guiY - 14, 240, 28);
            var bigBtn = new GUIStyle(_btn) { fontSize = 11, fontStyle = FontStyle.Bold };
            if (UiTheme.Button(rect, label, bigBtn, ok))
            {
                if (best.TryUpgrade(session.Resources))
                {
                    Sfx.Build();
                }
            }
        }

        // 모닥불 연료 보충 — 플레이어 3.5u 안의 모닥불에 1 Wood 마다 +30 fuel.
        private void DrawWorldRefuelButton()
        {
            if (Player == null) return;
            var session = GameSession.Instance;
            if (session == null) return;
            var auras = Object.FindObjectsByType<CampfireAura>(FindObjectsSortMode.None);
            CampfireAura best = null;
            float bestDist = 3.5f;
            Vector3 ppos = Player.transform.position;
            foreach (var a in auras)
            {
                if (a == null) continue;
                if (a.Fuel >= a.MaxFuel - 0.5f) continue; // 풀 연료면 버튼 X
                float d = Vector2.Distance(ppos, a.transform.position);
                if (d < bestDist) { bestDist = d; best = a; }
            }
            if (best == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(best.transform.position + new Vector3(0f, 1.0f, 0f));
            if (sp.z < 0) return;
            float guiY = Screen.height - sp.y;

            int wood = session.Resources.Get(ResourceKind.Wood);
            const int Cost = 1;
            const float FuelAdd = 30f;
            bool ok = wood >= Cost;
            int fuelPct = Mathf.RoundToInt(best.Fuel / best.MaxFuel * 100f);
            string label = ok ? $"🔥 장작 +{(int)FuelAdd} ({Cost}W)  연료 {fuelPct}%"
                              : $"🔥 Wood 부족  연료 {fuelPct}%";
            var rect = new Rect(sp.x - 80, guiY - 14, 160, 28);
            var bigBtn = new GUIStyle(_btn) { fontSize = 12, fontStyle = FontStyle.Bold };
            if (UiTheme.Button(rect, label, bigBtn, ok))
            {
                if (session.Resources.Spend(ResourceKind.Wood, Cost))
                {
                    best.AddFuel(FuelAdd);
                    Sfx.Build();
                }
            }
        }

        private void DrawWorldChopButton()
        {
            if (Player == null) return;
            DrawGatherButton(ResourceKind.Wood, "🪓 벌목", 3.5f);
            DrawGatherButton(ResourceKind.Stone, "⛏ 채굴", 3.5f);
        }

        private void DrawGatherButton(ResourceKind kind, string verb, float range)
        {
            // 플레이어 또는 동료가 근처에 있는 가장 가까운 (플레이어 기준) gatherable 찾기.
            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            var node = FindGatherableWithUnitsNearby(kind, range, companions);
            if (node == null) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(node.transform.position + new Vector3(0f, 1.0f, 0f));
            if (sp.z < 0) return;
            float guiY = Screen.height - sp.y;

            // 누가 이 노드 근처에 있나 분류
            float playerDist = Vector2.Distance(Player.transform.position, node.transform.position);
            bool playerNear = playerDist <= range;

            // 사용 가능한 동료 (= Hiding/Farming 아닌) 중 노드 근처
            var nearbyCompanions = new System.Collections.Generic.List<Companion>();
            foreach (var c in companions)
            {
                if (c == null || c.CurrentMode == Companion.Mode.Hiding || c.CurrentMode == Companion.Mode.Farming) continue;
                if (Vector2.Distance(c.transform.position, node.transform.position) <= range)
                    nearbyCompanions.Add(c);
            }

            int totalWorkers = (playerNear ? 1 : 0) + nearbyCompanions.Count;
            if (totalWorkers == 0) return;

            string who = playerNear
                ? (nearbyCompanions.Count > 0 ? $"Player + companions {nearbyCompanions.Count}" : "Player")
                : $"동료 {nearbyCompanions.Count}";
            string label = $"{verb} ({who})";
            var rect = new Rect(sp.x - 80, guiY - 14, 160, 28);
            var bigBtn = new GUIStyle(_btn) { fontSize = 12, fontStyle = FontStyle.Bold };
            if (UiTheme.Button(rect, label, bigBtn))
            {
                // 거리순 정렬, 동료는 최대 2명까지만 (요청)
                nearbyCompanions.Sort((a, b) =>
                    Vector2.Distance(a.transform.position, node.transform.position)
                    .CompareTo(Vector2.Distance(b.transform.position, node.transform.position)));
                int compCap = Mathf.Min(nearbyCompanions.Count, 2);

                if (playerNear && Gather != null)
                {
                    Gather.StartGathering(node);
                    for (int i = 0; i < compCap; i++) nearbyCompanions[i].AssignGather(node);
                }
                else
                {
                    for (int i = 0; i < compCap; i++) nearbyCompanions[i].AssignGather(node);
                }
            }
        }

        /// <summary>지정 종류의 Gatherable 중, 플레이어/동료가 range 안에 있는 것 중 플레이어에게서 가장 가까운 것.</summary>
        private Gatherable FindGatherableWithUnitsNearby(ResourceKind kind, float range, Companion[] companions)
        {
            var all = Object.FindObjectsByType<Gatherable>(FindObjectsSortMode.None);
            Gatherable best = null;
            float bestDistToPlayer = float.MaxValue;
            Vector3 ppos = Player.transform.position;
            foreach (var g in all)
            {
                if (g == null || g.YieldKind != kind) continue;
                if (g.GetComponent<AnimalAi>() != null) continue; // 동물에 붙은 Gatherable 은 제외

                bool anyNear = Vector2.Distance(ppos, g.transform.position) <= range;
                if (!anyNear)
                {
                    foreach (var c in companions)
                    {
                        if (c == null || c.CurrentMode == Companion.Mode.Hiding || c.CurrentMode == Companion.Mode.Farming) continue;
                        if (Vector2.Distance(c.transform.position, g.transform.position) <= range) { anyNear = true; break; }
                    }
                }
                if (!anyNear) continue;

                float dp = Vector2.Distance(ppos, g.transform.position);
                if (dp < bestDistToPlayer) { bestDistToPlayer = dp; best = g; }
            }
            return best;
        }

        private void DrawWorldFarmButtons()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var farms = Object.FindObjectsByType<FarmBuilding>(FindObjectsSortMode.None);
            foreach (var farm in farms)
            {
                if (farm == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(farm.transform.position + new Vector3(0f, 1.0f, 0f));
                if (sp.z < 0) continue;
                float guiX = sp.x;
                float guiY = Screen.height - sp.y;

                if (farm.HarvestReady)
                {
                    int yield = farm.EstimatedYield();
                    var rect = new Rect(guiX - 60, guiY - 26, 120, 22);
                    if (UiTheme.Button(rect, $"Harvest +{yield}", _smallBtn)) farm.Harvest();
                }
                else
                {
                    GUI.Label(new Rect(guiX - 60, guiY - 26, 120, 18),
                        farm.CropLabel(), _labelSubtle);
                }

                if (!farm.HarvestReady && farm.Workers.Count < farm.MaxWorkers && Player != null)
                {
                    if (Vector2.Distance(Player.transform.position, farm.transform.position) < 3f)
                    {
                        var rect2 = new Rect(guiX - 60, guiY - 4, 120, 20);
                        if (UiTheme.Button(rect2, $"Farmer {farm.Workers.Count}/{farm.MaxWorkers}", _smallBtn))
                        {
                            var c = FindNearestFreeCompanion(farm.transform.position);
                            if (c != null) farm.TryAssignWorker(c);
                        }
                    }
                }
            }
        }

        // ====================================================================
        // 영입 다이얼로그 (하단 중앙)
        // ====================================================================
        private void DrawRecruitDialog()
        {
            var npc = NearestRecruitable();
            if (npc == null) return;

            var panel = HudLayout.RecruitDialog();
            float W = panel.width;
            float H = panel.height;
            UiTheme.Panel(panel);

            var sr = npc.GetComponent<SpriteRenderer>();
            var col = sr != null ? sr.color : Color.white;
            var portrait = new Rect(panel.x + 10, panel.y + 10, 88, 88);
            UiTheme.Rect(new Rect(portrait.x - 1, portrait.y - 1, portrait.width + 2, portrait.height + 2), UiTheme.PanelBorder);
            UiTheme.Rect(portrait, col);
            if (sr != null && sr.sprite != null)
            {
                DrawSpriteInRect(sr.sprite, InnerRect(portrait, 6f));
            }

            int tx = (int)panel.x + 106;
            int tw = (int)panel.width - 116;

            GUI.Label(new Rect(tx, panel.y + 8, tw, 20), $"{npc.DisplayNamePublic}  ({npc.Role})", _section);
            GUI.Label(new Rect(tx, panel.y + 28, tw, 32), $"\"{npc.DialogText}\"", _labelSubtle);
            GUI.Label(new Rect(tx, panel.y + 60, tw, 16),
                $"\uC804\uD22C {Stars(npc.CombatRating)}  \uB18D\uC0AC {Stars(npc.FarmRating)}", _labelSubtle);

            int cap = RecruitableNpc.VillageCapacity();
            int have = RecruitableNpc.CurrentCompanionCount();
            bool canRecruit = have < cap;

            var oldC = GUI.contentColor;
            GUI.contentColor = canRecruit ? UiTheme.TextSubtle : UiTheme.TextDanger;
            GUI.Label(new Rect(tx, panel.y + H - 36, 120, 14), $"\uC218\uC6A9 {have}/{cap}", _labelSubtle);
            GUI.contentColor = oldC;

            string label = canRecruit ? "\uC601\uC785 (F)" : "\uAC70\uCC98 \uBD80\uC871";
            if (UiTheme.Button(new Rect(panel.x + W - 180, panel.y + H - 34, 84, 24), label, _smallBtn, canRecruit))
                npc.Recruit();
            if (UiTheme.Button(new Rect(panel.x + W - 90, panel.y + H - 34, 76, 24), "\uAC70\uC808", _smallBtn))
            {
                // Ignore for now; walking away hides the panel.
            }
        }

        private void ShowRecruitCutscene(string displayName, string role, string dialogText)
        {
            _recruitCutName = string.IsNullOrEmpty(displayName) ? "Visitor" : displayName;
            _recruitCutRole = string.IsNullOrEmpty(role) ? "동료" : role;
            _recruitCutDialog = string.IsNullOrEmpty(dialogText) ? "함께하겠습니다." : dialogText;
            _recruitCutPortrait = PortraitForRole(_recruitCutRole);
            _recruitCutLeft = 4.5f;
            if (!_recruitCutPaused)
            {
                _recruitCutPrevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _recruitCutPaused = true;
            }
        }

        private void DrawRecruitCutscene()
        {
            if (_recruitCutLeft <= 0f)
            {
                EndRecruitCutscene();
                return;
            }
            _recruitCutLeft -= Time.unscaledDeltaTime;
            if (Event.current != null && Event.current.type == EventType.MouseDown)
            {
                EndRecruitCutscene();
                Event.current.Use();
                return;
            }

            float fade = Mathf.Clamp01(Mathf.Min(_recruitCutLeft, 4.5f - _recruitCutLeft) / 0.35f);
            var oldC = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);

            UiTheme.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.48f * fade));

            int panelW = Mathf.Min(880, Screen.width - 48);
            int panelH = 210;
            var panel = new Rect(Screen.width / 2f - panelW / 2f, Screen.height - panelH - 32, panelW, panelH);
            UiTheme.Rect(new Rect(panel.x - 2, panel.y - 2, panel.width + 4, panel.height + 4), new Color(0.95f, 0.78f, 0.35f, 0.85f * fade));
            UiTheme.Rect(panel, new Color(0.055f, 0.07f, 0.10f, 0.96f * fade));

            float portraitW = Mathf.Min(260f, panel.width * 0.32f);
            float portraitH = Mathf.Min(360f, Mathf.Max(150f, panel.y - 16f));
            float portraitY = Mathf.Max(16f, panel.y - portraitH + 52f);
            var portraitRect = new Rect(panel.x + 22, portraitY, portraitW, portraitH);
            if (_recruitCutPortrait != null)
                GUI.DrawTexture(InnerRect(portraitRect, 4f), _recruitCutPortrait, ScaleMode.ScaleToFit, true);

            var nameStyle = new GUIStyle(_title) { fontSize = 26, alignment = TextAnchor.MiddleLeft };
            var roleStyle = new GUIStyle(_labelSubtle) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
            var dialogStyle = new GUIStyle(_label) { fontSize = 21, wordWrap = true, alignment = TextAnchor.UpperLeft };
            var hintStyle = new GUIStyle(_labelSubtle) { fontSize = 13, alignment = TextAnchor.MiddleRight };

            float tx = panel.x + portraitW + 52f;
            float tw = panel.width - portraitW - 82f;
            GUI.Label(new Rect(tx, panel.y + 24, tw, 34), $"{_recruitCutName}", nameStyle);
            GUI.Label(new Rect(tx, panel.y + 58, tw, 22), $"새 동료 · {_recruitCutRole}", roleStyle);
            UiTheme.Separator(new Rect(tx, panel.y + 88, tw, 1));
            GUI.Label(new Rect(tx, panel.y + 106, tw, 66), $"\"{_recruitCutDialog}\"", dialogStyle);
            GUI.Label(new Rect(tx, panel.y + panel.height - 34, tw, 22), "클릭해서 닫기", hintStyle);

            GUI.color = oldC;
        }

        private void EndRecruitCutscene()
        {
            _recruitCutLeft = 0f;
            if (_recruitCutPaused)
            {
                bool runeModalOpen = Progression != null && Progression.LevelUpPending;
                bool deathOpen = Player != null && Player.IsDead;
                Time.timeScale = runeModalOpen || deathOpen ? 0f : Mathf.Max(0.0001f, _recruitCutPrevTimeScale);
                _recruitCutPaused = false;
            }
        }

        private Texture2D PortraitForRole(string role)
        {
            string key = role switch
            {
                "아이" => "companion-child-bust",
                "농부" => "companion-aunt-bust",
                "노인" => "companion-aunt-bust",
                _ => "companion-uncle-bust",
            };

            if (SpriteBank.IsChildRole(role)) key = "companion-child-bust";
            else if (SpriteBank.IsFemaleRole(role)) key = "companion-aunt-bust";

            if (_portraitCache.TryGetValue(key, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>($"UI/portraits/{key}");
            _portraitCache[key] = tex;
            return tex;
        }

        // ====================================================================
        // 룬 모달 / 사망 오버레이
        // ====================================================================
        private void DrawRuneModal()
        {
            if (Progression == null || !Progression.LevelUpPending)
            {
                _runeOffer = null;
                if (Time.timeScale == 0f && (Player == null || !Player.IsDead)) Time.timeScale = 1f;
                return;
            }
            if (_runeOffer == null)
                _runeOffer = Progression.PickThreeOffer((uint)(Time.frameCount + Progression.Level * 113));
            Time.timeScale = 0f;

            UiTheme.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.65f));

            var modal = HudLayout.CenterModal(920f, 380f);
            int W = (int)modal.width;

            UiTheme.Panel(modal);
            UiTheme.TitleBar(modal, $"  LEVEL {Progression.Level} RUNE SELECT  ", _title);

            int btnW = 280, btnH = 270, gap = 20;
            int total = btnW * 3 + gap * 2;
            int bx = (int)modal.x + (W - total) / 2;
            int by = (int)modal.y + 60;

            var titleStyle = new GUIStyle(_weapon) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            var bodyStyle = new GUIStyle(_label) { fontSize = 18, wordWrap = true };
            var subStyle = new GUIStyle(_labelSubtle) { fontSize = 15, alignment = TextAnchor.MiddleCenter };

            for (int i = 0; i < _runeOffer.Count; i++)
            {
                var rune = _runeOffer[i];
                var rect = new Rect(bx + i * (btnW + gap), by, btnW, btnH);
                if (UiTheme.Button(rect, "", _btn))
                {
                    Sfx.Click();
                    Progression.ApplyRune(rune);
                    _runeOffer = null;
                    return;
                }
                int curStacks = Progression.GetStacks(rune);
                int next = curStacks + 1;
                bool willMaster = next == PlayerProgression.MaxStacks;
                string suffix = willMaster ? "  ★ MASTER" : (next == 2 ? "  +" : "");
                string title = PlayerProgression.Title(rune) + suffix;

                var icon = RuneIcon(rune);
                if (icon != null)
                {
                    var iconRect = new Rect(rect.x + rect.width / 2f - 40f, rect.y + 18f, 80f, 80f);
                    UiTheme.Rect(new Rect(iconRect.x - 4f, iconRect.y - 4f, iconRect.width + 8f, iconRect.height + 8f),
                        new Color(0.08f, 0.10f, 0.14f, 0.65f));
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                }

                GUI.Label(new Rect(rect.x + 16, rect.y + 104, rect.width - 32, 30), title, titleStyle);
                GUI.Label(new Rect(rect.x + 16, rect.y + 136, rect.width - 32, 22),
                    $"진행 {curStacks}/{PlayerProgression.MaxStacks} → {next}/{PlayerProgression.MaxStacks}", subStyle);
                UiTheme.Separator(new Rect(rect.x + 16, rect.y + 164, rect.width - 32, 1));
                GUI.Label(new Rect(rect.x + 16, rect.y + 178, rect.width - 32, btnH - 188),
                    PlayerProgression.DescribeAt(rune, next), bodyStyle);
            }
        }

        private Texture2D RuneIcon(RuneKind rune)
        {
            if (_runeIconCache.TryGetValue(rune, out var cached)) return cached;

            string name = rune switch
            {
                RuneKind.PoisonBlade => "poison-blade",
                RuneKind.IceArrow => "ice-arrow",
                RuneKind.LightningStrike => "lightning-strike",
                RuneKind.MultiShot => "multi-shot",
                RuneKind.Detonator => "detonator",
                RuneKind.SummonDog => "summon-dog",
                RuneKind.SummonHawk => "summon-hawk",
                RuneKind.Vampirism => "vampirism",
                RuneKind.Thorns => "thorns",
                RuneKind.Pierce => "pierce",
                RuneKind.AllyBoost => "ally-boost",
                RuneKind.ResourceGift => "resource-gift",
                _ => null,
            };

            Texture2D icon = null;
            if (!string.IsNullOrEmpty(name))
                icon = Resources.Load<Texture2D>($"UI/rune_choices/rune-choice-{name}");
            _runeIconCache[rune] = icon;
            return icon;
        }

        private bool _paused;
        private float _preTimeScale = 1f;
        private GUIStyle _pauseTitle;

        private void DrawPauseMenu()
        {
            // 사망/룬모달/튜토리얼이 떠 있을 때는 ESC 무시
            bool blockedByModal = (Player != null && Player.IsDead)
                || (Progression != null && Progression.LevelUpPending)
                || (!_tutorialDismissed && PlayerPrefs.GetInt(TutorialPrefKey, 0) == 0);

            if (Event.current != null && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape && !blockedByModal)
            {
                if (!_paused)
                {
                    _paused = true;
                    _preTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }
                else
                {
                    _paused = false;
                    Time.timeScale = _preTimeScale > 0f ? _preTimeScale : 1f;
                }
                Event.current.Use();
            }

            if (!_paused) return;

            UiTheme.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.7f));

            var modal = HudLayout.CenterModal(360f, 260f);
            int W = (int)modal.width;
            UiTheme.Panel(modal);
            UiTheme.TitleBar(modal, "  PAUSED  ", _title);

            if (_pauseTitle == null)
            {
                _pauseTitle = new GUIStyle(GUI.skin.label) {
                    fontSize = 22, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UiTheme.TextGold } };
            }

            int bx = (int)modal.x + 60;
            int by = (int)modal.y + 60;
            int bw = W - 120;

            if (UiTheme.Button(new Rect(bx, by, bw, 38), "Continue", _btn))
            {
                _paused = false;
                Time.timeScale = _preTimeScale > 0f ? _preTimeScale : 1f;
                Sfx.Click();
            }
            by += 46;
            if (UiTheme.Button(new Rect(bx, by, bw, 38), "Save Now", _btn))
            {
                if (GameSession.Instance != null) GameSession.Instance.SaveNow();
                Sfx.Click();
            }
            by += 46;
            if (UiTheme.Button(new Rect(bx, by, bw, 38), "Restart", _btn))
            {
                Sfx.Click();
                _paused = false;
                Time.timeScale = 1f;
                if (GameSession.Instance != null) GameSession.Instance.HardReset();
            }
        }

        private void DrawDeathOverlay()
        {
            if (Player == null || !Player.IsDead) return;
            Time.timeScale = 0f;
            UiTheme.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.92f));
            GUI.Label(new Rect(0, Screen.height / 2 - 150, Screen.width, 100), "YOU DIED", _bigDeath);

            var s = GameSession.Instance;
            if (s != null)
            {
                int deathDay = s.PlayerDeathDay;
                if (deathDay <= 0)
                {
                    deathDay = s.Cycle != null ? s.Cycle.Day : 1;
                    s.MarkPlayerDied(deathDay);
                }

                var statPanel = HudLayout.CenterModal(360f, 130f);
                UiTheme.Panel(statPanel);

                int sx = (int)statPanel.x;
                int sy = (int)statPanel.y;
                int W = (int)statPanel.width;
                int row = sy + 12;
                int lineH = 22;
                GUI.Label(new Rect(sx + 24, row, W - 48, lineH), $"생존 일수      Day {deathDay}", _section); row += lineH;
                GUI.Label(new Rect(sx + 24, row, W - 48, lineH), $"좀비 처치      {s.TotalKills}", _section); row += lineH;
                GUI.Label(new Rect(sx + 24, row, W - 48, lineH), $"동료 최대      {s.MaxCompanionsAtOnce}", _section); row += lineH;
                GUI.Label(new Rect(sx + 24, row, W - 48, lineH), $"동료 손실      {s.CompanionsLost}", _section); row += lineH + 4;

                var titleStyle = new GUIStyle(_title) { fontSize = 22, normal = { textColor = UiTheme.TextGold } };
                GUI.Label(new Rect(sx, row, W, 28), $"점수  {s.Score}", titleStyle);
            }

            int bx = Screen.width / 2 - 90;
            int by = Screen.height / 2 + 130;
            if (UiTheme.Button(new Rect(bx, by, 180, 46), "다시 시작", _btn))
            {
                Time.timeScale = 1f;
                if (GameSession.Instance != null) GameSession.Instance.HardReset();
            }
        }

        // ====================================================================
        // 헬퍼
        // ====================================================================
        private Texture2D HudIcon(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_hudIconCache.TryGetValue(key, out var cached)) return cached;
            string path = $"UI/hud/hud-{key}";
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null) tex = sprite.texture;
            }
            _hudIconCache[key] = tex;
            return tex;
        }

        private void DrawHudIcon(Rect rect, string key)
        {
            var icon = HudIcon(key);
            if (icon != null)
            {
                GUI.DrawTexture(InnerRect(rect, 1.5f), icon, ScaleMode.ScaleToFit, true);
                return;
            }
            UiTheme.Icon(InnerRect(rect, 1.5f), UiTheme.TextSubtle);
        }

        private static Rect InnerRect(Rect rect, float pad)
        {
            float p = Mathf.Min(pad, rect.width * 0.25f, rect.height * 0.25f);
            return new Rect(rect.x + p, rect.y + p, Mathf.Max(1f, rect.width - p * 2f), Mathf.Max(1f, rect.height - p * 2f));
        }

        private static void DrawSpriteInRect(Sprite sprite, Rect rect)
        {
            if (sprite == null || sprite.texture == null) return;
            Rect tr = sprite.textureRect;
            Rect uv = new Rect(
                tr.x / sprite.texture.width,
                tr.y / sprite.texture.height,
                tr.width / sprite.texture.width,
                tr.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        }

        private static string ResourceIconKey(ResourceKind kind)
        {
            return kind switch
            {
                ResourceKind.Wood => "wood",
                ResourceKind.Stone => "stone",
                ResourceKind.Meat => "meat",
                ResourceKind.Food => "food",
                _ => "stone",
            };
        }

        private static string StanceIconKey(Companion.Stance stance)
        {
            return stance switch
            {
                Companion.Stance.Hold => "hold",
                Companion.Stance.Aggressive => "aggressive",
                _ => "follow",
            };
        }

        private void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = UiTheme.TextCream } };
            _labelSubtle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = UiTheme.TextSubtle } };
            _section = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = UiTheme.TextCream } };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = UiTheme.TextGold } };
            _weapon = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.7f, 0.95f, 1f) } };
            _bigDeath = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = UiTheme.TextDanger } };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
            _smallBtn = new GUIStyle(GUI.skin.button) { fontSize = 11 };
        }

        private static Companion FindNearestFreeCompanion(Vector3 center)
        {
            var all = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            Companion best = null;
            float bestDist = float.MaxValue;
            foreach (var c in all)
            {
                if (c == null) continue;
                if (c.CurrentMode == Companion.Mode.Farming) continue;
                if (c.CurrentMode == Companion.Mode.Hiding) continue;
                float d = Vector2.Distance(center, c.transform.position);
                if (d < bestDist) { best = c; bestDist = d; }
            }
            return best;
        }

        private static RecruitableNpc FindNearestRecruitable(Vector3 center, float range)
        {
            var all = Object.FindObjectsByType<RecruitableNpc>(FindObjectsSortMode.None);
            RecruitableNpc best = null;
            float bestDist = range;
            foreach (var n in all)
            {
                if (n == null) continue;
                float d = Vector2.Distance(center, n.transform.position);
                if (d < bestDist) { best = n; bestDist = d; }
            }
            return best;
        }

        private RecruitableNpc NearestRecruitable()
        {
            if (Player == null) return null;
            return FindNearestRecruitable(Player.transform.position, 3.2f);
        }

        private static string Stars(int value)
        {
            int clamped = Mathf.Clamp(value, 0, 5);
            return new string('★', clamped) + new string('☆', 5 - clamped);
        }

        // ====================================================================
        // Spawn 헬퍼들 — 본체는 BuildingFactory 정적 메서드로 이전됨.
        // SimpleHud 는 단순 위임만 (PrefabGenerator 도 같은 정적 메서드 호출).
        // ====================================================================
        private void SpawnBarricade(Vector3 p) => BuildingFactory.SpawnBarricade(p);
        private void SpawnStorage(Vector3 p) => BuildingFactory.SpawnStorage(p);
        private void SpawnHuntersHut(Vector3 p) => BuildingFactory.SpawnHuntersHut(p);
        private void SpawnInfirmary(Vector3 p) => BuildingFactory.SpawnInfirmary(p);
        private void SpawnHouse(Vector3 p) => BuildingFactory.SpawnHouse(p);
        private void SpawnFarm(Vector3 p) => BuildingFactory.SpawnFarm(p);
        private void SpawnWatchtower(Vector3 p) => BuildingFactory.SpawnWatchtower(p);
        private void SpawnCampfire(Vector3 p) => VillageStarter.SpawnCampfire(p);
        private void SpawnBrazier(Vector3 p) => BuildingFactory.SpawnBrazier(p);
        private void SpawnBlacksmith(Vector3 p) => BuildingFactory.SpawnBlacksmith(p);
        private void SpawnSeedStorage(Vector3 p) => BuildingFactory.SpawnSeedStorage(p);
        private void SpawnCarpenter(Vector3 p) => BuildingFactory.SpawnCarpenter(p);
        private void SpawnTrainingCamp(Vector3 p) => BuildingFactory.SpawnTrainingCamp(p);
        private void SpawnFoodStorage(Vector3 p) => BuildingFactory.SpawnFoodStorage(p);
        private void SpawnLookoutPost(Vector3 p) => BuildingFactory.SpawnLookoutPost(p);
        private void SpawnSawmill(Vector3 p) => BuildingFactory.SpawnSawmill(p);
        private void SpawnChurch(Vector3 p) => BuildingFactory.SpawnChurch(p);
    }
}
