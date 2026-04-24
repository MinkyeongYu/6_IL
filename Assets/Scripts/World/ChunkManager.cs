using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 플레이어 주변 청크를 동적 로드/언로드. 각 청크에 트리/사슴을 절차 생성.
    /// 홈 청크(0,0)는 SnowfieldController가 직접 채움.
    /// </summary>
    public sealed class ChunkManager : MonoBehaviour
    {
        public Transform Player;
        public GameObject TreePrefab;
        public GameObject DeerPrefab;

        [Header("Tilemap")]
        public UnityEngine.Tilemaps.Tilemap GroundTilemap;
        public UnityEngine.Tilemaps.TileBase GroundTile;

        public int ChunkSize = 320;
        public int LoadRadius = 2;
        public int UnloadRadius = 4;

        private class ChunkData
        {
            public int Cx, Cy;
            public List<GameObject> Spawned = new();
        }

        private readonly Dictionary<(int, int), ChunkData> _loaded = new();

        private void Start()
        {
            // 홈 청크 표시 (SnowfieldController가 직접 배치)
            _loaded[(0, 0)] = new ChunkData { Cx = 0, Cy = 0 };
        }

        private void Update()
        {
            if (Player == null) return;
            EnsureLoaded();
            UnloadFar();
        }

        private void EnsureLoaded()
        {
            int pcx = Mathf.FloorToInt(Player.position.x / ChunkSize);
            int pcy = Mathf.FloorToInt(Player.position.y / ChunkSize);
            for (int dy = -LoadRadius; dy <= LoadRadius; dy++)
            {
                for (int dx = -LoadRadius; dx <= LoadRadius; dx++)
                {
                    int cx = pcx + dx, cy = pcy + dy;
                    if (!_loaded.ContainsKey((cx, cy))) LoadChunk(cx, cy);
                }
            }
        }

        private void UnloadFar()
        {
            int pcx = Mathf.FloorToInt(Player.position.x / ChunkSize);
            int pcy = Mathf.FloorToInt(Player.position.y / ChunkSize);
            var toRemove = new List<(int, int)>();
            foreach (var kv in _loaded)
            {
                if (kv.Key.Item1 == 0 && kv.Key.Item2 == 0) continue;
                int dist = Mathf.Max(Mathf.Abs(kv.Key.Item1 - pcx), Mathf.Abs(kv.Key.Item2 - pcy));
                if (dist > UnloadRadius)
                {
                    foreach (var go in kv.Value.Spawned)
                    {
                        if (go != null) Destroy(go);
                    }
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove) _loaded.Remove(k);
        }

        private void LoadChunk(int cx, int cy)
        {
            uint seed = unchecked((uint)((cx * 73856093) ^ (cy * 19349663)));
            var rng = new SeededRng(seed);
            var data = new ChunkData { Cx = cx, Cy = cy };

            int baseX = cx * ChunkSize;
            int baseY = cy * ChunkSize;

            int treeCount = rng.IntRange(0, 4);
            for (int i = 0; i < treeCount; i++)
            {
                int x = baseX + rng.IntRange(20, ChunkSize - 20);
                int y = baseY + rng.IntRange(20, ChunkSize - 20);
                if (TreePrefab != null)
                    data.Spawned.Add(Instantiate(TreePrefab, new Vector3(x, y, 0), Quaternion.identity));
            }
            int deerCount = rng.IntRange(0, 1);
            for (int i = 0; i < deerCount; i++)
            {
                int x = baseX + rng.IntRange(20, ChunkSize - 20);
                int y = baseY + rng.IntRange(20, ChunkSize - 20);
                if (DeerPrefab != null)
                    data.Spawned.Add(Instantiate(DeerPrefab, new Vector3(x, y, 0), Quaternion.identity));
            }

            FillGroundTiles(cx, cy);

            _loaded[(cx, cy)] = data;
        }

        private void FillGroundTiles(int cx, int cy)
        {
            if (GroundTilemap == null || GroundTile == null) return;
            int baseX = cx * ChunkSize;
            int baseY = cy * ChunkSize;
            // ChunkSize 는 월드 유닛 기준 (예: 320). Tilemap 셀은 1 유닛 = 1 타일.
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    GroundTilemap.SetTile(
                        new Vector3Int(baseX + x, baseY + y, 0),
                        GroundTile
                    );
                }
            }
        }
    }
}
