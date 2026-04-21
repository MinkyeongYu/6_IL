/**
 * 게임에서 발생하는 모든 도메인 이벤트. 새 이벤트를 추가할 때는 반드시 여기에 타입을 정의한다.
 */
export type GameEvents = {
  'day:started': { day: number };
  'day:ended': { day: number };
  'night:started': { day: number };
  'night:ended': { day: number };
  'evening:started': { day: number };
  'dawn:started': { day: number };
  'player:damaged': { amount: number; remaining: number };
  'player:died': { day: number };
  'zombie:died': { id: number; position: { x: number; y: number } };
  'resource:changed': { kind: string; delta: number; total: number };
  'building:destroyed': { eid: number; kind: string };
  'wave:started': { day: number; waveIndex: number; enemyCount: number };
  'wave:cleared': { day: number; waveIndex: number };
  'build:request': { kind: 'campfire' | 'barricade' };
};
