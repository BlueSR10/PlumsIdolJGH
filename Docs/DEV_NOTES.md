# DEV NOTES

> 프로그래머 관리. 기술 규약과 임시 값만 짧게 적는다. 기획 내용은 `GDD.md`.

## 기술 규약
- Full HD(1920×1080), URP 2D, Pixel Perfect Camera
- 스프라이트: 캔버스 64×64, PPU 64, Filter Point, Compression None
- 기준 해상도: 480×270 (4배, 임시. 캐릭터 몸 높이 확인 후 확정)
- 월드 구조: 플레이어는 화면 고정, `Stage`(kinematic)가 왼쪽으로 스크롤
- 입력: `RunnerInput`에서 코드로 정의 — 점프 Space/↑/W, 슬라이드 S/↓, 감속 LeftShift
- `Assets/Placeholders/`: 임시 사각형 스프라이트. 실제 아트 연결 후 삭제

## 임시 값 (기획 확정 시 삭제)
- 속도 조절: 기준 속도가 계속 증가(4 + 0.1/s²) + 감속 키, 키를 떼면 기준 속도로 복귀 (GDD 15장 미정)
- 공중에서 속도 조절: 허용 (GDD 15장 미정)
- 점프 높이 1.8, 슬라이드 시 충돌 높이 50%
