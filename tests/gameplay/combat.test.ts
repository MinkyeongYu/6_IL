import { describe, expect, it } from 'vitest';
import { calculateDamage } from '@/gameplay/combat';
import { LONGSWORD } from '@/config/weapons';
import { BASIC_ZOMBIE } from '@/config/enemies';

describe('combat', () => {
  it('calculates base damage = weapon damage - enemy defense', () => {
    const dmg = calculateDamage(LONGSWORD.damage, 0, BASIC_ZOMBIE.defense);
    expect(dmg).toBe(LONGSWORD.damage - BASIC_ZOMBIE.defense);
  });

  it('never goes below 1', () => {
    const dmg = calculateDamage(1, 0, 999);
    expect(dmg).toBe(1);
  });

  it('applies critical multiplier', () => {
    const base = LONGSWORD.damage - BASIC_ZOMBIE.defense;
    const dmg = calculateDamage(LONGSWORD.damage, 0, BASIC_ZOMBIE.defense, 2.0);
    expect(dmg).toBe(Math.max(1, Math.floor(base * 2.0)));
  });

  it('applies rune bonus as additive multiplier', () => {
    const runeBonus = 0.5;
    const base = LONGSWORD.damage - BASIC_ZOMBIE.defense;
    const dmg = calculateDamage(LONGSWORD.damage, runeBonus, BASIC_ZOMBIE.defense);
    expect(dmg).toBe(Math.max(1, Math.floor(base * (1 + runeBonus))));
  });
});
