import { hasComponent, type GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Health } from '@/ecs/components/health';
import { TreeTag, DeerTag } from '@/ecs/components/tags';
import { GATHER } from '@/config/balance';
import { distance } from '@/util/math';
import type { ResourceStore, ResourceKind } from '@/gameplay/resources/resource-store';
import { defineQuery } from 'bitecs';

const INTERACT_RADIUS = 32;

interface GatherState {
  targetEid: number;
  progress: number;
  durationSec: number;
  kind: ResourceKind;
  yield: number;
}

const gatherCandidateQuery = defineQuery([Position]);

export class GatherController {
  private active: GatherState | null = null;

  constructor(
    private readonly world: GameWorld,
    private readonly resources: ResourceStore,
  ) {}

  /** 플레이어가 E 키를 눌렀을 때 호출 */
  tryStart(playerEid: number): boolean {
    if (this.active) return false;
    const px = Position.x[playerEid] ?? 0;
    const py = Position.y[playerEid] ?? 0;
    const candidates = gatherCandidateQuery(this.world);
    let nearest: { eid: number; dist: number } | null = null;
    for (const eid of candidates) {
      if (eid === playerEid) continue;
      const isGatherable =
        hasComponent(this.world, TreeTag, eid) || hasComponent(this.world, DeerTag, eid);
      if (!isGatherable) continue;
      const d = distance(px, py, Position.x[eid] ?? 0, Position.y[eid] ?? 0);
      if (d <= INTERACT_RADIUS && (!nearest || d < nearest.dist)) {
        nearest = { eid, dist: d };
      }
    }
    if (!nearest) return false;

    let kind: ResourceKind;
    let yield_: number;
    let durationSec: number;
    if (hasComponent(this.world, TreeTag, nearest.eid)) {
      kind = 'wood';
      yield_ = GATHER.treeWoodYield;
      durationSec = GATHER.treeDurationSec;
    } else {
      kind = 'meat';
      yield_ = GATHER.deerMeatYield;
      durationSec = GATHER.deerDurationSec;
    }

    this.active = {
      targetEid: nearest.eid,
      progress: 0,
      durationSec,
      kind,
      yield: yield_,
    };
    return true;
  }

  cancel(): void {
    this.active = null;
  }

  /** 매 틱. dt를 진행도에 더하고 완료 시 자원 지급 */
  update(dt: number): void {
    if (!this.active) return;
    this.active.progress += dt / this.active.durationSec;
    if (this.active.progress >= 1) {
      this.resources.add(this.active.kind, this.active.yield);
      Health.dead[this.active.targetEid] = 1;
      this.active = null;
    }
  }

  get currentProgress(): number {
    return this.active?.progress ?? 0;
  }

  get isActive(): boolean {
    return this.active !== null;
  }
}
