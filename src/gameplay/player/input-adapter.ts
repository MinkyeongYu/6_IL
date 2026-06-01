import Phaser from 'phaser';
import { CONTROLS, type ActionName } from '@/config/controls';

export class InputAdapter {
  private readonly keys = new Map<string, Phaser.Input.Keyboard.Key>();

  constructor(private readonly scene: Phaser.Scene) {
    const kb = scene.input.keyboard;
    if (!kb) throw new Error('InputAdapter requires a keyboard plugin');

    const uniqueCodes = new Set<string>();
    for (const names of Object.values(CONTROLS)) {
      for (const n of names) uniqueCodes.add(n);
    }
    for (const code of uniqueCodes) {
      this.keys.set(code, kb.addKey(code));
    }
  }

  isDown(action: ActionName): boolean {
    return CONTROLS[action].some((code) => this.keys.get(code)?.isDown ?? false);
  }

  justPressed(action: ActionName): boolean {
    return CONTROLS[action].some((code) => {
      const key = this.keys.get(code);
      return key ? Phaser.Input.Keyboard.JustDown(key) : false;
    });
  }

  axis(neg: ActionName, pos: ActionName): number {
    return (this.isDown(pos) ? 1 : 0) - (this.isDown(neg) ? 1 : 0);
  }
}
