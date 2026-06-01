# Unity 아트 업그레이드 구현 플랜

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Snowfield 씬을 Core Keeper 스타일(픽셀 top-down + URP 2D Light + 블룸 + LPC/Kenney 에셋)로 완성한 뒤 동일 레시피를 Village 씬에 복제한다.

**Architecture:** Unity 2022 LTS 2D Core 프로젝트를 URP 2D Renderer 로 전환하고, 기존 `SpriteRenderer` 기반 prefab 5종(Tree/Deer/Zombie/Campfire/Barricade) + Player 의 스프라이트를 LPC 로 교체, 타일맵 + 2D Light + Volume(블룸/비네트) + 파티클을 계층화. 게임 로직(`PlayerController`, `WaveSpawner`, `DamageCalc`, `EventBus`, `DayNightController` 코어) 은 변경 없음.

**Tech Stack:** Unity 2022 LTS, Universal RP 14.x (2D Renderer), TextMeshPro, Unity Tilemap, Unity Particle System, Unity Test Framework (EditMode), LPC (CC-BY-SA 3.0), Kenney UI RPG Expansion + Particle Pack (CC0), m3x6 font (OFL).

**Related spec:** [../specs/2026-04-23-unity-art-upgrade-design.md](../specs/2026-04-23-unity-art-upgrade-design.md)

---

## File Structure

### New files

- `unity/Packages/` — Unity Package Manager lockfile/manifest (Unity-managed)
- `unity/ProjectSettings/` — Unity 설정 (Unity-managed)
- `unity/Assets/Settings/URP-2D-Renderer.asset` — URP 2D Renderer Data
- `unity/Assets/Settings/URP-PipelineAsset-2D.asset` — URP Pipeline Asset
- `unity/Assets/Settings/GlobalVolume.asset` — Post-processing Volume Profile
- `unity/Assets/Art/Tiles/Snow/` — LPC 설원 타일 스프라이트
- `unity/Assets/Art/Tiles/SnowRuleTile.asset` — Rule Tile
- `unity/Assets/Art/Props/` — 나무/돌/사슴/좀비/모닥불/바리케이드 LPC 스프라이트
- `unity/Assets/Art/Characters/` — Player LPC 스프라이트
- `unity/Assets/Art/UI/` — Kenney UI RPG 9-slice 패널, 아이콘
- `unity/Assets/Art/FX/` — Kenney 파티클 스프라이트 (snow, spark, leaf, dust)
- `unity/Assets/Fonts/m3x6.ttf` — 픽셀 폰트
- `unity/Assets/Fonts/m3x6-TMP.asset` — TMP Font Asset
- `unity/Assets/Prefabs/Tree.prefab`, `Deer.prefab`, `Zombie.prefab`, `Campfire.prefab`, `Barricade.prefab`, `Player.prefab` — 기존 SETUP.md 대로 생성 안 된 상태면 새로 만듦
- `unity/Assets/Prefabs/FX/HitSpark.prefab`, `TreeChop.prefab`, `DeerDeath.prefab`, `SnowFall.prefab`
- `unity/Assets/Prefabs/UI/` — HUD prefab 9개
- `unity/Assets/Scripts/Util/YSort.cs` — Y축 기반 Sorting Order 자동 세팅 MonoBehaviour
- `unity/Assets/Scripts/FX/LightFlicker.cs` — Light 2D intensity 진동 MonoBehaviour
- `unity/Assets/Scripts/Cycle/DayNightLightBinder.cs` — DayNightController 이벤트 구독해 Global Light 2D intensity/색 Lerp 하는 MonoBehaviour
- `unity/Assets/Scripts/Util/DayNightLightCalc.cs` — 위 바인더가 쓰는 pure 계산 로직 (testable)
- `unity/Assets/Scripts/Tests/IL6.Tests.asmdef` — Tests Assembly Definition
- `unity/Assets/Scripts/Tests/YSortTests.cs`
- `unity/Assets/Scripts/Tests/LightFlickerTests.cs`
- `unity/Assets/Scripts/Tests/DayNightLightCalcTests.cs`
- `unity/Assets/Scripts/IL6.asmdef` — 기존 스크립트 어셈블리 정의 (Tests asmdef 이 참조할 수 있게 필수)

### Modified files

- `unity/Assets/Scripts/World/ChunkManager.cs` — Tilemap 연동 메서드 1개 추가 (chunk 로드 시 눈 타일 채움)
- `unity/Assets/Scripts/Vision/VisionMask.cs` — URP 호환 실패 시에만, Light 2D 기반 재구현 (Task 7 결정)
- `README.md` (repo 루트) — LPC attribution Credits 섹션 추가

### Unchanged (로직 0줄 변경)

- `DayNightController.cs`, `PlayerController.cs`, `WaveSpawner.cs`, `DamageCalc.cs`, `EventBus.cs`, `GatherController.cs`, `Zombie.cs`, `Gatherable.cs`, `PlacementController.cs`, `Building.cs`, `BalanceConfig.cs`, 모든 UI 스크립트의 상태/이벤트 로직

---

## Phase 0: Unity 프로젝트 부트스트랩

### Task 1: Unity 프로젝트 생성/통합 검증

**Files:**
- Verify: `unity/Packages/manifest.json`, `unity/ProjectSettings/ProjectVersion.txt`

- [ ] **Step 1: 현재 Unity 프로젝트 상태 확인**

Run:
```bash
ls unity/Packages unity/ProjectSettings 2>&1
```

Expected: 둘 다 존재. **둘 다 없으면** `unity/SETUP.md` 옵션 A 를 따라 2D Core 프로젝트를 생성하고 `Packages/`, `ProjectSettings/` 를 `unity/` 에 복사한 뒤 이어감.

- [ ] **Step 2: Unity Hub 에서 `unity/` 를 Open 하고 컴파일 에러 0개 확인**

Unity Hub → Open → `c:/Users/bada/6_IL/unity/` 선택. 첫 로드 후 Console 창 확인.
Expected: Console 에러 0개. `using UnityEngine.UI` 관련 에러 나오면 Package Manager 에서 "Unity UI" 패키지 설치.

- [ ] **Step 3: SETUP.md 의 "3. 핵심 셋업" 절차로 BalanceConfig + 기본 씬 (BootScene, SnowfieldScene, VillageScene) + 기본 prefab(Tree, Deer, Zombie, Campfire, Barricade, Player) 이 준비됐는지 확인**

씬과 prefab 들이 없으면 SETUP.md 따라 만들고 Play 로 플레이어가 움직이는지 검증.

- [ ] **Step 4: Commit (반드시 모든 `.meta` 파일 포함)**

Unity 가 처음 열리면 `unity/Assets/Scripts/` 의 모든 `.cs` 파일, `IL6.asmdef` (Task 2 에서 미리 작성됨), 그리고 향후 추가될 에셋에 대한 `.meta` 파일을 자동 생성한다. **이 `.meta` 파일들은 반드시 커밋되어야 한다** — Unity 가 GUID 기반으로 에셋을 참조하기 때문에, `.meta` 가 없으면 매번 새 GUID 가 생성되어 asmdef references, prefab 참조 등이 깨진다.

특히 `unity/Assets/Scripts/IL6.asmdef.meta` 는 Task 11 의 `IL6.Tests.asmdef` 가 참조해야 하므로 필수.

```bash
cd c:/Users/bada/6_IL
git add unity/
git commit -m "chore(unity): bootstrap 2D Core project + generated .meta files"
```

`.gitignore` 에 `*.meta` 가 들어있으면 제거. Unity 프로젝트의 `.meta` 는 반드시 버전 관리 대상.

만약 Unity 가 자동 생성한 파일만 있고 실제 변경이 없으면 이 커밋은 스킵.

---

### Task 2: 기존 스크립트용 Assembly Definition 생성

**Why:** Tests asmdef 가 IL6 네임스페이스 코드를 참조하려면 production 코드도 asmdef 로 감싸야 함.

**Files:**
- Create: `unity/Assets/Scripts/IL6.asmdef`

- [ ] **Step 1: Project 창에서 `Assets/Scripts/` 우클릭 → Create → Assembly Definition, 이름 `IL6`**

기본 설정 그대로 (name=IL6, rootNamespace=IL6, allowUnsafeCode=false).

- [ ] **Step 2: 컴파일 확인**

Console 창. Expected: 에러 0개. 컴파일 재빌드 자동.

- [ ] **Step 3: Commit**

```bash
git add unity/Assets/Scripts/IL6.asmdef unity/Assets/Scripts/IL6.asmdef.meta
git commit -m "chore(unity): add IL6 assembly definition"
```

---

## Phase 1: URP 2D 파이프라인 전환

### Task 3: Universal RP 패키지 설치

**Files:**
- Modify: `unity/Packages/manifest.json`

- [ ] **Step 1: Package Manager 에서 Universal RP 설치**

Window → Package Manager → Packages: Unity Registry → "Universal RP" 검색 → Install. Unity 2022 LTS 는 14.x 계열.

- [ ] **Step 2: 설치 확인**

`unity/Packages/manifest.json` 에 `"com.unity.render-pipelines.universal"` 항목 존재.

- [ ] **Step 3: Commit**

```bash
git add unity/Packages/manifest.json unity/Packages/packages-lock.json
git commit -m "chore(unity): add Universal RP package"
```

---

### Task 4: URP 2D Renderer + Pipeline Asset 생성 및 할당

**Files:**
- Create: `unity/Assets/Settings/URP-2D-Renderer.asset`
- Create: `unity/Assets/Settings/URP-PipelineAsset-2D.asset`
- Modify: `unity/ProjectSettings/GraphicsSettings.asset`, `unity/ProjectSettings/QualitySettings.asset`

- [ ] **Step 1: Settings 폴더 준비**

Project 창에서 `Assets/` 우클릭 → Create → Folder → "Settings".

- [ ] **Step 2: 2D Renderer Data 생성**

`Assets/Settings/` 우클릭 → Create → Rendering → URP 2D Renderer. 이름 `URP-2D-Renderer`.

- [ ] **Step 3: Pipeline Asset 생성 및 2D Renderer 참조**

`Assets/Settings/` 우클릭 → Create → Rendering → URP Asset (with 2D Renderer). 이름 `URP-PipelineAsset-2D`. Inspector → Renderer List 에 방금 만든 `URP-2D-Renderer` 가 0번으로 들어가 있는지 확인.

- [ ] **Step 4: Project Settings 에 Pipeline Asset 할당**

Edit → Project Settings → Graphics → Scriptable Render Pipeline Settings 에 `URP-PipelineAsset-2D` 드래그.
Project Settings → Quality → 모든 Quality Level (Low/Medium/High 등) 의 Render Pipeline Asset 에도 동일하게 드래그.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Settings/ unity/ProjectSettings/GraphicsSettings.asset unity/ProjectSettings/QualitySettings.asset
git commit -m "feat(unity): add URP 2D Renderer + Pipeline Asset"
```

---

### Task 5: 머티리얼 URP 업그레이드 + Snowfield Play 검증

**Why:** Built-in RP 머티리얼이 URP에서 핑크색으로 깨지는 걸 방지.

- [ ] **Step 1: 머티리얼 자동 업그레이드**

Edit → Rendering → Materials → Convert All Built-in Materials to URP. 다이얼로그에서 Proceed. (2D Sprite-Default 머티리얼은 URP 에서도 대체로 호환되지만 안전용)

- [ ] **Step 2: Snowfield 씬 Play**

File → Open Scene → `Assets/Scenes/SnowfieldScene.unity` → Play.

Expected:
- Console 에러 0개
- 플레이어/나무/사슴이 보임 (스프라이트 분홍색 아님)
- WASD 이동 작동

만약 분홍색이면 해당 머티리얼을 선택 → Shader → Universal Render Pipeline → 2D → Sprite-Default 수동 변경.

- [ ] **Step 3: Commit**

```bash
git add unity/Assets/ unity/ProjectSettings/
git commit -m "chore(unity): upgrade materials to URP"
```

---

### Task 6: VisionMask URP 호환성 결정

**Why:** `SpriteMask` 가 URP 2D 에서 깨지면 Task 19 에서 Light 2D 기반으로 재구현해야 함. 지금 결정해야 이후 씬 설정이 일관됨.

- [ ] **Step 1: Snowfield 씬 Play 중 시야 마스크 확인**

플레이어 주변만 원형으로 보이고 나머지는 어두워야 함 (`VisionMask.cs` 의 기대 동작).

결과 기록:
- **A) 정상 동작**: VisionMask 유지, Task 19 에서 Light 2D 는 시각 보강 목적으로만 추가
- **B) 깨짐 (전체가 검거나 마스크가 안 보임)**: Task 19 에서 `VisionMask.cs` 를 Light 2D 기반으로 재구현

- [ ] **Step 2: 결정을 spec 에 기록**

`docs/superpowers/specs/2026-04-23-unity-art-upgrade-design.md` 의 "VisionMask 처리 전략" 섹션 아래에 실측 결과 한 줄 추가 ("실측 결과: A / B 채택").

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/specs/2026-04-23-unity-art-upgrade-design.md
git commit -m "docs(spec): record VisionMask URP compat test result"
```

---

## Phase 2: 에셋 다운로드와 임포트

### Task 7: LPC 통합 팩 다운로드 및 임포트

**Why:** 환경 타일, 플레이어, 좀비, 사슴, 나무, 건물 전부 한 팩으로 통일.

**Files:**
- Create: `unity/Assets/Art/Tiles/Snow/`, `unity/Assets/Art/Props/`, `unity/Assets/Art/Characters/`

- [ ] **Step 1: OpenGameArt 에서 LPC 팩 다운로드**

다음 URL 세 개를 다운로드 (외부 네트워크 필요):
- 설원 타일: https://opengameart.org/content/lpc-terrains (Snow Tiles 포함)
- 캐릭터 베이스: https://opengameart.org/content/lpc-character-bases
- 좀비: https://opengameart.org/content/lpc-zombies-skeletons-and-monsters
- 사슴/동물: https://opengameart.org/content/lpc-deer (없으면 https://opengameart.org/content/lpc-animals)
- 나무: https://opengameart.org/content/lpc-trees
- 건물/모닥불/바리케이드: https://opengameart.org/content/lpc-town

각 zip 에서 실제로 쓸 PNG (필요한 타일/캐릭터) 만 선별.

- [ ] **Step 2: Art 폴더 구조 생성 및 임포트**

Unity Project 창:
- `Assets/Art/Tiles/Snow/` 폴더 만들고 설원 타일 PNG 를 드래그 임포트
- `Assets/Art/Characters/` — player, zombie 스프라이트 시트
- `Assets/Art/Props/` — tree, deer, campfire, barricade, stone

- [ ] **Step 3: 임포트 설정 일괄 적용**

각 PNG 선택 → Inspector:
- Texture Type: `Sprite (2D and UI)`
- Pixels Per Unit: `32`
- Filter Mode: `Point (no filter)`
- Compression: `None`
- (스프라이트 시트인 경우) Sprite Mode: `Multiple` → Sprite Editor 로 Slice (Grid By Cell Size, 32×32 또는 64×64 팩 규격)
- Apply

- [ ] **Step 4: 컴파일/임포트 에러 0 확인**

Console 창.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Art/
git commit -m "feat(art): import LPC terrain + characters + props"
```

---

### Task 8: Kenney UI + Particle 팩 다운로드 및 임포트

**Files:**
- Create: `unity/Assets/Art/UI/`, `unity/Assets/Art/FX/`

- [ ] **Step 1: Kenney 팩 다운로드**

- UI RPG Expansion: https://kenney.nl/assets/ui-pack-rpg-expansion
- Particle Pack: https://kenney.nl/assets/particle-pack

- [ ] **Step 2: 임포트**

- `Assets/Art/UI/` 에 UI RPG Expansion 의 PNG 드래그 (panels, buttons, icons)
- `Assets/Art/FX/` 에 Particle Pack 의 필요 PNG (snowflake, spark, leaf, dust)

- [ ] **Step 3: 임포트 설정 일괄**

- UI 스프라이트: Texture Type `Sprite (2D and UI)`, Pixels Per Unit `32`, Filter `Point`, Compression `None`
- 9-slice 패널: Inspector → Sprite Editor → Border 설정 (L/R/T/B 각 8~16px)
- FX 스프라이트: 위와 동일 + Pivot `Center`

- [ ] **Step 4: Commit**

```bash
git add unity/Assets/Art/UI/ unity/Assets/Art/FX/
git commit -m "feat(art): import Kenney UI RPG + Particle pack"
```

---

### Task 9: m3x6 픽셀 폰트 + TMP Asset 생성

**Files:**
- Create: `unity/Assets/Fonts/m3x6.ttf`, `unity/Assets/Fonts/m3x6-TMP.asset`

- [ ] **Step 1: m3x6 다운로드**

https://managore.itch.io/m3x6 → Download (무료, OFL).

- [ ] **Step 2: TextMeshPro Essentials 임포트**

Window → TextMeshPro → Import TMP Essential Resources.

- [ ] **Step 3: TMP Font Asset 생성**

`Assets/Fonts/` 폴더 생성 후 m3x6.ttf 드래그.
Window → TextMeshPro → Font Asset Creator → Source Font File: `m3x6` → Sampling Point Size: `Custom 16` → Padding: `2` → Packing Method: `Fast` → Character Set: `ASCII` → Generate Font Atlas → Save as `m3x6-TMP.asset` 을 `Assets/Fonts/` 안에.

- [ ] **Step 4: Commit**

```bash
git add unity/Assets/Fonts/
git commit -m "feat(art): import m3x6 pixel font + TMP asset"
```

---

### Task 10: README Credits 섹션 추가

**Files:**
- Modify: `README.md`

- [ ] **Step 1: README 최하단에 Credits 섹션 추가**

`README.md` 끝에 다음 블록 추가:

```markdown

## Credits

### Art Assets

- **LPC (Liberated Pixel Cup)** — characters, props, tiles
  - Licensed under CC-BY-SA 3.0 or GPL 3.0 (dual-licensed, choose either)
  - License text: https://creativecommons.org/licenses/by-sa/3.0/ or https://www.gnu.org/licenses/gpl-3.0.html
  - Contributor list: https://opengameart.org/content/lpc-collection
- **Kenney — UI Pack RPG Expansion** — HUD panels and icons (CC0)
  - https://kenney.nl/assets/ui-pack-rpg-expansion
- **Kenney — Particle Pack** — VFX sprites (CC0)
  - https://kenney.nl/assets/particle-pack
- **m3x6 font** by Daniel Linssen (OFL)
  - https://managore.itch.io/m3x6
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: add art asset credits"
```

---

## Phase 3: C# 헬퍼 (TDD)

### Task 11: Tests Assembly Definition 생성

**Files:**
- Create: `unity/Assets/Scripts/Tests/IL6.Tests.asmdef`

- [ ] **Step 1: Unity Test Framework 패키지 확인**

Window → Package Manager → In Project 탭에서 "Test Framework" 가 설치돼 있는지 확인. 없으면 Unity Registry 에서 설치.

- [ ] **Step 2: Tests 폴더 + asmdef 생성**

`Assets/Scripts/` 우클릭 → Create → Folder → "Tests".
`Assets/Scripts/Tests/` 우클릭 → Create → Testing → Tests Assembly Folder (또는 asmdef 수동 생성).
이름 `IL6.Tests`.

- [ ] **Step 3: asmdef 설정**

Inspector:
- Name: `IL6.Tests`
- Assembly Definition References: `IL6` 추가
- Platforms: Editor only
- Define Constraints: (비움)
- 하단 Test Assemblies 체크 → UnityEngine.TestRunner, UnityEditor.TestRunner 추가

- [ ] **Step 4: Test Runner 창에서 비어있는 EditMode 섹션 확인**

Window → General → Test Runner → EditMode 탭. Assembly 에 `IL6.Tests` 보임.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/Tests/
git commit -m "chore(unity): add IL6.Tests assembly definition"
```

---

### Task 12: YSort — TDD

**Files:**
- Create: `unity/Assets/Scripts/Util/YSort.cs`
- Create: `unity/Assets/Scripts/Tests/YSortTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

`Assets/Scripts/Tests/YSortTests.cs`:

```csharp
using NUnit.Framework;
using IL6;

namespace IL6.Tests
{
    public class YSortTests
    {
        [Test]
        public void ComputeOrder_YZero_Returns_Zero()
        {
            Assert.AreEqual(0, YSort.ComputeOrder(0f));
        }

        [Test]
        public void ComputeOrder_YPositive_Returns_Negative()
        {
            Assert.AreEqual(-500, YSort.ComputeOrder(5f));
        }

        [Test]
        public void ComputeOrder_YNegative_Returns_Positive()
        {
            Assert.AreEqual(300, YSort.ComputeOrder(-3f));
        }

        [Test]
        public void ComputeOrder_RoundsToNearestInt()
        {
            Assert.AreEqual(-123, YSort.ComputeOrder(1.234f));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행으로 실패 확인**

Test Runner → EditMode → Run All. Expected: FAIL "YSort does not exist".

- [ ] **Step 3: 최소 구현**

`Assets/Scripts/Util/YSort.cs`:

```csharp
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// Y축 월드 좌표 기반으로 SpriteRenderer 의 sortingOrder 를 설정.
    /// 아래쪽(더 큰 -y)에 있는 오브젝트가 위쪽 오브젝트보다 앞에 그려짐.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class YSort : MonoBehaviour
    {
        private SpriteRenderer _sr;

        public static int ComputeOrder(float y) => Mathf.RoundToInt(-y * 100f);

        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }

        private void LateUpdate()
        {
            _sr.sortingOrder = ComputeOrder(transform.position.y);
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → Run All. Expected: 4/4 PASS.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/Util/YSort.cs unity/Assets/Scripts/Tests/YSortTests.cs
git commit -m "feat(unity): add YSort component with tests"
```

---

### Task 13: LightFlicker — TDD

**Files:**
- Create: `unity/Assets/Scripts/FX/LightFlicker.cs`
- Create: `unity/Assets/Scripts/Tests/LightFlickerTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

`Assets/Scripts/Tests/LightFlickerTests.cs`:

```csharp
using NUnit.Framework;
using IL6;

namespace IL6.Tests
{
    public class LightFlickerTests
    {
        [Test]
        public void ComputeIntensity_AtTimeZero_Returns_BaseIntensity()
        {
            float result = LightFlicker.ComputeIntensity(1.5f, 0f, 0.2f, 3f);
            Assert.AreEqual(1.5f, result, 0.0001f);
        }

        [Test]
        public void ComputeIntensity_OscillatesWithinBand()
        {
            // 0.25 주기의 1/4 = Sin(pi/2) = 1 → intensity = base + amp
            float result = LightFlicker.ComputeIntensity(1.0f, 0.25f, 0.2f, 1f);
            Assert.AreEqual(1.2f, result, 0.01f);
        }

        [Test]
        public void ComputeIntensity_NeverNegative()
        {
            float result = LightFlicker.ComputeIntensity(0.1f, 0.75f, 0.5f, 1f);
            Assert.GreaterOrEqual(result, 0f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행으로 실패 확인**

Test Runner → Run All. Expected: FAIL "LightFlicker does not exist".

- [ ] **Step 3: 최소 구현**

`Assets/Scripts/FX/LightFlicker.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IL6
{
    /// <summary>
    /// Light 2D intensity 를 sin wave 로 진동 (모닥불 플리커).
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class LightFlicker : MonoBehaviour
    {
        public float BaseIntensity = 1.5f;
        public float Amplitude = 0.2f;
        public float Frequency = 3f; // Hz

        private Light2D _light;

        public static float ComputeIntensity(float baseIntensity, float time, float amp, float freq)
        {
            float raw = baseIntensity + Mathf.Sin(time * freq * 2f * Mathf.PI) * amp;
            return Mathf.Max(0f, raw);
        }

        private void Awake() { _light = GetComponent<Light2D>(); }

        private void Update()
        {
            _light.intensity = ComputeIntensity(BaseIntensity, Time.time, Amplitude, Frequency);
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Expected: 3/3 PASS.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/FX/LightFlicker.cs unity/Assets/Scripts/Tests/LightFlickerTests.cs
git commit -m "feat(unity): add LightFlicker with tests"
```

---

### Task 14: DayNightLightCalc (순수 로직) — TDD

**Why:** 낮/밤 Phase 에 따라 Light 2D intensity + color 를 결정하는 로직을 pure C# 으로 분리해 MonoBehaviour 없이 테스트 가능하게 함.

**Files:**
- Create: `unity/Assets/Scripts/Util/DayNightLightCalc.cs`
- Create: `unity/Assets/Scripts/Tests/DayNightLightCalcTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

`Assets/Scripts/Tests/DayNightLightCalcTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using IL6;

namespace IL6.Tests
{
    public class DayNightLightCalcTests
    {
        [Test]
        public void DayTarget_IsBrightWarmWhite()
        {
            var (intensity, color) = DayNightLightCalc.Target(Phase.Day);
            Assert.AreEqual(1.0f, intensity, 0.001f);
            Assert.Greater(color.r, 0.9f);
            Assert.Greater(color.g, 0.9f);
        }

        [Test]
        public void NightTarget_IsDarkCoolBlue()
        {
            var (intensity, color) = DayNightLightCalc.Target(Phase.Night);
            Assert.AreEqual(0.25f, intensity, 0.001f);
            Assert.Less(color.r, 0.5f);
            Assert.Greater(color.b, color.r);
        }

        [Test]
        public void Lerp_HalfwayBetweenDayAndNight_IsMidpoint()
        {
            var (i, _) = DayNightLightCalc.Lerp(Phase.Day, Phase.Night, 0.5f);
            Assert.AreEqual((1.0f + 0.25f) / 2f, i, 0.001f);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Test Runner → Run All. Expected: FAIL.

- [ ] **Step 3: 최소 구현**

`Assets/Scripts/Util/DayNightLightCalc.cs`:

```csharp
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 페이즈별 Global Light 2D 타겟 값 계산 (pure).
    /// </summary>
    public static class DayNightLightCalc
    {
        public static (float intensity, Color color) Target(Phase phase) => phase switch
        {
            Phase.Day     => (1.0f, new Color(0.96f, 0.96f, 0.94f)), // #f5f5f0
            Phase.Evening => (0.65f, new Color(0.85f, 0.65f, 0.55f)), // 주황빛 석양
            Phase.Night   => (0.25f, new Color(0.23f, 0.29f, 0.48f)), // #3a4a7a
            Phase.Dawn    => (0.65f, new Color(0.85f, 0.70f, 0.70f)), // 연분홍 새벽
            _             => (1.0f, Color.white),
        };

        public static (float intensity, Color color) Lerp(Phase from, Phase to, float t)
        {
            var a = Target(from); var b = Target(to);
            return (
                Mathf.Lerp(a.intensity, b.intensity, t),
                Color.Lerp(a.color, b.color, t)
            );
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Expected: 3/3 PASS.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/Util/DayNightLightCalc.cs unity/Assets/Scripts/Tests/DayNightLightCalcTests.cs
git commit -m "feat(unity): add DayNightLightCalc pure logic with tests"
```

---

### Task 15: DayNightLightBinder (MonoBehaviour 접착층)

**Files:**
- Create: `unity/Assets/Scripts/Cycle/DayNightLightBinder.cs`

- [ ] **Step 1: 구현**

`Assets/Scripts/Cycle/DayNightLightBinder.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IL6
{
    /// <summary>
    /// DayNightController 의 Phase 이벤트를 구독해 Global Light 2D 의 intensity/color 를
    /// 현재 → 다음 페이즈 타겟 값으로 Lerp 전환.
    /// DayNightController 자체는 pure 로 유지하고 이 MonoBehaviour 가 Unity 와 접착.
    /// </summary>
    public sealed class DayNightLightBinder : MonoBehaviour
    {
        public Light2D GlobalLight;

        private Phase _currentPhase = Phase.Day;
        private Phase _targetPhase = Phase.Day;
        private float _transitionDuration = 5f;
        private float _elapsed;

        private void Start()
        {
            if (GlobalLight == null)
            {
                Debug.LogWarning("[DayNightLightBinder] GlobalLight 미할당");
                return;
            }
            var t = DayNightLightCalc.Target(_currentPhase);
            GlobalLight.intensity = t.intensity;
            GlobalLight.color = t.color;

            EventBus.Instance.Subscribe<EveningStartedPayload>(_ => BeginTransition(Phase.Evening));
            EventBus.Instance.Subscribe<NightStartedPayload>(_ => BeginTransition(Phase.Night));
            EventBus.Instance.Subscribe<DawnStartedPayload>(_ => BeginTransition(Phase.Dawn));
            EventBus.Instance.Subscribe<DayStartedPayload>(_ => BeginTransition(Phase.Day));
        }

        private void BeginTransition(Phase to)
        {
            _targetPhase = to;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (GlobalLight == null) return;
            if (_currentPhase == _targetPhase) return;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _transitionDuration);
            var v = DayNightLightCalc.Lerp(_currentPhase, _targetPhase, t);
            GlobalLight.intensity = v.intensity;
            GlobalLight.color = v.color;
            if (t >= 1f) _currentPhase = _targetPhase;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Expected: 에러 0개. (참고: `EventBus.Instance.Subscribe<T>(Action<T>)` 가 실제 API. `Phase` 페이로드 구조체들 (`DayStartedPayload`, `EveningStartedPayload` 등) 은 `namespace IL6` 안에 정의돼 있어 별도 using 불필요.)

- [ ] **Step 3: Commit**

```bash
git add unity/Assets/Scripts/Cycle/DayNightLightBinder.cs
git commit -m "feat(unity): add DayNightLightBinder MonoBehaviour"
```

---

## Phase 4: Sorting Layer + Prefab 스프라이트 교체

### Task 16: Sorting Layer 정의

- [ ] **Step 1: Project Settings → Tags and Layers → Sorting Layers 에 다음 순서로 추가**

1. `Ground`
2. `Props`
3. `Creatures`
4. `FX`
5. (Default — 기본으로 존재)

순서 중요: Ground 가 최하단, 위로 올라갈수록 앞에 그려짐. UI 는 Canvas Render Mode 로 따로 관리.

- [ ] **Step 2: 기존 `VisionMask` 의 sortingOrder 1000 이 어느 Layer 인지 확인**

`VisionMask.cs:31` 은 Default Layer 기준. 어둠 오버레이가 FX 레이어 위에도 있어야 하므로 Sorting Layer 를 `FX` 로 세팅하는 건 별도 작업(Task 19 에서 다룸).

- [ ] **Step 3: Commit**

```bash
git add unity/ProjectSettings/TagManager.asset
git commit -m "chore(unity): define sorting layers (Ground/Props/Creatures/FX)"
```

---

### Task 17: Prefab 스프라이트 교체 + Pivot + YSort 부착

**Files:**
- Modify: `Tree.prefab`, `Deer.prefab`, `Zombie.prefab`, `Campfire.prefab`, `Barricade.prefab`, `Player.prefab`

- [ ] **Step 1: 각 prefab 의 SpriteRenderer.sprite 를 LPC 에셋으로 교체**

Project 창에서 각 prefab 더블클릭 → Prefab Mode → SpriteRenderer 컴포넌트 → Sprite 필드에 `Assets/Art/Props/` 또는 `Assets/Art/Characters/` 의 해당 스프라이트 드래그.

매핑:
- `Tree.prefab` ← LPC tree sprite (evergreen 선호, 설원 배경에 어울리는 어두운 침엽수)
- `Deer.prefab` ← LPC deer sprite
- `Zombie.prefab` ← LPC zombie sprite
- `Campfire.prefab` ← LPC town fire pit sprite
- `Barricade.prefab` ← LPC wooden fence sprite
- `Player.prefab` ← LPC character base (남성 또는 여성 중세)

- [ ] **Step 2: Pivot 통일**

각 스프라이트 선택 → Sprite Editor → Pivot: `Bottom Center`. (LPC 스프라이트 시트의 경우 Slice 후 일괄 설정.)

- [ ] **Step 3: Sorting Layer 할당**

각 prefab 의 SpriteRenderer → Sorting Layer:
- Tree: `Props`
- Deer/Zombie/Player: `Creatures`
- Campfire/Barricade: `Props` (건물은 정적이므로 Props 로 충분)

- [ ] **Step 4: `YSort` 컴포넌트 부착**

Creatures 레이어인 것(Deer, Zombie, Player) 과 Tree 에 `YSort` 컴포넌트 추가 (Add Component → `YSort`).

Campfire/Barricade 는 정적이므로 YSort 불필요 (고정 sortingOrder 사용).

- [ ] **Step 5: SnowfieldScene Play 검증**

Play → 플레이어가 나무 뒤로 걸어들어가면 가려지고, 앞으로 나오면 보이는지 확인.

Expected: 나무/사슴/플레이어 간 Y축 자동 정렬 정상.

- [ ] **Step 6: Commit**

```bash
git add unity/Assets/Prefabs/ unity/Assets/Art/
git commit -m "feat(unity): swap prefab sprites to LPC + attach YSort"
```

---

## Phase 5: 타일맵 환경

### Task 18: Snow Rule Tile 생성

**Files:**
- Create: `unity/Assets/Art/Tiles/SnowRuleTile.asset`

- [ ] **Step 1: 2D Tilemap Extras 패키지 설치**

Window → Package Manager → My Registries/Unity Registry → "2D Tilemap Extras" 검색 설치 (Rule Tile 제공).

- [ ] **Step 2: Rule Tile 생성**

`Assets/Art/Tiles/` 우클릭 → Create → 2D → Tiles → Rule Tile. 이름 `SnowRuleTile`.

- [ ] **Step 3: Rule Tile 설정**

Inspector:
- Default Sprite: 기본 설원 타일 (가장 일반적인 variant)
- Tiling Rules → 리스트에 4~5개 랜덤 variant 추가 (Game Object 는 비움, Random 체크, 각 Rule 마다 다른 snow variant sprite 할당)
- 목표: 동일 타일을 깔아도 자동으로 variant 중 랜덤 선택되어 격자 티 제거

- [ ] **Step 4: Commit**

```bash
git add unity/Assets/Art/Tiles/SnowRuleTile.asset unity/Assets/Art/Tiles/SnowRuleTile.asset.meta unity/Packages/manifest.json
git commit -m "feat(art): add SnowRuleTile with random variants"
```

---

### Task 19: Snowfield 씬에 Tilemap 추가 + ChunkManager 연동

**Files:**
- Create: Snowfield 씬의 `Grid + Tilemap` GameObject
- Modify: `unity/Assets/Scripts/World/ChunkManager.cs`

- [ ] **Step 1: SnowfieldScene 에 Grid + Tilemap 추가**

Hierarchy 우클릭 → 2D Object → Tilemap → Rectangular. 생성된 `Grid/Tilemap` 확인.

Tilemap 의 `Tilemap Renderer` 컴포넌트 → Sorting Layer: `Ground`, Order in Layer: `0`.

Grid → Cell Size: `X=1, Y=1, Z=0` (월드 유닛 기준), Cell Gap: `0`. (스프라이트가 32px, PPU=32 이므로 한 타일 = 1 유닛.)

- [ ] **Step 2: ChunkManager 에 Tilemap 연동 메서드 추가**

`unity/Assets/Scripts/World/ChunkManager.cs` 편집. 필드와 `LoadChunk` 수정 + 새 메서드 1개:

`ChunkManager.cs:13` 근처 `public GameObject DeerPrefab;` 아래에 추가:

```csharp
        [Header("Tilemap")]
        public UnityEngine.Tilemaps.Tilemap GroundTilemap;
        public UnityEngine.Tilemaps.TileBase GroundTile;
```

`ChunkManager.cs:76` 의 `LoadChunk` 메서드 최하단 `_loaded[(cx, cy)] = data;` 바로 위에 추가:

```csharp
            FillGroundTiles(cx, cy);
```

파일 하단, 클래스 닫히기 전에 새 메서드 추가:

```csharp
        private void FillGroundTiles(int cx, int cy)
        {
            if (GroundTilemap == null || GroundTile == null) return;
            int baseX = cx * ChunkSize;
            int baseY = cy * ChunkSize;
            // ChunkSize 는 월드 유닛 기준 (예: 320). Tilemap 셀은 1 유닛 = 1 타일.
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    GroundTilemap.SetTile(
                        new Vector3Int(baseX + x, baseY + y, 0),
                        GroundTile
                    );
                }
            }
        }
```

> **주의**: ChunkSize 가 320 이면 한 청크당 320×320 = 102,400 타일. LPC 기본 64px 이면 타일 1개 = 2 유닛이 더 적절할 수도 있음 — Inspector 에서 Grid Cell Size 를 2 로 올리고 루프도 step 2 로 조정하거나, `ChunkSize` 를 40~64 로 축소 (후자가 권장). 이번 단계에선 Inspector 튜닝으로 맞춤.

- [ ] **Step 3: Inspector 에서 ChunkManager.GroundTilemap / GroundTile 할당**

Snowfield 씬의 ChunkManager GameObject 선택 → Inspector → GroundTilemap 필드에 씬의 Tilemap 드래그, GroundTile 필드에 `SnowRuleTile` 드래그.

- [ ] **Step 4: Play 검증**

Snowfield 씬 Play.

Expected:
- 바닥이 설원 타일로 깔림 (플레이어 이동 시 새 청크 로드되며 타일 추가)
- variant 랜덤 덕에 격자 티 없음
- 프레임레이트 정상 (타일 너무 많으면 ChunkSize 축소)

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/World/ChunkManager.cs unity/Assets/Scenes/SnowfieldScene.unity
git commit -m "feat(world): wire ChunkManager to Tilemap for snow ground"
```

---

## Phase 6: 라이팅

### Task 20: Global Light 2D 추가 + DayNightLightBinder 연결

**Files:**
- Modify: `SnowfieldScene.unity`

- [ ] **Step 1: Global Light 2D 추가**

SnowfieldScene Hierarchy → 우클릭 → Light → 2D → Global Light 2D. GameObject 이름 `GlobalLight`.

Inspector:
- Intensity: `1.0`
- Target Sorting Layers: 모두 체크 (Default, Ground, Props, Creatures, FX)

- [ ] **Step 2: DayNightLightBinder MonoBehaviour 추가**

Snowfield GameObject (또는 Snowfield 컨트롤러 GameObject) 에 `DayNightLightBinder` 컴포넌트 추가.

Inspector:
- Global Light: 방금 만든 `GlobalLight` GameObject 드래그

- [ ] **Step 3: Play 검증 (낮→밤 전환 기다리기)**

BalanceConfig 의 DayDurationSec 이 짧으면 (예: 10초) 쉽게 검증 가능. 또는 테스트 실행 중 DayNightController 의 `Update` 를 임시로 가속.

Expected:
- 낮: 화면 전체가 밝음 (약간 크림빛)
- 저녁: 주황 톤 전환
- 밤: 화면이 청보라 톤으로 어두워짐
- 새벽: 연분홍 톤
- Lerp 부드럽게 진행

- [ ] **Step 4: Commit**

```bash
git add unity/Assets/Scenes/SnowfieldScene.unity
git commit -m "feat(unity): add Global Light 2D + DayNightLightBinder"
```

---

### Task 21: Campfire Point Light 2D + LightFlicker

**Files:**
- Modify: `Campfire.prefab`

- [ ] **Step 1: Campfire prefab 에 자식 Light 2D 추가**

Project 창에서 `Campfire.prefab` 더블클릭 → Prefab Mode.
Hierarchy → Campfire 우클릭 → Create Child → 빈 GameObject, 이름 `FireLight`.
`FireLight` 에 컴포넌트 추가 → Light 2D.

Inspector:
- Light Type: `Point`
- Color: RGB(255, 136, 68) = `#ff8844`
- Intensity: `1.5`
- Inner Radius: `0`
- Outer Radius: `4`
- Falloff Intensity: `0.7`
- Target Sorting Layers: 모두 체크

- [ ] **Step 2: LightFlicker 컴포넌트 추가**

`FireLight` 에 `LightFlicker` 컴포넌트 추가.

Inspector:
- BaseIntensity: `1.5`
- Amplitude: `0.2`
- Frequency: `3`

- [ ] **Step 3: Play 검증 (밤에 모닥불 확인)**

Village 씬에서 모닥불 배치 후 밤까지 기다리기. (Snowfield 에는 모닥불이 기본 없으면 임시로 하나 배치 후 검증.)

Expected:
- 어두운 밤에 모닥불 주변만 원형으로 주황빛 환함
- Intensity 가 살짝 깜빡이는 플리커

- [ ] **Step 4: Commit**

```bash
git add unity/Assets/Prefabs/Campfire.prefab
git commit -m "feat(fx): add campfire Point Light 2D with flicker"
```

---

### Task 22: 플레이어 시야 라이트 (VisionMask 처리)

**Files:**
- Modify: `Player.prefab` 또는 `unity/Assets/Scripts/Vision/VisionMask.cs`

**Branch A** (Task 6 에서 `VisionMask` 정상 동작으로 판명된 경우):

- [ ] **Step 1A: Player prefab 에 자식 Light 2D 추가 (시각 보강)**

Player prefab → Create Child → 빈 GameObject `PlayerLight`. Light 2D:
- Light Type: `Point`
- Color: 흰색 살짝 따뜻 (`#fff4d8`)
- Intensity: `0.8`
- Outer Radius: `3.5` (VisionMask RadiusUnits 와 동일)

- [ ] **Step 2A: Play 검증**

낮에는 효과 거의 없음, 밤에 플레이어 주변이 살짝 환함.

**Branch B** (Task 6 에서 `VisionMask` 깨짐으로 판명된 경우):

- [ ] **Step 1B: VisionMask 를 Light 2D 기반으로 재구현**

`unity/Assets/Scripts/Vision/VisionMask.cs` 를 다음으로 교체:

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IL6
{
    /// <summary>
    /// URP 2D 기반 시야: 플레이어 자식에 Point Light 2D 1개.
    /// Global Light 를 낮은 intensity 로 유지하고 이 Light 가 주변을 밝힘.
    /// </summary>
    public sealed class VisionMask : MonoBehaviour
    {
        public Transform Target;
        public float RadiusUnits = 3.2f;
        public Color LightColor = new(1f, 0.96f, 0.85f);

        private Light2D _light;

        private void Awake()
        {
            var go = new GameObject("VisionLight");
            go.transform.SetParent(transform);
            _light = go.AddComponent<Light2D>();
            _light.lightType = Light2D.LightType.Point;
            _light.pointLightOuterRadius = RadiusUnits;
            _light.pointLightInnerRadius = RadiusUnits * 0.5f;
            _light.intensity = 1.0f;
            _light.color = LightColor;
        }

        private void LateUpdate()
        {
            if (Target == null || _light == null) return;
            _light.transform.position = new Vector3(Target.position.x, Target.position.y, 0);
        }

        public void SetRadius(float radiusUnits)
        {
            RadiusUnits = radiusUnits;
            if (_light != null)
            {
                _light.pointLightOuterRadius = radiusUnits;
                _light.pointLightInnerRadius = radiusUnits * 0.5f;
            }
        }
    }
}
```

- [ ] **Step 2B: Play 검증**

밤에 플레이어 주변만 환하고 바깥은 어두움. SetRadius 호출 시 반지름 반응.

- [ ] **Step 3: Commit (브랜치 무관)**

```bash
git add unity/Assets/Prefabs/Player.prefab unity/Assets/Scripts/Vision/VisionMask.cs
git commit -m "feat(vision): player vision light under URP 2D"
```

---

### Task 23: URP Volume 추가 (Bloom + Vignette)

**Files:**
- Create: `unity/Assets/Settings/GlobalVolume.asset`
- Modify: `SnowfieldScene.unity`

- [ ] **Step 1: Volume Profile 생성**

`Assets/Settings/` 우클릭 → Create → Volume Profile. 이름 `GlobalVolume`.

Inspector → Add Override:
- Bloom: Intensity `0.4`, Threshold `1.0`, Scatter `0.7`, Tint white
- Vignette: Intensity `0.2`, Smoothness `0.3`, Color black

- [ ] **Step 2: 씬에 Global Volume GameObject 추가**

Hierarchy 우클릭 → Volume → Global Volume. Inspector → Profile: `GlobalVolume`.

- [ ] **Step 3: Main Camera 에 Post Processing 활성**

Main Camera 선택 → Inspector → Rendering → Post Processing 체크.

- [ ] **Step 4: Play 검증**

Expected:
- 모닥불 주변 주황빛이 살짝 번짐 (bloom)
- 화면 외곽이 약간 어두워짐 (vignette)
- 과하지 않게 "은은한" 수준

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Settings/GlobalVolume.asset unity/Assets/Scenes/SnowfieldScene.unity
git commit -m "feat(unity): add URP Volume with Bloom + Vignette"
```

---

## Phase 7: VFX 파티클

### Task 24: 눈 파티클 (상시)

**Files:**
- Create: `unity/Assets/Prefabs/FX/SnowFall.prefab`

- [ ] **Step 1: ParticleSystem GameObject 생성**

Hierarchy → Effects → Particle System. 이름 `SnowFall`.

Inspector (Particle System):
- Duration: `10`, Looping: 체크
- Start Lifetime: `5`
- Start Speed: `-1` (아래로 떨어짐, 기본이 위로면 Gravity Modifier 0.3 로 대체)
- Start Size: `0.1`
- Start Color: 흰색 알파 0.7
- Shape: `Edge`, Radius `20` (카메라 위 가로 선), Angle `0`
- Emission Rate: `30`
- Renderer → Material: `Default-Particle` → Sprite 를 `Kenney snowflake` 로 바꾸려면 새 Material (`Assets/Art/FX/SnowMat.mat`) 만들어 Shader `Universal Render Pipeline/Particles/Unlit` + Main Texture 로 snowflake PNG 할당

- [ ] **Step 2: Main Camera 자식으로 이동**

Hierarchy 에서 SnowFall GameObject 를 Main Camera 아래로 드래그. Local Position `(0, 10, 0)` (카메라 위쪽).

Sorting Layer: `FX`, Order in Layer: `10`.

- [ ] **Step 3: Prefab 으로 저장**

SnowFall GameObject 를 `Assets/Prefabs/FX/` 로 드래그해 prefab 화.

- [ ] **Step 4: Play 검증**

화면 상단에서 눈이 내림. 카메라 이동해도 상시 재생.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Prefabs/FX/ unity/Assets/Art/FX/
git commit -m "feat(fx): add constant snow fall particles"
```

---

### Task 25: 타격/채집/사망 파티클

**Files:**
- Create: `unity/Assets/Prefabs/FX/HitSpark.prefab`, `TreeChop.prefab`, `DeerDeath.prefab`

- [ ] **Step 1: HitSpark prefab 만들기**

ParticleSystem GameObject. 설정:
- Duration: `0.3`, Looping: off, Play On Awake: on
- Start Lifetime: `0.3`, Start Speed: `3`, Start Size: `0.15`
- Emission: Burst 1개 (Count: 6)
- Shape: Cone, Angle 30
- Renderer: spark material (Kenney Particle Pack 의 spark)
- Sorting Layer: `FX`

Prefab 화: `Assets/Prefabs/FX/HitSpark.prefab`.

- [ ] **Step 2: TreeChop prefab (녹색 잎)**

HitSpark 복제 → 색 녹색, Start Size `0.2`, Burst Count 4, Shape Sphere.

- [ ] **Step 3: DeerDeath prefab (흙먼지 + 흰 털)**

복제 → 색 연한 갈색/흰색, Start Size `0.25`, Burst Count 5.

- [ ] **Step 4: 게임 코드에서 Instantiate 호출**

**이 단계는 원래 로직 코드를 건드려야 하므로 스코프 상 선택적**. 간단히 연결하려면:

`unity/Assets/Scripts/Combat/DamageCalc.cs` 는 순수 계산이라 파티클과 무관. 좀비 피격 시 파티클은 `Zombie.cs` 또는 전투 트리거 콜백에서 호출해야 함. **Prefab 만 준비하고 실제 연결은 후속 태스크로 분리** (이번 플랜에선 VFX 에셋 준비 단계까지만). 간단 검증을 위해 테스트로 Play 중 Hierarchy 에서 HitSpark prefab 을 수동 Instantiate 해 동작 확인.

- [ ] **Step 5: Play 검증 — HitSpark 수동 생성**

Play 중 HitSpark prefab 을 씬에 드래그 → 즉시 재생 후 사라짐.

- [ ] **Step 6: Commit**

```bash
git add unity/Assets/Prefabs/FX/
git commit -m "feat(fx): add hit/chop/deer-death particle prefabs"
```

---

## Phase 8: UI/HUD 폴리시

### Task 26: Canvas + 공통 TMP 셋업

**Why:** UI 9종이 공통으로 쓸 Canvas Scaler, TMP 폰트, Pixel Perfect 설정을 먼저 마련.

**Files:**
- Modify: `SnowfieldScene.unity` (Canvas), `VillageScene.unity`

- [ ] **Step 1: Canvas 존재 여부 확인**

SnowfieldScene Hierarchy 에 Canvas GameObject 있는지 확인. 없으면 UI → Canvas 추가.

- [ ] **Step 2: Canvas 설정**

Canvas 컴포넌트:
- Render Mode: `Screen Space - Overlay`
- Pixel Perfect: 체크

Canvas Scaler:
- UI Scale Mode: `Scale With Screen Size`
- Reference Resolution: `1920 x 1080`
- Screen Match Mode: `Match Width Or Height`
- Match: `0.5`

- [ ] **Step 3: EventSystem 있는지 확인**

없으면 UI → Event System 추가.

- [ ] **Step 4: Commit**

```bash
git add unity/Assets/Scenes/SnowfieldScene.unity
git commit -m "feat(ui): configure Canvas Scaler + Pixel Perfect"
```

---

### Task 27: HUD 상위 4종 스프라이트 스왑 (ResourcePanel, HealthBar, FloatingHpBars, WeaponSlot)

**Why:** 자주 보이는 것부터.

- [ ] **Step 1: ResourcePanel 교체**

Canvas 하위 ResourcePanel GameObject → 구성: Image (배경) + TMP_Text (수치) + Image (아이콘).
- 배경 Image: Kenney `panel_beige` 9-slice. Image Type: `Sliced`. Color: 네이비 `#1a2332` 살짝 비침.
- 아이콘 Image: 나무(wood) 아이콘
- TMP_Text: m3x6-TMP Font, Font Size 24, Color 크림 `#f4e8d0`

- [ ] **Step 2: HealthBar 교체**

- 배경: 9-slice `panel_brown`
- Fill Image: Image Type `Filled`, Fill Method `Horizontal`, Fill Origin `Left`, Color 붉은 그라디언트 Image 또는 단색 `#c23635`
- 저체력 깜빡임은 기존 `HealthBar.cs` 의 로직 유지 (건드리지 않음)

- [ ] **Step 3: FloatingHpBars 교체**

각 creature 머리 위 World Space Canvas 가 아닌 Screen Space Overlay 로 띄운다면 카메라 변환 로직은 기존 그대로. 스프라이트/색만 교체.
- 배경: 얇은 검정 1px
- Fill: 붉은 막대
- 대미지 숫자: TMP_Text prefab, 0.5초 fade-up 애니 (추후 DOTween 없으면 간단한 Coroutine)

- [ ] **Step 4: WeaponSlot 교체**

- 배경: 정사각 9-slice `panel_square`
- 무기 아이콘 Image
- 쿨다운 오버레이: 별도 Image, Image Type `Filled`, Fill Method `Radial 360`, Fill Origin `Top`, Color 검정 알파 0.6 — 기존 쿨다운 로직이 fillAmount 조절하는지 `WeaponSlot.cs` 에서 확인 (기존 로직 유지)

- [ ] **Step 5: Play 검증**

Expected: 4종 모두 픽셀 폰트 + 9-slice + 아이콘 렌더, 1920×1080 과 1280×720 둘 다 깨지지 않음 (Game 뷰 해상도 변경 테스트).

- [ ] **Step 6: Commit**

```bash
git add unity/Assets/Prefabs/UI/ unity/Assets/Scenes/SnowfieldScene.unity
git commit -m "feat(ui): polish ResourcePanel/HealthBar/FloatingHpBars/WeaponSlot"
```

---

### Task 28: HUD 하위 5종 스프라이트 스왑 (PhaseBanner, VillageArrow, DeathOverlay, BuildButtons, Tutorial)

- [ ] **Step 1: PhaseBanner 교체**

- 배너 배경: 가로 긴 9-slice `panel_beige`
- TMP_Text: m3x6, Font Size 48, Color 크림. 내용 "밤이 찾아옵니다"
- 애니: `PhaseBanner.cs` 의 RectTransform 트윈 로직 유지 (화면 위/아래로 슬라이드)

- [ ] **Step 2: VillageArrow 교체**

- 화살표 Image: Kenney UI `arrow_white` (크기 64×64)
- 거리 TMP_Text: 하단, m3x6 Font Size 16, Color 크림

- [ ] **Step 3: DeathOverlay 교체**

- 검은 풀스크린 Image, Color 검정 알파 0.85
- TMP_Text "YOU DIED": 화면 중앙, m3x6 Font Size 96, Color 붉은 `#c23635`
- Restart 버튼: 9-slice button sprite, TMP_Text "다시 시작" Font Size 24

- [ ] **Step 4: BuildButtons 교체**

Campfire/Barricade 각 버튼:
- 배경 9-slice 버튼
- 아이콘 Image (모닥불/펜스 픽셀 아이콘)
- 비용 TMP_Text "wood: 5"
- 비활성 상태: `Button.interactable=false` 시 Image color alpha 0.4 (기존 로직이 이걸 안 하면 스크립트 한 줄만 추가 — UI 스크립트의 초기화 부분 한정)

- [ ] **Step 5: Tutorial 교체**

- 하단 중앙 9-slice 패널
- TMP_Text, m3x6 Font Size 20, 크림
- 타이핑 효과는 기존 `Tutorial.cs` 의 Coroutine 로직 유지

- [ ] **Step 6: Play 검증 — 전 HUD 요소 1920×1080 / 1280×720 양쪽 체크**

Expected: 모든 배치 유지, 폰트 깨짐 없음, 아이콘 누락 없음.

- [ ] **Step 7: Commit**

```bash
git add unity/Assets/Prefabs/UI/ unity/Assets/Scenes/SnowfieldScene.unity
git commit -m "feat(ui): polish PhaseBanner/VillageArrow/DeathOverlay/BuildButtons/Tutorial"
```

---

## Phase 9: Village 씬 복제

### Task 29: Village 씬에 동일 설정 적용

**Files:**
- Modify: `unity/Assets/Scenes/VillageScene.unity`

- [ ] **Step 1: Village 씬 열기**

File → Open Scene → `VillageScene.unity`.

- [ ] **Step 2: SnowfieldScene 의 Global Light + Volume + Canvas 구성 복사**

SnowfieldScene 에서 다음 GameObject 를 선택해 Copy → Village 에 Paste:
- `GlobalLight` (Global Light 2D)
- `Global Volume` (Volume Profile 참조)
- `Canvas` (HUD 프리팹 전체)

DayNightLightBinder GameObject 도 복사 (Village 에도 낮/밤 작동).

- [ ] **Step 3: Village 의 Tilemap 추가**

Hierarchy → 2D Object → Tilemap → Rectangular. Tilemap Renderer Sorting Layer: `Ground`.

Village 는 마을 내부라 지면이 다를 수도 있음. 간단히 `SnowRuleTile` 재사용하거나, `Assets/Art/Tiles/` 에 `DirtRuleTile` 를 추가로 만들어 사용 (이번엔 SnowRuleTile 재사용으로 통일).

- [ ] **Step 4: ChunkManager 는 Village 에서 비활성** (Village 는 고정 맵)

Village 씬에 ChunkManager 가 있다면 비활성화.

- [ ] **Step 5: Play 검증**

Expected:
- Village 씬도 LPC 스프라이트 + 타일맵 + 라이팅 + HUD 전부 Snowfield 와 동일 수준
- 좀비 웨이브 렌더 정상
- Campfire 주변 밤에 환함

- [ ] **Step 6: Commit**

```bash
git add unity/Assets/Scenes/VillageScene.unity
git commit -m "feat(unity): replicate art upgrade to VillageScene"
```

---

## Phase 10: 최종 검증

### Task 30: Snowfield + Village 전체 검증 체크리스트

**Why:** Spec 의 "검증 게이트" 8개 항목을 실측 기록.

- [ ] **Step 1: Snowfield 슬라이스 검증 (spec 기준)**

Snowfield Play 후 다음 8개 항목 각각 확인:

1. [ ] Console 에러 0개
2. [ ] 타일맵 설원 바닥 렌더, 격자 티 없음
3. [ ] 플레이어/좀비/사슴/나무 LPC 스프라이트, bottom-center pivot, Y축 정렬 작동 (나무 뒤로 걸어들어가기 가능)
4. [ ] 낮 → 밤 전환 시 Global Light 2D 가 어두워지고 모닥불 Point Light 가 주변을 밝힘
5. [ ] 눈 입자가 화면에 내림
6. [ ] 타격/채집 VFX 확인 (수동 Instantiate 또는 실제 게임플레이)
7. [ ] HUD 9종 모두 픽셀 폰트 + 9-slice + 아이콘으로 교체, 1920×1080 / 1280×720 에서 깨지지 않음
8. [ ] README 에 LPC attribution 존재

- [ ] **Step 2: Village 슬라이스 검증 (위 1~7 동일)**

- [ ] **Step 3: 검증 결과 기록**

결과를 `docs/superpowers/specs/2026-04-23-unity-art-upgrade-design.md` 의 "검증 게이트" 섹션 아래에 체크리스트로 추가 (모든 항목 PASS 여부).

- [ ] **Step 4: Final commit**

```bash
git add docs/superpowers/specs/2026-04-23-unity-art-upgrade-design.md
git commit -m "docs(spec): record final art upgrade verification results"
```

---

## 참고: 태스크 간 의존성 그래프

```
Task 1 (Unity 부트스트랩)
  └─ Task 2 (IL6 asmdef)
       ├─ Task 3 (URP 설치)
       │    └─ Task 4 (Renderer+Pipeline)
       │         └─ Task 5 (머티리얼 업그레이드)
       │              └─ Task 6 (VisionMask 호환 결정)
       │                   └─ Task 22 (플레이어 시야 라이트, A/B 분기)
       │
       ├─ Task 7-10 (에셋 임포트 + Credits) — 병렬 가능
       │
       └─ Task 11 (Tests asmdef)
            ├─ Task 12 (YSort)
            ├─ Task 13 (LightFlicker)
            └─ Task 14 (DayNightLightCalc)
                 └─ Task 15 (DayNightLightBinder)
                      └─ Task 20 (Global Light 2D 연결)

Task 16 (Sorting Layer) → Task 17 (prefab 스프라이트 스왑, YSort 부착 — Task 12 필요)
Task 18 (Rule Tile) → Task 19 (Tilemap + ChunkManager 연동)
Task 20 (Global Light) → Task 21 (Campfire Flicker — LightFlicker 필요) → Task 22 (Vision Light)
Task 23 (Volume) — Task 20 이후
Task 24 (Snow) / Task 25 (Hit/Chop/Death) — 독립
Task 26 (Canvas) → Task 27 (HUD 상위 4) → Task 28 (HUD 하위 5)
Task 29 (Village 복제) — 나머지 완료 후
Task 30 (최종 검증)
```
