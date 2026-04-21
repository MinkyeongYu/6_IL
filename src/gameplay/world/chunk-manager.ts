import type Phaser from 'phaser';
import { type GameWorld, removeEntity } from '@/ecs/world';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { spawnTree } from '@/gameplay/gather/tree-factory';
import { spawnDeer } from '@/gameplay/animals/deer-factory';
import { createRng } from '@/util/rng';

const CHUNK_SIZE = 320; // px (10 tiles)
const LOAD_RADIUS = 2; // chunks
const UNLOAD_RADIUS = 4; // chunks

interface ChunkData {
  cx: number;
  cy: number;
  entities: number[];
}

function chunkSeed(cx: number, cy: number): number {
  return ((cx * 73856093) ^ (cy * 19349663)) >>> 0;
}

function chunkKey(cx: number, cy: number): string {
  return `${cx},${cy}`;
}

/**
 * 플레이어 주변 청크를 동적으로 로드/언로드.
 * 홈 청크(0,0)는 SnowfieldScene이 직접 채우므로 ChunkManager는 건드리지 않음.
 */
export class ChunkManager {
  private readonly loaded = new Map<string, ChunkData>();

  constructor(
    private readonly world: GameWorld,
    private readonly scene: Phaser.Scene,
    private readonly spriteMap: Map<number, Phaser.GameObjects.Image>,
    private readonly nextGid: () => number,
  ) {
    // 홈 청크는 항상 로드된 것으로 간주(SnowfieldScene이 자체 배치)
    this.loaded.set(chunkKey(0, 0), { cx: 0, cy: 0, entities: [] });
  }

  ensureLoaded(playerX: number, playerY: number): void {
    const pcx = Math.floor(playerX / CHUNK_SIZE);
    const pcy = Math.floor(playerY / CHUNK_SIZE);
    for (let dy = -LOAD_RADIUS; dy <= LOAD_RADIUS; dy++) {
      for (let dx = -LOAD_RADIUS; dx <= LOAD_RADIUS; dx++) {
        const cx = pcx + dx;
        const cy = pcy + dy;
        const key = chunkKey(cx, cy);
        if (!this.loaded.has(key)) {
          this.loadChunk(cx, cy);
        }
      }
    }
  }

  unloadFar(playerX: number, playerY: number): void {
    const pcx = Math.floor(playerX / CHUNK_SIZE);
    const pcy = Math.floor(playerY / CHUNK_SIZE);
    for (const [key, chunk] of this.loaded) {
      if (chunk.cx === 0 && chunk.cy === 0) continue; // 홈 청크 보존
      const dist = Math.max(Math.abs(chunk.cx - pcx), Math.abs(chunk.cy - pcy));
      if (dist > UNLOAD_RADIUS) {
        for (const eid of chunk.entities) {
          const gid = SpriteRef.gid[eid];
          if (gid !== undefined) {
            this.spriteMap.get(gid)?.destroy();
            this.spriteMap.delete(gid);
          }
          removeEntity(this.world, eid);
        }
        this.loaded.delete(key);
      }
    }
  }

  private loadChunk(cx: number, cy: number): void {
    const seed = chunkSeed(cx, cy);
    const rng = createRng(seed);
    const entities: number[] = [];
    const baseX = cx * CHUNK_SIZE;
    const baseY = cy * CHUNK_SIZE;

    const treeCount = rng.intRange(0, 4);
    for (let i = 0; i < treeCount; i++) {
      const x = baseX + rng.intRange(20, CHUNK_SIZE - 20);
      const y = baseY + rng.intRange(20, CHUNK_SIZE - 20);
      entities.push(spawnTree(this.world, this.scene, this.spriteMap, this.nextGid, x, y));
    }
    const deerCount = rng.intRange(0, 1);
    for (let i = 0; i < deerCount; i++) {
      const x = baseX + rng.intRange(20, CHUNK_SIZE - 20);
      const y = baseY + rng.intRange(20, CHUNK_SIZE - 20);
      entities.push(spawnDeer(this.world, this.scene, this.spriteMap, this.nextGid, x, y));
    }

    this.loaded.set(chunkKey(cx, cy), { cx, cy, entities });
  }

  reset(): void {
    this.loaded.clear();
    this.loaded.set(chunkKey(0, 0), { cx: 0, cy: 0, entities: [] });
  }
}
