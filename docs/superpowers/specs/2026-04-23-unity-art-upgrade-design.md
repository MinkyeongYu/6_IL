# Unity 아트 업그레이드 설계 — Snowfield 버티컬 슬라이스

> **⚠ 상태 (2026-04-25):** 이 스펙은 집필 시점의 결정을 기록한 스냅샷. 구현 중 다음 사항이 달라짐:
> - 리포 구조가 `unity/` 서브폴더 → 리포 루트로 평탄화 (이 문서의 `unity/Assets/...` 경로는 모두 `Assets/...` 로 읽어야 함)
> - URP 패키지 설치만 완료, **Renderer/PipelineAsset 미생성**. 따라서 2D Light 기반 라이팅 작업(Global Light/모닥불/블룸) 은 시작 전
> - LPC/Kenney 에셋 임포트 미이행 — 임시로 `ColorFallback.cs` 런타임 단색 스프라이트 사용
> - HUD 는 Canvas/TMP 대신 OnGUI `SimpleHud` 로 임시 대체
>
> 전체 진행 현황: [../specs/2026-04-25-implementation-status.md](./2026-04-25-implementation-status.md)

- 작성일: 2026-04-23
- 대상: `unity/` (Unity 2022 LTS, 2D Core) — **현재는 리포 루트로 평탄화됨**
- 관련 기존 문서: [2026-04-21-6il-game-design.md](./2026-04-21-6il-game-design.md)

## 목표

현재 Unity 포트는 [unity/SETUP.md](../../../unity/SETUP.md) 기준으로 단순 도형 스프라이트 또는 Kenney Tiny Town(16px) 기본 스프라이트 상태. 이를 **Core Keeper 스타일의 픽셀 top-down + 2D Light + 블룸 + 촘촘한 환경/UI** 로 끌어올려 "상용 인디 게임처럼 보이는" 수준까지 도달한다.

작업은 **Snowfield 씬 한 개를 완성(버티컬 슬라이스)** 한 뒤, 동일 레시피를 Village 씬에 복제하는 순서로 진행한다.

## 결정 사항

| 항목 | 선택 | 대안 |
|---|---|---|
| 스타일 | Core Keeper 느낌 (픽셀 top-down 32px + 조명/VFX) | 픽셀 기본 / 핸드드로운 2D / Low-poly 3D / 2.5D (검토 후 기각) |
| 실행 방식 | 버티컬 슬라이스 (Snowfield 먼저 전 레이어 완성) | 수평 레이어 / 에셋만 먼저 |
| 렌더 파이프라인 | URP 2D Renderer 전환 | Built-in RP 유지 (기각 — D 스타일의 80%가 2D Light) |
| 초점 영역 | 환경(타일/프롭) + UI/HUD | 캐릭터 / 조명 단독 |
| 에셋 소싱 | 전액 무료 (LPC + Kenney) | 유료 팩 조합 — 기록만, 이번엔 미채택 |

## 스코프와 비-스코프

**스코프** (이번 작업):
1. URP 2D 파이프라인 전환
2. LPC/Kenney 기반 환경 타일맵·프롭·캐릭터·UI 스프라이트 교체
3. Global Light 2D + 모닥불 Point Light 2D + 블룸 Volume
4. 눈/타격/채집 VFX 입자
5. HUD 9개 요소 폰트/아이콘/패널 폴리시
6. Snowfield 씬 완성 → Village 씬 복제

**비-스코프** (이번엔 건드리지 않음):
- 게임 로직 (`PlayerController`, `WaveSpawner`, `GatherController`, `DamageCalc`, `EventBus`, `DayNightController` 의 밸런스/이벤트 로직)
- 세이브 포맷
- Phaser 웹 버전 (`src/`)
- 3D 전환 / 카메라 원근 전환
- 새 게임 기능 추가

## 에셋 선정 — 무료 확정안

| 영역 | 팩 | 라이선스 | 용도 |
|---|---|---|---|
| 환경 타일 + 캐릭터 + 좀비/동물 | **LPC (Liberated Pixel Cup)** 통합 팩 | CC-BY-SA 3.0 / GPL 3.0 | 32px 설원·마을 타일, 플레이어·좀비·사슴 스프라이트 |
| UI/아이콘 | **Kenney — UI Pack RPG Expansion** | CC0 | 패널 9-slice, 자원·체력·스킬 아이콘 |
| 파티클 | **Kenney — Particle Pack** | CC0 | 눈·불꽃·타격 스파크 |
| 폰트 | **m3x6** (Daniel Linssen) 또는 **Press Start 2P** | OFL | 픽셀 폰트 (TextMeshPro 변환) |
| 보조 | **0x72 DungeonTileset II**, **Kenney Tiny Town** | CC0 | 특수 프롭(없는 것) 보충 |

**CC-BY-SA 의무**: `unity/README.md` (또는 최상위 `README.md`) 하단에 "## Credits" 섹션을 추가해 LPC 기여자 목록 URL + 라이선스 명시. 추후 인게임 크레딧 화면이 생기면 그쪽으로 이전. 상용 배포 가능.

## 유료 업그레이드 기록 (나중에 품질 더 필요할 때)

| 팩 | 가격 | 교체 영역 |
|---|---|---|
| Mana Seed "Village Tileset" + "Character Base" (Seliel the Shaper, itch.io) | ~$25 | 환경 + 캐릭터 (같은 제작자, 통일감 최상) |
| Cainos "Pixel Art Top Down Basic" (Unity Asset Store) | ~$15 | 환경만 (자동 타일링 포함) |
| Szadi art "Winter Tileset" (itch.io) | ~$8 | 설원 특화 보충 |

## 렌더 파이프라인: URP 2D 전환

### 순서
1. Package Manager → **Universal RP** 설치 (Unity 2022 LTS 호환 버전)
2. `Assets/Settings/` 에 `URP-2D-Renderer.asset` + `URP-PipelineAsset-2D.asset` 생성 (Create → Rendering → URP → 2D Renderer Data → Pipeline Asset이 2D Renderer를 참조)
3. Project Settings → Graphics → Scriptable Render Pipeline Settings 에 Pipeline Asset 할당 + Quality 모든 레벨에도 할당
4. 기존 `SpriteRenderer` 는 URP에서 그대로 렌더됨 — 재작업 없음
5. `VisionMask` 호환성 검증 — `SpriteMask` 는 URP 2D에서 지원되지만 실기 확인 필요

### VisionMask 처리 전략
- **원칙**: URP 전환 후 Play 로 `SpriteMask` 동작 확인
- **정상 작동 시**: 현 `VisionMask.cs` 유지. 플레이어 자식에 Point Light 2D 는 "시야 영역에 불 켜진 느낌" 을 위해 추가 (시각 보강 목적, 시야 로직과 무관)
- **동작 불가 시**: `VisionMask.cs` 를 Light 2D 기반으로 재구현 — Global Light 2D 어둡게 + 플레이어 Point Light 2D 반지름 = `VISION` 상수. "시야 = 조명" 으로 통합되어 코드 단순화. Core Keeper 룩에 더 부합하므로 **처음부터 이 방식으로 갈지 구현 단계에서 결정 가능**

### 검증 게이트
- Snowfield Play → 플레이어 이동, 나무/사슴/좀비 렌더, 시야 반경 동작, Console 에러 0개

### 파일 변경
- 추가: `Assets/Settings/URP-2D-Renderer.asset`, `Assets/Settings/URP-PipelineAsset-2D.asset`, `ProjectSettings` 편집
- 수정 가능성: `unity/Assets/Scripts/Vision/VisionMask.cs` (Light 2D 대체 시)

## 환경 스왑

### 설원 바닥 타일맵
1. `Assets/Art/Tiles/Snow/` 에 LPC 설원 타일 import, PPU=32, Filter=Point, Compression=None
2. Snowfield 씬에 **Tilemap (Grid + Tilemap)** GameObject 추가, Rule Tile 로 타일 variant 자동 배치 (현재 스코프: 눈 바닥 단일 바이옴. 흙/물 등 추가 바이옴은 별도 작업)
3. `ChunkManager` 는 로직만, 렌더는 `Tilemap.SetTile()` 을 chunk 단위로 호출하도록 얇게 수정 (신규 메서드 1개 추가)
4. 시각적 다양성: 같은 "snow" 타일에 variant 3~4개 두고 랜덤 — 격자 티 제거

### 프롭 프리팹 교체
- 기존 prefab(`Tree.prefab`, `Deer.prefab`, `Zombie.prefab`, `Campfire.prefab`, `Barricade.prefab`) 의 `SpriteRenderer.sprite` 만 LPC 로 교체. 콜라이더·로직 그대로
- **Sorting Layer**: `Ground` (타일맵) → `Props` (나무/돌) → `Creatures` (플레이어/사슴/좀비) → `FX` (이펙트) → `UI`
- **Pivot**: 모든 캐릭터/나무 bottom-center 통일

### Y축 정렬 (depth feel)
- 신규 `YSort.cs` (MonoBehaviour, ~20줄): 매 프레임 `SpriteRenderer.sortingOrder = -transform.position.y * 100`
- `Creatures` 레이어의 나무/플레이어/좀비/사슴에 부착
- 나무 뒤로 플레이어가 걸어들어가는 원근감 확보

### 눈 입자
- Kenney Particle Pack 의 snowflake 스프라이트 + Unity Particle System 을 Main Camera 자식에 부착
- 화면 상단에서 떨어뜨림, 낮/밤 사이클 이벤트와 연동해 밤에 강도 ↑ (선택)

### 파일 변경
- 추가: `Assets/Art/Tiles/`, `Assets/Art/Props/`, `Assets/Art/FX/`, `Assets/Scripts/Util/YSort.cs`
- 수정: `ChunkManager.cs` (Tilemap 연동 메서드 1개), Tree/Deer/Zombie/Campfire/Barricade prefab 의 SpriteRenderer 참조

## 라이팅 + VFX

### Global Light 2D
- 씬 루트에 `Light 2D (Global)` 1개 추가
- `DayNightController.cs` 에 Inspector 필드 `Light2D globalLight` 추가 → 기존 Phase 이벤트 받아 낮/밤 intensity·색 Lerp
  - 낮: intensity 1.0, 색 거의 흰색 (0xf5f5f0)
  - 밤: intensity 0.25, 색 차가운 청보라 (0x3a4a7a)
- 밸런스/이벤트 로직은 그대로, Light 2D 참조와 보간 블록만 추가 (~20줄)

### Point Light 2D (모닥불)
- `Campfire.prefab` 에 자식 GameObject 로 `Light 2D (Point)` 추가, 반지름 ~4m, 색 0xff8844, intensity 1.5
- 신규 `LightFlicker.cs` (~10줄, sin wave 로 intensity 진동) 부착
- Barricade 는 Light 없음 — 모닥불이 어둠 속 랜드마크 역할

### 플레이어 시야 라이트
- `VisionMask` URP 대체 시 (섹션 "VisionMask URP 대체안" 참조) 플레이어 자식에 Point Light 2D, 반지름 = `VISION`
- `VisionMask` 가 URP에서 정상 작동하면 이 라이트는 선택 — 체감 상승을 위해 추가 권장

### Post-Processing (Volume)
- URP Volume 1개 (씬 루트, Global Profile: `Assets/Settings/GlobalVolume.asset`)
- Bloom: intensity 0.3~0.5, threshold 1.0 (과하지 않게)
- Vignette: intensity 0.2 (외곽 살짝 어둡게, 포커스 중앙 집중)

### VFX
- 타격: 작은 원형 흰색 스파크 입자
- 나무 채집: 녹색 잎사귀 입자 3개
- 사슴 처치: 흙먼지 + 흰 털 입자
- 전부 ParticleSystem 컴포넌트, Play-OneShot (풀링 미적용, 필요시 추후 최적화)

### 파일 변경
- 추가: `Assets/Scripts/FX/LightFlicker.cs`, `Assets/Settings/GlobalVolume.asset`, FX prefab 3~5개
- 수정: `DayNightController.cs` (Light 2D 참조 + Lerp, ~20줄), `Campfire.prefab`, `Player.prefab`

## UI/HUD 폴리시

### 대상 UI 9종

| 기존 스크립트 (Phaser 기준) | Unity 대응 | 작업 |
|---|---|---|
| `resource-panel.ts` | `ResourcePanel` | Kenney UI RPG 9-slice 패널 + 목재 아이콘 + 픽셀 폰트 수치 |
| `health-bar.ts` | `HealthBar` | 9-slice 테두리 + 붉은 그라디언트 채움 + 체력 낮을 때 깜빡임 |
| `floating-hp-bars.ts` | `FloatingHpBars` | 작은 막대 + 대미지 숫자 튀어오름 (0.5초 fade-up) |
| `phase-banner.ts` | `PhaseBanner` | 전체 스크린 가로 배너 슬라이드-인/아웃, "밤이 찾아옵니다" 픽셀 폰트 |
| `weapon-slot.ts` | `WeaponSlot` | 무기 아이콘 틀 + 쿨다운 원형 오버레이 (Image type=Filled, Radial 360) |
| `village-arrow.ts` | `VillageArrow` | 화면 가장자리 화살표 아이콘 + 거리 텍스트 |
| `death-overlay.ts` | `DeathOverlay` | 전체 화면 검은 페이드 (0.5s) + "YOU DIED" 픽셀 폰트 + Restart 버튼 |
| `build-buttons.ts` | `BuildButtons` | 버튼 3개 (모닥불/바리케이드/등) 아이콘 + 자원 비용 + 비활성 회색 |
| `tutorial.ts` | `Tutorial` | 하단 중앙 픽셀 패널 + 타이핑 효과 (글자 0.03초 간격) |

### 공통 원칙
- **Canvas Scaler**: Scale With Screen Size, Reference 1920×1080, Match 0.5
- **TextMeshPro**: TMP Essential Resources 임포트 후 m3x6 → TMP Font Asset 변환 (Font Asset Creator)
- **픽셀 완벽성**: UI 이미지 Point filter, Canvas Pixel Perfect 체크
- **색 팔레트** (LPC 환경과 충돌 회피):
  - 어두운 네이비 `#1a2332`
  - 금장 `#c9a961`
  - 크림 `#f4e8d0`
  - 중세 양피지/금장 느낌

### 건드리지 않을 것
- 이벤트 구독 구조, `EventBus`, UI 상태 로직
- `Image`/`TMP_Text` 스왑과 RectTransform 배치·스타일링만

### 파일 변경
- 추가: `Assets/Art/UI/`, `Assets/Fonts/m3x6-TMP.asset`, UI prefab 9개
- 수정: 각 UI 스크립트의 초기화 부분에서 Sprite 참조만 (로직 0줄 변경 목표)

## 검증 게이트

### Snowfield 슬라이스 완성 기준
1. Play 진입 시 Console 에러 0개
2. 타일맵으로 설원 바닥 렌더, 격자 티 없음 (variant 랜덤)
3. 플레이어/좀비/사슴/나무 모두 LPC 스프라이트, bottom-center pivot, Y축 정렬 작동 (나무 뒤로 걸어들어갈 수 있음)
4. 낮 → 밤 전환 시 Global Light 2D 가 어두워지고 모닥불 Point Light 가 주변을 밝힘
5. 눈 입자가 화면에 내림
6. 타격/채집 VFX 확인
7. HUD 9종 모두 픽셀 폰트 + 9-slice 패널 + 아이콘으로 교체, 레이아웃 1920×1080 → 1280×720 에서 깨지지 않음
8. `README.md` 에 Credits 섹션(LPC attribution) 추가

### Village 씬 복제
- 위 1~7 기준을 Village 씬에 동일 적용 (기존 Snowfield 설정을 prefab/Volume/Light 복사)
- VillageController 의 좀비 웨이브 렌더 정상 동작

## 리스크와 대응

| 리스크 | 영향 | 대응 |
|---|---|---|
| `VisionMask` 의 `SpriteMask` 가 URP 2D 에서 렌더되지 않음 | Snowfield 씬의 시야 표현 실패 | Light 2D 기반 대체안 (~1~2시간) 준비, 실제로 더 Core Keeper 룩에 부합 |
| LPC 팩들 사이 톤 편차 (작가 다수) | 스타일 덜 통일 | 한 기여자 시리즈에서 가능한 한 몰아서 선택, 부족분만 다른 기여자 차용 |
| URP 전환 후 기존 셰이더/머티리얼 핑크화 | 전 씬 렌더 깨짐 | URP 전환 직후 머티리얼 업그레이더 (Edit → Rendering → Materials → Convert All to URP) 실행 |
| Tilemap + Sprite 혼용 시 정렬 꼬임 | 프롭이 타일 뒤로 사라짐 | Sorting Layer 엄격히 Ground < Props < Creatures < FX < UI, Tilemap Renderer 는 Ground 레이어 고정 |
| CC-BY-SA attribution 누락 | 라이선스 위반 | 크레딧 화면 prefab 에 LPC 기여자 URL + 라이선스 표기 추가 |

## 산출물

1. Snowfield 씬 Play 영상 (낮 → 밤 전환 포함)
2. Village 씬 Play 영상
3. README Credits 섹션 (LPC attribution) 갱신
4. Post 작업: 이후 Phaser 웹 버전에도 같은 에셋 재활용 고려 (별도 스펙)
