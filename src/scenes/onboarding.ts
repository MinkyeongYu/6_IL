import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT } from '@/config/constants';

const CW = GAME_WIDTH;
const CH = GAME_HEIGHT;

// ── colour palette (matches hud / game-over chrome) ──────────────────────────
const C = {
  bg: 0x05080f,
  panel: 0x17191f,
  border: 0x6f4e3a,
  text: '#f5f1e8',
  muted: '#8fa0b8',
  accent: '#ffd47a',
  danger: '#ff6f5f',
  green: '#8ac064',
  ice: '#9bd4f0',
  hint: '#ffb3a7',
};

const FONT = 'ui-monospace, monospace';

type Slide = {
  phase: 'day' | 'night';
  headline: string;
  body: string[];
  tip: string;
  confirmLabel: string;
};

const SLIDES: Slide[] = [
  {
    phase: 'day',
    headline: '낮 — 설원을 탐험하라',
    body: [
      'WASD  이동',
      'E 길게 누르기  나무 / 돌 / 나뭇가루 수집',
      '클릭 (근접)  사슴 사냥 → 고기 획득',
      '',
      '마을 중앙의 모닥불을 지켜라.',
      '모닥불이 꺼지면 밤을 버틸 수 없다.',
    ],
    tip: '자원을 최대한 모아야 바리케이드를 세울 수 있다.',
    confirmLabel: '알겠다, 계속',
  },
  {
    phase: 'night',
    headline: '밤 — 좀비 웨이브를 막아라',
    body: [
      '좀비가 사방에서 밀려온다.',
      '바리케이드가 없으면 모닥불이 무너진다.',
      '',
      '모닥불 반경에 들어온 좀비는',
      '서서히 불타 쓰러진다.',
      '',
      '모든 웨이브를 버티면 새벽이 온다.',
    ],
    tip: '해가 지기 전에 준비하라!',
    confirmLabel: '시작하기',
  },
];

// ── helper ────────────────────────────────────────────────────────────────────

function addText(
  scene: Phaser.Scene,
  x: number,
  y: number,
  msg: string,
  opts: Partial<Phaser.Types.GameObjects.Text.TextStyle> = {},
): Phaser.GameObjects.Text {
  return scene.add
    .text(x, y, msg, {
      fontFamily: FONT,
      fontSize: '16px',
      color: C.text,
      stroke: '#10151c',
      strokeThickness: 3,
      ...opts,
    })
    .setScrollFactor(0)
    .setDepth(3010);
}

// ── scene ─────────────────────────────────────────────────────────────────────

export class OnboardingScene extends Phaser.Scene {
  private slideIndex = 0;
  private slideObjects: Phaser.GameObjects.GameObject[] = [];
  private confirmKey!: Phaser.Input.Keyboard.Key;
  private spaceKey!: Phaser.Input.Keyboard.Key;

  constructor() {
    super({ key: 'Onboarding' });
  }

  create(): void {
    this.cameras.main.setBackgroundColor(C.bg);
    this.confirmKey = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.ENTER);
    this.spaceKey = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.SPACE);
    this.slideIndex = 0;
    this.buildSlide(SLIDES[0]!);
  }

  override update(): void {
    if (
      Phaser.Input.Keyboard.JustDown(this.confirmKey) ||
      Phaser.Input.Keyboard.JustDown(this.spaceKey)
    ) {
      this.advance();
    }
  }

  private advance(): void {
    this.slideIndex++;
    if (this.slideIndex >= SLIDES.length) {
      this.scene.start('Game');
      return;
    }
    this.buildSlide(SLIDES[this.slideIndex]!);
  }

  private buildSlide(slide: Slide): void {
    // destroy previous objects
    for (const obj of this.slideObjects) obj.destroy();
    this.slideObjects = [];

    const cx = CW / 2;
    const cy = CH / 2;
    const pw = 620;
    const ph = 370;
    const px = cx - pw / 2;
    const py = cy - ph / 2;

    // ── dim backdrop ─────────────────────────────────────────────────────────
    const backdrop = this.add
      .rectangle(cx, cy, CW, CH, C.bg, 0.88)
      .setScrollFactor(0)
      .setDepth(3000);
    this.slideObjects.push(backdrop);

    // ── panel ────────────────────────────────────────────────────────────────
    const panel = this.add.graphics().setScrollFactor(0).setDepth(3001);
    panel.fillStyle(C.panel, 0.97);
    panel.fillRoundedRect(px, py, pw, ph, 6);
    panel.lineStyle(2, C.border, 1);
    panel.strokeRoundedRect(px, py, pw, ph, 6);
    this.slideObjects.push(panel);

    // ── phase pill ───────────────────────────────────────────────────────────
    const isDay = slide.phase === 'day';
    const pillColor = isDay ? 0xffd47a : 0x3d5a8a;
    const pillLabel = isDay ? '낮  DAY' : '밤  NIGHT';
    const pill = this.add.graphics().setScrollFactor(0).setDepth(3002);
    pill.fillStyle(pillColor, 0.22);
    pill.fillRoundedRect(px + 20, py + 14, 110, 26, 4);
    pill.lineStyle(1, pillColor, 0.55);
    pill.strokeRoundedRect(px + 20, py + 14, 110, 26, 4);
    this.slideObjects.push(pill);

    const pillTxt = addText(this, px + 75, py + 27, pillLabel, {
      fontSize: '13px',
      color: isDay ? C.accent : C.ice,
      strokeThickness: 0,
    }).setOrigin(0.5);
    this.slideObjects.push(pillTxt);

    // ── slide indicator (e.g. 1 / 2) ─────────────────────────────────────────
    const indicatorTxt = addText(
      this,
      px + pw - 24,
      py + 20,
      `${this.slideIndex + 1} / ${SLIDES.length}`,
      { fontSize: '13px', color: C.muted, strokeThickness: 0 },
    ).setOrigin(1, 0.5);
    this.slideObjects.push(indicatorTxt);

    // ── headline ─────────────────────────────────────────────────────────────
    const headline = addText(this, cx, py + 62, slide.headline, {
      fontSize: '22px',
      color: isDay ? C.accent : C.ice,
      strokeThickness: 4,
    }).setOrigin(0.5, 0);
    this.slideObjects.push(headline);

    // ── divider ──────────────────────────────────────────────────────────────
    const divider = this.add.graphics().setScrollFactor(0).setDepth(3002);
    divider.lineStyle(1, C.border, 0.5);
    divider.lineBetween(px + 20, py + 98, px + pw - 20, py + 98);
    this.slideObjects.push(divider);

    // ── body lines ───────────────────────────────────────────────────────────
    // Lines that begin with a KEY token (e.g. "WASD  이동") are split so the
    // key part is rendered in the phase accent colour.
    // Pattern: one or more non-whitespace tokens, then two+ spaces, then rest.
    const KEY_HINT_RE = /^(\S.*?)\s{2,}(.+)$/;
    let lineY = py + 112;
    for (const line of slide.body) {
      if (line === '') {
        lineY += 8;
        continue;
      }
      const keyMatch = line.match(KEY_HINT_RE);
      if (keyMatch) {
        const keyPart = keyMatch[1]!;
        const descPart = keyMatch[2]!;
        // Render as a single rich string: "[key]  desc"
        // Use two separate text objects side-by-side so colours differ.
        const KEY_COL_W = 190; // reserved width for the key column
        const keyTxt = addText(this, px + 48, lineY, keyPart, {
          fontSize: '15px',
          color: isDay ? C.accent : C.ice,
        });
        this.slideObjects.push(keyTxt);
        const descTxt = addText(this, px + 48 + KEY_COL_W, lineY, descPart, {
          fontSize: '15px',
          color: C.text,
        });
        this.slideObjects.push(descTxt);
      } else {
        const bodyTxt = addText(this, px + 48, lineY, line, {
          fontSize: '15px',
          color: C.text,
        });
        this.slideObjects.push(bodyTxt);
      }
      lineY += 26;
    }

    // ── tip banner ───────────────────────────────────────────────────────────
    const tipBg = this.add.graphics().setScrollFactor(0).setDepth(3002);
    const tipY = py + ph - 90;
    tipBg.fillStyle(isDay ? 0x4a3010 : 0x1a2840, 0.85);
    tipBg.fillRoundedRect(px + 16, tipY, pw - 32, 32, 4);
    tipBg.lineStyle(1, isDay ? 0xffd47a : 0x3d5a8a, 0.45);
    tipBg.strokeRoundedRect(px + 16, tipY, pw - 32, 32, 4);
    this.slideObjects.push(tipBg);

    const tipTxt = addText(this, cx, tipY + 16, `⚠  ${slide.tip}`, {
      fontSize: '14px',
      color: isDay ? C.hint : '#ff6f5f',
      strokeThickness: 0,
    }).setOrigin(0.5);
    this.slideObjects.push(tipTxt);

    // ── confirm button ────────────────────────────────────────────────────────
    const btnY = py + ph - 44;
    const btnW = 200;
    const btnH = 32;
    const btnX = cx - btnW / 2;

    const btn = this.add.graphics().setScrollFactor(0).setDepth(3002);
    btn.fillStyle(isDay ? 0x3a2d14 : 0x1a2840, 1);
    btn.fillRoundedRect(btnX, btnY, btnW, btnH, 4);
    btn.lineStyle(2, isDay ? 0xffd47a : 0x9bd4f0, 1);
    btn.strokeRoundedRect(btnX, btnY, btnW, btnH, 4);
    this.slideObjects.push(btn);

    const btnTxt = addText(
      this,
      cx,
      btnY + btnH / 2,
      `${slide.confirmLabel}  [Enter]`,
      { fontSize: '15px', color: isDay ? C.accent : C.ice, strokeThickness: 0 },
    ).setOrigin(0.5);
    this.slideObjects.push(btnTxt);

    // Mouse / touch click on button also advances
    const hitArea = this.add
      .rectangle(cx, btnY + btnH / 2, btnW, btnH, 0xffffff, 0)
      .setScrollFactor(0)
      .setDepth(3003)
      .setInteractive({ useHandCursor: true });
    hitArea.once('pointerdown', () => this.advance());
    this.slideObjects.push(hitArea);

    // ── fade-in tween ─────────────────────────────────────────────────────────
    const targets = this.slideObjects.filter(
      (o): o is Phaser.GameObjects.GameObject & { alpha: number } =>
        'alpha' in o,
    );
    for (const t of targets) t.alpha = 0;
    this.tweens.add({
      targets,
      alpha: 1,
      duration: 260,
      ease: 'Quad.easeOut',
    });
  }
}
