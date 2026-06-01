import type Phaser from 'phaser';

/**
 * 단순 원형 시야. 카메라에 검은 오버레이를 씌우고 플레이어 위치에 원형 구멍.
 */
export class VisionMask {
  private readonly overlay: Phaser.GameObjects.Graphics;
  private readonly mask: Phaser.GameObjects.Graphics;
  private radius: number;

  constructor(
    private readonly scene: Phaser.Scene,
    width: number,
    height: number,
    initialRadius: number,
  ) {
    this.radius = initialRadius;
    this.overlay = scene.add.graphics();
    this.overlay.fillStyle(0x000000, 0.85);
    this.overlay.fillRect(0, 0, width, height);
    this.overlay.setScrollFactor(0);
    this.overlay.setDepth(1000);

    this.mask = scene.make.graphics({ x: 0, y: 0 });
    const bitmapMask = this.mask.createGeometryMask();
    bitmapMask.invertAlpha = true;
    this.overlay.setMask(bitmapMask);
  }

  update(worldX: number, worldY: number): void {
    this.mask.clear();
    this.mask.fillStyle(0xffffff, 1);
    const cam = this.scene.cameras.main;
    const sx = worldX - cam.scrollX;
    const sy = worldY - cam.scrollY;
    this.mask.fillCircle(sx, sy, this.radius);
  }

  setRadius(px: number): void {
    this.radius = px;
  }

  destroy(): void {
    this.overlay.destroy();
    this.mask.destroy();
  }
}
