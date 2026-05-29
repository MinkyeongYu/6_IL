using UnityEngine;
using UnityEngine.SceneManagement;

namespace IL6
{
    /// <summary>
    /// 게임 시작 전 2-슬라이드 튜토리얼.
    /// 빈 씬에 이 컴포넌트만 붙이면 동작.
    /// Enter / Space / 클릭으로 슬라이드 전환 → SnowfieldScene 으로 이동.
    /// </summary>
    public sealed class OnboardingController : MonoBehaviour
    {
        public string SnowfieldSceneName = "SnowfieldScene";

        // ── slide data ──────────────────────────────────────────────────────────
        private enum Phase { Day, Night }

        private struct Slide
        {
            public Phase Phase;
            public string Headline;
            public string[] Body;       // "KEY  설명" 형식 → 자동 파싱
            public string Tip;
            public string ConfirmLabel;
        }

        private static readonly Slide[] Slides = new[]
        {
            new Slide
            {
                Phase = Phase.Day,
                Headline = "낮 — 설원을 탐험하라",
                Body = new[]
                {
                    "WASD  이동",
                    "F 길게 누르기  나무 / 돌 수집",
                    "근접 공격  사슴 사냥 → 고기 획득",
                    "",
                    "마을 중앙의 모닥불을 지켜라.",
                    "모닥불이 꺼지면 밤을 버틸 수 없다.",
                },
                Tip = "자원을 최대한 모아야 바리케이드를 세울 수 있다.",
                ConfirmLabel = "알겠다, 계속  [Enter]",
            },
            new Slide
            {
                Phase = Phase.Night,
                Headline = "밤 — 좀비 웨이브를 막아라",
                Body = new[]
                {
                    "좀비가 사방에서 밀려온다.",
                    "바리케이드가 없으면 모닥불이 무너진다.",
                    "",
                    "모닥불 반경에 들어온 좀비는",
                    "서서히 불타 쓰러진다.",
                    "",
                    "모든 웨이브를 버티면 새벽이 온다.",
                },
                Tip = "해가 지기 전에 준비하라!",
                ConfirmLabel = "시작하기  [Enter]",
            },
        };

        // ── state ───────────────────────────────────────────────────────────────
        private int _index;
        private float _alpha;
        private bool _advancing;

        // ── layout constants ────────────────────────────────────────────────────
        private const float PW = 620f;
        private const float PH = 380f;
        private const float KEY_COL = 200f;

        // ── GUIStyles (built once) ──────────────────────────────────────────────
        private GUIStyle _headlineStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _bodyAccentStyle;
        private GUIStyle _tipStyle;
        private GUIStyle _pillStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _indicatorStyle;
        private bool _stylesBuilt;

        private void Start()
        {
            _index = 0;
            _alpha = 0f;
        }

        private void Update()
        {
            // fade in
            _alpha = Mathf.MoveTowards(_alpha, 1f, Time.deltaTime * 4f);

            if (_advancing) return;

            bool confirm = Input.GetKeyDown(KeyCode.Return)
                        || Input.GetKeyDown(KeyCode.Space)
                        || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (confirm) Advance();
        }

        private void Advance()
        {
            _index++;
            if (_index >= Slides.Length)
            {
                SceneManager.LoadScene(SnowfieldSceneName);
                _advancing = true;
                return;
            }
            _alpha = 0f;
        }

        // ── IMGUI ───────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            BuildStyles();

            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _alpha);

            float sw = Screen.width;
            float sh = Screen.height;
            float px = (sw - PW) * 0.5f;
            float py = (sh - PH) * 0.5f;

            ref readonly var slide = ref Slides[_index];
            bool isDay = slide.Phase == Phase.Day;
            Color accentColor = isDay ? UiTheme.TextGold : new Color(0.6f, 0.85f, 0.95f);
            Color tipColor    = isDay ? new Color(1f, 0.7f, 0.65f) : UiTheme.TextDanger;

            // ── full-screen dim ──────────────────────────────────────────────────
            UiTheme.Rect(new Rect(0, 0, sw, sh), new Color(0.02f, 0.04f, 0.08f, 0.88f));

            // ── panel ────────────────────────────────────────────────────────────
            UiTheme.Panel(new Rect(px, py, PW, PH));

            // ── phase pill ───────────────────────────────────────────────────────
            string pillLabel = isDay ? "낮  DAY" : "밤  NIGHT";
            Color pillBg = isDay
                ? new Color(0.30f, 0.20f, 0.05f, 0.6f)
                : new Color(0.10f, 0.18f, 0.35f, 0.6f);
            UiTheme.Rect(new Rect(px + 20, py + 14, 110, 26), pillBg);
            UiTheme.Rect(new Rect(px + 20, py + 14, 110, 1), accentColor * 0.55f);
            UiTheme.Rect(new Rect(px + 20, py + 39, 110, 1), accentColor * 0.55f);
            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, _alpha);
            GUI.Label(new Rect(px + 20, py + 17, 110, 22), pillLabel, _pillStyle);

            // ── slide indicator ──────────────────────────────────────────────────
            GUI.color = new Color(UiTheme.TextSubtle.r, UiTheme.TextSubtle.g, UiTheme.TextSubtle.b, _alpha);
            GUI.Label(new Rect(px + PW - 80, py + 16, 70, 20),
                $"{_index + 1} / {Slides.Length}", _indicatorStyle);

            // ── headline ─────────────────────────────────────────────────────────
            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, _alpha);
            GUI.Label(new Rect(px + 20, py + 52, PW - 40, 30), slide.Headline, _headlineStyle);

            // ── divider ──────────────────────────────────────────────────────────
            UiTheme.Separator(new Rect(px + 20, py + 88, PW - 40, 1));

            // ── body ─────────────────────────────────────────────────────────────
            float lineY = py + 100f;
            GUI.color = new Color(1f, 1f, 1f, _alpha);
            foreach (var line in slide.Body)
            {
                if (string.IsNullOrEmpty(line)) { lineY += 8f; continue; }

                int splitIdx = line.IndexOf("  ");
                if (splitIdx > 0 && splitIdx < line.Length - 2)
                {
                    string key  = line.Substring(0, splitIdx);
                    string desc = line.Substring(splitIdx + 2);
                    GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, _alpha);
                    GUI.Label(new Rect(px + 32, lineY, KEY_COL, 22), key, _bodyAccentStyle);
                    GUI.color = new Color(UiTheme.TextCream.r, UiTheme.TextCream.g, UiTheme.TextCream.b, _alpha);
                    GUI.Label(new Rect(px + 32 + KEY_COL, lineY, PW - 32 - KEY_COL - 20, 22), desc, _bodyStyle);
                }
                else
                {
                    GUI.color = new Color(UiTheme.TextCream.r, UiTheme.TextCream.g, UiTheme.TextCream.b, _alpha);
                    GUI.Label(new Rect(px + 32, lineY, PW - 64, 22), line, _bodyStyle);
                }
                lineY += 24f;
            }

            // ── tip banner ───────────────────────────────────────────────────────
            float tipY = py + PH - 94f;
            Color tipBg = isDay
                ? new Color(0.29f, 0.19f, 0.06f, 0.85f)
                : new Color(0.10f, 0.16f, 0.26f, 0.85f);
            UiTheme.Rect(new Rect(px + 16, tipY, PW - 32, 32), tipBg);
            GUI.color = new Color(tipColor.r, tipColor.g, tipColor.b, _alpha);
            GUI.Label(new Rect(px + 16, tipY + 6, PW - 32, 22), $"⚠  {slide.Tip}", _tipStyle);

            // ── confirm button ────────────────────────────────────────────────────
            float btnY = py + PH - 48f;
            float btnW = 240f;
            float btnX = px + (PW - btnW) * 0.5f;
            GUI.color = new Color(1f, 1f, 1f, _alpha);
            if (UiTheme.Button(new Rect(btnX, btnY, btnW, 34), slide.ConfirmLabel, _btnStyle))
                Advance();

            GUI.color = oldColor;
        }

        private void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _headlineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _headlineStyle.normal.textColor = UiTheme.TextGold;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
            };
            _bodyStyle.normal.textColor = UiTheme.TextCream;

            _bodyAccentStyle = new GUIStyle(_bodyStyle)
            {
                fontStyle = FontStyle.Bold,
            };
            _bodyAccentStyle.normal.textColor = UiTheme.TextGold;

            _tipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
            };
            _tipStyle.normal.textColor = UiTheme.TextDanger;

            _pillStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _pillStyle.normal.textColor = UiTheme.TextGold;

            _indicatorStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
            };
            _indicatorStyle.normal.textColor = UiTheme.TextSubtle;

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
            _btnStyle.normal.textColor = UiTheme.TextGold;
            _btnStyle.hover.textColor  = Color.white;
        }
    }
}
