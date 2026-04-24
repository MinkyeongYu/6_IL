# 6_IL 구현 상태 및 기획서 대비 감사

- **작성일**: 2026-04-25
- **관련 문서**:
  - [원본 게임 기획서 (2026-04-21)](./2026-04-21-6il-game-design.md) — 전체 게임 디자인
  - [Unity 아트 업그레이드 스펙 (2026-04-23)](./2026-04-23-unity-art-upgrade-design.md) — 아트 폴리시 스펙 (부분 실행)
  - [아트 업그레이드 구현 플랜](../plans/2026-04-23-unity-art-upgrade.md) — 30 태스크 플랜

---

## 요약

현재 Unity 2022.3.62f3 포트가 **"최소 플레이 가능" 상태** (WASD 이동 + 채집 + OnGUI HUD). 하지만:

- **원본 기획서 (§1~9)** 대비 **게임플레이 구현 ~15%** — 전투/동료/마을방어/룬/눈보라 등 거의 미구현
- **아트 업그레이드 스펙** 대비 **시각 전달 0%** — URP 라이팅/LPC 에셋/픽셀폰트 모두 미착수. 준비 스크립트(YSort, LightFlicker, DayNightLightCalc, DayNightLightBinder)는 완료되었으나 씬에 미적용
- **스펙과 현실 구조 불일치** — 스펙은 `unity/` 서브폴더 전제. 현재는 리포 루트로 평탄화 (EPERM 이슈로 인한 긴급 조정)

전체 상황은 **"인프라 삽질로 시간 많이 쓴 뒤 최소 MVP 도달, 아트/게임플레이 본격 시작 전"**.

---

## 1. 원본 기획서 대비 구현도

### §1. 비전 (설원 뱀서+타워디펜스)
- N/A (개념 기술)

### §2. 기술 스택
| 항목 | 스펙 | 현실 |
|---|---|---|
| 1차 플랫폼 | **Phaser 3 TS** | `src/` 에 Phaser 코드 존재 (플레이 여부 미확인) |
| 2차 이식 | Unity (장기) | **현재 이 Unity 포트 작업이 주력이 되어버림** |

**주의**: 원본 기획서는 Phaser 를 1차로 보고 Unity 이식은 **장기 로드맵** 이었음. 지난 며칠의 작업 흐름(아트 업그레이드 → Unity 포트 → EPERM 디버깅 → 플랫 구조)이 기획과 다름. 논의 필요.

### §3. 게임 루프 (낮/밤 사이클)
| 요소 | 스펙 | 구현 |
|---|---|---|
| 낮 Snowfield 탐험 | 동료 영입, 자원, 거대동물 | ⚠ 부분 — WASD 이동, 나무/사슴 채집만. 동료/거대동물 미구현 |
| 밤 Village 방어 | 좀비 웨이브 + 바리게이트 | ❌ Village 씬 자체 없음 |
| 초대형 눈보라 | 특수 이벤트 | ❌ 미구현 |
| DayNightController | 낮→저녁→밤→새벽 상태머신 | ✅ C# 로직 완료. 단, 씬에서 구동 안 함 (SnowfieldController 에서 호출은 하나 검증 안 됨) |

### §4. 전투 & 장비
| 요소 | 스펙 | 구현 |
|---|---|---|
| 무기 (영속/런타임) | 6종 기본 + 룬/축복 | ❌ WeaponDefinition ScriptableObject 정의만 존재, 에셋 생성 안 됨 |
| 룬/축복 | 런타임 빌드 | ❌ 미구현 |
| 진화 무기 | 특정 조건 합체 | ❌ 미구현 |
| 동료 AI | 탱커/딜러/서포터 스탠스 | ❌ 미구현 (DeerAi 는 동물용) |
| 대미지 계산 | 결정적 (시드) | ✅ `DamageCalc.cs` 존재, 호출 안 됨 |
| PlayerAttackController | 무기 트리거 | ✅ 스크립트 존재, 씬에 미부착 |

### §5. 마을 시스템 & 자원
| 요소 | 스펙 | 구현 |
|---|---|---|
| 자원 6종 | Wood/Meat/Food/Frostbloom/Stone/Iron | ⚠ 부분 — 4종(Wood/Meat/Food/Frostbloom)만 `ResourceKind` 에 정의. Stone/Iron 없음 |
| 건물 7종 + 바리게이트 | 모닥불/바리게이트/종탑/밭 등 | ⚠ `Building.cs` + `BuildingKind` 만 존재. 실제 빌딩 기능은 SimpleHud 의 "모닥불 버튼"(주황 사각형 스폰) 만 |
| 건설 UI | `BuildButtons` | ⚠ `BuildButton.cs` 존재하나 Village 씬 필요. SimpleHud 로 임시 대체 |
| 튜토리얼 | 인게임 컨텍스트 기반 | ❌ 미구현 |

### §6. 동료 시스템
- 영입/상태/스탠스/사기/사망 — **전부 미구현**. 관련 C# 파일 자체가 없음

### §7. 시야/그리드/바리게이트
| 요소 | 스펙 | 구현 |
|---|---|---|
| 그리드 | `VillageGrid.cs` | ✅ 로직 완료, Village 씬 필요 |
| 시야 (FoW) | `VisionMask.cs` | ⚠ 스크립트 존재, 씬 미부착 |
| 바리게이트 | 그리드 배치 | ⚠ `PlacementController.cs` 존재, Village 씬 필요 |
| 경로 탐색 | 좀비 AI | ⚠ `Zombie.cs` 단순 추적만 |
| HUD | 체력바/자원/무기슬롯/... (9종) | ⚠ OnGUI `SimpleHud` 단일 박스로 임시 |

### §8. 아트/오디오/메타
| 요소 | 스펙 | 구현 |
|---|---|---|
| 아트 | LPC 픽셀 32px + URP Light 2D | ❌ 런타임 생성 단색 원 (`ColorFallback`) |
| 오디오 | BGM + SFX | ❌ 미구현 |
| 메타 프로그레션 | 해금 시스템 | ❌ 미구현 |
| 리더보드 | 마을 기록 | ❌ 미구현 |
| 업적 | 20~30종 | ❌ 미구현 |

### §9. 마일스톤
- 원본 스펙은 **Phase 1 수직 슬라이스 → Phase 9 Steam 출시** 까지 9단계
- 현재 위치: **Phase 0 (프로토타입) ~ Phase 1 (수직 슬라이스) 경계**. Unity 에서 "플레이 버튼 누르면 움직이는 원" 상태

---

## 2. 아트 업그레이드 스펙 대비 구현도

스펙의 6개 작업 영역별:

| 영역 | 스펙 | 구현 | 비고 |
|---|---|---|---|
| **1. URP 2D 전환** | 패키지 + Renderer + PipelineAsset 할당 | ⚠ 패키지 설치됨 (`14.0.12`), **Renderer/PipelineAsset 미생성** | `UniversalRenderPipelineGlobalSettings.asset` 은 자동 생성되어 있으나 실제 Pipeline Asset 없음 → URP 2D 라이팅 기능 사용 불가 |
| **2. 에셋 임포트** | LPC + Kenney UI RPG + Kenney Particle + m3x6 | ❌ 전혀 안 함 | README 크레딧만 추가됨 |
| **3. 환경 스왑** | 타일맵 + LPC 프롭 + Y-sort + 눈입자 | ❌ 타일맵 없음. `YSort.cs` 존재하나 프롭에 미부착 | 씬에 인라인 나무/사슴 — `ColorFallback` 런타임 생성 원 |
| **4. 라이팅/VFX** | Global Light 2D + 모닥불 Flicker + Volume(Bloom/Vignette) + 파티클 | ❌ 전혀 없음 | `LightFlicker.cs`, `DayNightLightBinder.cs`, `DayNightLightCalc.cs` 존재하지만 씬에 미부착 |
| **5. UI/HUD 폴리시 (9종)** | Canvas + TMP + Kenney 9-slice + 아이콘 + 픽셀폰트 | ❌ | `SimpleHud` (OnGUI IMGUI 한 개) 로 임시. 9개 개별 요소 없음 |
| **6. Village 복제** | Snowfield → Village 동일 레시피 | ❌ Village 씬 자체 없음 |

**시각적 전달도: ~0%.** 스펙이 약속한 "Core Keeper 스타일 픽셀 top-down + 조명" 은 **하나도 구현 안 됨**. 준비(스크립트/테스트) 는 80% 돼 있으나 활용 미이행.

---

## 3. 플랜(30 태스크) 대비 완료

[구현 플랜 파일](../plans/2026-04-23-unity-art-upgrade.md) 기준:

| Phase | 태스크 수 | 완료 | 현황 |
|---|---|---|---|
| 0. 부트스트랩 (Task 1~2) | 2 | ✅ 2 | |
| 1. URP 2D 파이프라인 (Task 3~6) | 4 | ⚠ 1 | 패키지만 설치 (Task 3). Renderer/PipelineAsset 미생성 (Task 4~6) |
| 2. 에셋 임포트 (Task 7~10) | 4 | ⚠ 1 | README 크레딧 (Task 10) 만. LPC/Kenney/m3x6 (Task 7~9) 미이행 |
| 3. C# 헬퍼 TDD (Task 11~15) | 5 | ✅ 5 | 전부 코드 작성. 실제 테스트 실행은 Unity Test Runner 세팅 이슈로 미검증 |
| 4. Sorting Layer + Prefab (Task 16~17) | 2 | ❌ 0 | |
| 5. 타일맵 환경 (Task 18~19) | 2 | ⚠ 1 | ChunkManager.cs 타일맵 메서드 추가 (Task 19 의 코드 부분) |
| 6. 라이팅 (Task 20~23) | 4 | ❌ 0 | |
| 7. VFX (Task 24~25) | 2 | ❌ 0 | |
| 8. UI/HUD 폴리시 (Task 26~28) | 3 | ❌ 0 | SimpleHud 로 우회 |
| 9. Village 복제 (Task 29) | 1 | ❌ 0 | |
| 10. 최종 검증 (Task 30) | 1 | ❌ 0 | |

**완료: 9 / 30 태스크 (30%)**. 미완료 대부분은 Unity Editor 수동 작업 (사용자 직접 필요) — 에셋 임포트, 씬 편집, Volume 생성 등.

---

## 4. 계획 외 구현 (스펙에 없지만 현실에 있는 것)

| 항목 | 목적 | 스펙에 있어야 할까? |
|---|---|---|
| `ColorFallback.cs` | 런타임 생성 단색 원/사각형 스프라이트 — 외부 에셋 없이 플레이 가능 | 임시방편. LPC 임포트 후 제거 가능 |
| `DebugMove.cs` | BalanceConfig 바이패스 WASD 이동 | 디버그용. PlayerController 완성 시 제거 |
| `SimpleHud.cs` (OnGUI) | 자원/HP/위치/채집진행률 + Build 버튼 | 임시방편. 정식 HUD 완성 시 제거 |
| 리포 루트 평탄화 | `unity/` 서브폴더 제거 | EPERM 대응 과정에서 구조 변경. 스펙/플랜 문서들이 `unity/` 경로 전제라 문구 stale |

---

## 5. 권장 다음 액션 (우선순위)

### A. "지금 바로" — 플레이 경험 확정
1. **입력 동작 검증** — Unity Game 뷰 클릭해서 포커스 주고 WASD 로 플레이어 (주황 원) 이동되는지 확인. HUD 에서 `Pos` 숫자 변하는지. → 아직 안 되면 `DebugMove.LogInput = true` 로 Console 확인
2. **채집 검증** — 나무 옆에서 E 키 → HUD 의 Wood 증가 → Build 버튼 → 모닥불 스폰
3. 안 되면 screenshot/Console 공유 → 원인별 수정

### B. "다음 주" — 게임플레이 확장 방향 정하기
원본 기획서의 거대한 범위(§1~9) 대비 현재 위치 고려해서 다음 중 하나 결정:
1. **A안: 기획서 스코프 다운** — 뱀서바이벌 핵심(웨이브 + 채집 + 자동공격)만 최소 MVP 집중
2. **B안: 아트 업그레이드 스펙 계속** — 나무/사슴/플레이어에 LPC 스프라이트 바꾸고 URP 라이팅 넣어서 "보기 좋은 프로토타입" 우선
3. **C안: Phaser 버전 복귀** — 원본 기획대로 Phaser 주력, Unity 는 일단 멈춤. `src/` 상태 점검부터

### C. "스펙 관리"
- **아트 업그레이드 스펙 (2026-04-23)** 은 `unity/` 경로 전제로 작성된 시점 스냅샷 — 이후 평탄화 등으로 경로 stale. 재작업 시 "아트 업그레이드 스펙 v2" 새로 쓰거나 대대적 개정 필요
- **원본 기획서 (2026-04-21)** 은 Phaser 1차/Unity 2차 전제 — 현실과 어긋남. 방향 재정의 필요

---

## 6. Known Divergences (구조/명령 측면)

| 영역 | 스펙/플랜 | 현실 |
|---|---|---|
| Unity 프로젝트 경로 | `unity/Assets`, `unity/Packages`, `unity/ProjectSettings` | 리포 루트 (`Assets/`, `Packages/`, `ProjectSettings/`) |
| Unity 버전 | 2022 LTS (2022.3.40f1) | 2022.3.62f3 |
| 렌더 파이프라인 | URP 2D Renderer 활성 | URP 패키지 설치만, Pipeline Asset 미생성 → 사실상 Built-in |
| 이동 구현 | `PlayerController` (BalanceConfig.Instance.PlayerMoveSpeed) | `DebugMove` (하드코딩 5 u/s) 가 velocity 덮어씀 |
| HUD 구현 | Canvas + TMP + Kenney UI prefab 9개 | OnGUI `SimpleHud` 한 개 |
| 스프라이트 | LPC 픽셀아트 | `ColorFallback` 런타임 생성 단색 도형 |

---

## 결론

- **인프라/바인딩은 도달** — Unity 열림, 씬 로드, 스크립트 컴파일, Play 진입. 이 기반을 얻는 데만 수 시간 소요 (EPERM/버전 미스매치/클라우드 동기화 등).
- **시각적 완성도는 0** — 아트 업그레이드 스펙이 약속한 어느 결과물도 아직 없음.
- **기능적 완성도는 15%** — 원본 기획서의 1/7 가량. 최소 MVP 조차 미도달.
- **다음 단계 우선순위 결정 필요** — 스펙 축소, 아트 진행, Phaser 복귀 중 선택.
