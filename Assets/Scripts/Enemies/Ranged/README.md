# 원거리 몬스터

## Attachable — 씬/프리팹 컴포넌트
- RangedEnemy.cs: 적 루트. Inspector 설정, 컴포넌트 준비, 실행 순서, 피해/사망.
- RangedEnemySpawner.cs: 감지 지점. 플레이어 검색 → 범위 진입 판정 → 생성/재생성.

기존 Assets/RangedEnemy의 프리팹을 사용합니다. 스크립트를 다시 붙일 필요가 없습니다.

## Internal — 직접 부착하지 않음
- RangedEnemy.Movement.cs: 대상 검색, 접근/정지, 축별 벽 충돌 이동, 좌우 반전.
- RangedEnemy.Combat.cs: 공격 시작, 클립 길이, 발사 시점, 쿨타임, 경직 취소.
- RangedEnemy.Projectiles.cs: 발사 순간 조준, 투사체 Sprite/Collider 생성.
- RangedEnemy.Collisions.cs: Kinematic Rigidbody 설정과 플레이어 몸 충돌 제외/복구.
- ReflectProjectile.cs: 생성된 탄환의 이동·수명·충돌·피해·패링·반사. 발사 코드가 자동 부착합니다.
- Editor/RangedEnemySetup.cs: 누락된 프리팹·Animator·클립 생성 메뉴. Unity Editor 전용입니다.

RangedEnemy.*는 RangedEnemy 하나를 분할한 partial 코드입니다. 독립 컴포넌트가 아니며 새로 붙이지 않습니다.
직렬화 필드와 .meta GUID를 유지하여 기존 Inspector 참조·수치·스프라이트를 보존합니다.

## 유지하는 동작
- 감지 범위 진입 시 생성, 이동 후 공격 범위에서 주기적 공격.
- 오른쪽 이동 스프라이트 사용, 왼쪽은 flipX.
- 공격 클립 진행률에 1발 발사하며 실제 발사 순간의 플레이어 위치를 조준.
- 발사 후 비유도 직진, 패링/반사에 따른 방향 변경은 유지.
- 기본 투사체 속도 10, 가속 오류는 이동 속도에 적용.
- 애니메이션용 Sprite 드롭은 SpriteRenderer만 사용하며 projectileSprite는 NotKeyable 유지.
