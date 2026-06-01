import type { ResourceSnapshot } from '@/gameplay/resources/resource-store';

export const SAVE_KEY = '6il.save.v1';

export interface SaveFileV1 {
  version: 1;
  currentDay: number;
  resources: ResourceSnapshot;
  weatherRng: number;
}

export function saveGame(data: SaveFileV1): void {
  try {
    localStorage.setItem(SAVE_KEY, JSON.stringify(data));
  } catch (err) {
    console.error('[save] failed:', err);
  }
}

export function loadGame(): SaveFileV1 | null {
  try {
    const raw = localStorage.getItem(SAVE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as unknown;
    if (
      typeof parsed !== 'object' ||
      parsed === null ||
      (parsed as { version?: unknown }).version !== 1
    ) {
      return null;
    }
    return parsed as SaveFileV1;
  } catch {
    return null;
  }
}

export function clearSave(): void {
  localStorage.removeItem(SAVE_KEY);
}
