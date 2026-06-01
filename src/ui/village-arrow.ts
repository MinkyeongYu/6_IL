import type Phaser from 'phaser';
import { Position } from '@/ecs/components/position';
import { GAME_WIDTH, GAME_HEIGHT } from '@/config/constants';

const MARGIN = 40;
const VILLAGE_X = 480;
const VILLAGE_Y = 270;

/**
 * 마을이 카메라 밖일 때 화면 가장자리에 화살표.
 * Snowfield 신의 카메라를 참조해 마을 월드 좌표를 화면 좌표로 변환.
 */
export class VillageArrow {
  private readonly arrow: Phaser.GameObjects.Triangle;
  private readonly label: Phaser.GameObjects.Text;
  private readonly snowfieldKey = 'Snowfield';

  constructor(
    private readonly scene: Phaser.Scene,
    private readonly playerEid: number,
  ) {
    this.arrow = scene.add
      .triangle(0, 0, 0, -12, -10, 8, 10, 8, 0xffd080)
      .setStrokeStyle(2, 0x000000)
      .setVisible(false);
    this.label = scene.add
      .text(0, 0, '🏠 마을', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '12px',
        color: '#ffd080',
      })
      .setOrigin(0.5, 1.5)
      .setVisible(false);
  }

  update(): void {
    const target = this.scene.scene.get(this.snowfieldKey);
    if (!target || !target.scene.isActive()) {
      this.arrow.setVisible(false);
      this.label.setVisible(false);
      return;
    }
    const cam = target.cameras.main;
    const villageScreenX = VILLAGE_X - cam.scrollX;
    const villageScreenY = VILLAGE_Y - cam.scrollY;

    const onScreen =
      villageScreenX >= 0 &&
      villageScreenX <= GAME_WIDTH &&
      villageScreenY >= 0 &&
      villageScreenY <= GAME_HEIGHT;

    if (onScreen) {
      this.arrow.setVisible(false);
      this.label.setVisible(false);
      return;
    }

    const px = Position.x[this.playerEid] ?? 0;
    const py = Position.y[this.playerEid] ?? 0;
    const dx = VILLAGE_X - px;
    const dy = VILLAGE_Y - py;
    const angle = Math.atan2(dy, dx);

    // 화면 중앙에서 각도 방향으로 viewport 가장자리 계산
    const cx = GAME_WIDTH / 2;
    const cy = GAME_HEIGHT / 2;
    const halfW = GAME_WIDTH / 2 - MARGIN;
    const halfH = GAME_HEIGHT / 2 - MARGIN;
    const dirX = Math.cos(angle);
    const dirY = Math.sin(angle);
    const scaleX = dirX !== 0 ? halfW / Math.abs(dirX) : Infinity;
    const scaleY = dirY !== 0 ? halfH / Math.abs(dirY) : Infinity;
    const scale = Math.min(scaleX, scaleY);
    const ax = cx + dirX * scale;
    const ay = cy + dirY * scale;

    this.arrow.setVisible(true).setPosition(ax, ay).setRotation(angle + Math.PI / 2);
    this.label.setVisible(true).setPosition(ax, ay);
  }
}
