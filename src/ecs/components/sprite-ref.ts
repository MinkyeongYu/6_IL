import { defineComponent, Types } from 'bitecs';

/**
 * ECS 엔티티를 Phaser GameObject와 연결. 실제 sprite 인스턴스는
 * sprite-map(숫자 id → Phaser 객체)에 저장하고, 여기선 id만 보관.
 */
export const SpriteRef = defineComponent({ gid: Types.ui32 });
