using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 마우스로 건물 배치. start(kind) 호출 후 마우스 클릭으로 confirm.
    /// VillageGrid에 셀 점유 등록.
    /// </summary>
    public sealed class PlacementController : MonoBehaviour
    {
        public Camera MainCamera;
        public ResourceStore Store;
        public VillageGrid Grid;
        public GameObject CampfirePrefab;
        public GameObject BarricadePrefab;

        private BuildingKind? _currentKind;
        private SpriteRenderer _cursor;
        private static int _nextEid = 1;

        private void Awake()
        {
            if (MainCamera == null) MainCamera = Camera.main;
            EnsureCursor();
        }

        private void EnsureCursor()
        {
            if (_cursor != null) return;
            var go = new GameObject("PlacementCursor");
            _cursor = go.AddComponent<SpriteRenderer>();
            _cursor.color = new Color(0.29f, 0.56f, 0.89f, 0.4f);
            _cursor.sortingOrder = 50;
            _cursor.enabled = false;
        }

        public void Begin(BuildingKind kind)
        {
            _currentKind = kind;
            _cursor.enabled = true;
            int w = (kind == BuildingKind.Campfire) ? 2 : 1;
            int h = w;
            _cursor.size = new Vector2(w * Grid.TileSize, h * Grid.TileSize);
            _cursor.drawMode = SpriteDrawMode.Sliced;
            if (_cursor.sprite == null)
            {
                // 생성: 흰색 단색 텍스처
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _cursor.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0, 0), 1f);
            }
        }

        public void Cancel()
        {
            _currentKind = null;
            if (_cursor != null) _cursor.enabled = false;
        }

        private void Update()
        {
            if (_currentKind == null) return;

            Vector2 worldPos = MainCamera.ScreenToWorldPoint(Input.mousePosition);
            var tile = Grid.WorldToTile(worldPos);
            int w = _currentKind == BuildingKind.Campfire ? 2 : 1;
            int h = w;
            _cursor.transform.position = new Vector3(tile.x * Grid.TileSize, tile.y * Grid.TileSize, 0);

            if (Input.GetMouseButtonDown(0))
            {
                TryConfirm(tile.x, tile.y, w, h, _currentKind.Value);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
            }
        }

        private void TryConfirm(int tx, int ty, int w, int h, BuildingKind kind)
        {
            if (Store == null || Grid == null) return;
            int cost = (kind == BuildingKind.Campfire)
                ? BalanceConfig.Instance.CampfireCost
                : BalanceConfig.Instance.BarricadeCost;
            if (Store.Get(ResourceKind.Wood) < cost) return;
            if (!Grid.IsFree(tx, ty, w, h)) return;

            Store.Spend(ResourceKind.Wood, cost);
            int eid = _nextEid++;

            var prefab = kind == BuildingKind.Campfire ? CampfirePrefab : BarricadePrefab;
            if (prefab == null) { Debug.LogWarning("Building prefab not assigned."); return; }

            var center = Grid.TileToWorld(tx, ty);
            var go = Instantiate(prefab, new Vector3(center.x + (w - 1) * Grid.TileSize / 2f, center.y + (h - 1) * Grid.TileSize / 2f, 0), Quaternion.identity);
            var b = go.GetComponent<Building>();
            if (b != null) { b.Kind = kind; b.Eid = eid; b.Grid = Grid; }
            Grid.Place(tx, ty, w, h, eid);
        }
    }
}
