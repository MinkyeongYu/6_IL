import { beforeEach, describe, expect, it, vi } from 'vitest';
import { saveGame, loadGame, SAVE_KEY, type SaveFileV1 } from '@/gameplay/persistence/save-load';

class MemoryStorage {
  private store = new Map<string, string>();
  getItem(k: string): string | null {
    return this.store.get(k) ?? null;
  }
  setItem(k: string, v: string): void {
    this.store.set(k, v);
  }
  removeItem(k: string): void {
    this.store.delete(k);
  }
}

const sample: SaveFileV1 = {
  version: 1,
  currentDay: 3,
  resources: { wood: 12, meat: 2, food: 4, frostbloom: 0 },
  weatherRng: 42,
};

beforeEach(() => {
  vi.stubGlobal('localStorage', new MemoryStorage());
});

describe('save-load', () => {
  it('saves and loads a round-trip', () => {
    saveGame(sample);
    const loaded = loadGame();
    expect(loaded).toEqual(sample);
  });

  it('returns null when no save exists', () => {
    expect(loadGame()).toBeNull();
  });

  it('returns null for corrupted json', () => {
    localStorage.setItem(SAVE_KEY, 'not-json');
    expect(loadGame()).toBeNull();
  });

  it('returns null for unsupported version', () => {
    localStorage.setItem(SAVE_KEY, JSON.stringify({ version: 999 }));
    expect(loadGame()).toBeNull();
  });
});
