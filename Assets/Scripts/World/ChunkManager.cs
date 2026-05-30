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
            SpawnSnowPatches(cx, cy, rng, data);

            _loaded[(cx, cy)] = data;
        }

        /// <summary>
        /// 청크 경계가 뚝뚝 끊겨 보이는 현상 완화.
        /// 반투명 흰 원(snow_patch)을 청크 전체에 8-12개 랜덤 배치하고,
        /// 경계 가장자리(폭 ChunkSize/4)에 4개 추가해 인접 청크와 자연스럽게 혼합되게 함.
        /// sortingOrder = -1 로 타일 아래에 깔림. 청크 언로드 시 data.Spawned 로 같이 제거.
        /// </summary>
        private void SpawnSnowPatches(int cx, int cy, SeededRng rng, ChunkData data)
        {
            float baseX = cx * ChunkSize;
            float baseY = cy * ChunkSize;
            float edgeBand = ChunkSize * 0.25f; // 경계 가장자리 범위

            // 청크 내부 전체 — 8-12개
            int patchCount = rng.IntRange(8, 12);
            for (int i = 0; i < patchCount; i++)
            {
                float x = baseX + rng.Next() * ChunkSize;
                float y = baseY + rng.Next() * ChunkSize;
                float scale = 2.5f + rng.Next() * 2.0f; // 2.5 ~ 4.5
                float alpha = 0.15f + rng.Next() * 0.20f; // 0.15 ~ 0.35
                data.Spawned.Add(CreateSnowPatch(x, y, scale, alpha));
            }

            // 4방향 가장자리에 각 2-3개 추가 — 인접 청크 경계 블렌딩
            int edgeCount = rng.IntRange(2, 3);
            // 하단 경계
            for (int i = 0; i < edgeCount; i++)
            {
                float x = baseX + rng.Next() * ChunkSize;
                float y = baseY + rng.Next() * edgeBand;
                float scale = 3.0f + rng.Next() * 1.5f;
                float alpha = 0.20f + rng.Next() * 0.15f;
                data.Spawned.Add(CreateSnowPatch(x, y, scale, alpha));
            }
            // 상단 경계
            for (int i = 0; i < edgeCount; i++)
            {
                float x = baseX + rng.Next() * ChunkSize;
                float y = baseY + ChunkSize - rng.Next() * edgeBand;
                float scale = 3.0f + rng.Next() * 1.5f;
                float alpha = 0.20f + rng.Next() * 0.15f;
                data.Spawned.Add(CreateSnowPatch(x, y, scale, alpha));
            }
            // 좌측 경계
            for (int i = 0; i < edgeCount; i++)
            {
                float x = baseX + rng.Next() * edgeBand;
                float y = baseY + rng.Next() * ChunkSize;
                float scale = 3.0f + rng.Next() * 1.5f;
                float alpha = 0.20f + rng.Next() * 0.15f;
                data.Spawned.Add(CreateSnowPatch(x, y, scale, alpha));
            }
            // 우측 경계
            for (int i = 0; i < edgeCount; i++)
            {
                float x = baseX + ChunkSize - rng.Next() * edgeBand;
                float y = baseY + rng.Next() * ChunkSize;
                float scale = 3.0f + rng.Next() * 1.5f;
                float alpha = 0.20f + rng.Next() * 0.15f;
                data.Spawned.Add(CreateSnowPatch(x, y, scale, alpha));
            }
        }

        private static GameObject CreateSnowPatch(float x, float y, float scale, float alpha)
        {
            var go = new GameObject("snow_patch");
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -1; // 타일맵 아래에 깔림

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 1f, 1f, alpha);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 64;
            cf.OutlineWidth = 0; // 외곽선 없음 — 부드럽게 녹아들도록
            return go;
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
