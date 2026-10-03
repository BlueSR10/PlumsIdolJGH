# 은호 FireToLong

은호_FireToLong.prefab을 오븐(S3) 맵의 Hierarchy에서 내 Map 아래로 배치합니다. 기본 Y=-0.86, Z=0, Scale=1을 유지하고 X만 옮깁니다.

- Trigger Distance: 초기 Fire 중심과 플레이어의 가로 거리. 기본 2유닛 이하에서 즉시 FireLong으로 변합니다.
- FireLong 상태는 재시작 전까지 유지됩니다. 사망/클리어 중에는 변신하지 않습니다.
- 변신 시 Fire의 그림/충돌은 꺼지고 FireLong의 그림/충돌이 켜집니다.
- Fire는 거대화로 파괴 가능하고 FireLong은 파괴 불가입니다. 이미 부서진 Fire는 변신하지 않습니다.
- 재시작하면 Fire 상태와 충돌 판정이 복구되고 다시 변신 가능합니다.
- 두 원본을 nested prefab으로 사용합니다. 각 기본 높이와 Scale을 보존하고 공통 Hazard/Trigger 판정을 사용합니다.

원본 팔레트, StageRig 및 다른 사람의 맵은 수정하지 않습니다. Overrides > Apply All 금지. Play를 끄고 설정/저장하고 .meta를 함께 버전 관리합니다. 변신하는 높이와 충돌 범위를 고려해 직접 클리어 가능한 배치인지 확인하세요. 기존 확장 Fire 기믹과 별도 프리팹입니다.
