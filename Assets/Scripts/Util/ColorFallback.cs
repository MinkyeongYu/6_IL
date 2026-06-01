using UnityEngine;

namespace IL6
{
    public enum FallbackShape { Circle, Square, Triangle, Rounded }

    /// <summary>
    /// 에셋 없이 런타임에 색상/모양의 스프라이트 생성 + 외곽선.
    /// PixelSize 를 PPU 로 써서 기본 1 유닛.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ColorFallback : MonoBehaviour
    {
        public Color Tint = Color.white;
        public FallbackShape Shape = FallbackShape.Circle;
        [Tooltip("레거시 호환: true=원, false=사각형. Shape 필드가 Circle 이면 이 값 사용")]
        public bool Circle = true;
        public int PixelSize = 64;
        [Tooltip("0 이면 외곽선 없음. 1~4 권장.")]
        public int OutlineWidth = 2;
        public Color OutlineColor = new Color(0.1f, 0.1f, 0.15f, 1f);

        // Start (not Awake) so that fields set between AddComponent<>() and
        // end of initialization are applied before sprite generation.
        private void Start()
        {
            var sr = GetComponent<SpriteRenderer>();

            // 이름 기반 자동 스프라이트 로드 + 스케일 보정 (씬 수동 배치 포함)
            if (sr.sprite == null)
            {
                string n = gameObject.name.ToLowerInvariant();
                Sprite auto = null;
                Vector3 autoScale = Vector3.zero; // 0 = 스케일 유지

                if (n.Contains("pine") || (n.Contains("tree") && !n.Contains("bare")))
                    { auto = SpriteBank.PineTree();      autoScale = Vector3.one * 2.2f; }
                else if (n.Contains("bare"))
                    { auto = SpriteBank.BareTree();      autoScale = Vector3.one * 1.8f; }
                else if (n.Contains("rock") || n.Contains("snow_rock"))
                    { auto = SpriteBank.SnowRocks();     autoScale = new Vector3(2.0f, 1.5f, 1f); }
                else if (n.Contains("small_rock"))
                    { auto = SpriteBank.SmallRocks();    autoScale = Vector3.one * 1.2f; }
                else if (n.Contains("campfire"))
                    { auto = SpriteBank.Campfire();      autoScale = Vector3.one * 1.4f; }
                else if (n.Contains("stump"))
                    { auto = SpriteBank.Stump();         autoScale = Vector3.one * 1.0f; }
                else if (n.Contains("bush"))
                    { auto = SpriteBank.SnowBush();      autoScale = Vector3.one * 1.0f; }
                else if (n.Contains("log"))
                    { auto = SpriteBank.Logs();          autoScale = Vector3.one * 1.2f; }

                if (auto != null)
                {
                    sr.sprite = auto;
                    // 스케일이 기본값(1,1,1)이면 권장 스케일 적용
                    if (autoScale != Vector3.zero && transform.localScale == Vector3.one)
                        transform.localScale = autoScale;
                }
            }

            if (sr.sprite != null) return;

            var shape = Shape;
            if (shape == FallbackShape.Circle && !Circle) shape = FallbackShape.Square;

            var tex = new Texture2D(PixelSize, PixelSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
            };
            var transparent = new Color(0, 0, 0, 0);

            // Outline 을 텍스처의 알파로 표현하면 안 됨 (sr.color 로 Tint 되어야 하므로).
            // 대신: 외곽선 픽셀은 OutlineColor 그대로 두고, 내부는 Tint 만 받도록 흰색.
            // SpriteRenderer.color 는 곱연산이므로 흰색 * Tint = Tint. 외곽선은 OutlineColor * Tint 이 되어
            // 약간 Tint 영향을 받지만 시각적으로 충분히 외곽선 역할 함.

            for (int y = 0; y < PixelSize; y++)
            {
                for (int x = 0; x < PixelSize; x++)
                {
                    if (!IsInside(x, y, PixelSize, shape))
                    {
                        tex.SetPixel(x, y, transparent);
                        continue;
                    }
                    if (OutlineWidth > 0 && IsNearEdge(x, y, PixelSize, shape, OutlineWidth))
                    {
                        tex.SetPixel(x, y, OutlineColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }
            tex.Apply();

            sr.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, PixelSize, PixelSize),
                new Vector2(0.5f, 0.5f),
                PixelSize);
            sr.color = Tint;
        }

        private static bool IsInside(int x, int y, int size, FallbackShape shape)
        {
            float cx = size / 2f, cy = size / 2f;
            float dx = x - cx, dy = y - cy;
            float r = size / 2f;
            switch (shape)
            {
                case FallbackShape.Circle:
                    return dx * dx + dy * dy < r * r;
                case FallbackShape.Square:
                    return true;
                case FallbackShape.Triangle:
                {
                    // Pointing up: apex at (cx, 0), base y = size-1
                    float yNorm = y / (float)(size - 1); // 0 bottom apex ... 1 top in texture coords
                    // flip so apex is at top: we want narrow at top
                    float apexDistFromTop = (size - 1 - y) / (float)(size - 1); // 0 at top, 1 at bottom
                    float halfWidth = apexDistFromTop * r;
                    return Mathf.Abs(dx) < halfWidth;
                }
                case FallbackShape.Rounded:
                {
                    float cornerR = r * 0.35f;
                    float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
                    if (ax <= r - cornerR || ay <= r - cornerR) return true;
                    float px = ax - (r - cornerR), py = ay - (r - cornerR);
                    return px * px + py * py < cornerR * cornerR;
                }
                default: return false;
            }
        }

        private static bool IsNearEdge(int x, int y, int size, FallbackShape shape, int width)
        {
            // "Near edge" = inside but within 'width' pixels of being outside.
            for (int dy = -width; dy <= width; dy++)
            {
                for (int dx = -width; dx <= width; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size) return true;
                    if (!IsInside(nx, ny, size, shape)) return true;
                }
            }
            return false;
        }
    }
}
