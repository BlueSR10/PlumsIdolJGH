# 은호 DisappearingFire

은호_DisappearingFire.prefab은 S3_Fire 원본의 Variant입니다. 오븐(S3) 맵의 Hierarchy에서 내 Map 아래로 끌어 넣습니다. X만 옮기고 기본 Y=-0.86, Z=0, Scale=1을 유지합니다. 폭은 Art Piece > Width로 조절합니다.

- Trigger Distance: Fire 중심과 플레이어의 가로 거리. 기본 3유닛 이하에서 즉시 사라집니다.
- 그림과 Trigger 충돌이 함께 꺼지고, 플레이어가 멀어져도 재시작 전까지 사라진 상태를 유지합니다.
- 재시작하면 그림과 충돌이 복구되고 다시 작동합니다.
- Running 중에만 동작하고 사망/클리어 중에는 사라지지 않습니다.
- 원본 Hazard와 거대화 파괴 가능 규칙을 유지합니다. 접근 소멸은 파괴 이펙트를 발생시키지 않습니다.

원본 팔레트, StageRig 및 다른 사람의 맵을 수정하지 않습니다. Overrides > Apply All 금지. Play를 끄고 설정과 저장을 진행하고 .meta를 함께 버전 관리합니다. 폭이 넓으면 중심에서 3유닛에 도달하기 전에 부딪힐 수 있으므로 실제 클리어 가능한지 직접 확인하세요. 다른 Fire 기믹들과 별도 프리팹입니다.
