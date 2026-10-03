# 은호 FallingFireLong

은호_FallingFireLong.prefab을 오븐(S3) 맵의 Hierarchy에서 내 Map 아래로 배치합니다. X만 옮기고 기본 Y, Z=0, Scale=1과 회전 0을 유지합니다. 루트는 불꽃 그림의 밑부분에 있는 회전축이므로 S3_FireLong 원본 중심 높이와 루트 Y가 다릅니다. 자식 FireLong의 높이를 따로 바꾸지 않습니다.

- Trigger Distance: 회전축과 플레이어의 가로 거리. 기본 2유닛 이하에서 작동합니다.
- Fall Duration: 기본 0.4초 동안 밑부분을 축으로 왼쪽 90도 회전합니다.
- 넘어지면 유지하고 재시작하면 처음 각도로 돌아와 다시 작동합니다.
- 그림과 Trigger 충돌 범위가 함께 회전합니다. 원본 FireLong의 파괴 불가 Hazard 판정을 유지합니다.
- Running 중에만 StageTime 기준으로 회전하고 사망/클리어 중에는 멈춥니다.

원본 팔레트와 StageRig를 수정하지 않고 Overrides > Apply All을 누르지 않습니다. Play를 끄고 설정과 저장을 진행하고 .meta를 함께 버전 관리합니다. 왼쪽으로 넘어지면서 플레이어 쪽을 덮치므로 점프/감속/비행으로 실제 클리어가 가능한지 직접 확인합니다. 기본 수치는 초안입니다. 기존 상승 FireLong 및 Fire 변신 기믹과 별도입니다.
