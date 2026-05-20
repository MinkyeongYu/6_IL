export interface EnemyDef {
  id: string;
  name: string;
  hp: number;
  damage: number;
  defense: number;
  speed: number;
  attackRange: number;
  attackCooldownMs: number;
}

export const BASIC_ZOMBIE: EnemyDef = {
  id: 'zombie_basic',
  name: '좀비',
  hp: 60,
  damage: 10,
  defense: 2,
  speed: 40,
  attackRange: 32,
  attackCooldownMs: 1200,
};
