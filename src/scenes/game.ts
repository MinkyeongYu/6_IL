import Phaser from 'phaser';
import { defineQuery } from 'bitecs';
import { createGameWorld, addEntity, addComponent, removeEntity, hasComponent } from '@/ecs/world';
import type { GameWorld } from '@/ecs/world';
import {
  Position, Velocity, Health, Combat, PlayerTag, ZombieTag, DeerTag, ResourceNode,
} from '@/ecs/components';
import { movementSystem } from '@/ecs/systems/movement-system';
import { combatSystem } from '@/ecs/systems/combat-system';
import { zombieAiSystem } from '@/ecs/systems/zombie-ai-system';
import { deerAiSystem } from '@/ecs/systems/deer-ai-system';
import { createDeathSystem } from '@/ecs/systems/death-system';
import { InputAdapter } from '@/input/input-adapter';
import { DayNightCycle, Phase } from '@/gameplay/day-night-cycle';
import { ResourceManager, ResourceKind } from '@/gameplay/resource-manager';
import { VillageGrid, BuildingType } from '@/gameplay/village-grid';
import { WaveSpawner } from '@/gameplay/wave-spawner';
import { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';
import { LONGSWORD } from '@/config/weapons';
import { BASIC_ZOMBIE } from '@/config/enemies';
import { PLAYER, GATHERING, BONFIRE, VISION } from '@/config/balance';
import { TILE_SIZE, VILLAGE_GRID_SIZE } from '@/config/constants';
import { HUD } from '@/ui/hud';
import { GameOverScreen } from '@/ui/game-over';
import { createRng } from '@/util/rng';

const SNOWFIELD_SIZE = 2400;
const VILLAGE_PX = VILLAGE_GRID_SIZE * TILE_SIZE; // 768
const VILLAGE_OFFSET = (SNOWFIELD_SIZE - VILLAGE_PX) / 2; // 816

const zombieQuery = defineQuery([ZombieTag, Health]);

export class GameScene extends Phaser.Scene {
  private world!: GameWorld;
  private bus!: EventBus<GameEvents>;
  private inputAdapter!: InputAdapter;
  private cycle!: DayNightCycle;
  private resources!: ResourceManager;
  private village!: VillageGrid;
  private waveSpawner!: WaveSpawner;
  private hud!: HUD;
  private gameOver!: GameOverScreen;
  private deathSystem!: (w: GameWorld) => number[];
  private rng!: ReturnType<typeof createRng>;

  private playerEid = -1;
  private kills = 0;
  private isDead = false;

  // Night wave state
  private currentWave = 0;
  private waveTimer = 0;
  private wavePending = false;

  // Gathering state
  private gatherTarget = -1;
  private gatherProgress = 0;
  private gatherBar!: Phaser.GameObjects.Graphics;

  // Vision mask
  private visionMask!: Phaser.GameObjects.Graphics;

  // Tracked entity lists
  private snowfieldEntities: number[] = [];
  private villageSprites: Phaser.GameObjects.GameObject[] = [];

  constructor() {
    super({ key: 'Game' });
  }

  create(): void {
    this.bus = new EventBus<GameEvents>();
    this.world = createGameWorld();
    this.inputAdapter = new InputAdapter();
    this.inputAdapter.register(this);
    this.cycle = new DayNightCycle(this.bus);
    this.resources = new ResourceManager(this.bus);
    this.village = new VillageGrid();
    this.waveSpawner = new WaveSpawner();
    this.deathSystem = createDeathSystem(this.bus);
    this.gameOver = new GameOverScreen();
    this.rng = createRng(Date.now());
    this.kills = 0;
    this.isDead = false;
    this.currentWave = 0;
    this.waveTimer = 0;
    this.wavePending = false;
    this.gatherTarget = -1;
    this.gatherProgress = 0;
    this.snowfieldEntities = [];
    this.villageSprites = [];

    this.drawBackground();
    this.drawEnvironmentDetails();

    // Place starting village
    this.village.place(BuildingType.Bonfire, 11, 11);
    this.placeStartingBarricades();
    this.drawVillage();

    // Create player
    this.playerEid = this.spawnPlayer();
    this.hud = new HUD(this, this.resources, this.cycle, this.playerEid);

    // Spawn snowfield content
    this.spawnSnowfieldContent();

    // Camera
    const playerSprite = this.world.sprites.get(this.playerEid);
    if (playerSprite) {
      this.cameras.main.startFollow(playerSprite, true, 0.1, 0.1);
      this.cameras.main.setBounds(0, 0, SNOWFIELD_SIZE, SNOWFIELD_SIZE);
    }

    // Vision + gather bar graphics
    this.visionMask = this.add.graphics().setDepth(900);
    this.gatherBar = this.add.graphics().setDepth(950);

    // Events
    this.bus.on('night:started', () => this.onNightStart());
    this.bus.on('dawn:started', () => this.onDawnStart());
    this.bus.on('zombie:died', () => { this.kills++; });
    this.bus.on('player:died', () => {
      if (!this.isDead) {
        this.isDead = true;
        this.gameOver.show(this, this.cycle.day, this.kills);
      }
    });
  }

  override update(_time: number, delta: number): void {
    if (this.isDead) return;

    const dtSec = delta / 1000;
    this.world.deltaTime = dtSec;
    this.world.elapsed += delta;

    // Day/night cycle
    this.cycle.update(delta);

    // Player input
    const inp = this.inputAdapter.poll();
    Velocity.vx[this.playerEid] = inp.dx * PLAYER.speed;
    Velocity.vy[this.playerEid] = inp.dy * PLAYER.speed;

    // Gathering
    this.updateGathering(inp.interact, delta);

    // Night-specific systems
    if (this.cycle.phase === Phase.Night) {
      this.updateNightWaves(delta);
      zombieAiSystem(this.world);
      combatSystem(this.world);
      this.applyBonfireDamage(dtSec);
    }

    // Day-specific: deer flee + player melee attacks deer
    if (this.cycle.phase === Phase.Day) {
      deerAiSystem(this.world);
      this.playerAttackDeer();
    }

    movementSystem(this.world);

    // Death system
    const dead = this.deathSystem(this.world);
    for (const eid of dead) {
      // Drop meat from deer
      if (hasComponent(this.world, DeerTag, eid)) {
        this.resources.add(ResourceKind.Meat, GATHERING.deerMeat);
      }
      const sprite = this.world.sprites.get(eid);
      if (sprite) {
        sprite.destroy();
        this.world.sprites.delete(eid);
      }
      removeEntity(this.world, eid);
      this.snowfieldEntities = this.snowfieldEntities.filter((e) => e !== eid);
    }

    // Sync sprites
    this.world.sprites.forEach((sprite, eid) => {
      sprite.x = Position.x[eid]!;
      sprite.y = Position.y[eid]!;
    });

    // Vision
    this.drawVision();

    // HUD
    this.hud.update();
  }

  // --- Spawning ---

  private spawnPlayer(): number {
    const eid = addEntity(this.world);
    addComponent(this.world, Position, eid);
    addComponent(this.world, Velocity, eid);
    addComponent(this.world, Health, eid);
    addComponent(this.world, Combat, eid);
    addComponent(this.world, PlayerTag, eid);

    const cx = SNOWFIELD_SIZE / 2;
    const cy = SNOWFIELD_SIZE / 2;
    Position.x[eid] = cx;
    Position.y[eid] = cy;
    Health.current[eid] = PLAYER.maxHp;
    Health.max[eid] = PLAYER.maxHp;
    Combat.damage[eid] = LONGSWORD.damage;
    Combat.range[eid] = LONGSWORD.range;
    Combat.cooldown[eid] = LONGSWORD.cooldownMs;
    Combat.lastAttackTime[eid] = 0;

    const sprite = this.add.sprite(cx, cy, 'player').setDepth(100);
    this.world.sprites.set(eid, sprite);
    return eid;
  }

  private spawnSnowfieldContent(): void {
    const cx = SNOWFIELD_SIZE / 2;
    const cy = SNOWFIELD_SIZE / 2;

    // Trees
    for (let i = 0; i < 30; i++) {
      const angle = this.rng.next() * Math.PI * 2;
      const dist = 200 + this.rng.next() * 600;
      this.spawnResourceNode(
        cx + Math.cos(angle) * dist,
        cy + Math.sin(angle) * dist,
        0, GATHERING.treeWood, GATHERING.treeGatherMs, 'tree',
      );
    }

    // Rocks
    for (let i = 0; i < 15; i++) {
      const angle = this.rng.next() * Math.PI * 2;
      const dist = 250 + this.rng.next() * 500;
      this.spawnResourceNode(
        cx + Math.cos(angle) * dist,
        cy + Math.sin(angle) * dist,
        1, GATHERING.rockStone, GATHERING.rockGatherMs, 'rock',
      );
    }

    // Deer
    for (let i = 0; i < 8; i++) {
      const angle = this.rng.next() * Math.PI * 2;
      const dist = 300 + this.rng.next() * 500;
      this.spawnDeer(cx + Math.cos(angle) * dist, cy + Math.sin(angle) * dist);
    }
  }

  private spawnResourceNode(
    x: number, y: number, kind: number, amount: number, gatherTime: number, texture: string,
  ): void {
    const eid = addEntity(this.world);
    addComponent(this.world, Position, eid);
    addComponent(this.world, ResourceNode, eid);
    Position.x[eid] = x;
    Position.y[eid] = y;
    ResourceNode.kind[eid] = kind;
    ResourceNode.amount[eid] = amount;
    ResourceNode.gatherTime[eid] = gatherTime;

    const sprite = this.add.sprite(x, y, texture).setDepth(50);
    this.world.sprites.set(eid, sprite);
    this.snowfieldEntities.push(eid);
  }

  private spawnDeer(x: number, y: number): void {
    const eid = addEntity(this.world);
    addComponent(this.world, Position, eid);
    addComponent(this.world, Velocity, eid);
    addComponent(this.world, Health, eid);
    addComponent(this.world, DeerTag, eid);
    Position.x[eid] = x;
    Position.y[eid] = y;
    Health.current[eid] = 30;
    Health.max[eid] = 30;

    const sprite = this.add.sprite(x, y, 'deer').setDepth(60);
    this.world.sprites.set(eid, sprite);
    this.snowfieldEntities.push(eid);
  }

  private spawnZombie(x: number, y: number): void {
    const eid = addEntity(this.world);
    addComponent(this.world, Position, eid);
    addComponent(this.world, Velocity, eid);
    addComponent(this.world, Health, eid);
    addComponent(this.world, Combat, eid);
    addComponent(this.world, ZombieTag, eid);
    Position.x[eid] = x;
    Position.y[eid] = y;
    Health.current[eid] = BASIC_ZOMBIE.hp;
    Health.max[eid] = BASIC_ZOMBIE.hp;
    Combat.damage[eid] = BASIC_ZOMBIE.damage;
    Combat.range[eid] = BASIC_ZOMBIE.attackRange;
    Combat.cooldown[eid] = BASIC_ZOMBIE.attackCooldownMs;
    Combat.lastAttackTime[eid] = 0;

    const sprite = this.add.sprite(x, y, 'zombie').setDepth(80);
    this.world.sprites.set(eid, sprite);
  }

  // --- Village ---

  private placeStartingBarricades(): void {
    const positions = [
      [10, 10], [11, 10], [12, 10], [13, 10],
      [10, 13], [11, 13], [12, 13], [13, 13],
      [10, 11], [10, 12],
      [13, 11], [13, 12],
    ];
    for (const [gx, gy] of positions) {
      this.village.place(BuildingType.Barricade, gx!, gy!);
    }
  }

  private drawBackground(): void {
    // Use a single tileSprite for snow and draw village area separately
    const snowBg = this.add.tileSprite(
      SNOWFIELD_SIZE / 2, SNOWFIELD_SIZE / 2,
      SNOWFIELD_SIZE, SNOWFIELD_SIZE,
      'snow_tile',
    ).setDepth(0);
    void snowBg;

    // Village ground overlay
    const villageBg = this.add.tileSprite(
      VILLAGE_OFFSET + VILLAGE_PX / 2,
      VILLAGE_OFFSET + VILLAGE_PX / 2,
      VILLAGE_PX, VILLAGE_PX,
      'village_tile',
    ).setDepth(1);
    void villageBg;
  }

  private drawEnvironmentDetails(): void {
    const addDecor = (key: string, count: number, minDist: number, maxDist: number, depth: number) => {
      const cx = SNOWFIELD_SIZE / 2;
      const cy = SNOWFIELD_SIZE / 2;

      for (let i = 0; i < count; i++) {
        const angle = this.rng.next() * Math.PI * 2;
        const dist = minDist + this.rng.next() * (maxDist - minDist);
        const sprite = this.add
          .sprite(cx + Math.cos(angle) * dist, cy + Math.sin(angle) * dist, key)
          .setDepth(depth);
        sprite.setFlipX(this.rng.next() > 0.5);
        sprite.setAlpha(0.85 + this.rng.next() * 0.15);
      }
    };

    addDecor('snow_patch', 70, 120, 1050, 2);
    addDecor('footprints', 36, 120, 900, 3);
    addDecor('small_rock', 24, 260, 1000, 4);
    addDecor('stump', 14, 260, 920, 4);

    const forestPositions = [
      { x: 180, y: 240 }, { x: 260, y: 420 }, { x: 170, y: 700 },
      { x: 2230, y: 300 }, { x: 2150, y: 590 }, { x: 2240, y: 840 },
      { x: 430, y: 160 }, { x: 680, y: 190 }, { x: 1740, y: 170 },
      { x: 1970, y: 150 }, { x: 390, y: 2180 }, { x: 770, y: 2230 },
      { x: 1710, y: 2210 }, { x: 2050, y: 2160 },
    ];

    for (const pos of forestPositions) {
      this.add.sprite(pos.x, pos.y, 'tree').setDepth(10).setScale(1.2);
    }
  }

  private drawVillage(): void {
    for (const s of this.villageSprites) s.destroy();
    this.villageSprites = [];

    const villageCenterX = VILLAGE_OFFSET + VILLAGE_PX / 2;
    const villageCenterY = VILLAGE_OFFSET + VILLAGE_PX / 2;
    const glow = this.add.sprite(villageCenterX, villageCenterY, 'warm_glow').setDepth(30).setScale(1.7);
    const cabinA = this.add.sprite(villageCenterX - 180, villageCenterY - 135, 'cabin').setDepth(35);
    const cabinB = this.add.sprite(villageCenterX + 185, villageCenterY + 120, 'cabin').setDepth(35).setScale(0.92);
    const crates = this.add.sprite(villageCenterX - 230, villageCenterY + 120, 'crate_stack').setDepth(36);
    const logs = this.add.sprite(villageCenterX + 230, villageCenterY - 90, 'log_stack').setDepth(36);
    this.villageSprites.push(glow, cabinA, cabinB, crates, logs);

    for (const b of this.village.getBuildings()) {
      const def = b.type === BuildingType.Bonfire
        ? { w: 2, h: 2 }
        : { w: 1, h: 1 };
      const px = VILLAGE_OFFSET + b.gridX * TILE_SIZE + (def.w * TILE_SIZE) / 2;
      const py = VILLAGE_OFFSET + b.gridY * TILE_SIZE + (def.h * TILE_SIZE) / 2;
      const texture = b.type === BuildingType.Bonfire ? 'bonfire' : 'barricade';
      const sprite = this.add.sprite(px, py, texture).setDepth(40);
      if (b.type === BuildingType.Bonfire) {
        sprite.setDepth(45);
      }
      this.villageSprites.push(sprite);
    }
  }

  // --- Night Waves ---

  private onNightStart(): void {
    this.currentWave = 0;
    this.waveTimer = 0;
    this.wavePending = true;
    this.hud.setWaveText('밤 시작!');
  }

  private onDawnStart(): void {
    this.hud.setWaveText('');

    // Kill remaining zombies
    const remaining = zombieQuery(this.world);
    for (let i = 0; i < remaining.length; i++) {
      const eid = remaining[i]!;
      Health.current[eid] = 0;
    }

    // Respawn snowfield content
    this.spawnSnowfieldContent();
  }

  private updateNightWaves(dtMs: number): void {
    if (!this.wavePending) return;

    this.waveTimer += dtMs;
    const waveInterval = 60_000;

    // Count alive zombies
    const alive = zombieQuery(this.world);
    let aliveCount = 0;
    for (let i = 0; i < alive.length; i++) {
      if (Health.current[alive[i]!]! > 0) aliveCount++;
    }

    const shouldSpawn =
      this.currentWave === 0 ||
      (aliveCount <= 0 && this.currentWave < this.waveSpawner.totalWaves) ||
      this.waveTimer >= waveInterval;

    if (shouldSpawn) {
      this.currentWave++;
      if (this.currentWave > this.waveSpawner.totalWaves) {
        this.wavePending = false;
        return;
      }

      const wave = this.waveSpawner.getWave(this.cycle.day, this.currentWave);
      const cx = SNOWFIELD_SIZE / 2;
      const cy = SNOWFIELD_SIZE / 2;

      for (const pos of wave.positions) {
        this.spawnZombie(cx + pos.x, cy + pos.y);
      }
      this.waveTimer = 0;
      this.hud.setWaveText(`Wave ${this.currentWave}/${this.waveSpawner.totalWaves} (${wave.count})`);
      this.bus.emit('wave:started', { waveNumber: this.currentWave, count: wave.count });
    }
  }

  // --- Gathering ---

  private updateGathering(interactPressed: boolean, dtMs: number): void {
    if (!interactPressed || this.cycle.phase !== Phase.Day) {
      this.gatherTarget = -1;
      this.gatherProgress = 0;
      this.gatherBar.clear();
      return;
    }

    const px = Position.x[this.playerEid]!;
    const py = Position.y[this.playerEid]!;

    // Find nearest resource node
    if (this.gatherTarget < 0) {
      let nearest = -1;
      let nearestDist = Infinity;
      for (const eid of this.snowfieldEntities) {
        if (!hasComponent(this.world, ResourceNode, eid)) continue;
        const dx = Position.x[eid]! - px;
        const dy = Position.y[eid]! - py;
        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist < 48 && dist < nearestDist) {
          nearestDist = dist;
          nearest = eid;
        }
      }
      this.gatherTarget = nearest;
    }

    if (this.gatherTarget < 0) {
      this.gatherBar.clear();
      return;
    }

    const target = this.gatherTarget;

    // Check if target still exists
    if (!hasComponent(this.world, ResourceNode, target)) {
      this.gatherTarget = -1;
      this.gatherProgress = 0;
      this.gatherBar.clear();
      return;
    }

    const gatherTime = ResourceNode.gatherTime[target]!;
    this.gatherProgress += dtMs;

    // Progress bar
    const nx = Position.x[target]!;
    const ny = Position.y[target]! - 20;
    const progress = Math.min(this.gatherProgress / gatherTime, 1);
    this.gatherBar.clear();
    this.gatherBar.fillStyle(0x333333);
    this.gatherBar.fillRect(nx - 16, ny, 32, 4);
    this.gatherBar.fillStyle(0x44cc44);
    this.gatherBar.fillRect(nx - 16, ny, 32 * progress, 4);

    if (this.gatherProgress >= gatherTime) {
      const kind = ResourceNode.kind[target]!;
      const amount = ResourceNode.amount[target]!;
      const resourceKind =
        kind === 0 ? ResourceKind.Wood :
        kind === 1 ? ResourceKind.Stone :
        ResourceKind.Meat;
      this.resources.add(resourceKind, amount);

      // Remove node
      const sprite = this.world.sprites.get(target);
      if (sprite) {
        sprite.destroy();
        this.world.sprites.delete(target);
      }
      removeEntity(this.world, target);
      this.snowfieldEntities = this.snowfieldEntities.filter((e) => e !== target);

      this.gatherTarget = -1;
      this.gatherProgress = 0;
      this.gatherBar.clear();
    }
  }

  // --- Bonfire AOE ---

  private applyBonfireDamage(dtSec: number): void {
    const bonfires = this.village
      .getBuildings()
      .filter((b) => b.type === BuildingType.Bonfire);
    if (bonfires.length === 0) return;

    const zombies = zombieQuery(this.world);
    const radiusSq = BONFIRE.radius * BONFIRE.radius;
    const dmg = BONFIRE.damagePerSec * dtSec;

    for (const bonfire of bonfires) {
      const bx = VILLAGE_OFFSET + bonfire.gridX * TILE_SIZE + TILE_SIZE;
      const by = VILLAGE_OFFSET + bonfire.gridY * TILE_SIZE + TILE_SIZE;

      for (let i = 0; i < zombies.length; i++) {
        const zid = zombies[i]!;
        if (Health.current[zid]! <= 0) continue;
        const dx = Position.x[zid]! - bx;
        const dy = Position.y[zid]! - by;
        if (dx * dx + dy * dy < radiusSq) {
          Health.current[zid]! -= dmg;
        }
      }
    }
  }

  // --- Player attacks deer (melee) ---

  private playerAttackDeer(): void {
    const now = this.world.elapsed;
    const cd = Combat.cooldown[this.playerEid]!;
    if (now - Combat.lastAttackTime[this.playerEid]! < cd) return;

    const px = Position.x[this.playerEid]!;
    const py = Position.y[this.playerEid]!;
    const range = Combat.range[this.playerEid]!;
    const rangeSq = range * range;

    for (const eid of this.snowfieldEntities) {
      if (!hasComponent(this.world, DeerTag, eid)) continue;
      if (Health.current[eid]! <= 0) continue;
      const dx = Position.x[eid]! - px;
      const dy = Position.y[eid]! - py;
      if (dx * dx + dy * dy <= rangeSq) {
        Health.current[eid]! -= Combat.damage[this.playerEid]!;
        Combat.lastAttackTime[this.playerEid] = now;
        return; // one attack per frame
      }
    }
  }

  // --- Vision ---

  private drawVision(): void {
    const px = Position.x[this.playerEid]!;
    const py = Position.y[this.playerEid]!;
    const isNight = this.cycle.phase === Phase.Night || this.cycle.phase === Phase.Evening;
    const radiusTiles = isNight ? VISION.nightRadiusTiles : VISION.dayRadiusTiles;
    const radius = radiusTiles * TILE_SIZE;
    const darkness = isNight ? 0.85 : 0.35;

    const cam = this.cameras.main;
    const left = cam.scrollX;
    const top = cam.scrollY;
    const w = cam.width;
    const h = cam.height;

    this.visionMask.clear();

    // Outer darkness: 4 rectangles forming a frame around a central opening
    const outerR = radius;
    this.visionMask.fillStyle(0x0c1626, darkness);
    // Top
    this.visionMask.fillRect(left, top, w, Math.max(0, py - outerR - top));
    // Bottom
    this.visionMask.fillRect(left, py + outerR, w, Math.max(0, top + h - py - outerR));
    // Left
    this.visionMask.fillRect(left, py - outerR, Math.max(0, px - outerR - left), outerR * 2);
    // Right
    this.visionMask.fillRect(px + outerR, py - outerR, Math.max(0, left + w - px - outerR), outerR * 2);

    // Corner fill (rectangles leave corners uncovered)
    // Draw dark triangles as small rects at corners of the opening
    const cornerSize = outerR * 0.3;
    for (let cx = -1; cx <= 1; cx += 2) {
      for (let cy = -1; cy <= 1; cy += 2) {
        const cornerX = px + cx * outerR;
        const cornerY = py + cy * outerR;
        this.visionMask.fillStyle(0x0c1626, darkness * 0.7);
        this.visionMask.fillRect(
          cx > 0 ? cornerX - cornerSize : cornerX,
          cy > 0 ? cornerY - cornerSize : cornerY,
          cornerSize, cornerSize,
        );
      }
    }
  }
}
