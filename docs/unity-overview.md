# 6_IL — Unity 포트

판타지 중세 설원 뱀서바이벌 게임의 Unity 2022 LTS 포트.

## 셋업 (한 번만)

1. **Unity Hub 열기** → Projects 탭 → "Add" 또는 "Open"
2. **`6_IL/unity` 폴더 선택** (현재 이 폴더)
3. Unity가 처음 열 때:
   - "This project does not have a Unity project structure. Initialize?" → Yes
   - 또는 Hub에서 "New Project" → Template: **2D Core** → Project name: 임의 → Location: 임의 → 프로젝트 생성 후 `Assets/Scripts/` 폴더를 이 리포의 `unity/Assets/Scripts/`로 복사 (또는 Unity 프로젝트 폴더 자체를 `unity/`로 옮김)

> **권장**: Unity Hub에서 새로운 2D Core 프로젝트를 임시 위치에 만들고, 생성된 `Assets`, `Packages`, `ProjectSettings` 세 폴더를 이 `unity/` 안으로 복사. 이미 준비된 `Scripts/` 폴더와 자연스레 통합됨.

## 폴더 구조

```
unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Config/        # 게임 상수, 밸런스
│   │   ├── Events/        # 이벤트 버스
│   │   ├── Util/          # RNG, 수학 유틸
│   │   ├── Resources/     # 자원 관리
│   │   ├── Combat/        # 무기, 대미지
│   │   ├── Cycle/         # 낮/밤 사이클
│   │   ├── Player/        # 플레이어 컨트롤러
│   │   ├── Gather/        # 채집 (나무·돌)
│   │   ├── Animals/       # 사슴
│   │   ├── Enemies/       # 좀비, 웨이브
│   │   ├── Village/       # 그리드, 건물, 배치
│   │   ├── World/         # 청크 매니저
│   │   ├── Vision/        # 시야 마스크
│   │   ├── UI/            # HUD, 패널
│   │   └── Persistence/   # 세이브/로드
│   └── Scenes/
│       ├── BootScene.unity
│       ├── SnowfieldScene.unity
│       ├── VillageScene.unity
│       └── HudScene.unity
├── Packages/
└── ProjectSettings/
```

## 아키텍처 메모

- **MonoBehaviour 우선** + 좀비/투사체는 풀링 (사용자 선택: 하이브리드)
- **이벤트 버스**: `EventBus.Instance` 싱글톤 (씬 전환 보존)
- **RNG**: 시드 기반 mulberry32 (`SeededRng`)
- **밸런스**: `BalanceConfig` ScriptableObject (Resources 폴더 또는 Inspector 할당)
- **세이브**: `Application.persistentDataPath/save.json`

## Phaser 원본 대응

| Phaser (TypeScript) | Unity (C#) |
|---|---|
| `src/util/rng.ts` | `Util/SeededRng.cs` |
| `src/events/event-bus.ts` | `Events/EventBus.cs` |
| `src/config/balance.ts` | `Config/BalanceConfig.cs` (ScriptableObject) |
| `src/gameplay/resources/resource-store.ts` | `Resources/ResourceStore.cs` |
| `src/gameplay/combat/damage-calc.ts` | `Combat/DamageCalc.cs` |
| `src/gameplay/cycle/day-night-controller.ts` | `Cycle/DayNightController.cs` |
| `src/gameplay/village/grid.ts` | `Village/VillageGrid.cs` |
| `src/gameplay/enemies/wave-spawner.ts` | `Enemies/WaveSpawner.cs` |
| `src/gameplay/persistence/save-load.ts` | `Persistence/SaveLoad.cs` |
| `src/scenes/snowfield.ts` | `SnowfieldScene` GameObject + `SnowfieldController.cs` |
| `src/scenes/village.ts` | `VillageScene` GameObject + `VillageController.cs` |

## 다음 단계

1. Unity 프로젝트 생성/통합 후 `Assets/Scripts/` 컴파일 확인
2. 첫 씬 `BootScene` 만들기 → 플레이어 게임오브젝트 + `PlayerController` + 카메라
3. 점진적 확장 (Phase 1 수직 슬라이스 따라가기)
