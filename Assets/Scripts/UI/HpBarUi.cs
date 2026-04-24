using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 엔티티 머리 위 HP 바. Player 또는 Zombie 참조 중 하나 세팅.
    /// HP == MaxHp 일 때 자동 숨김. 런타임에 자식 SpriteRenderer 2개 생성.
    /// </summary>
    public sealed class HpBarUi : MonoBehaviour
    {
        public PlayerController Player;
        public Zombie Zombie;
        public Vector2 Offset = new Vector2(0f, 0.7f);
        public Vector2 Size = new Vector2(0.9f, 0.12f);
        public Color BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
        public Color FillColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        private GameObject _root;
        private Transform _fill;

        private void Awake()
        {
            _root = new GameObject("HpBar");
            _root.transform.SetParent(transform, false);
            _root.transform.localPosition = (Vector3)Offset;
            _root.transform.localScale = new Vector3(Size.x, Size.y, 1f);

            var bgSr = _root.AddComponent<SpriteRenderer>();
            bgSr.sortingOrder = 20;
            bgSr.color = BgColor;
            var bgCf = _root.AddComponent<ColorFallback>();
            bgCf.Tint = BgColor;
            bgCf.Shape = FallbackShape.Square;
            bgCf.Circle = false;
            bgCf.PixelSize = 32;
            bgCf.OutlineWidth = 0;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_root.transform, false);
            fillGo.transform.localPosition = Vector3.zero;
            fillGo.transform.localScale = Vector3.one;
            var fillSr = fillGo.AddComponent<SpriteRenderer>();
            fillSr.sortingOrder = 21;
            fillSr.color = FillColor;
            var fillCf = fillGo.AddComponent<ColorFallback>();
            fillCf.Tint = FillColor;
            fillCf.Shape = FallbackShape.Square;
            fillCf.Circle = false;
            fillCf.PixelSize = 32;
            fillCf.OutlineWidth = 0;
            _fill = fillGo.transform;

            _root.SetActive(false);
        }

        private void LateUpdate()
        {
            float ratio = GetHpRatio();
            bool show = ratio < 0.999f && ratio > 0f;
            if (_root.activeSelf != show) _root.SetActive(show);
            if (show && _fill != null)
            {
                _fill.localScale = new Vector3(ratio, 1f, 1f);
                _fill.localPosition = new Vector3(-(1f - ratio) * 0.5f, 0f, 0f);
            }
        }

        private float GetHpRatio()
        {
            if (Player != null) return (float)Player.CurrentHp / Mathf.Max(1, Player.MaxHp);
            if (Zombie != null) return (float)Zombie.CurrentHp / Mathf.Max(1, Zombie.MaxHp);
            return 1f;
        }
    }
}
