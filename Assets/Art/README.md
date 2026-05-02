# 6_IL 아트 자산 폴더

여기에 외부에서 받은 / 직접 그린 모든 아트 자산을 넣으면 됨.
경로 한번 정해두면 코드에서 `Resources.Load` 로 가져오거나
씬 인스펙터에서 직접 SpriteRenderer.sprite 에 드래그해서 사용.

## 권장 파일 명명 규칙
- 소문자 + snake_case (`fox_run_01.png`)
- 같은 시리즈는 인덱스 (`zombie_walk_01.png` … `zombie_walk_08.png`)
- 변종은 접미사 (`zombie_archer.png`, `zombie_tank.png`)
- 메타는 Unity 가 자동 생성 — 절대 손대지 말고 그냥 같이 커밋

## 폴더 구조

```
Assets/Art/
├── Sprites/
│   ├── Player/         # 플레이어 캐릭터 (idle/walk/attack)
│   ├── Companions/     # 동료 5종 (사냥꾼/전사/치유사/농부/방랑객)
│   ├── Animals/        # 토끼·여우·사슴·멧돼지·흰토끼·늑대·곰·맘모스
│   ├── Enemies/        # 좀비 (Normal/Fast/Tank/Archer) + 보스
│   ├── Buildings/      # 모닥불/집/펜스/창고/농장/망루/의무실/사냥꾼오두막/문
│   ├── Resources/      # 나무·돌·고기·식량·서리꽃 아이콘 + 월드 노드
│   ├── Effects/        # 슬래시·폭발·번개·눈송이·피격 플래시
│   └── UI/             # 룬 아이콘·HP/XP 바·버튼 프레임·미니맵 마커
├── Audio/
│   ├── Music/          # 페이즈별 BGM (.ogg/.wav)
│   └── SFX/            # 효과음 (.wav)
└── Tilemaps/           # 타일맵 / 지면 텍스처
```

## 코드 연결 방법

### A) 인스펙터에서 직접
1. ProceduralSpawner / VillageStarter / Pet.cs 등의 Spawn 메서드에서
   `var sr = go.AddComponent<SpriteRenderer>();` 다음 줄에
   `sr.sprite = ...;` 로 넣고, 인스펙터 노출 필드를 만들어 드래그.

### B) Resources.Load
파일을 `Assets/Resources/Sprites/...` 로 옮기고:
```csharp
sr.sprite = Resources.Load<Sprite>("Sprites/Player/idle");
```

### C) 프리팹화
가장 권장 — 자세한 건 Prefabs/README 참조 (다음 단계에서 생성 예정).

## 현재 상태
지금은 모든 sprite 가 `ColorFallback.cs` 의 절차적 사각/원/삼각으로 그려지고 있음.
실제 아트 넣으려면:
1. `ColorFallback` 컴포넌트 제거 (또는 RequireComponent 가드 우회)
2. `SpriteRenderer.sprite = (load한 Sprite)` 직접 세팅
3. 색은 `sr.color` 로 tint
