import { defineComponent, Types } from 'bitecs';

export const Gather = defineComponent({
  targetEid: Types.ui32,  // 채집 대상 엔티티
  progress: Types.f32,    // 0..1
  durationSec: Types.f32,
});
