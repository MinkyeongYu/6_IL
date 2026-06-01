import { defineComponent, Types } from 'bitecs';

/**
 * mode: 0=idle, 1=seekPlayer, 2=attackTarget, 3=flee, 4=seekBuilding
 * targetEid: 추적/공격 대상 엔티티
 */
export const Ai = defineComponent({
  mode: Types.ui8,
  targetEid: Types.ui32,
  attackCooldown: Types.f32,
});
