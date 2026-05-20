import { describe, expect, it } from 'vitest';
import { SpriteRegistry } from '@/util/sprite-registry';

describe('SpriteRegistry', () => {
  it('registers and retrieves a sprite-like object', () => {
    const reg = new SpriteRegistry();
    const fakeSprite = { x: 0, y: 0, destroy: () => {} } as any;
    reg.set(1, fakeSprite);
    expect(reg.get(1)).toBe(fakeSprite);
  });

  it('returns undefined for unknown entity', () => {
    const reg = new SpriteRegistry();
    expect(reg.get(999)).toBeUndefined();
  });

  it('removes a sprite', () => {
    const reg = new SpriteRegistry();
    const fakeSprite = { x: 0, y: 0, destroy: () => {} } as any;
    reg.set(1, fakeSprite);
    reg.delete(1);
    expect(reg.get(1)).toBeUndefined();
  });

  it('clear removes all entries', () => {
    const reg = new SpriteRegistry();
    reg.set(1, { x: 0 } as any);
    reg.set(2, { x: 0 } as any);
    reg.clear();
    expect(reg.get(1)).toBeUndefined();
    expect(reg.get(2)).toBeUndefined();
  });
});
