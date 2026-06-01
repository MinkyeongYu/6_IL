export const CONTROLS = {
  up: ['W', 'UP'],
  down: ['S', 'DOWN'],
  left: ['A', 'LEFT'],
  right: ['D', 'RIGHT'],
  interact: ['E', 'SPACE'],
  placeBuilding: ['B'],
  confirm: ['ENTER'],
  cancel: ['ESC'],
} as const;

export type ActionName = keyof typeof CONTROLS;
