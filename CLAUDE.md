# InisLand

## 프로젝트
- 이름: InisLand
- 버전: 0.2.0
- 스택: Phaser 3 + Vite + Electron + TypeScript + bitecs (ECS)
- 빌드: tsc --noEmit && npx vite build
- 패키징: npx electron-builder --win --x64
- 테스트: npx vitest run

## 구조
- src/main.ts: 엔트리포인트
- src/ecs/: ECS 컴포넌트/시스템 (bitecs)
- src/scenes/: 게임 씬
- src/gameplay/: 게임플레이 로직
- src/input/: 입력 처리
- src/events/: 이벤트 시스템
- src/ui/: UI
- src/config/: 설정
- src/util/: 유틸리티
