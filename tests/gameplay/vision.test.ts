import { describe, expect, it } from 'vitest';
import { createVisionProfile } from '@/gameplay/vision';
import { Phase } from '@/gameplay/day-night-cycle';

describe('createVisionProfile', () => {
  it('uses a tighter bright radius at night than during the day', () => {
    const day = createVisionProfile(Phase.Day, 32);
    const night = createVisionProfile(Phase.Night, 32);

    expect(night.clearRadius).toBeLessThan(day.clearRadius);
    expect(night.darkness).toBeGreaterThan(day.darkness);
  });

  it('adds a fade band around the clear view so distant night objects are obscured', () => {
    const night = createVisionProfile(Phase.Night, 32);

    expect(night.fadeRadius).toBeGreaterThan(night.clearRadius);
    expect(night.fadeSteps.length).toBeGreaterThan(2);
    expect(night.fadeSteps[0]!.alpha).toBeLessThan(night.darkness);
    expect(night.fadeSteps.at(-1)!.alpha).toBeCloseTo(night.darkness);
  });
});
