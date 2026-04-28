using UnityEngine;

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

        private GUIStyle _label, _labelSubtle, _title, _section, _weapon, _bigDeath, _btn, _smallBtn;

        // 장비 모드: 근접 / 원거리 / 건축. BuildHotbar 는 건축에서만, 무기는 모드 따라 자동 전환.
        public enum HudMode { Melee, Ranged, Build }
        private HudMode _hudMode = HudMode.Melee;

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
            DrawStatCard();        // 상단 좌측: HP/XP/모드 탭/무기
            DrawResourceBar();     // 상단 우측: 자원 세로 카드
            DrawWaveStanceBar();   // 하단 좌측: 동료 스탠스 + 웨이브 정보
            if (_hudMode == HudMode.Build) DrawBuildHotbar(); // 건축 모드에서만
            DrawDebugCorner();     // 하단 우측: 디버그 + SFX
            DrawWorldChopButton();
            DrawWorldRepairButton();
            DrawWorldRefuelButton();
            DrawWorldFarmButtons();
            DrawRecruitDialog();
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
                fontSize = 22, fontStyle = FontStyle.Bold,
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
            int wood = Inflate(BaseWoodCost(kind), kind);
            int stone = kind == BuildingKind.Watchtower ? Inflate(4, kind) : 0;

            if (session.Resources.Get(ResourceKind.Wood) < wood) return;
            if (stone > 0 && session.Resources.Get(ResourceKind.Stone) < stone) return;

            if (!session.Resources.Spend(ResourceKind.Wood, wood)) return;
            if (stone > 0 && !session.Resources.Spend(ResourceKind.Stone, stone)) return;

            // ConstructionSite 스폰
            SpawnConstructionSite(kind, wp);
            _pendingBuildKind = null;
            Sfx.Build();
        }

        private static int BaseWoodCost(BuildingKind k) => k switch
        {
            BuildingKind.Campfire => 5,
            BuildingKind.House => 6,
            BuildingKind.Fence => 1,
            BuildingKind.Storage => 8,
            BuildingKind.Farm => 6,
            BuildingKind.Watchtower => 8,
            BuildingKind.Infirmary => 7,
            _ => 5,
        };

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
                    fontSize = 36, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.95f, 0.7f) }
                };
            }
            float textA = intensity / 0.55f;
            var oldC = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.95f, 0.7f, textA);
            GUI.Label(new Rect(0, Screen.height / 2 - 24, Screen.width, 48), "☀  아침이 밝았다", _dawnStyle);
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
                    fontSize = 28, fontStyle = FontStyle.Bold,
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

        private void OnEnable()
        {
            _unsubE = EventBus.Instance.Subscribe<EveningStartedPayload>(p => ShowBanner($"Day {p.Day}  🌅  저녁"));
            _unsubN = EventBus.Instance.Subscribe<NightStartedPayload>(p => ShowBanner($"Day {p.Day}  🌙  밤이 찾아옵니다"));
            _unsubD = EventBus.Instance.Subscribe<DawnStartedPayload>(p => ShowBanner($"Day {p.Day}  🌄  새벽"));
            _unsubA = EventBus.Instance.Subscribe<DayStartedPayload>(p => ShowBanner($"Day {p.Day}  ☀  새 날"));
            HookFadeEvents();
        }

        private void OnDisable()
        {
            _unsubE?.Invoke(); _unsubN?.Invoke(); _unsubD?.Invoke(); _unsubA?.Invoke();
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
                Phase.Day => $"Day {s.Cycle.Day}  ☀  새 날",
                Phase.Evening => $"Day {s.Cycle.Day}  🌅  저녁",
                Phase.Night => $"Day {s.Cycle.Day}  🌙  밤",
                Phase.Dawn => $"Day {s.Cycle.Day}  🌄  새벽",
                _ => "",
            };
            ShowBanner(txt);
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
                    fontSize = 32, fontStyle = FontStyle.Bold,
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

        private void DrawPhaseClock()
        {
            var s = GameSession.Instance;
            if (s == null || s.Cycle == null) return;

            if (_clockBig == null)
            {
                _clockBig = new GUIStyle(GUI.skin.label) {
                    fontSize = 30, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UiTheme.TextGold } };
                _clockSmall = new GUIStyle(GUI.skin.label) {
                    fontSize = 14, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UiTheme.TextCream } };
            }

            string phaseName = s.Cycle.Phase switch
            {
                Phase.Day => "낮",
                Phase.Evening => "저녁",
                Phase.Night => "밤",
                Phase.Dawn => "새벽",
                _ => "",
            };
            string phaseIcon = s.Cycle.Phase switch
            {
                Phase.Day => "☀",
                Phase.Evening => "🌅",
                Phase.Night => "🌙",
                Phase.Dawn => "🌄",
                _ => "·",
            };
            float dur = s.Cycle.PhaseDurationSec;
            float rem = Mathf.Max(0f, dur - s.Cycle.ElapsedInPhase);
            int mm = Mathf.FloorToInt(rem / 60f);
            int ss = Mathf.FloorToInt(rem % 60f);
            string clock = $"{mm:00}:{ss:00}";

            int W = 300, H = 64;
            var r = new Rect(Screen.width / 2 - W / 2, 12, W, H);

            // 배경 패널 (페이즈 색조)
            Color tint = s.Cycle.Phase switch
            {
                Phase.Day     => new Color(0.65f, 0.78f, 0.95f, 0.95f),
                Phase.Evening => new Color(0.85f, 0.55f, 0.45f, 0.95f),
                Phase.Night   => new Color(0.18f, 0.22f, 0.45f, 0.97f),
                Phase.Dawn    => new Color(0.95f, 0.78f, 0.55f, 0.95f),
                _ => new Color(0.1f, 0.1f, 0.15f, 0.95f),
            };
            // 외곽 골드
            UiTheme.Rect(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), UiTheme.PanelBorder);
            // 어두운 베이스
            UiTheme.Rect(r, new Color(0.05f, 0.07f, 0.12f, 0.95f));
            // 페이즈 색조 띠 (좌우 4px)
            UiTheme.Rect(new Rect(r.x, r.y, 4, r.height), tint);
            UiTheme.Rect(new Rect(r.xMax - 4, r.y, 4, r.height), tint);
            // 진행 바 (하단)
            float progress = dur > 0 ? Mathf.Clamp01(1f - rem / dur) : 0f;
            UiTheme.Rect(new Rect(r.x, r.yMax - 4, r.width * progress, 4), tint);

            // 큰 시계 텍스트 + 아이콘
            GUI.Label(new Rect(r.x, r.y + 4, r.width, 36), $"{phaseIcon}  {clock}  남음", _clockBig);
            // Day + 페이즈명
            GUI.Label(new Rect(r.x, r.y + 40, r.width, 22), $"Day {s.Cycle.Day}  ·  {phaseName}", _clockSmall);
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
            GUI.Label(new Rect(r.x + 36, r.y, r.width - 36, H), $"{dist:F0}u  →  집", _compassDist);
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
                GUI.Label(new Rect(x, y, w, 24), "당신은 이 작은 마을의 마지막 수호자.", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 28), "눈보라 너머에는 두려움도, 희망도 함께 살고 있다.", _tutLore); y += 30;
            }
            else if (_tutPage == 1)
            {
                GUI.Label(new Rect(x, y, w, 24), "🌅  낮 — 자원을 모으고 마을을 키워라", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 22), "·  나무·돌을 캐고 동물을 사냥해 식량 확보", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  문 밖에서 떠도는 방랑자(NPC)를 만나 영입", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  마을에 모닥불·울타리·망루를 지어 방어 강화", _tutBody); y += 30;

                GUI.Label(new Rect(x, y, w, 24), "🌙  밤 — 마을을 사수하라", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 22), "·  좀비 웨이브가 사방에서 몰려온다", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  5일·10일·15일째 밤에는 보스가 출현", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  레벨이 오를수록 낮과 밤이 길어진다 — 더 많은 시간, 더 큰 위협", _tutBody); y += 30;
            }
            else // page 2
            {
                GUI.Label(new Rect(x, y, w, 24), "🎮  조작", _tutTitle); y += 28;
                GUI.Label(new Rect(x, y, w, 22), "·  WASD / 방향키 — 이동 (W = 위)", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  E — 근처 자원 채집  ·  F — 방랑자 영입", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  공격은 자동 — 사거리 안 적을 즉시 공격", _tutBody); y += 22;
                GUI.Label(new Rect(x, y, w, 22), "·  ESC — 일시정지 / 저장 / 처음부터", _tutBody); y += 30;

                GUI.Label(new Rect(x, y, w, 24), "📈  성장", _tutTitle); y += 28;
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
            string nextLabel = _tutPage < 2 ? "다음 ▶" : "시작하기";
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
                    fontSize = 38, fontStyle = FontStyle.Bold,
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
        // STAT CARD (top-left): HP / XP / 무기 — 현대 RPG 풍 컴팩트 카드
        // ====================================================================
        private void DrawStatCard()
        {
            const int W = 460, H = 200;
            // 하단 좌측으로 이동 — HP/XP/장비를 아래로
            var panel = new Rect(12, Screen.height - H - 12, W, H);
            UiTheme.Panel(panel);
            int innerX = (int)panel.x + 14;
            int innerW = W - 28;
            int y = (int)panel.y + 12;

            if (Player != null)
            {
                // HP 바 with text overlay
                float hpPct = Player.MaxHp > 0 ? (float)Player.CurrentHp / Player.MaxHp : 0f;
                Color hpFill = Color.Lerp(new Color(0.85f, 0.2f, 0.18f), new Color(0.4f, 0.85f, 0.4f), hpPct);
                UiTheme.Bar(new Rect(innerX, y, innerW, 26), hpPct, hpFill);
                var hpStyle = new GUIStyle(_section) {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    fontSize = 17
                };
                GUI.Label(new Rect(innerX, y, innerW, 26), $"HP  {Player.CurrentHp} / {Player.MaxHp}", hpStyle);
                y += 32;
            }
            else
            {
                GUI.Label(new Rect(innerX, y, innerW, 22), "Player NULL", _label);
                y += 28;
            }

            // XP 바 with text overlay
            if (Progression != null)
            {
                float xpPct = Progression.XpToNext > 0 ? (float)Progression.Xp / Progression.XpToNext : 0f;
                UiTheme.Bar(new Rect(innerX, y, innerW, 20), xpPct, UiTheme.BarXpFill);
                var xpStyle = new GUIStyle(_label) {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.05f, 0.1f, 0.18f) },
                    fontSize = 14, fontStyle = FontStyle.Bold
                };
                GUI.Label(new Rect(innerX, y, innerW, 20),
                    $"Lv {Progression.Level}    {Progression.Xp} / {Progression.XpToNext} XP", xpStyle);
                y += 26;
            }

            // 모드 탭 — 근접 / 원거리 / 건축
            int tabW = (innerW - 12) / 3;
            DrawModeTab(new Rect(innerX, y, tabW, 30), HudMode.Melee, "⚔ 근접 [1]");
            DrawModeTab(new Rect(innerX + tabW + 6, y, tabW, 30), HudMode.Ranged, "🏹 원거리 [2]");
            DrawModeTab(new Rect(innerX + (tabW + 6) * 2, y, tabW, 30), HudMode.Build, "🏠 건축 [3]");
            y += 36;

            // 모드별 표시
            if (_hudMode == HudMode.Build)
            {
                GUI.Label(new Rect(innerX, y, innerW, 22), "건축 모드 — 하단 핫바에서 선택", _labelSubtle);
                y += 24;
            }
            else if (Attacker != null && Attacker.Weapon != null)
            {
                var w = Attacker.Weapon;
                GUI.Label(new Rect(innerX, y, innerW, 24), $"⚔ {w.DisplayName}", _weapon);
                y += 28;
                // 쿨타임 바 제거 — 사용자 요청
            }

            // 채집 진행 (활성일 때만)
            if (Gather != null && Gather.IsActive)
            {
                UiTheme.Bar(new Rect(innerX, y, innerW, 10), Gather.Progress, new Color(0.6f, 0.85f, 0.4f));
                GUI.Label(new Rect(innerX, y - 18, innerW, 16),
                    $"채집 {(Gather.Progress * 100):F0}%", _labelSubtle);
            }
        }

        private void DrawModeTab(Rect r, HudMode mode, string label)
        {
            bool active = _hudMode == mode;
            // 활성 탭은 골드 보더 + 밝은 배경
            Color border = active ? UiTheme.PanelBorder : UiTheme.PanelBorderDim;
            Color bg = active ? new Color(0.18f, 0.20f, 0.28f, 1f) : new Color(0.10f, 0.12f, 0.16f, 1f);
            UiTheme.Rect(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), border);
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
                Companion.Stance.Follow => "👣 따르기",
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
        // RESOURCE BAR (top-right): 자원 세로 카드 5줄
        // ====================================================================
        private void DrawResourceBar()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            ResourceKind[] kinds = { ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Meat, ResourceKind.Food, ResourceKind.Frostbloom };
            string[] names = { "Wood", "Stone", "Meat", "Food", "Frost" };

            const int W = 290, H = 244;
            var panel = new Rect(Screen.width - W - 12, 12, W, H);
            UiTheme.Panel(panel);
            UiTheme.TitleBar(panel, "  자원  ", _title);

            int innerX = (int)panel.x + 12;
            int innerW = W - 24;
            int y = (int)panel.y + 42;
            int rowH = 36;

            for (int i = 0; i < kinds.Length; i++)
            {
                var k = kinds[i];
                // 컬러 아이콘
                UiTheme.Icon(new Rect(innerX, y + 6, 18, 18), UiTheme.ResColor(k));
                // 이름
                GUI.Label(new Rect(innerX + 26, y + 4, 80, 22), names[i], _label);
                // 수량 / 캡 (우측 정렬)
                int cur = session.Resources.Get(k);
                int cap = session.Resources.GetCap(k);
                var oldC = GUI.contentColor;
                GUI.contentColor = cur >= cap ? UiTheme.TextDanger : UiTheme.TextCream;
                var amountStyle = new GUIStyle(_section) {
                    fontSize = 16, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                };
                GUI.Label(new Rect(innerX + 80, y + 4, innerW - 80, 22), $"{cur} / {cap}", amountStyle);
                GUI.contentColor = oldC;
                y += rowH;
            }
        }

        // ====================================================================
        // WAVE / STANCE BAR (bottom-left): 좀비 웨이브 + 동료 스탠스
        // ====================================================================
        private void DrawWaveStanceBar()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            const int W = 320, H = 120;
            // 상단 좌측으로 이동 — StatCard 가 하단으로 내려가서 자리 비움
            var panel = new Rect(12, 12, W, H);
            UiTheme.Panel(panel);

            int innerX = (int)panel.x + 12;
            int innerW = W - 24;
            int y = (int)panel.y + 8;

            // 페이즈별 컨텍스트 라인
            if (Night != null && Night.CurrentPhase == Phase.Night)
            {
                var oldC = GUI.contentColor;
                GUI.contentColor = new Color(0.95f, 0.5f, 0.5f);
                GUI.Label(new Rect(innerX, y, innerW, 22),
                    $"🧟 활성 {Night.ActiveZombies}  ·  대기 {Night.WavePending}", _section);
                GUI.contentColor = oldC;
                y += 24;
                if (Night.IsBlizzard)
                {
                    GUI.contentColor = new Color(0.55f, 0.85f, 1f);
                    GUI.Label(new Rect(innerX, y, innerW, 22), "❄ 눈보라", _section);
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
                GUI.Label(new Rect(innerX, y, innerW, 22), "☀ 평온한 낮", _labelSubtle);
                y += 24;
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
                Companion.Stance.Follow => "👣 따르기",
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

        private void DrawBuildHotbar()
        {
            var session = GameSession.Instance;
            if (session == null || Player == null) return;

            int wood = session.Resources.Get(ResourceKind.Wood);
            int stone = session.Resources.Get(ResourceKind.Stone);

            bool farmAllowed = FarmBuilding.CurrentFarmCount() < FarmBuilding.MaxFarmsAllowed();

            BuildSlot[] slots = {
                new BuildSlot { Icon = "🔥", Name = "모닥불",
                    CostWood = Inflate(5, BuildingKind.Campfire),
                    Kind = BuildingKind.Campfire, Available = true,
                    Color = new Color(1f, 0.55f, 0.2f) },
                new BuildSlot { Icon = "🏠", Name = "집 (+4)",
                    CostWood = Inflate(6, BuildingKind.House),
                    Kind = BuildingKind.House, Available = true,
                    Color = new Color(0.85f, 0.6f, 0.4f) },
                new BuildSlot { Icon = "🥕", Name = "울타리",
                    CostWood = Inflate(1, BuildingKind.Fence),
                    Kind = BuildingKind.Fence, Available = true,
                    Color = new Color(0.78f, 0.62f, 0.30f) },
                new BuildSlot { Icon = "📦", Name = "창고",
                    CostWood = Inflate(8, BuildingKind.Storage),
                    Kind = BuildingKind.Storage, Available = true,
                    Color = new Color(0.55f, 0.45f, 0.3f) },
                new BuildSlot { Icon = farmAllowed ? "🌾" : "🌾✖",
                    Name = farmAllowed ? "농장" : "창고 필요",
                    CostWood = Inflate(6, BuildingKind.Farm),
                    Kind = BuildingKind.Farm, Available = farmAllowed,
                    Color = new Color(0.5f, 0.85f, 0.35f) },
                new BuildSlot { Icon = "🏹", Name = "망루",
                    CostWood = Inflate(8, BuildingKind.Watchtower),
                    CostStone = Inflate(4, BuildingKind.Watchtower),
                    Kind = BuildingKind.Watchtower, Available = true,
                    Color = new Color(0.6f, 0.85f, 0.55f) },
                new BuildSlot { Icon = "🏥", Name = "의무실",
                    CostWood = Inflate(7, BuildingKind.Infirmary),
                    Kind = BuildingKind.Infirmary, Available = true,
                    Color = new Color(0.9f, 0.95f, 0.95f) },
            };

            const int CellW = 96, CellH = 96, Gap = 6;
            int totalW = CellW * slots.Length + Gap * (slots.Length - 1);
            int startX = Screen.width / 2 - totalW / 2;
            int y = Screen.height - CellH - 12;

            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                int cx = startX + i * (CellW + Gap);
                var r = new Rect(cx, y, CellW, CellH);
                bool ok = wood >= s.CostWood && stone >= s.CostStone;

                // 셀 배경 + 보더
                UiTheme.Rect(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), ok ? UiTheme.PanelBorder : UiTheme.PanelBorderDim);
                UiTheme.Rect(r, ok ? UiTheme.PanelBg : new Color(0.08f, 0.08f, 0.1f, 0.95f));
                // 컬러 띠 상단
                UiTheme.Rect(new Rect(r.x, r.y, r.width, 4), s.Color);

                // 아이콘 (큰 글자)
                var iconStyle = new GUIStyle(_title) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
                var oldC = GUI.contentColor;
                GUI.contentColor = ok ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(r.x, r.y + 10, r.width, 36), s.Icon, iconStyle);

                // 이름
                GUI.contentColor = ok ? UiTheme.TextCream : new Color(1f, 1f, 1f, 0.4f);
                var nameStyle = new GUIStyle(_label) {
                    fontSize = 14, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                GUI.Label(new Rect(r.x, r.y + 46, r.width, 18), s.Name, nameStyle);

                // 비용
                string cost = s.CostStone > 0 ? $"{s.CostWood}W + {s.CostStone}S" : $"{s.CostWood}W";
                GUI.contentColor = ok ? UiTheme.TextSubtle : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                var costStyle = new GUIStyle(_labelSubtle) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(r.x, r.y + 66, r.width, 16), cost, costStyle);
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

            const int W = 280, H = 160;
            var panel = new Rect(Screen.width - W - 12, Screen.height - H - 12, W, H);
            UiTheme.Panel(panel);

            int innerX = (int)panel.x + 10;
            int innerW = W - 20;
            int y = (int)panel.y + 10;

            GUI.Label(new Rect(innerX, y, innerW, 16), "디버그", _labelSubtle);
            y += 18;

            int half = (innerW - 6) / 2;
            if (UiTheme.Button(new Rect(innerX, y, half, 24), "▶ 페이즈+1", _smallBtn))
                session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
            if (UiTheme.Button(new Rect(innerX + half + 6, y, half, 24), "🌙 강제 밤", _smallBtn))
                if (Night != null) Night.StartNight(session.Cycle.Day);
            y += 28;

            if (UiTheme.Button(new Rect(innerX, y, half, 24), "🧟 좀비+1", _smallBtn))
                if (Night != null) Night.SpawnDebugZombie();
            if (UiTheme.Button(new Rect(innerX + half + 6, y, half, 24), "☀ 낮으로", _smallBtn))
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
            DrawRes(ResourceKind.Frostbloom, "Frostbloom");

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
            GUI.Label(new Rect(innerX, y, innerW, 18), "디버그", _labelSubtle);
            y += 18;

            int half = (innerW - 8) / 2;
            if (UiTheme.Button(new Rect(innerX, y, half, 26), "▶ 페이즈 +1", _smallBtn))
            {
                session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
            }
            if (UiTheme.Button(new Rect(innerX + half + 8, y, half, 26), "🌙 강제 밤", _smallBtn))
            {
                if (Night != null) Night.StartNight(session.Cycle.Day);
            }
            y += 30;

            if (UiTheme.Button(new Rect(innerX, y, half, 26), "🧟 좀비 +1", _smallBtn))
            {
                if (Night != null) Night.SpawnDebugZombie();
            }
            if (UiTheme.Button(new Rect(innerX + half + 8, y, half, 26), "☀ 낮으로", _smallBtn))
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
                : $"🔨 수리  Wood 부족";
            var rect = new Rect(sp.x - 130, guiY - 26, 260, 56);
            var bigBtn = new GUIStyle(_btn) { fontSize = 18, fontStyle = FontStyle.Bold };
            if (UiTheme.Button(rect, label, bigBtn, canAfford))
            {
                if (session.Resources.Spend(ResourceKind.Wood, Cost))
                {
                    best.RepairHp(healAmount);
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
            var rect = new Rect(sp.x - 130, guiY - 26, 260, 56);
            var bigBtn = new GUIStyle(_btn) { fontSize = 17, fontStyle = FontStyle.Bold };
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
                ? (nearbyCompanions.Count > 0 ? $"나 + 동료 {nearbyCompanions.Count}" : "나")
                : $"동료 {nearbyCompanions.Count}";
            string label = $"{verb} ({who})";
            var rect = new Rect(sp.x - 130, guiY - 26, 260, 56);
            var bigBtn = new GUIStyle(_btn) { fontSize = 18, fontStyle = FontStyle.Bold };
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
                if (g.GetComponent<DeerAi>() != null || g.GetComponent<WolfAi>() != null) continue;

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
                    int yield = farm.BaseYield + farm.Workers.Count * farm.PerWorkerBonus;
                    var rect = new Rect(guiX - 75, guiY - 36, 150, 28);
                    if (UiTheme.Button(rect, $"🌾 수확 +{yield}", _smallBtn)) farm.Harvest();
                }
                else
                {
                    GUI.Label(new Rect(guiX - 75, guiY - 36, 150, 22),
                        $"성장중 {farm.NightsPassed}/{farm.NightsToRipe}", _labelSubtle);
                }

                if (!farm.HarvestReady && farm.Workers.Count < farm.MaxWorkers && Player != null)
                {
                    if (Vector2.Distance(Player.transform.position, farm.transform.position) < 3f)
                    {
                        var rect2 = new Rect(guiX - 75, guiY - 8, 150, 26);
                        if (UiTheme.Button(rect2, $"동료 배치 {farm.Workers.Count}/{farm.MaxWorkers}", _smallBtn))
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
            if (Player == null) return;
            var npc = FindNearestRecruitable(Player.transform.position, 2.2f);
            if (npc == null) return;

            const int W = 540;
            const int H = 160;
            var panel = new Rect(Screen.width / 2 - W / 2, Screen.height - H - 20, W, H);
            UiTheme.Panel(panel);

            // 초상 (스프라이트 색 사각형 + 굵은 골드 보더)
            var sr = npc.GetComponent<SpriteRenderer>();
            var col = sr != null ? sr.color : Color.white;
            var portrait = new Rect(panel.x + 14, panel.y + 14, 132, 132);
            UiTheme.Rect(new Rect(portrait.x - 2, portrait.y - 2, portrait.width + 4, portrait.height + 4), UiTheme.PanelBorder);
            UiTheme.Rect(portrait, col);
            // 안쪽 인셋 하이라이트
            UiTheme.Rect(new Rect(portrait.x + 2, portrait.y + 2, portrait.width - 4, 2), new Color(1f, 1f, 1f, 0.25f));

            int tx = (int)panel.x + 160;
            int tw = (int)panel.width - 174;

            GUI.Label(new Rect(tx, panel.y + 14, tw, 24), $"{npc.DisplayNamePublic}", _title);
            GUI.Label(new Rect(tx, panel.y + 38, tw, 20), $"({npc.Role}{(npc.IsCombat ? "" : " · 비전투")})", _labelSubtle);

            GUI.Label(new Rect(tx, panel.y + 60, tw, 40), $"\"{npc.DialogText}\"", _label);

            GUI.Label(new Rect(tx, panel.y + 100, 60, 18), "전투", _labelSubtle);
            GUI.Label(new Rect(tx + 60, panel.y + 100, 120, 18), Stars(npc.CombatRating), _label);
            GUI.Label(new Rect(tx, panel.y + 120, 60, 18), "농사", _labelSubtle);
            GUI.Label(new Rect(tx + 60, panel.y + 120, 120, 18), Stars(npc.FarmRating), _label);

            // 마을 수용 한도 체크 — 건물 수만큼만 영입 가능 (초반 12명까지는 무료)
            int cap = RecruitableNpc.VillageCapacity();
            int have = RecruitableNpc.CurrentCompanionCount();
            bool canRecruit = have < cap;

            // 수용 인원 표시
            var capStyle = new GUIStyle(_labelSubtle) { fontSize = 14 };
            var oldC = GUI.contentColor;
            GUI.contentColor = canRecruit ? UiTheme.TextSubtle : UiTheme.TextDanger;
            GUI.Label(new Rect(tx, panel.y + panel.height - 60, 200, 18),
                $"마을 수용  {have} / {cap}", capStyle);
            GUI.contentColor = oldC;

            string label = canRecruit ? "영입 (F)" : "건물 더 필요";
            if (UiTheme.Button(new Rect(tx + 200, panel.y + panel.height - 38, 130, 28), label, _btn, canRecruit))
            {
                npc.Recruit();
            }
            if (UiTheme.Button(new Rect(tx + 338, panel.y + panel.height - 38, 90, 28), "거절", _smallBtn))
            {
                // 범위 벗어나면 자동 닫힘
            }
        }

        private static string Stars(int n)
        {
            n = Mathf.Clamp(n, 0, 5);
            return new string('★', n) + new string('☆', 5 - n);
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

            const int W = 920;
            const int H = 380;
            var modal = new Rect(Screen.width / 2 - W / 2, Screen.height / 2 - H / 2, W, H);
            UiTheme.Panel(modal);
            UiTheme.TitleBar(modal, $"  LEVEL {Progression.Level} — 룬 선택  ", _title);

            int btnW = 280, btnH = 270, gap = 20;
            int total = btnW * 3 + gap * 2;
            int bx = (int)modal.x + (W - total) / 2;
            int by = (int)modal.y + 60;

            var titleStyle = new GUIStyle(_weapon) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
            var bodyStyle = new GUIStyle(_label) { fontSize = 18, wordWrap = true };
            var subStyle = new GUIStyle(_labelSubtle) { fontSize = 16 };

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

                GUI.Label(new Rect(rect.x + 16, rect.y + 18, rect.width - 32, 32), title, titleStyle);
                GUI.Label(new Rect(rect.x + 16, rect.y + 56, rect.width - 32, 22),
                    $"진행 {curStacks}/{PlayerProgression.MaxStacks} → {next}/{PlayerProgression.MaxStacks}", subStyle);
                UiTheme.Separator(new Rect(rect.x + 16, rect.y + 86, rect.width - 32, 1));
                GUI.Label(new Rect(rect.x + 16, rect.y + 100, rect.width - 32, btnH - 110),
                    PlayerProgression.DescribeAt(rune, next), bodyStyle);
            }
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

            int W = 360, H = 260;
            var modal = new Rect(Screen.width / 2 - W / 2, Screen.height / 2 - H / 2, W, H);
            UiTheme.Panel(modal);
            UiTheme.TitleBar(modal, "  일시정지  ", _title);

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

            if (UiTheme.Button(new Rect(bx, by, bw, 38), "▶ 계속하기", _btn))
            {
                _paused = false;
                Time.timeScale = _preTimeScale > 0f ? _preTimeScale : 1f;
                Sfx.Click();
            }
            by += 46;
            if (UiTheme.Button(new Rect(bx, by, bw, 38), "💾 즉시 저장", _btn))
            {
                if (GameSession.Instance != null) GameSession.Instance.SaveNow();
                Sfx.Click();
            }
            by += 46;
            if (UiTheme.Button(new Rect(bx, by, bw, 38), "🔄 처음부터 (저장 삭제)", _btn))
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
                const int W = 360;
                int sx = Screen.width / 2 - W / 2;
                int sy = Screen.height / 2 - 30;
                var statPanel = new Rect(sx, sy, W, 130);
                UiTheme.Panel(statPanel);

                int row = sy + 12;
                int lineH = 22;
                GUI.Label(new Rect(sx + 24, row, W - 48, lineH), $"생존 일수      Day {s.Cycle.Day}", _section); row += lineH;
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
        private void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 20, normal = { textColor = UiTheme.TextCream } };
            _labelSubtle = new GUIStyle(GUI.skin.label) { fontSize = 17, normal = { textColor = UiTheme.TextSubtle } };
            _section = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = UiTheme.TextCream } };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = UiTheme.TextGold } };
            _weapon = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.7f, 0.95f, 1f) } };
            _bigDeath = new GUIStyle(GUI.skin.label) { fontSize = 88, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = UiTheme.TextDanger } };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            _smallBtn = new GUIStyle(GUI.skin.button) { fontSize = 18 };
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

        // ====================================================================
        // Spawn 헬퍼들
        // ====================================================================
        private void SpawnBarricade(Vector3 playerPos)
        {
            var go = new GameObject("Barricade");
            go.transform.position = playerPos;
            go.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.45f, 0.28f, 0.15f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.2f, 0.1f, 0.05f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Barricade;
            var hp = go.AddComponent<HpBarUi>(); hp.Building = b;
            hp.Offset = new Vector2(0f, 0.6f); hp.Size = new Vector2(1.0f, 0.1f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f); hp.FillColor = new Color(0.6f, 0.4f, 0.2f);
        }

        private void SpawnStorage(Vector3 playerPos)
        {
            var go = new GameObject("Storage");
            go.transform.position = playerPos;
            go.transform.localScale = new Vector3(1.0f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 0.45f, 0.3f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.25f, 0.18f, 0.1f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Storage;
        }

        private void SpawnInfirmary(Vector3 playerPos)
        {
            var go = new GameObject("Infirmary");
            go.transform.position = playerPos;
            go.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.95f, 0.97f, 0.95f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.5f, 0.7f, 0.5f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Infirmary;
            go.AddComponent<HealingShrine>();
        }

        private void SpawnHouse(Vector3 playerPos)
        {
            var go = new GameObject("House");
            go.transform.position = playerPos;
            go.transform.localScale = new Vector3(1.1f, 1.0f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.85f, 0.6f, 0.4f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.4f, 0.2f, 0.1f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.House;
        }

        private void SpawnFarm(Vector3 playerPos)
        {
            var go = new GameObject("Farm");
            go.transform.position = playerPos;
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.35f, 0.55f, 0.25f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.18f, 0.3f, 0.12f, 1f);
            go.AddComponent<FarmBuilding>();
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Farm;
        }

        private void SpawnWatchtower(Vector3 playerPos)
        {
            var go = new GameObject("Watchtower");
            go.transform.position = playerPos;
            go.transform.localScale = new Vector3(0.7f, 1.4f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.5f, 0.4f, 0.28f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.2f, 0.15f, 0.08f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Watchtower;
            go.AddComponent<Watchtower>();
            var hp = go.AddComponent<HpBarUi>(); hp.Building = b;
            hp.Offset = new Vector2(0f, 0.85f); hp.Size = new Vector2(0.8f, 0.1f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            hp.FillColor = new Color(0.4f, 0.85f, 0.55f);
        }

        private void SpawnCampfire(Vector3 playerPos)
        {
            var go = new GameObject("Campfire");
            go.transform.position = playerPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 0.5f, 0.1f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.3f, 0.1f, 0f, 1f);
            var col = go.AddComponent<CircleCollider2D>(); col.radius = 0.45f;
            var aura = go.AddComponent<CampfireAura>();
            aura.Radius = 2.5f; aura.DamagePerSecond = 6f; aura.TickInterval = 0.5f;
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Campfire;
            var hp = go.AddComponent<HpBarUi>(); hp.Building = b;
            hp.Offset = new Vector2(0f, 0.7f); hp.Size = new Vector2(1.0f, 0.12f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f); hp.FillColor = new Color(1f, 0.55f, 0.2f);
        }
    }
}
