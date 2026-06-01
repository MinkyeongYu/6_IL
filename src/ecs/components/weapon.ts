import { defineComponent, Types } from 'bitecs';

export const Weapon = defineComponent({
  weaponId: Types.ui16,   // content 테이블 참조
  cooldown: Types.f32,    // 남은 쿨다운(초)
  range: Types.f32,       // 타겟 감지 반경(px)
});
