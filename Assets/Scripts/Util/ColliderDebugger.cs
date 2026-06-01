using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 런타임 충돌 범위 시각화 — 디버그용.
    /// SnowfieldScene 의 아무 오브젝트에 붙이면 모든 Collider2D 경계선 표시.
    /// 메뉴: IL6 > Toggle Collider Debug (또는 SimpleHud 디버그 패널 버튼)
    /// </summary>
    public sealed class ColliderDebugger : MonoBehaviour
    {
        public static bool Enabled = true;

        private static readonly Color _boxColor    = new Color(0f,   1f,   0f,   0.85f);
        private static readonly Color _circleColor = new Color(0f,   0.8f, 1f,   0.85f);
        private static readonly Color _trigColor   = new Color(1f,   0.9f, 0f,   0.6f);

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void OnGUI()
        {
            if (!Enabled) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            var cols = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            foreach (var col in cols)
            {
                if (col == null) continue;
                Color c = col.isTrigger ? _trigColor :
                          col is CircleCollider2D ? _circleColor : _boxColor;

                if (col is CircleCollider2D circle)
                    DrawCircle(circle, c);
                else
                    DrawBox(col, c);
            }
        }

        // ── Box Collider ───────────────────────────────────────────────────────
        private void DrawBox(Collider2D col, Color c)
        {
            Bounds b = col.bounds;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(b.min.x, b.min.y, 0f),
                new Vector3(b.max.x, b.min.y, 0f),
                new Vector3(b.max.x, b.max.y, 0f),
                new Vector3(b.min.x, b.max.y, 0f),
            };

            Vector2[] screen = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                var sp = _cam.WorldToScreenPoint(corners[i]);
                screen[i] = new Vector2(sp.x, Screen.height - sp.y);
            }
            DrawQuad(screen, c);
        }

        // ── Circle Collider ────────────────────────────────────────────────────
        private void DrawCircle(CircleCollider2D circle, Color c)
        {
            Vector2 center = (Vector2)circle.transform.position + circle.offset;
            float r = circle.radius * Mathf.Abs(circle.transform.lossyScale.x);
            const int segs = 16;
            Vector2[] pts = new Vector2[segs];
            for (int i = 0; i < segs; i++)
            {
                float ang = i / (float)segs * Mathf.PI * 2f;
                Vector3 wp = new Vector3(center.x + Mathf.Cos(ang) * r,
                                         center.y + Mathf.Sin(ang) * r, 0f);
                var sp = _cam.WorldToScreenPoint(wp);
                pts[i] = new Vector2(sp.x, Screen.height - sp.y);
            }
            for (int i = 0; i < segs; i++)
                DrawLine(pts[i], pts[(i + 1) % segs], c);
        }

        // ── Primitives ─────────────────────────────────────────────────────────
        private static void DrawQuad(Vector2[] p, Color c)
        {
            DrawLine(p[0], p[1], c);
            DrawLine(p[1], p[2], c);
            DrawLine(p[2], p[3], c);
            DrawLine(p[3], p[0], c);
        }

        private static void DrawLine(Vector2 a, Vector2 b, Color c)
        {
            float dx = b.x - a.x, dy = b.y - a.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f) return;

            var old = GUI.matrix;
            var oldC = GUI.color;

            // 회전 행렬로 선 그리기 (1px 높이 직사각형)
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.color = c;
            GUI.DrawTexture(new Rect(a.x, a.y - 0.5f, len, 1f), Texture2D.whiteTexture);

            GUI.matrix = old;
            GUI.color = oldC;
        }
    }
}
