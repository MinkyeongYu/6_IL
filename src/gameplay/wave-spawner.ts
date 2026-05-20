import { VILLAGE_GRID_SIZE, TILE_SIZE } from '@/config/constants';

export interface WaveData {
  waveNumber: number;
  count: number;
  positions: { x: number; y: number }[];
}

const BASE_COUNT = 8;
const COUNT_PER_DAY = 4;
const SPAWN_DISTANCE = (VILLAGE_GRID_SIZE * TILE_SIZE) / 2 + 64;

export class WaveSpawner {
  readonly totalWaves = 3;

  getWave(day: number, waveNumber: number): WaveData {
    const count = BASE_COUNT + (day - 1) * COUNT_PER_DAY + (waveNumber - 1) * 2;
    const positions = this.generatePositions(count);
    return { waveNumber, count, positions };
  }

  private generatePositions(count: number): { x: number; y: number }[] {
    const positions: { x: number; y: number }[] = [];
    for (let i = 0; i < count; i++) {
      const angle = (Math.PI * 2 * i) / count + (Math.random() * 0.3 - 0.15);
      const dist = SPAWN_DISTANCE + Math.random() * 40;
      positions.push({
        x: Math.cos(angle) * dist,
        y: Math.sin(angle) * dist,
      });
    }
    return positions;
  }
}
