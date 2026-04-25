using UnityEngine;

namespace IL6
{
    /// <summary>
    /// IMGUI HUD 공용 색상 + 헬퍼. 모든 패널/바/아이콘 그릴 때 이 함수만 호출하면
    /// 일관된 중세 양피지/네이비 톤이 유지됨.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color PanelBg = new Color(0.07f, 0.09f, 0.14f, 0.94f);
        public static readonly Color PanelInnerBg = new Color(0.11f, 0.13f, 0.18f, 1f);
        public static readonly Color PanelBorder = new Color(0.78f, 0.62f, 0.30f, 1f);
        public static readonly Color PanelBorderDim = new Color(0.45f, 0.36f, 0.18f, 1f);
        public static readonly Color SeparatorGold = new Color(0.78f, 0.62f, 0.30f, 0.4f);

        public static readonly Color TextCream = new Color(0.96f, 0.94f, 0.88f);
        public static readonly Color TextGold = new Color(1f, 0.86f, 0.45f);
        public static readonly Color TextDanger = new Color(0.95f, 0.35f, 0.32f);
        public static readonly Color TextSubtle = new Color(0.66f, 0.7f, 0.78f);
        public static readonly Color TextHealth = new Color(0.86f, 0.95f, 0.85f);

        public static readonly Color BarBg = new Color(0.04f, 0.05f, 0.08f, 1f);
        public static readonly Color BarHpFill = new Color(0.85f, 0.2f, 0.18f);
        public static readonly Color BarXpFill = new Color(0.4f, 0.85f, 1f);
        public static readonly Color BarCdFill = new Color(0.95f, 0.78f, 0.35f);

        // 자원별 색
        public static Color ResColor(ResourceKind k) => k switch
        {
            ResourceKind.Wood => new Color(0.55f, 0.36f, 0.18f),
            ResourceKind.Stone => new Color(0.55f, 0.55f, 0.6f),
            ResourceKind.Meat => new Color(0.78f, 0.30f, 0.36f),
            ResourceKind.Food => new Color(0.7f, 0.78f, 0.32f),
            ResourceKind.Frostbloom => new Color(0.45f, 0.85f, 1f),
            _ => Color.white,
        };

        public static void Rect(Rect r, Color c)
        {
            var oldC = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = oldC;
        }

        public static void Panel(Rect r)
        {
            // 외곽 골드 보더 1px
            Rect(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), PanelBorder);
            // 배경
            Rect(r, PanelBg);
            // 상단 골드 하이라이트 라인
            Rect(new Rect(r.x + 4, r.y + 4, r.width - 8, 1), new Color(1f, 0.86f, 0.45f, 0.18f));
        }

        public static void TitleBar(Rect r, string title, GUIStyle titleStyle)
        {
            Rect(new Rect(r.x, r.y, r.width, 24), new Color(0.78f, 0.62f, 0.30f, 0.18f));
            // 좌우 끝 작은 다이아몬드 표시
            Rect(new Rect(r.x + 6, r.y + 10, 4, 4), TextGold);
            Rect(new Rect(r.x + r.width - 10, r.y + 10, 4, 4), TextGold);
            GUI.Label(new Rect(r.x, r.y + 2, r.width, 20), title, titleStyle);
            // 하단 골드 라인
            Rect(new Rect(r.x + 4, r.y + 24, r.width - 8, 1), SeparatorGold);
        }

        public static void Separator(Rect r)
        {
            Rect(r, SeparatorGold);
        }

        public static void Icon(Rect r, Color fill)
        {
            // 검은 외곽
            Rect(r, new Color(0f, 0f, 0f, 1f));
            // 안쪽
            Rect(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), fill);
            // 상단 하이라이트
            Rect(new Rect(r.x + 2, r.y + 2, r.width - 4, 1), new Color(1f, 1f, 1f, 0.25f));
        }

        public static void Bar(Rect r, float pct, Color fill)
        {
            // 깊은 어두운 배경
            Rect(r, BarBg);
            // 바깥 1px 어두운 골드
            Rect(new Rect(r.x, r.y, r.width, 1), PanelBorderDim);
            Rect(new Rect(r.x, r.yMax - 1, r.width, 1), PanelBorderDim);
            Rect(new Rect(r.x, r.y, 1, r.height), PanelBorderDim);
            Rect(new Rect(r.xMax - 1, r.y, 1, r.height), PanelBorderDim);
            // 채움
            float w = (r.width - 2) * Mathf.Clamp01(pct);
            Rect(new Rect(r.x + 1, r.y + 1, w, r.height - 2), fill);
            // 상단 하이라이트 라인 (살짝 밝은 톤)
            if (w > 2)
            {
                Rect(new Rect(r.x + 1, r.y + 1, w, 1), new Color(1f, 1f, 1f, 0.3f));
            }
        }

        public static bool Button(Rect r, string label, GUIStyle style, bool enabled = true)
        {
            if (!enabled)
            {
                Rect(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), PanelBorderDim);
                Rect(r, new Color(0.10f, 0.10f, 0.13f, 0.85f));
                var oldC = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(r, label, style);
                GUI.color = oldC;
                return false;
            }
            return GUI.Button(r, label, style);
        }
    }
}
