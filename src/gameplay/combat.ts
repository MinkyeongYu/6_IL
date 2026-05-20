/**
 * Damage formula from spec section 4.6:
 * finalDamage = (baseDamage - defense) * (1 + runeBonus) * critMultiplier
 * Minimum 1.
 */
export function calculateDamage(
  baseDamage: number,
  runeBonus: number,
  defense: number,
  critMultiplier = 1.0,
): number {
  const raw = (baseDamage - defense) * (1 + runeBonus) * critMultiplier;
  return Math.max(1, Math.floor(raw));
}
