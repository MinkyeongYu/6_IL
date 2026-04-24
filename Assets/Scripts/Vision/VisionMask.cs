using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 단순 원형 시야 마스크. SpriteMask + 검정 오버레이를 카메라에 부착.
    /// MVP: 검은 풀스크린 + 플레이어 위치 원형 구멍 (스프라이트 마스크).
    /// 더 정교한 셰이더 기반 시야는 추후.
    /// </summary>
    public sealed class VisionMask : MonoBehaviour
    {
        public Transform Target;
        public float RadiusUnits = 3.2f; // = 10 tiles * 32px / 100 PPU
        public Camera Cam;

        private SpriteRenderer _overlay;
        private SpriteMask _mask;

        private void Awake()
        {
            if (Cam == null) Cam = Camera.main;
            BuildOverlay();
            BuildMask();
        }

        private void BuildOverlay()
        {
            var go = new GameObject("VisionOverlay");
            go.transform.SetParent(transform);
            _overlay = go.AddComponent<SpriteRenderer>();
            _overlay.color = new Color(0, 0, 0, 0.85f);
            _overlay.sortingOrder = 1000;
            // 1x1 흰 텍스처
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _overlay.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _overlay.drawMode = SpriteDrawMode.Sliced;
            _overlay.size = new Vector2(200, 200);
            _overlay.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        }

        private void BuildMask()
        {
            var go = new GameObject("VisionHole");
            go.transform.SetParent(transform);
            _mask = go.AddComponent<SpriteMask>();
            // 원 텍스처 만들기
            var tex = MakeCircleTexture(64);
            _mask.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f / (RadiusUnits * 2));
        }

        private static Texture2D MakeCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r, dy = y - r;
                    bool inside = dx * dx + dy * dy < r * r;
                    tex.SetPixel(x, y, inside ? Color.white : new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return tex;
        }

        private void LateUpdate()
        {
            if (Target == null || Cam == null) return;
            // 오버레이는 카메라 따라
            transform.position = new Vector3(Cam.transform.position.x, Cam.transform.position.y, 0);
            // 구멍은 타겟 (플레이어) 위에
            if (_mask != null) _mask.transform.position = new Vector3(Target.position.x, Target.position.y, 0);
        }

        public void SetRadius(float radiusUnits)
        {
            RadiusUnits = radiusUnits;
            if (_mask != null && _mask.sprite != null)
            {
                _mask.transform.localScale = new Vector3(radiusUnits / 3.2f, radiusUnits / 3.2f, 1);
            }
        }
    }
}
