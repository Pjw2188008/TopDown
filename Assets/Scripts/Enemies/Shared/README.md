# 몬스터 공통 컴포넌트

## Attachable
- MovingEnemy.cs: 왕복 순찰과 가속 배율. 단순 이동 테스트 대상이나 MeleeEnemy와 함께 사용합니다. 이미 이동하는 EnemyController/RangedEnemy와 중복 부착하지 않습니다.
- EnemyStagger.cs: 패링 경직 시간과 IsStunned 상태. EnemyController/MeleeEnemy/RangedEnemy가 RequireComponent로 자동 준비합니다. 각 이동/공격 코드가 경직을 확인합니다.

현재 독립 Internal 구현은 없습니다. 빈 폴더를 만들기 위해 코드를 억지로 분리하지 않습니다.
