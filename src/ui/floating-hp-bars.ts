import type Phaser from 'phaser';
import { defineQuery, hasComponent } from 'bitecs';
import type { GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Health } from '@/ecs/components/health';
import { ZombieTag, DeerTag } from '@/ecs/components/tags';

const BAR_W = 24;
const BAR_H = 3;
const Y_OFFSET = -20;

const damagableQuery = defineQuery([Health, Position]);

interface BarPair {
  bg: Phaser.GameObjects.Rectangle;
  fg: Phaser.GameObjects.Rectangle;
}

/**
 * 좀비/사슴이 풀피가 아닐 때 머리 위에 작은 HP 바 표시.
 */
export class FloatingHpBars {
  private readonly bars = new Map<number, BarPair>();

  constructor(
    private readonly scene: Phaser.Scene,
    private readonly world: GameWorld,
  ) {}

  update(): void {
    const ents = damagableQuery(this.world);
    const seen = new Set<number>();
    for (const eid of ents) {
      const cur = Health.current[eid] ?? 0;
      const max = Health.max[eid] ?? 1;
      if (max <= 0) continue;
      if (cur >= max) continue;
      if (!hasComponent(this.world, ZombieTag, eid) && !hasComponent(this.world, DeerTag, eid)) {
        continue;
      }

      seen.add(eid);
      let bar = this.bars.get(eid);
      const x = Position.x[eid] ?? 0;
      const y = (Position.y[eid] ?? 0) + Y_OFFSET;
      if (!bar) {
        const bg = this.scene.add.rectangle(x, y, BAR_W, BAR_H, 0x222222).setDepth(900);
        const fg = this.scene.add
          .rectangle(x - BAR_W / 2, y, BAR_W, BAR_H, 0xff5050)
          .setOrigin(0, 0.5)
          .setDepth(901);
        bar = { bg, fg };
        this.bars.set(eid, bar);
      }
      bar.bg.setPosition(x, y);
      bar.fg.setPosition(x - BAR_W / 2, y);
      bar.fg.width = BAR_W * Math.max(0, Math.min(1, cur / max));
    }

    for (const [eid, bar] of this.bars) {
      if (!seen.has(eid)) {
        bar.bg.destroy();
        bar.fg.destroy();
        this.bars.delete(eid);
      }
    }
  }

  destroy(): void {
    for (const bar of this.bars.values()) {
      bar.bg.destroy();
      bar.fg.destroy();
    }
    this.bars.clear();
  }
}
