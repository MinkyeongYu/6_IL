export interface DamageInput {
  base: number;
  armor: number;
  critRoll: number;           // 0..1 (rng.next())
  critChance: number;         // 0..1
  critMult: number;
}

export function calcDamage(input: DamageInput): number {
  const crit = input.critRoll < input.critChance ? input.critMult : 1;
  const raw = input.base * crit - input.armor;
  return Math.max(1, Math.floor(raw));
}
