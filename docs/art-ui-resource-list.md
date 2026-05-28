# 6IL Image and UI Resource List

Last updated: 2026-05-28

이 문서는 실제 제작/생성 대상으로 우선 필요한 이미지 리소스와 UI 리소스를 작업 단위로 정리한 목록입니다. 전체 마스터 목록은 `docs/resource-inventory.html`에 있고, 이 문서는 이미지 생성 및 아트 작업 발주용 축약본입니다.

## 1. 이미지 리소스 우선순위

| ID | 우선순위 | 리소스 | 파일명 제안 | 필요 구성 |
|---|---:|---|---|---|
| IMG-001 | P0 | 플레이어 생존자 | `public/assets/characters/player_survivor.png` | idle, walk 4방향, attack, hit, death |
| IMG-002 | P0 | 기본 좀비 | `public/assets/characters/zombie_basic.png` | walk, attack, hit, death |
| IMG-003 | P0 | 사슴 | `public/assets/characters/animal_deer.png` | idle, run, hit, death, carcass |
| IMG-004 | P0 | 모닥불 | `public/assets/buildings/campfire_levels.png` | L1-L5, 켜짐, 약해짐, 꺼짐 |
| IMG-005 | P0 | 나무 바리케이드/울타리 | `public/assets/buildings/barricade_wood.png` | 정상, 손상 1, 손상 2, 파괴 |
| IMG-006 | P0 | 설원 타일셋 | `public/assets/tilesets/snowfield_base.png` | 눈 평지, 음영, 경계, 발자국 |
| IMG-007 | P0 | 마을 지면 타일셋 | `public/assets/tilesets/village_ground.png` | 다져진 눈, 길, 목재 바닥, 경계 |
| IMG-008 | P1 | 오두막/숙소 | `public/assets/buildings/lodging_levels.png` | L1-L5, 손상, 파괴 |
| IMG-009 | P1 | 창고/상자/장작더미 | `public/assets/props/village_decor_set.png` | 상자, 장작, 깃발, 울타리 소품 |
| IMG-010 | P1 | 나무/바위/철 광맥 | `public/assets/props/resource_nodes.png` | 기본, 채집 중, 고갈 |
| IMG-011 | P1 | 전투 이펙트 | `public/assets/fx/combat_fx.png` | 베기, 피격, 사망, 얼음 파편 |
| IMG-012 | P1 | 모닥불/시야 이펙트 | `public/assets/fx/campfire_vision_fx.png` | 불꽃, 광원, 밤 시야 마스크 |
| IMG-013 | P2 | 동료 주민 10종 | `public/assets/characters/companions.png` | 아이, 청년, 장년, 노인 남/여 |
| IMG-014 | P2 | 좀비 변종 2종 | `public/assets/characters/zombie_variants.png` | 빠른 좀비, 중장갑 좀비 |
| IMG-015 | P2 | 설원 동물 3종 | `public/assets/characters/snow_animals.png` | 토끼, 늑대, 거대 사슴 |
| IMG-016 | P3 | 보스 5종 | `public/assets/characters/bosses.png` | 서리 좀비, 겨울기사, 거인, 리치, 눈보라 네임드 |

## 2. UI 리소스 우선순위

| ID | 우선순위 | 리소스 | 파일명 제안 | 필요 구성 |
|---|---:|---|---|---|
| UI-001 | P0 | 자원 아이콘 | `public/assets/ui/icons/resources.png` | 나무, 돌, 철, 고기, 식량, 눈꽃 결정 |
| UI-002 | P0 | 플레이어 HUD | `public/assets/ui/hud_player.png` | HP, 경험치, 쿨다운, 낮밤 타이머 |
| UI-003 | P1 | 경고/알림 패널 | `public/assets/ui/notification_panels.png` | 밤 시작, 웨이브, 눈보라, 건물 위험 |
| UI-004 | P1 | 건설 배치 UI | `public/assets/ui/build_menu.png` | 건물 카드, 유효/불가 타일, 회전, 수리 |
| UI-005 | P1 | 무기 아이콘 | `public/assets/ui/icons/weapons_basic.png` | 롱소드, 창, 활, 서리 지팡이, 해머, 단검 |
| UI-006 | P1 | 장비/룬 아이콘 | `public/assets/ui/icons/equipment_runes.png` | 방어구, 부적, 룬, 진화 무기 |
| UI-007 | P2 | 미니맵 아이콘 | `public/assets/ui/icons/minimap.png` | 플레이어, 마을, 적, 동료, 자원, 보스 |
| UI-008 | P2 | 동료 초상화 | `public/assets/ui/portraits/companions.png` | 동료 타입/성별별 10종 |
| UI-009 | P2 | 동료 상태 아이콘 | `public/assets/ui/icons/companion_status.png` | HP 낮음, 부상, 후퇴, 배치, 사망 위험 |
| UI-010 | P3 | 메타 진행 UI | `public/assets/ui/meta_progression.png` | 기억의 돌, 명예의 전당, 해금, 점수 |
| UI-011 | P3 | 메뉴/게임오버 UI | `public/assets/ui/menu_screens.png` | 타이틀, 일시정지, 설정, 게임오버 |
| UI-012 | P4 | 앱 아이콘/브랜딩 | `public/assets/branding/app_icon.png` | 1024px 원본, ico 변환용 |

## 3. 생성된 참고 이미지

아래 파일은 실제 게임에 바로 연결하기 전, 아트 방향과 스타일을 맞추기 위한 생성 시트입니다.

| 파일 | 용도 |
|---|---|
| `docs/assets/6il-image-resource-sheet.png` | 캐릭터, 건물, 환경, 이펙트 제작 방향 참고 |
| `docs/assets/6il-ui-resource-sheet.png` | HUD, 아이콘, 패널, 무기/룬 UI 제작 방향 참고 |

## 4. 스타일 기준

- 탑다운 2D 픽셀 아트.
- 차가운 청록/회백색 설원 팔레트에 모닥불 주황색을 강조색으로 사용.
- 모든 캐릭터와 오브젝트는 어두운 밤 배경에서도 외곽 실루엣이 읽혀야 함.
- UI는 작은 크기에서도 식별 가능한 단순한 실루엣을 우선.
- 최종 게임 적용용 이미지는 투명 PNG 또는 atlas로 분리 필요.
