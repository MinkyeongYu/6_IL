# InisLand — 눈보라 마을 기획서

> "끝없는 겨울. 마지막 마을의 마지막 수호자."

**장르:** 뱀서바이벌 + 낮/밤 베이스 디펜스 + 탑다운 생존  
**버전:** v0.2.0 · 2026-05-29  
**스택:** Phaser 3 + TypeScript + bitecs(ECS) + Electron

---

## 1. 문제 정의

"혼자서도 짧은 세션 안에 생존과 경영 두 재미를 모두 느낄 수 있는 게임이 없다."  
낮에는 설원 탐험가, 밤에는 마을 수호자 — 두 역할이 하나의 세션 안에서 자연스럽게 전환된다.

---

## 2. 페르소나

**주 페르소나**: 이진혁, 27세, 직장인  
- 자투리 시간(20~30분)에 게임. 긴 세션 불가.  
- 로그라이크/생존류 좋아하지만 복잡한 조작은 부담.  
- "오늘 밤은 버텼다"는 성취감을 원한다.

---

## 3. 코어 루프

```
낮(40s) → 설원 탐험 · 나무/돌/고기 채집 · 동물 사냥 · 동료 영입
   ↓ 저녁 자동 텔레포트
밤(40s) → 좀비 웨이브 격퇴 · 모닥불 방어 · 바리케이드 사수
   ↓ 새벽 회복: HP 복구 · 건물 +25%HP · NPC 1명 스폰
낮(다음 레벨)...
```

레벨 스케일: 낮/밤 페이즈 길이 = 40s × (1 + 0.25 × (레벨-1))

---

## 4. 주요 시스템

### 낮 페이즈
- **이동**: WASD, 설원 맵 자유 탐험
- **채집**: 나무(W), 돌(R), 사슴 사냥(공격)
- **영입**: 떠돌이 NPC 발견 시 대화로 동료 추가 (마을 House 수 한도)

### 밤 페이즈
- **좀비 웨이브**: 무한 증가, 25킬마다 보스 등장
- **모닥불**: 2.5u DPS 화상 오라 + 마을 온도 유지 (연료 100s)
- **바리케이드**: 건물 HP 소진 시 마을 함락

### 건물 (10종)
| 건물 | 역할 | 비용 |
|-----|------|------|
| 🔥 Campfire | 좀비 화상 오라 + 온도 유지 | 5W |
| 🏠 House | 인구 한도 +4 | 6W |
| 🏹 Watchtower | 야간 자동 사격 | 8W+4S |
| 🌾 Farm | 식량 생산 | 6W |
| 📦 Storage | 자원 캡 +50 | 8W |
| 🥕 Fence | 외곽 차단 | 1W |

### 동료/NPC 타입
아이(빠름·약함·비전투), 노인(느림·농사+5), 사냥꾼, 치유사, 무사

---

### 신규 기획 반영: 동료 특성과 낮 탐험 발견물
- **동료 특성**: 영입/출생 시 동료마다 Brave, Quick Hands, Cold Resistant, Light Eater, Defender, Caregiver 중 하나를 부여한다. 특성은 전투 피해, 공격 속도, 최대 HP, 채집 보조 속도, 일일 식량 소비량에 직접 영향을 준다.
- **낮 탐험 발견물**: 낮에 처음 방문하는 설원 청크에서 보급 상자, 부서진 썰매, 채석 흔적, 사냥 가방, 서리꽃 군락 같은 발견물이 낮은 확률로 등장한다. 플레이어는 기존 채집 조작으로 발견물을 조사하고 자원을 획득한다.
- **중기 목표**: 보육소 대신 식량 안정, 방어선 완성, 온기 중심지, 사냥 루트, 숙련된 원정대 목표를 HUD에서 추적한다. 조건을 달성하면 식량 저장 한도, 자원 보급, 건물 수리, 동료 XP 같은 즉시 보상을 지급해 3~7일차 성장 방향을 만든다.

## 5. 성공 KPI

- Day 3 생존율 50% 이상
- 세션당 평균 플레이 시간 15~25분
- 밤 1회 웨이브 격퇴 시 "성취감" 체감 (설문 7/10 이상)

---

## 6. 기술 스택

- **렌더링**: Phaser 3.60 (Canvas)
- **ECS**: bitecs — 유닛/자원/이펙트 컴포넌트 분리
- **패키징**: Electron 35 + electron-builder (Windows Portable)
- **테스트**: Vitest 72개 단위 테스트

---

## 7. 변경 이력

| 버전 | 날짜 | 내용 |
|-----|------|------|
| v0.2.0-build-refresh | 2026-06-05 | Unity 단독 개발 기준, 프로젝트 넘버링 실행파일 `6IL_v0.2.0_portable.exe`, HUD 동일 레이어 비겹침 검증 절차 반영 |
| v0.2.0 | 2026-05-29 | UX 온보딩 씬 추가 (낮/밤 페이즈 설명) |
| v0.1.0 | 2026-05-20 | 초기 ECS 기반 프로토타입 |

## 8. 최신 빌드/UX/VFX 확인 기준

- **권위 빌드 경로**: Unity `IL6.EditorBuild.BuildScript.BuildWindows`가 `Build/Windows/IL6.exe`를 만들고, 루트에는 공유용 `6IL_v0.2.0_portable.exe`만 남긴다.
- **실행파일 최신성**: 모든 소스, 설정, 기획서 변경 이후 마지막에 한 번만 Windows 빌드를 수행하고 `LastWriteTime`이 변경 파일보다 최신인지 확인한다.
- **HUD UX 규칙**: 같은 레이어의 HUD 요소는 서로 겹치지 않는 것을 기본값으로 하며, 자원/체력/목표/선택 피드백은 플레이 액션 근처에서 즉시 읽히게 한다.
- **VFX 기준**: 공격, 피격, 채집, 건설, 동료 합류, 아이템 획득, 웨이브 시작/종료는 시작-결과 피드백이 보여야 한다. 명확한 일회성 효과는 스프라이트 시트, 반복/잔류 효과는 파티클이나 경량 절차형 효과를 우선한다.
- **공유 이미지**: 아래 리소스 미리보기 이미지는 문서와 `assets/` 폴더를 함께 공유했을 때 열릴 수 있는 상대 경로를 유지한다.

<!-- APPLIED_RESOURCES_START -->
## 적용 리소스

> 자동 갱신: 2026-06-04. 코드, 씬, 프리팹, 설정 파일에서 참조가 확인된 리소스 기준입니다.

- 이미지/스프라이트: `assets/art/Sprites/Player/player_survivor_axe.png`, `assets/art/Sprites/Player/player_survivor_bow.png`
- Unity/프리팹: `assets/UniversalRenderPipelineGlobalSettings.asset`

메모:
- 6_IL은 Unity 프로젝트 기준으로 Assets/ 씬, 프리팹, C# 참조를 우선 반영합니다.
- Unity 기준 적용 리소스만 유지하며, 웹 프로토타입용 `public/` 리소스는 제외했습니다.
<!-- APPLIED_RESOURCES_END -->

<!-- RESOURCE_PREVIEWS_START -->
## 공유용 이미지 미리보기

> 자동 갱신: 2026-06-04. 공유 시 문서와 함께 아래 이미지 경로가 포함되어야 합니다.

![6_IL player_survivor_axe](../assets/art/Sprites/Player/player_survivor_axe.png)
- `assets/art/Sprites/Player/player_survivor_axe.png`

![6_IL player_survivor_bow](../assets/art/Sprites/Player/player_survivor_bow.png)
- `assets/art/Sprites/Player/player_survivor_bow.png`

<!-- RESOURCE_PREVIEWS_END -->
### 2026-06-05 Gameplay Update: Church and Companion Catch-up
- Church building added as a morale support structure.
- Church reduces morale-loss severity by chance. Level 1 starts at 20%, then increases by 10% per level. When triggered, the morale loss is halved.
- When companion morale is high, an active Church improves companion efficiency. Morale 80+ grants +5% damage and movement/work efficiency per Church level.
- Companion catch-up spawning added. If the village has fewer than 3 companions, recruitable NPCs spawn closer to the village, spawn more often, and allow a higher alive NPC cap. If the village has 3-5 companions, NPCs still spawn slightly closer and faster than normal.
- Wolf encounters are excluded from animal spawn rolls while the village has 3 or fewer companions.

### 2026-06-05 Visual Update: HUD and Tree Readability
- HUD icon resources replaced with a cohesive cozy winter pixel style, keeping 64x64 transparent PNGs and existing resource paths.
- Tree resources expanded from two base sprites to nine variants: pine, bare, and snow-covered silhouettes.
- Procedural tree spawning now picks deterministic variants by world position for more natural map variety.
- Tree collision is reduced to the visible trunk/base area instead of the full canopy, making movement blocking easier to read.
- HUD icons were revised again to match the applied gameplay/reference pixel art more closely: transparent object icons, dark pixel outlines, and no circular badge frame.
- Low-detail procedural art made with PowerShell System.Drawing was removed from runtime resources. HUD icons now reuse approved generated UI icons, and tree variants are restored/extracted from existing project art sources.
- Tree runtime sprites were re-extracted with alpha-trimmed padding so no tree variant touches the 64x64 sprite edge, and the mistaken vertical-fence crop was removed from tree spawning.
- Runtime character, animal, enemy, and prop sprites were re-extracted as single 64x64 transparent frames with safe padding so applied Resources sprites no longer show clipped edges or full animation sheets.
- Always-on HUD frames were removed or made translucent so status, resources, phase, mode tabs, build slots, and context actions block less of the game view.
- Crop farming was expanded with Potato, Turnip, and Wheat crop choices. Seed Storage levels unlock advanced crops, blizzards stall vulnerable crops unless farms are upgraded, and Farm levels improve yield, worker slots, and late-stage growth speed.
