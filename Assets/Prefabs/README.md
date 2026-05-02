# 6_IL 프리팹 폴더

각 게임 오브젝트(동물·동료·건물·이펙트)를 .prefab 자산으로 보관.
Unity 에디터에서 sprite/머티리얼만 갈아끼우면 코드 수정 없이 모양 교체 가능.

## 폴더 구조

```
Assets/Prefabs/
├── Animals/           # Rabbit / Fox / Deer / Boar / SnowHare / Wolf / Bear / Mammoth
├── Companions/        # 사냥꾼 / 전사 / 치유사 / 농부 / 방랑객 (5종 NPC archetype)
├── Buildings/         # Campfire / Fence / Gate / House / Storage / Farm / Watchtower / Infirmary / HuntersHut / Barricade
└── Effects/           # ConstructionSite / Boom / Slash / SnowPuff / FloatText
```

## 자동 생성 (Editor 메뉴)

1. Unity 에디터 열기
2. 메뉴 **`IL6 > Tools > Generate All Prefabs`** 클릭
3. `Assets/Prefabs/Buildings/` 에 Campfire / Fence / Gate 자동 생성됨
4. 콘솔에 saved 메시지 확인

> **현재 한계:** 건물 일부만 자동 생성 가능 — Animals/Companions 는 SimpleHud 의
> 인스턴스 메서드에 묶여있어 정적 호출 불가. 두 옵션:
> - Editor 에서 빈 GameObject 생성 → 컴포넌트 직접 추가 (DeerAi/Rigidbody2D/...) → "Save as Prefab"
> - 또는 SimpleHud 의 SpawnX 메서드를 static 으로 빼서 PrefabGenerator 도 호출 가능하게 리팩토링

## 수동 프리팹 생성 (가장 간단)

1. 씬에서 게임을 한 번 실행
2. 일시정지 → Hierarchy 에서 원하는 GameObject (예: Wolf_proc) 선택
3. 드래그해서 Project 창의 `Assets/Prefabs/Animals/` 에 떨굼 → "Original" 선택
4. 이름 정리 (`Wolf_proc` → `Wolf`)
5. 게임 종료. 프리팹은 그대로 자산으로 남음.

## 코드 연결 후 작업

프리팹 만든 다음, ProceduralSpawner 의 CreateOneAnimal 같은 함수에서:

**현재 (코드로 모든 것 조립):**
```csharp
var go = new GameObject(a.Name);
go.AddComponent<SpriteRenderer>();
go.AddComponent<Rigidbody2D>();
go.AddComponent<DeerAi>();
// ... 수십 줄
```

**프리팹 사용 (5줄):**
```csharp
[SerializeField] GameObject wolfPrefab; // 인스펙터에서 드래그
var go = Instantiate(wolfPrefab, new Vector3(x, y, 0), Quaternion.identity);
go.GetComponent<AnimalAi>().InitHp(hp);
```

## 명명 규칙

- PascalCase (`Wolf.prefab`, `HuntersHut.prefab`)
- 변종은 접미사 없이 별도 프리팹 (`ZombieFast`, `ZombieTank`, `ZombieArcher`)
- 프리팹 색은 SpriteRenderer.color 또는 Material 로 — 같은 메쉬 다른 색 OK
