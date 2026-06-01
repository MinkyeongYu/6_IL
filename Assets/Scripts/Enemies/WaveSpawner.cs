using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    public class WavePlan
    {
        public int EnemyCount;
        public List<Vector2> SpawnPoints = new();
    }

    /// <summary>
    /// 결정적 좀비 웨이브 플래너. RNG 시드는 호출자가 주입.
    /// </summary>
    public sealed class WaveSpawner
    {
        private readonly SeededRng _rng;
        private readonly BalanceConfig _balance;

        // 마을 외곽 기본 스폰 후보
        private static readonly Vector2[] Candidates = new[]
        {
            new Vector2(-100, 384),
            new Vector2(864, 384),
            new Vector2(384, -100),
            new Vector2(384, 868),
        };

        public WaveSpawner(SeededRng rng, BalanceConfig balance)
        {
            _rng = rng;
            _balance = balance;
        }

        public WavePlan PlanWave(int day)
        {
            int rawCount = _balance.WaveBaseCount + (day - 1) * _balance.WavePerDay;
            int enemyCount = Mathf.Min(_balance.WaveMaxCount, rawCount);

            int numPoints = _rng.IntRange(2, Candidates.Length);
            var pool = new List<Vector2>(Candidates);
            var picked = new List<Vector2>(numPoints);
            for (int i = 0; i < numPoints && pool.Count > 0; i++)
            {
                int idx = _rng.IntRange(0, pool.Count - 1);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return new WavePlan { EnemyCount = enemyCount, SpawnPoints = picked };
        }
    }
}
