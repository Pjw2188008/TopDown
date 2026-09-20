# 몬스터 스크립트

- Melee: 근접 몬스터. Attachable에 본체, Internal에 EnemyController 분할 구현.
- Ranged: 원거리 몬스터와 스포너. Attachable에 본체, Internal에 이동·공격·탄환·충돌 구현과 편집기 도구.
- Shared: 적 공통 순찰과 경직 컴포넌트.

EnemyController / MeleeEnemy / RangedEnemy는 각각 체력과 공격을 소유하므로 같은 적에 중복 부착하지 않습니다.
기존 테스트용 발사 스크립트는 제거된 상태를 유지합니다. 새 원거리 적은 RangedEnemySpawner 프리팹으로 배치합니다.
