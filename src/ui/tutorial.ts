import type Phaser from 'phaser';

const TUTORIAL_KEY = '6il.tutorial.seen';

export interface TutorialFlags {
  opening: boolean;
  firstGather: boolean;
  firstNight: boolean;
}

function load(): TutorialFlags {
  try {
    const raw = localStorage.getItem(TUTORIAL_KEY);
    if (!raw) return { opening: false, firstGather: false, firstNight: false };
    return JSON.parse(raw) as TutorialFlags;
  } catch {
    return { opening: false, firstGather: false, firstNight: false };
  }
}

function save(flags: TutorialFlags): void {
  try {
    localStorage.setItem(TUTORIAL_KEY, JSON.stringify(flags));
  } catch (err) {
    console.error('[tutorial] save failed', err);
  }
}

export function getTutorialFlags(): TutorialFlags {
  return load();
}

export function markSeen(name: keyof TutorialFlags): void {
  const flags = load();
  flags[name] = true;
  save(flags);
}

export function clearTutorial(): void {
  try {
    localStorage.removeItem(TUTORIAL_KEY);
  } catch (err) {
    console.error('[tutorial] clear failed', err);
  }
}

interface ToastOptions {
  scene: Phaser.Scene;
  title: string;
  body: string;
  durationMs?: number;
  onClose?: () => void;
}

export function showToast(opts: ToastOptions): void {
  const w = Number(opts.scene.scale.width);
  const h = Number(opts.scene.scale.height);
  const boxW = 480;
  const boxH = 140;
  const x = (w - boxW) / 2;
  const y = h - boxH - 30;

  const bg = opts.scene.add
    .rectangle(x, y, boxW, boxH, 0x111822, 0.92)
    .setOrigin(0, 0)
    .setStrokeStyle(2, 0xffd080)
    .setScrollFactor(0)
    .setDepth(1500);
  const title = opts.scene.add
    .text(x + 16, y + 14, opts.title, {
      fontFamily: 'ui-monospace, monospace',
      fontSize: '16px',
      color: '#ffd080',
    })
    .setScrollFactor(0)
    .setDepth(1501);
  const body = opts.scene.add
    .text(x + 16, y + 42, opts.body, {
      fontFamily: 'ui-monospace, monospace',
      fontSize: '13px',
      color: '#e0e6ed',
      wordWrap: { width: boxW - 32 },
    })
    .setScrollFactor(0)
    .setDepth(1501);
  const dismiss = opts.scene.add
    .text(x + boxW - 16, y + boxH - 12, '클릭하여 닫기', {
      fontFamily: 'ui-monospace, monospace',
      fontSize: '11px',
      color: '#888888',
    })
    .setOrigin(1, 1)
    .setScrollFactor(0)
    .setDepth(1501);

  const close = (): void => {
    bg.destroy();
    title.destroy();
    body.destroy();
    dismiss.destroy();
    opts.onClose?.();
  };

  bg.setInteractive({ useHandCursor: true }).on('pointerdown', close);

  if (opts.durationMs !== undefined) {
    opts.scene.time.delayedCall(opts.durationMs, close);
  }
}

interface OpeningOptions {
  scene: Phaser.Scene;
  onComplete: () => void;
}

const OPENING_SLIDES = [
  '끝나지 않는 겨울의 땅, 설원에 버려진 마을 하나.',
  '낮의 해는 짧고, 밤은 굶주린 망자들을 데려온다.',
  '모닥불만이 죽은 자들을 밀어낸다. 함께할 자들을 찾아라.',
];

export function showOpening(opts: OpeningOptions): void {
  const w = Number(opts.scene.scale.width);
  const h = Number(opts.scene.scale.height);
  let slideIndex = 0;

  const overlay = opts.scene.add
    .rectangle(0, 0, w, h, 0x000000, 0.95)
    .setOrigin(0, 0)
    .setScrollFactor(0)
    .setDepth(3000);
  const text = opts.scene.add
    .text(w / 2, h / 2 - 20, OPENING_SLIDES[0]!, {
      fontFamily: 'ui-monospace, monospace',
      fontSize: '18px',
      color: '#e0e6ed',
      align: 'center',
      wordWrap: { width: w - 80 },
    })
    .setOrigin(0.5)
    .setScrollFactor(0)
    .setDepth(3001);
  const hint = opts.scene.add
    .text(w / 2, h - 40, '클릭하여 다음', {
      fontFamily: 'ui-monospace, monospace',
      fontSize: '12px',
      color: '#888888',
    })
    .setOrigin(0.5)
    .setScrollFactor(0)
    .setDepth(3001);

  overlay.setInteractive({ useHandCursor: true }).on('pointerdown', () => {
    slideIndex += 1;
    if (slideIndex >= OPENING_SLIDES.length) {
      overlay.destroy();
      text.destroy();
      hint.destroy();
      opts.onComplete();
    } else {
      text.setText(OPENING_SLIDES[slideIndex]!);
    }
  });
}
