import type { Rng } from '@/util/rng';

export interface SpawnPoint {
  x: number;
  y: number;
}

export interface WavePlan {
  enemyCount: number;
  spawnPoints: SpawnPoint[];
}

export interface WaveSpawner {
  planWave(day: number): WavePlan;
}

const BASE_COUNT = 8;
const PER_DAY = 4;
const MAX_COUNT = 300;

// 마을 외곽 기본 스폰 위치 후보 (월드 좌표)
const SPAWN_CANDIDATES: SpawnPoint[] = [
  { x: -100, y: 384 },
  { x: 864, y: 384 },
  { x: 384, y: -100 },
  { x: 384, y: 868 },
];

export function createWaveSpawner(rng: Rng): WaveSpawner {
  return {
    planWave(day) {
      const rawCount = BASE_COUNT + (day - 1) * PER_DAY;
      const enemyCount = Math.min(MAX_COUNT, rawCount);

      // 1~4개 스폰 포인트 선택 (시드 결정적)
      const numPoints = rng.intRange(2, SPAWN_CANDIDATES.length);
      const picked: SpawnPoint[] = [];
      const pool = [...SPAWN_CANDIDATES];
      for (let i = 0; i < numPoints; i++) {
        const idx = rng.intRange(0, pool.length - 1);
        const pt = pool.splice(idx, 1)[0];
        if (pt) picked.push(pt);
      }
      return { enemyCount, spawnPoints: picked };
    },
  };
}
