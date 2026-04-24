# Unity 셋업 가이드

`unity/Assets/Scripts/`에 모든 C# 스크립트가 준비됐습니다. Unity 프로젝트를 열어서 씬과 프리팹만 구성하면 됩니다.

## 1. Unity 프로젝트 생성/통합

**옵션 A (권장): 임시 위치에 새 프로젝트 만들고 통합**

1. Unity Hub → "New Project"
2. 템플릿: **2D Core** (URP 아님)
3. Project name: `6IL_temp`
4. Location: 임의 (예: `C:\Users\bada\Desktop\`)
5. 생성 완료 후 Unity 닫기
6. `6IL_temp/` 폴더 안의 `Assets`, `Packages`, `ProjectSettings` 세 폴더를 `C:\Users\bada\6_IL\unity\` 안으로 복사 (덮어쓰기 — 우리 `Assets/Scripts/`는 보존됨)
7. `6IL_temp/` 폴더 삭제
8. Unity Hub → "Open" → `C:\Users\bada\6_IL\unity\` 선택

**옵션 B: 기존 unity/ 안에 직접 생성**

1. Unity Hub → "New Project" → 2D Core
2. Location: `C:\Users\bada\6_IL\` (Project name: `unity`)
3. Unity가 unity/ 폴더가 비어있다고 가정하고 만들 수도 있는데 우리 `Assets/Scripts/`가 이미 있으면 충돌 → 옵션 A 권장

## 2. 첫 실행 검증

Unity가 열리면 자동으로 우리 스크립트 컴파일 시작. Console 창에서 에러 0개면 성공.

흔한 에러:
- `using UnityEngine.UI;` — 옛 UI(Legacy)를 사용 중. 만약 컴파일 에러 나면 `Window > Package Manager`에서 "Unity UI" 패키지 확인.

## 3. 핵심 셋업

### 3.1. BalanceConfig 생성

1. Project 창에서 `Assets/` 우클릭 → Create → 6IL → Balance Config
2. 이름: `BalanceConfig`
3. **`Assets/Resources/` 폴더로 이동** (없으면 만들기) — `BalanceConfig.Instance`가 자동 로드

### 3.2. Weapon Definition 생성

1. Project → Create → 6IL → Weapon Definition
2. 이름: `Longsword`
3. Inspector에서 기본값 확인 (BaseDamage 12, Range 48, CooldownSec 0.75 등)

### 3.3. 씬 만들기

**BootScene**:
1. File → New Scene → 2D (built-in)
2. 빈 GameObject "Boot" 추가 → `BootController` 컴포넌트
3. SnowfieldSceneName 필드 = "SnowfieldScene"
4. Save Scene as → `Assets/Scenes/BootScene.unity`

**SnowfieldScene**:
1. New Scene → 2D
2. Main Camera는 자동 추가됨
3. 빈 GameObject "Snowfield" 추가:
   - `SnowfieldController` 컴포넌트
4. 빈 GameObject "Player" (태그를 **Player**로):
   - `Rigidbody2D` (Body Type: Dynamic, Gravity Scale: 0)
   - `CircleCollider2D` (Radius: 0.5)
   - `SpriteRenderer` (Sprite: 임의의 원형 — Knight from Tiny Town pack 등)
   - `InputReader`, `PlayerController`, `GatherController`, `PlayerAttackController`
   - PlayerAttackController.Weapon = `Longsword` 에셋 드래그
5. 빈 GameObject "ChunkManager":
   - `ChunkManager` 컴포넌트
   - TreePrefab/DeerPrefab은 아래에서 만든 후 할당
6. 빈 GameObject "Vision":
   - `VisionMask` 컴포넌트
7. Snowfield 컨트롤러의 Inspector:
   - Player = Player GameObject
   - Gather = Player의 GatherController
   - Input = Player의 InputReader
   - Chunks = ChunkManager
   - Vision = Vision
   - MainCamera = Main Camera
   - VillageSceneName = "VillageScene"
8. Save as `Assets/Scenes/SnowfieldScene.unity`

**VillageScene**:
1. New Scene → 2D
2. Player (태그 Player) — Snowfield와 동일하게 만들거나 prefab으로 통일
3. 빈 GameObject "Village":
   - `VillageController`
   - `PlacementController`
4. 좀비 prefab + 건물 prefab은 아래 참조
5. Save as `Assets/Scenes/VillageScene.unity`

### 3.4. Build Settings에 씬 등록

File → Build Settings:
1. Add Open Scenes (BootScene이 0번)
2. SnowfieldScene 추가
3. VillageScene 추가

## 4. Prefab 만들기

각 prefab은 `Assets/Prefabs/` 안에:

**Tree.prefab**:
- Empty GameObject + SpriteRenderer (초록 원/사각) + CircleCollider2D + `Gatherable`
- Gatherable: YieldKind = Wood, YieldAmount = 3, DurationSec = 4, DestroyOnGather = true

**Deer.prefab**:
- Empty + SpriteRenderer + CircleCollider2D + Rigidbody2D (Gravity 0) + `Gatherable` + `DeerAi`
- Gatherable: YieldKind = Meat, YieldAmount = 2, DurationSec = 2

**Zombie.prefab**:
- Empty + SpriteRenderer + CircleCollider2D + Rigidbody2D (Gravity 0) + `Zombie`

**Campfire.prefab / Barricade.prefab**:
- Empty + SpriteRenderer + CircleCollider2D + `Building`
- Building.Kind 설정

ChunkManager에 Tree/Deer prefab 할당. PlacementController에 Campfire/Barricade prefab 할당.

## 5. 무료 아트 (Kenney)

- https://kenney.nl/assets/tiny-town — 8x8 픽셀 타운 + 캐릭터 (CC0)
- https://kenney.nl/assets/topdown-shooter — 좀비 스프라이트
- 다운로드 후 `Assets/Sprites/` 안에 풀고 PPU(Pixels Per Unit)를 32 또는 16으로 설정

## 6. 실행

Play 버튼 → BootScene → SnowfieldScene 자동 전환 → WASD 이동 → 나무 옆 E 키 → 자원 누적.

## 알려진 제약 (Phase 1 대응)

- 좀비 풀링 미적용 — 다수 시 GC 부담. Phase 2에서 ObjectPool로 교체.
- 시야 마스크 = SpriteMask 단순 버전. URP 셰이더 기반 더 정교한 시야는 추후.
- 튜토리얼/사망 페이드 UI는 미포함. Phaser 버전 참고하여 추후 추가.
- ScriptableObject Asset 자동 생성은 Editor 스크립트로 가능 (별도 작업).

## 다음 단계 권장

1. Unity 열어서 컴파일 에러 0개 확인
2. 위 셋업 따라 BootScene + SnowfieldScene 만들고 Player가 움직이는지 검증
3. ChunkManager 동작 (멀리 가면 새 나무 생성) 확인
4. VillageScene 셋업 후 좀비 웨이브 검증
5. 막히는 부분 알려주면 도움
