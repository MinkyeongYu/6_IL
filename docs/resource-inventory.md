# 6IL Resource Inventory

Last updated: 2026-05-20

## Summary

현재 게임은 외부 게임 리소스 파일을 거의 사용하지 않는다. 실행에 필요한 플레이어, 적, 동물, 자원, 건물, 배경 타일은 `src/scenes/preload.ts`에서 Phaser `generateTexture()`로 런타임 생성된다.

따라서 지금 상태는 "실행 가능하지만 대부분 placeholder"다. 실제 파일로 존재하는 리소스는 문서용 프리뷰 이미지뿐이며, 게임용 스프라이트, 오디오, 아이콘, 아틀라스 폴더는 아직 없다.

## Existing Files

| Path | Type | Used in game | Status | Notes |
| --- | --- | --- | --- | --- |
| `docs/6IL_gameplay_preview.png` | PNG screenshot/mockup | No | Exists | 문서/프리뷰용 이미지. 빌드에 포함되는 게임 리소스는 아님. |

## Generated Placeholder Textures

이 항목들은 파일로 존재하지 않지만 현재 빌드에서 런타임 생성되므로 게임 실행에는 문제가 없다.

| Texture key | Current source | Current size | In-game use | Asset status |
| --- | --- | ---: | --- | --- |
| `player` | Generated solid rectangle | 24x24 | 플레이어 캐릭터 | Missing final sprite |
| `zombie` | Generated solid rectangle | 20x20 | 기본 좀비 | Missing final sprite |
| `deer` | Generated solid rectangle | 18x18 | 사슴/고기 획득 대상 | Missing final sprite |
| `tree` | Generated solid rectangle | 24x32 | 나무 자원 노드 | Missing final sprite |
| `rock` | Generated solid rectangle | 20x16 | 바위 자원 노드 | Missing final sprite |
| `bonfire` | Generated solid rectangle | 48x48 | 중심 모닥불/방어 오브젝트 | Missing final sprite/effect |
| `barricade` | Generated solid rectangle | 28x28 | 바리케이드 | Missing final sprite/damage states |
| `snow_tile` | Generated tile | 32x32 | 설원 배경 | Missing final tile |
| `village_tile` | Generated tile | 32x32 | 마을 지면 배경 | Missing final tile |

## Required For Current Playable Slice

### Visual Assets

| Priority | Resource | Count | Exists? | Current fallback | Notes |
| --- | --- | ---: | --- | --- | --- |
| P0 | Player idle/move sprite | 1 set | No | `player` rectangle | 4-direction or 8-direction pixel animation recommended. |
| P0 | Basic zombie sprite | 1 set | No | `zombie` rectangle | Needs readable silhouette at 20-32 px. |
| P0 | Deer sprite | 1 set | No | `deer` rectangle | Daytime resource target, should be visually distinct from enemies. |
| P0 | Tree resource sprite | 1 | No | `tree` rectangle | Should communicate gatherable wood. |
| P0 | Rock resource sprite | 1 | No | `rock` rectangle | Should communicate gatherable stone. |
| P0 | Bonfire sprite | 1 base + 1 glow/effect | No | `bonfire` rectangle | Core village landmark; high visual priority. |
| P0 | Barricade sprite | 1 base | No | `barricade` rectangle | Later should gain damaged/broken states. |
| P0 | Snowfield ground tile | 1-3 tiles | No | `snow_tile` generated tile | Needs subtle variation to avoid flat repetition. |
| P0 | Village ground tile | 1-3 tiles | No | `village_tile` generated tile | Should distinguish safe zone from snowfield. |
| P1 | Attack hit effect | 1-2 effects | No | None | Current combat has no visual feedback beyond HP changes/death. |
| P1 | Gathering progress/impact effect | 1-2 effects | Partial | Graphics progress bar | Add chop/mining hit particles later. |
| P1 | Death/despawn effect | 1 effect | No | Instant sprite destroy | Useful for clarity when enemies/deer die. |
| P1 | Night/darkness overlay polish | 1 effect set | Partial | Graphics mask | Functional, but no final art. |
| P1 | HUD icons for wood/stone/meat/food | 4 icons | No | Text only | Also iron/frostbloom if displayed later. |
| P1 | Game icon/app icon | 1 `.ico` | No | Electron default icon | `electron-builder` currently reports default Electron icon. |

### Audio Assets

| Priority | Resource | Count | Exists? | Current fallback | Notes |
| --- | --- | ---: | --- | --- | --- |
| P1 | Day ambience/BGM | 1 loop | No | Silence | Snowfield exploration mood. |
| P1 | Night combat/BGM | 1 loop | No | Silence | Wave phase needs tension cue. |
| P1 | Chop SFX | 1-2 | No | Silence | For tree gathering. |
| P1 | Mining SFX | 1-2 | No | Silence | For rock gathering. |
| P1 | Melee attack SFX | 2-3 | No | Silence | Player attack and enemy hit feedback. |
| P1 | Zombie voice SFX | 3-5 | No | Silence | Spawn/attack/death variations. |
| P1 | Bonfire flame loop | 1 loop | No | Silence | Important if bonfire is visual center. |
| P1 | Barricade hit/break SFX | 2-3 | No | Silence | Needed when barricade damage is implemented. |
| P1 | Phase transition stinger | 2-3 | No | Silence | Evening/night/dawn clarity. |
| P2 | Boss/mega-blizzard cue | 1-2 | No | Silence | Future phase content. |

## Required For Phase 2+

These are not required for the current executable to run, but are implied by the design docs and game direction.

| Category | Resource | Estimated count | Exists? | Notes |
| --- | --- | ---: | --- | --- |
| Companion sprites | Companion character types | 5 types x 2 states | No | Child, hunter, priestess, old soldier, merchant concepts from design direction. |
| Companion UI | Portraits/status icons | 5+ | No | Needed once companion recruitment/permadeath is added. |
| Buildings | Storage, workshop, inn, shrine, etc. | 5-7 base | No | Phase 2 building expansion. |
| Building upgrades | Upgrade/damage overlays | 2-5 per building | No | Can be overlays instead of full redraws. |
| Resource nodes | Iron, food source, frostbloom | 3+ | No | Resource kinds already exist in code but are not gatherable yet. |
| Enemies | Zombie variants | 3+ | No | Current code only has `zombie_basic`. |
| Bosses | Boss sprites/effects | 3-5 | No | Phase 3 direction. |
| Weapons/equipment UI | Weapon icons and upgrade cards | 20+ | No | Design doc estimates about 20. |
| Tilesets | Snowfield, village, outer zones | 2-4 tile groups | No | Future zones need richer environment art. |
| Atlases | Packed sprite atlases | 1+ | No | Design expects `assets/atlases`; no atlas pipeline exists yet. |
| Localization font | Korean-capable bitmap/web font | 1 | No | Current source text appears encoding-corrupted in several files; font/text cleanup should happen before final UI art pass. |

## Missing Folder Structure

Recommended structure once real assets are added:

```text
public/assets/
  sprites/
    player/
    enemies/
    animals/
    resources/
    buildings/
    effects/
    ui/
  audio/
    bgm/
    sfx/
    ambience/
  atlases/
  icons/
```

## Current Gaps

| Gap | Impact | Suggested next step |
| --- | --- | --- |
| No real game art files | Game is playable but reads as prototype blocks | Replace P0 placeholder textures first. |
| No audio files | No phase/combat feedback through sound | Add minimal P1 SFX pack after P0 visuals. |
| No app icon | Packaged exe uses default Electron icon | Add `build.win.icon` and `.ico` asset. |
| No atlas pipeline | Many separate future assets may become messy | Add atlas loading only when several sprites exist. |
| Korean text encoding issues | HUD/game over text appears corrupted | Fix source file encoding/text before final UI copy. |

## Recommended Asset Order

1. P0 visual replacement pack: player, zombie, deer, tree, rock, bonfire, barricade, snow tile, village tile.
2. Feedback pack: hit effect, death effect, gathering effect, resource/HUD icons.
3. Audio MVP: day loop, night loop, chop, mining, melee, zombie, bonfire, transition.
4. Packaging polish: `.ico` app icon and updated Electron build config.
5. Phase 2 expansion assets: companions, additional buildings, iron/food/frostbloom nodes, enemy variants.
