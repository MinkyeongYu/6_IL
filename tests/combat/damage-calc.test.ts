import { describe, expect, it } from 'vitest';
import { calcDamage } from '@/gameplay/combat/damage-calc';

describe('calcDamage', () => {
  it('base damage applied when no modifiers', () => {
    const d = calcDamage({ base: 10, armor: 0, critRoll: 0.5, critChance: 0, critMult: 2 });
    expect(d).toBe(10);
  });

  it('armor subtracts from damage', () => {
    const d = calcDamage({ base: 10, armor: 3, critRoll: 0.5, critChance: 0, critMult: 2 });
    expect(d).toBe(7);
  });

  it('minimum 1 damage even with heavy armor', () => {
    const d = calcDamage({ base: 5, armor: 100, critRoll: 0.5, critChance: 0, critMult: 2 });
    expect(d).toBe(1);
  });

  it('critical doubles when critRoll < critChance', () => {
    const d = calcDamage({ base: 10, armor: 0, critRoll: 0.1, critChance: 0.2, critMult: 2 });
    expect(d).toBe(20);
  });

  it('no crit when critRoll >= critChance', () => {
    const d = calcDamage({ base: 10, armor: 0, critRoll: 0.3, critChance: 0.2, critMult: 2 });
    expect(d).toBe(10);
  });
});
