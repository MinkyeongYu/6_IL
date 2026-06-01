import type Phaser from 'phaser';

export interface PlaceholderSpec {
  key: string;
  shape: 'circle' | 'square';
  size: number;
  color: number; // 0xrrggbb
  border?: number; // 테두리 색
}

/**
 * Phase 3에서 실제 픽셀 아트로 교체될 플레이스홀더 텍스처를
 * Graphics로 그리고 텍스처로 캐시.
 */
export function generatePlaceholders(scene: Phaser.Scene, specs: PlaceholderSpec[]): void {
  for (const spec of specs) {
    if (scene.textures.exists(spec.key)) continue;
    const g = scene.add.graphics({ x: 0, y: 0 });
    g.fillStyle(spec.color, 1);
    if (spec.border !== undefined) {
      g.lineStyle(2, spec.border, 1);
    }
    const half = spec.size / 2;
    if (spec.shape === 'circle') {
      g.fillCircle(half, half, half);
      if (spec.border !== undefined) g.strokeCircle(half, half, half - 1);
    } else {
      g.fillRect(0, 0, spec.size, spec.size);
      if (spec.border !== undefined) g.strokeRect(1, 1, spec.size - 2, spec.size - 2);
    }
    g.generateTexture(spec.key, spec.size, spec.size);
    g.destroy();
  }
}

export const PLACEHOLDER_SPECS: PlaceholderSpec[] = [
  { key: 'player', shape: 'circle', size: 24, color: 0x4a90e2, border: 0xffffff },
  { key: 'zombie', shape: 'circle', size: 22, color: 0x7a9e6e, border: 0x2a4020 },
  { key: 'deer', shape: 'circle', size: 20, color: 0xa88b60, border: 0x000000 },
  { key: 'tree', shape: 'circle', size: 28, color: 0x2f6b2f, border: 0x1a3a1a },
  { key: 'stone', shape: 'square', size: 26, color: 0x808080, border: 0x404040 },
  { key: 'campfire', shape: 'square', size: 64, color: 0xf25f27, border: 0x8b2d00 },
  { key: 'barricade', shape: 'square', size: 32, color: 0x8b5a2b, border: 0x3d2810 },
  { key: 'projectile', shape: 'circle', size: 8, color: 0xffffff },
];
