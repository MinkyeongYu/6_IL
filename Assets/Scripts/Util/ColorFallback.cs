using UnityEngine;

namespace IL6
{
    /// <summary>
    /// SpriteRenderer 에 sprite 할당이 없을 때, 지정한 색/모양으로 런타임에 스프라이트 생성.
    /// 에셋 파일 없이도 씬이 즉시 플레이 가능하도록 함. PixelSize 를 PPU 로 사용하므로
    /// 기본 1 유닛 크기.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ColorFallback : MonoBehaviour
    {
        public Color Tint = Color.white;
        public bool Circle = true;
        public int PixelSize = 64;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite != null) return;

            var tex = new Texture2D(PixelSize, PixelSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
            };
            float r = PixelSize / 2f;
            var transparent = new Color(0, 0, 0, 0);
            for (int y = 0; y < PixelSize; y++)
            {
                for (int x = 0; x < PixelSize; x++)
                {
                    bool inside = true;
                    if (Circle)
                    {
                        float dx = x - r, dy = y - r;
                        inside = dx * dx + dy * dy < r * r;
                    }
                    tex.SetPixel(x, y, inside ? Color.white : transparent);
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
    }
}
