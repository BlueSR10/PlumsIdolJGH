# 은호 ExtendingFire

은호_ExtendingFire.prefab은 S3_Fire 원본의 Variant입니다. 오븐(S3) 맵의 내 Map 아래에 배치합니다. 기본 Y, Z=0, Scale=1을 유지합니다. 팔레트 원본 및 StageRig 수정, Overrides > Apply All은 금지합니다.

- 초기 폭: Art Piece > Width (원본처럼 그림 한 장 폭의 배수).
- Trigger Distance: 플레이어와 초기 불꽃 왼쪽 끝의 가로 거리. 기본 2유닛 이하에서 시작.
- Extra Width: 오른쪽에 추가되는 길이. 기본 3유닛 (초기 폭 1이면 최종 폭 4).
- Extend Duration: 기본 0.5초 동안 확장.
- 불꽃 두 장을 나란히 표시하고 각 그림을 Sliced 방식으로 가로로 늘립니다. 확장 전후 개수는 항상 두 장이며 Trigger 충돌 범위도 함께 넓힙니다. Transform Scale, 왼쪽 끝과 높이는 유지됩니다.
- 한 번 확장하면 유지되며 재시작하면 위치와 그림/충돌 폭, 접근 상태가 모두 초기화됩니다.
- 사망/클리어 중 확장은 정지합니다. 시간은 StageTime 기준입니다.
- 기존 Hazard 판정을 사용하며 S3_Fire처럼 거대화로 파괴 가능합니다.

Play를 끄고 설정을 저장하세요. 실제로 점프해 통과할 수 있는지 직접 클리어하고, 다른 장애물과 확장 범위가 겹치지 않는지 확인하세요. 스크립트/프리팹과 .meta를 함께 버전 관리합니다. 기본 수치는 초안입니다.
