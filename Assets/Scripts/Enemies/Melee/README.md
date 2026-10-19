# 근접 몬스터
`Attachable/MeleeEnemy.cs` 하나를 사용합니다. 기존 EnemyController / TriangleMeleeMonster / MovingEnemy를 함께 붙이지 않습니다.

몬스터 프리팹을 Hierarchy에 직접 배치하고 Aggro Mode, Chase Area Offset / Size를 설정합니다. 타격 위치는 Hit Offset, 크기는 Hit Size로 조절하며 왼쪽에서는 X 위치가 반전됩니다. Attack 상태의 Hit Normalized Time에서 한 번 타격합니다.

기존 Enemy, MeleeMonster, mon_1 프리팹은 이 AI로 변경되었습니다. 이미지와 Animator 연결은 유지하며 원래 개별 돌진/충격파 공격은 기본 근접 타격으로 통합했습니다. 보스 공격은 변경하지 않았습니다.

전체 설정은 [몬스터 안내](../README.md)를 참고하세요.
