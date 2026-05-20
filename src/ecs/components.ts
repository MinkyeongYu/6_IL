import { defineComponent, Types } from 'bitecs';

// --- Spatial ---
export const Position = defineComponent({ x: Types.f32, y: Types.f32 });
export const Velocity = defineComponent({ vx: Types.f32, vy: Types.f32 });

// --- Combat ---
export const Health = defineComponent({ current: Types.f32, max: Types.f32 });
export const Combat = defineComponent({
  damage: Types.f32,
  range: Types.f32,
  cooldown: Types.f32,
  lastAttackTime: Types.f32,
});

// --- Tags ---
export const PlayerTag = defineComponent();
export const ZombieTag = defineComponent();
export const DeerTag = defineComponent();

// --- Resource Nodes ---
/** kind: 0=wood, 1=stone, 2=meat */
export const ResourceNode = defineComponent({
  kind: Types.ui8,
  amount: Types.ui8,
  gatherTime: Types.f32,
});

// --- Environment ---
export const Warmth = defineComponent({ radius: Types.f32 });
