import { defineQuery } from 'bitecs';
import { Position } from './components/position';
import { Velocity } from './components/velocity';
import { Health } from './components/health';
import { Lifetime } from './components/lifetime';
import { SpriteRef } from './components/sprite-ref';

export const movementQuery = defineQuery([Position, Velocity]);
export const lifetimeQuery = defineQuery([Lifetime]);
export const healthQuery = defineQuery([Health]);
export const spriteQuery = defineQuery([Position, SpriteRef]);
