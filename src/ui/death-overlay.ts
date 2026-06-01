import type Phaser from 'phaser';

/**
 * 사망 시 화면 어두워짐 + "죽었습니다" 텍스트 + 잠시 후 콜백.
 */
export class DeathOverlay {
  static show(scene: Phaser.Scene, onComplete: () => void): void {
    const w = Number(scene.scale.width);
    const h = Number(scene.scale.height);

    const overlay = scene.add
      .rectangle(0, 0, w, h, 0x000000, 0)
      .setOrigin(0, 0)
      .setScrollFactor(0)
      .setDepth(2000);

    const title = scene.add
      .text(w / 2, h / 2 - 20, '죽었습니다', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '36px',
        color: '#ff4040',
      })
      .setOrigin(0.5)
      .setScrollFactor(0)
      .setDepth(2001)
      .setAlpha(0);

    const sub = scene.add
      .text(w / 2, h / 2 + 24, '마을이 무너졌습니다. Day 1 부터 다시 시작합니다…', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '14px',
        color: '#ffffff',
      })
      .setOrigin(0.5)
      .setScrollFactor(0)
      .setDepth(2001)
      .setAlpha(0);

    scene.cameras.main.shake(500, 0.015);

    scene.tweens.add({
      targets: overlay,
      fillAlpha: 0.85,
      duration: 1200,
      ease: 'Quad.Out',
    });
    scene.tweens.add({
      targets: [title, sub],
      alpha: 1,
      duration: 800,
      delay: 500,
    });
    scene.time.delayedCall(2800, onComplete);
  }
}
