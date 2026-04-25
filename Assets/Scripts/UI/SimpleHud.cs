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

        private void OnGUI()
        {
            EnsureStyles();
            DrawLeftPanel();
            DrawRightPanel();
            DrawWorldChopButton();
            DrawWorldFarmButtons();
            DrawRecruitDialog();
            DrawRuneModal();
            DrawPhaseBanner();
            DrawBossWarning();
            DrawAutoSaveToast();
            DrawAchievementToast();
            DrawHomeCompass();
            DrawPhaseClock();
            DrawControlsHint();
            DrawTutorialOverlay();
            DrawPauseMenu();
            DrawDeathOverlay();
            DrawDamageFlash();
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
            int W = 320, H = 60;
            var r = new Rect(Screen.width - W - 20, Screen.height - H - 60, W, H);
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
        }

        private void OnDisable()
        {
            _unsubE?.Invoke(); _unsubN?.Invoke(); _unsubD?.Invoke(); _unsubA?.Invoke();
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

            int W = 110, H = 36;
            var r = new Rect(Screen.width - W - 290, 14, W, H);
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

        private const string TutorialPrefKey = "il6_tutorial_seen_v1";
        private float _tutorialTimer = 0f;
        private bool _tutorialDismissed;
        private GUIStyle _tutTitle, _tutBody;

        private void DrawTutorialOverlay()
        {
            if (_tutorialDismissed) return;
            if (PlayerPrefs.GetInt(TutorialPrefKey, 0) == 1) { _tutorialDismissed = true; return; }
            _tutorialTimer += Time.unscaledDeltaTime;
            // 8초 자동 종료
            if (_tutorialTimer > 12f) { DismissTutorial(); return; }

            int W = 480, H = 280;
            var r = new Rect(Screen.width / 2 - W / 2, Screen.height / 2 - H / 2 - 30, W, H);
            UiTheme.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.45f));
            UiTheme.Panel(r);
            UiTheme.TitleBar(r, "  처음 오신 분께  ", _title);

            if (_tutTitle == null)
            {
                _tutTitle = new GUIStyle(GUI.skin.label) {
                    fontSize = 16, fontStyle = FontStyle.Bold,
                    normal = { textColor = UiTheme.TextGold }, wordWrap = true };
                _tutBody = new GUIStyle(GUI.skin.label) {
                    fontSize = 13, normal = { textColor = UiTheme.TextCream }, wordWrap = true };
            }

            int x = (int)r.x + 22, y = (int)r.y + 44, w = (int)r.width - 44;
            GUI.Label(new Rect(x, y, w, 22), "🎮  조작", _tutTitle); y += 24;
            GUI.Label(new Rect(x, y, w, 18), "·  WASD / 방향키  →  이동", _tutBody); y += 18;
            GUI.Label(new Rect(x, y, w, 18), "·  E  →  근처 자원 채집  ·  F  →  방랑자 영입", _tutBody); y += 18;
            GUI.Label(new Rect(x, y, w, 18), "·  공격은 자동 — 좀비가 사거리 안에 들어오면 사격/베기.", _tutBody); y += 22;

            GUI.Label(new Rect(x, y, w, 22), "🌙  밤이 옵니다", _tutTitle); y += 24;
            GUI.Label(new Rect(x, y, w, 18), "·  좀비 웨이브가 사방에서 몰려옵니다.  모닥불 근처를 사수하세요.", _tutBody); y += 18;
            GUI.Label(new Rect(x, y, w, 18), "·  5일·10일·15일 밤은 보스가 등장합니다.", _tutBody); y += 22;

            GUI.Label(new Rect(x, y, w, 22), "🏠  마을 만들기", _tutTitle); y += 24;
            GUI.Label(new Rect(x, y, w, 18), "·  좌측 패널의 모닥불·바리게이트·창고·농장을 지어보세요.", _tutBody); y += 18;
            GUI.Label(new Rect(x, y, w, 18), "·  레벨업 시 룬 3 종 중 하나를 골라 강해집니다 (최대 3중첩).", _tutBody); y += 18;

            int btnW = 120;
            if (UiTheme.Button(new Rect(r.x + r.width / 2 - btnW / 2, r.yMax - 36, btnW, 26), "시작하기", _btn))
            {
                DismissTutorial();
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
            int W = 140, H = 28;
            var r = new Rect(Screen.width / 2 - W / 2, 14, W, H);
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
        // 좌측: 플레이어 / 무기 / 빌드
        // ====================================================================
        private void DrawLeftPanel()
        {
            var panel = new Rect(12, 90, 290, 580);
            UiTheme.Panel(panel);
            UiTheme.TitleBar(panel, "  플레이어  ", _title);

            int y = (int)panel.y + 36;
            int innerX = (int)panel.x + 14;
            int innerW = (int)panel.width - 28;

            if (Player != null)
            {
                // HP 바
                GUI.Label(new Rect(innerX, y, 80, 20), "HP", _label);
                GUI.Label(new Rect(innerX + 200, y, 80, 20), $"{Player.CurrentHp} / {Player.MaxHp}", _labelSubtle);
                y += 18;
                float hpPct = Player.MaxHp > 0 ? (float)Player.CurrentHp / Player.MaxHp : 0f;
                UiTheme.Bar(new Rect(innerX, y, innerW, 12), hpPct,
                    Color.Lerp(Color.red, Color.green, hpPct));
                y += 18;

                var p = Player.transform.position;
                GUI.Label(new Rect(innerX, y, innerW, 18), $"위치 ({p.x:F1}, {p.y:F1})", _labelSubtle);
                y += 20;
            }
            else
            {
                GUI.Label(new Rect(innerX, y, innerW, 22), "Player NULL", _label);
                y += 22;
            }

            if (Gather != null && Gather.IsActive)
            {
                GUI.Label(new Rect(innerX, y, innerW, 18), $"채집중 {(Gather.Progress * 100):F0}%", _labelSubtle);
                y += 18;
                UiTheme.Bar(new Rect(innerX, y, innerW, 8), Gather.Progress, new Color(0.6f, 0.85f, 0.4f));
                y += 14;
            }

            if (Progression != null)
            {
                UiTheme.Separator(new Rect(innerX, y + 2, innerW, 1));
                y += 10;
                GUI.Label(new Rect(innerX, y, 80, 20), $"Lv {Progression.Level}", _section);
                GUI.Label(new Rect(innerX + 80, y, innerW - 80, 20), $"XP {Progression.Xp}/{Progression.XpToNext}", _labelSubtle);
                y += 18;
                float xpPct = Progression.XpToNext > 0 ? (float)Progression.Xp / Progression.XpToNext : 0f;
                UiTheme.Bar(new Rect(innerX, y, innerW, 8), xpPct, UiTheme.BarXpFill);
                y += 14;
            }

            if (Attacker != null && Attacker.Weapon != null)
            {
                UiTheme.Separator(new Rect(innerX, y + 2, innerW, 1));
                y += 10;
                var w = Attacker.Weapon;
                if (UiTheme.Button(new Rect(innerX, y, 28, 24), "<", _smallBtn)) Attacker.CycleWeapon(-1);
                GUI.Label(new Rect(innerX + 32, y + 2, innerW - 64, 22), w.DisplayName, _weapon);
                if (UiTheme.Button(new Rect(innerX + innerW - 28, y, 28, 24), ">", _smallBtn)) Attacker.CycleWeapon(+1);
                y += 28;

                GUI.Label(new Rect(innerX, y, innerW, 18),
                    $"DMG {w.BaseDamage}   RNG {w.Range:F1}u   CD {w.CooldownSec:F2}s", _labelSubtle);
                y += 18;

                float cd = Attacker.CurrentCooldown;
                float ready = 1f - Mathf.Clamp01(cd / Mathf.Max(0.01f, w.CooldownSec));
                UiTheme.Bar(new Rect(innerX, y, innerW - 60, 10), ready, UiTheme.BarCdFill);
                GUI.Label(new Rect(innerX + innerW - 56, y - 4, 56, 18),
                    ready >= 1f ? "READY" : $"{cd:F1}s", _labelSubtle);
                y += 16;
            }

            UiTheme.Separator(new Rect(innerX, y + 2, innerW, 1));
            y += 8;

            var session = GameSession.Instance;
            int wood = session != null ? session.Resources.Get(ResourceKind.Wood) : 0;

            DrawBuildButton(new Rect(innerX, y, innerW, 28), "🔥 모닥불", 5, wood, ResourceKind.Wood,
                () => SpawnCampfire(Player.transform.position));
            y += 32;
            DrawBuildButton(new Rect(innerX, y, innerW, 28), "🪵 바리게이트", 5, wood, ResourceKind.Wood,
                () => SpawnBarricade(Player.transform.position));
            y += 32;
            DrawBuildButton(new Rect(innerX, y, innerW, 28), "📦 창고 (+50 cap)", 8, wood, ResourceKind.Wood,
                () => { SpawnStorage(Player.transform.position); session.Resources.IncreaseCap(50); });
            y += 32;
            DrawBuildButton(new Rect(innerX, y, innerW, 28), "🌾 농장", 6, wood, ResourceKind.Wood,
                () => SpawnFarm(Player.transform.position));
            y += 32;

            int stone = session.Resources.Get(ResourceKind.Stone);
            bool watchtowerOk = wood >= 8 && stone >= 4;
            if (UiTheme.Button(new Rect(innerX, y, innerW, 28), $"🏹 망루     8 Wood + 4 Stone", _btn, watchtowerOk))
            {
                if (session.Resources.Spend(ResourceKind.Wood, 8) && session.Resources.Spend(ResourceKind.Stone, 4))
                {
                    Sfx.Build();
                    SpawnWatchtower(Player.transform.position);
                }
            }
            y += 36;

            // 동료 스탠스 토글 (모든 동료 일괄)
            var allComps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int liveCount = 0;
            Companion.Stance majorityStance = Companion.Stance.Follow;
            foreach (var c in allComps)
            {
                if (c == null || c.IsDead || c.CurrentMode == Companion.Mode.Hiding) continue;
                liveCount++;
                majorityStance = c.CurrentStance;
            }
            string sLabel = majorityStance switch
            {
                Companion.Stance.Follow => "👣 따르기",
                Companion.Stance.Hold => "🛡 사수",
                Companion.Stance.Aggressive => "⚔ 공세",
                _ => ""
            };
            if (UiTheme.Button(new Rect(innerX, y, innerW, 28), $"동료 스탠스: {sLabel} ({liveCount})", _btn, liveCount > 0))
            {
                var next = majorityStance switch
                {
                    Companion.Stance.Follow => Companion.Stance.Hold,
                    Companion.Stance.Hold => Companion.Stance.Aggressive,
                    _ => Companion.Stance.Follow
                };
                foreach (var c in allComps) if (c != null) c.SetStance(next);
            }
        }

        private void DrawBuildButton(Rect r, string label, int cost, int have, ResourceKind kind, System.Action onClick)
        {
            bool enabled = have >= cost && Player != null && GameSession.Instance != null;
            string txt = $"{label}     {cost} {kind}";
            if (UiTheme.Button(r, txt, _btn, enabled))
            {
                if (GameSession.Instance.Resources.Spend(kind, cost))
                {
                    Sfx.Build();
                    onClick();
                }
            }
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
        private void DrawWorldChopButton()
        {
            if (Player == null) return;
            var tree = FindNearestTreeInRange(Player.transform.position, 3.5f);
            if (tree == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(tree.transform.position + new Vector3(0f, 0.8f, 0f));
            if (sp.z < 0) return;
            float guiY = Screen.height - sp.y;

            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int workers = 0;
            foreach (var c in companions) if (c != null && c.CurrentMode != Companion.Mode.Hiding && c.CurrentMode != Companion.Mode.Farming) workers++;

            string label = workers > 0 ? $"🪓 벌목 ({workers})" : "벌목 불가";
            var rect = new Rect(sp.x - 75, guiY - 16, 150, 30);
            if (UiTheme.Button(rect, label, _smallBtn, workers > 0))
            {
                foreach (var c in companions) if (c != null && c.CurrentMode != Companion.Mode.Hiding && c.CurrentMode != Companion.Mode.Farming) c.AssignGather(tree);
            }
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

            if (UiTheme.Button(new Rect(tx + 200, panel.y + panel.height - 38, 110, 28), "영입 (F)", _btn))
            {
                npc.Recruit();
            }
            if (UiTheme.Button(new Rect(tx + 318, panel.y + panel.height - 38, 90, 28), "거절", _smallBtn))
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

            const int W = 640;
            const int H = 260;
            var modal = new Rect(Screen.width / 2 - W / 2, Screen.height / 2 - H / 2, W, H);
            UiTheme.Panel(modal);
            UiTheme.TitleBar(modal, $"  LEVEL {Progression.Level} — 룬 선택  ", _title);

            int btnW = 188, btnH = 160, gap = 14;
            int total = btnW * 3 + gap * 2;
            int bx = (int)modal.x + (W - total) / 2;
            int by = (int)modal.y + 50;
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
                string title = PlayerProgression.Title(rune) + (willMaster ? "  ★ MASTER" : (next == 2 ? "  +" : ""));
                GUI.Label(new Rect(rect.x + 10, rect.y + 14, rect.width - 20, 24), title, _weapon);
                GUI.Label(new Rect(rect.x + 10, rect.y + 36, rect.width - 20, 16),
                    $"진행 {curStacks}/{PlayerProgression.MaxStacks} → {next}/{PlayerProgression.MaxStacks}", _labelSubtle);
                UiTheme.Separator(new Rect(rect.x + 10, rect.y + 56, rect.width - 20, 1));
                GUI.Label(new Rect(rect.x + 10, rect.y + 64, rect.width - 20, 90), PlayerProgression.DescribeAt(rune, next), _label);
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
            _label = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = UiTheme.TextCream } };
            _labelSubtle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = UiTheme.TextSubtle } };
            _section = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = UiTheme.TextCream } };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = UiTheme.TextGold } };
            _weapon = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.7f, 0.95f, 1f) } };
            _bigDeath = new GUIStyle(GUI.skin.label) { fontSize = 80, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = UiTheme.TextDanger } };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            _smallBtn = new GUIStyle(GUI.skin.button) { fontSize = 15 };
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

        private static Gatherable FindNearestTreeInRange(Vector3 center, float range)
        {
            var all = Object.FindObjectsByType<Gatherable>(FindObjectsSortMode.None);
            Gatherable best = null;
            float bestDist = range;
            foreach (var g in all)
            {
                if (g == null || g.YieldKind != ResourceKind.Wood) continue;
                float d = Vector2.Distance(center, g.transform.position);
                if (d < bestDist) { best = g; bestDist = d; }
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
            go.transform.position = playerPos + new Vector3(0f, 1.2f, 0f);
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
            go.transform.position = playerPos + new Vector3(-1.4f, 0f, 0f);
            go.transform.localScale = new Vector3(1.0f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 0.45f, 0.3f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.25f, 0.18f, 0.1f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Campfire; // 비-바리게이트로 분류
        }

        private void SpawnFarm(Vector3 playerPos)
        {
            var go = new GameObject("Farm");
            go.transform.position = playerPos + new Vector3(0f, -1.4f, 0f);
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.35f, 0.55f, 0.25f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.18f, 0.3f, 0.12f, 1f);
            go.AddComponent<FarmBuilding>();
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Campfire; // 비-바리게이트
        }

        private void SpawnWatchtower(Vector3 playerPos)
        {
            var go = new GameObject("Watchtower");
            go.transform.position = playerPos + new Vector3(0f, 1.6f, 0f);
            go.transform.localScale = new Vector3(0.7f, 1.4f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.5f, 0.4f, 0.28f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.2f, 0.15f, 0.08f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Campfire; // 비-바리게이트 분류 (호스팅 가능)
            go.AddComponent<Watchtower>();
            var hp = go.AddComponent<HpBarUi>(); hp.Building = b;
            hp.Offset = new Vector2(0f, 0.85f); hp.Size = new Vector2(0.8f, 0.1f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            hp.FillColor = new Color(0.4f, 0.85f, 0.55f);
        }

        private void SpawnCampfire(Vector3 playerPos)
        {
            var go = new GameObject("Campfire");
            go.transform.position = playerPos + new Vector3(1.2f, 0f, 0f);
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
