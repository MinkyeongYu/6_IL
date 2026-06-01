import { describe, expect, it } from 'vitest';
import { clamp, dist2, distance, lerp, vec2Length, vec2Normalize } from '@/util/math';

describe('math utils', () => {
  it('clamp keeps value in range', () => {
    expect(clamp(5, 0, 10)).toBe(5);
    expect(clamp(-1, 0, 10)).toBe(0);
    expect(clamp(11, 0, 10)).toBe(10);
  });

  it('lerp interpolates', () => {
    expect(lerp(0, 10, 0)).toBe(0);
    expect(lerp(0, 10, 1)).toBe(10);
    expect(lerp(0, 10, 0.5)).toBe(5);
  });

  it('dist2 returns squared distance', () => {
    expect(dist2(0, 0, 3, 4)).toBe(25);
  });

  it('distance returns euclidean distance', () => {
    expect(distance(0, 0, 3, 4)).toBe(5);
  });

  it('vec2Length', () => {
    expect(vec2Length(3, 4)).toBe(5);
  });

  it('vec2Normalize returns unit vector', () => {
    const [nx, ny] = vec2Normalize(3, 4);
    expect(Math.hypot(nx, ny)).toBeCloseTo(1);
    expect(nx).toBeCloseTo(0.6);
    expect(ny).toBeCloseTo(0.8);
  });

  it('vec2Normalize handles zero vector safely', () => {
    const [nx, ny] = vec2Normalize(0, 0);
    expect(nx).toBe(0);
    expect(ny).toBe(0);
  });
});
