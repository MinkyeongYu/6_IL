using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 마을 타일 그리드. 건물 풋프린트 단위로 셀에 entity id를 기록.
    /// </summary>
    public sealed class VillageGrid
    {
        private const int Empty = 0;
        public int Width { get; }
        public int Height { get; }
        public int TileSize { get; }
        private readonly int[] _cells;

        public VillageGrid(int width, int height, int tileSize = 32)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            _cells = new int[width * height];
        }

        private int Idx(int tx, int ty) => ty * Width + tx;

        private bool InBounds(int tx, int ty, int w, int h) =>
            tx >= 0 && ty >= 0 && tx + w <= Width && ty + h <= Height && w > 0 && h > 0;

        public bool IsFree(int tx, int ty, int w, int h)
        {
            if (!InBounds(tx, ty, w, h)) return false;
            for (int y = ty; y < ty + h; y++)
                for (int x = tx; x < tx + w; x++)
                    if (_cells[Idx(x, y)] != Empty) return false;
            return true;
        }

        public bool Place(int tx, int ty, int w, int h, int eid)
        {
            if (!IsFree(tx, ty, w, h)) return false;
            for (int y = ty; y < ty + h; y++)
                for (int x = tx; x < tx + w; x++)
                    _cells[Idx(x, y)] = eid;
            return true;
        }

        public void Remove(int eid)
        {
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] == eid) _cells[i] = Empty;
        }

        public int GetCell(int tx, int ty)
        {
            if (!InBounds(tx, ty, 1, 1)) return Empty;
            return _cells[Idx(tx, ty)];
        }

        public Vector2 TileToWorld(int tx, int ty) =>
            new(tx * TileSize + TileSize / 2f, ty * TileSize + TileSize / 2f);

        public Vector2Int WorldToTile(Vector2 worldPos) =>
            new(Mathf.FloorToInt(worldPos.x / TileSize), Mathf.FloorToInt(worldPos.y / TileSize));
    }
}
