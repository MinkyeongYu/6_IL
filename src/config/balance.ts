/**
 * 게임 밸런스 단일 소스.
 */

export const DAY_CYCLE = {
  dayDurationSec: 540,
  nightDurationSec: 360,
  eveningTransitionSec: 30,
  dawnTransitionSec: 30,
} as const;

export const VISION = {
  dayRadiusTiles: 10,
  nightRadiusTiles: 6,
  megaBlizzardRadiusTiles: 3,
} as const;

export const RESOURCES = {
  startingWood: 15,
  startingStone: 5,
  startingIron: 0,
  startingMeat: 0,
  startingFood: 5,
  startingFrostbloom: 0,
} as const;

export const GATHER = {
  treeDurationSec: 4,
  stoneDurationSec: 6,
  deerDurationSec: 2,
  treeWoodYield: 3,
  stoneYield: 2,
  deerMeatYield: 2,
} as const;

export const PLAYER = {
  maxHp: 100,
  moveSpeed: 180, // px/s
  respawnSec: 3,
  armor: 0,
} as const;

export const ZOMBIE = {
  maxHp: 20,
  moveSpeed: 60,
  attackDamage: 8,
  attackCooldownSec: 1,
  attackRange: 36,
  armor: 0,
} as const;

export const WAVE = {
  baseCount: 8,
  perDay: 4,
  maxCount: 300,
} as const;

export const BUILDING_HP = {
  campfire: 400,
  barricadeWood: 200,
} as const;

export type BalanceConfig = {
  dayCycle: typeof DAY_CYCLE;
  vision: typeof VISION;
  resources: typeof RESOURCES;
  gather: typeof GATHER;
  player: typeof PLAYER;
  zombie: typeof ZOMBIE;
  wave: typeof WAVE;
  buildingHp: typeof BUILDING_HP;
};

export const BALANCE: BalanceConfig = {
  dayCycle: DAY_CYCLE,
  vision: VISION,
  resources: RESOURCES,
  gather: GATHER,
  player: PLAYER,
  zombie: ZOMBIE,
  wave: WAVE,
  buildingHp: BUILDING_HP,
};
