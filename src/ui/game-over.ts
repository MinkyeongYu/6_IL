import Phaser from 'phaser';

export class GameOverScreen {
  show(scene: Phaser.Scene, day: number, kills: number): void {
    const { width, height } = scene.scale;

    const bg = scene.add
      .rectangle(width / 2, height / 2, width, height, 0x05080f, 0.74)
      .setScrollFactor(0)
      .setDepth(2000);

    const panel = scene.add
      .rectangle(width / 2, height / 2, 420, 210, 0x17191f, 0.94)
      .setScrollFactor(0)
      .setDepth(2001);
    panel.setStrokeStyle(3, 0x6f4e3a, 1);

    const title = scene.add
      .text(width / 2, height / 2 - 65, 'The village has fallen', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '28px',
        color: '#ff6f5f',
        stroke: '#10151c',
        strokeThickness: 5,
      })
      .setOrigin(0.5)
      .setScrollFactor(0)
      .setDepth(2002);

    const stats = scene.add
      .text(width / 2, height / 2, `Days survived: ${day}\nEnemies defeated: ${kills}`, {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '18px',
        color: '#f5f1e8',
        align: 'center',
      })
      .setOrigin(0.5)
      .setScrollFactor(0)
      .setDepth(2002);

    const restart = scene.add
      .text(width / 2, height / 2 + 70, 'Press R to restart', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '16px',
        color: '#ffd47a',
      })
      .setOrigin(0.5)
      .setScrollFactor(0)
      .setDepth(2002);

    void bg;
    void title;
    void stats;
    void restart;

    scene.input.keyboard!.once('keydown-R', () => {
      scene.scene.restart();
    });
  }
}
