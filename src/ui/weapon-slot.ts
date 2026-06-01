import type Phaser from 'phaser';
import { Weapon } from '@/ecs/components/weapon';
import { WEAPONS } from '@/gameplay/combat/weapon-definitions';

const SLOT_W = 64;
const SLOT_H = 64;

/**
 * 우하단에 장착 무기 표시. 아이콘(컬러 박스) + 이름 + 쿨다운 바.
 */
export class WeaponSlot {
  private readonly bg: Phaser.GameObjects.Rectangle;
  private readonly icon: Phaser.GameObjects.Image;
  private readonly nameText: Phaser.GameObjects.Text;
  private readonly cdBg: Phaser.GameObjects.Rectangle;
  private readonly cdFg: Phaser.GameObjects.Rectangle;

  constructor(
    scene: Phaser.Scene,
    x: number,
    y: number,
    private readonly playerEid: number,
  ) {
    this.bg = scene.add.rectangle(x, y, SLOT_W, SLOT_H, 0x222831).setStrokeStyle(2, 0x4a90e2);
    // 무기 아이콘은 placeholder 텍스처 'projectile' (흰색 원) 활용
    this.icon = scene.add.image(x, y - 8, 'projectile').setScale(2.5);
    this.nameText = scene.add
      .text(x, y + 18, 'Longsword', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '11px',
        color: '#e0e6ed',
      })
      .setOrigin(0.5);

    this.cdBg = scene.add
      .rectangle(x - SLOT_W / 2, y + SLOT_H / 2, SLOT_W, 4, 0x333333)
      .setOrigin(0, 1);
    this.cdFg = scene.add
      .rectangle(x - SLOT_W / 2, y + SLOT_H / 2, SLOT_W, 4, 0x4a90e2)
      .setOrigin(0, 1);
  }

  update(): void {
    const cd = Weapon.cooldown[this.playerEid] ?? 0;
    const max = WEAPONS.longsword!.cooldownSec;
    const ratio = Math.max(0, Math.min(1, 1 - cd / max));
    this.cdFg.width = SLOT_W * ratio;
  }

  destroy(): void {
    this.bg.destroy();
    this.icon.destroy();
    this.nameText.destroy();
    this.cdBg.destroy();
    this.cdFg.destroy();
  }
}
