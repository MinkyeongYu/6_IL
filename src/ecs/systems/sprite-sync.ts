import type Phaser from 'phaser';
import type { GameWorld } from '../world';
import { Position } from '../components/position';
import { SpriteRef } from '../components/sprite-ref';
import { spriteQuery } from '../queries';

/**
 * ECS Position을 Phaser sprite에 복사.
 * spriteMap: SpriteRef.gid → Phaser GameObject
 */
export function spriteSyncSystem(
  world: GameWorld,
  spriteMap: Map<number, Phaser.GameObjects.GameObject & Phaser.GameObjects.Components.Transform>,
): void {
  const entities = spriteQuery(world);
  for (const eid of entities) {
    const gid = SpriteRef.gid[eid]!;
    const obj = spriteMap.get(gid);
    if (obj) {
      obj.x = Position.x[eid]!;
      obj.y = Position.y[eid]!;
    }
  }
}
