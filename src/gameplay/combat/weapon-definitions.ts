export interface WeaponDef {
  id: string;
  displayName: string;
  baseDamage: number;
  range: number;              // px
  cooldownSec: number;
  critChance: number;         // 0..1
  critMult: number;
  hitRadius: number;          // 근접 반경(px) 또는 투사체 반경
}

export const WEAPONS: Record<string, WeaponDef> = {
  longsword: {
    id: 'longsword',
    displayName: 'Longsword',
    baseDamage: 12,
    range: 48,
    cooldownSec: 0.75,
    critChance: 0.08,
    critMult: 2,
    hitRadius: 36,
  },
} as const;

export type WeaponId = keyof typeof WEAPONS;
