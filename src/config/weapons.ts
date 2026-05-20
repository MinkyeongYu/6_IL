export interface WeaponDef {
  id: string;
  name: string;
  damage: number;
  range: number;
  cooldownMs: number;
  pattern: 'melee_sweep';
}

export const LONGSWORD: WeaponDef = {
  id: 'longsword',
  name: '롱소드',
  damage: 25,
  range: 48,
  cooldownMs: 600,
  pattern: 'melee_sweep',
};
