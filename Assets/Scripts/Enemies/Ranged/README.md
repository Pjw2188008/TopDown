# 원거리 몬스터

## Attachable — 씬/프리팹 컴포넌트
- RangedEnemy.cs: 적 루트. Inspector 설정, 컴포넌트 준비, 실행 순서, 피해/사망.
- RangedEnemySpawner.cs: 감지 지점. 플레이어 검색 → 사각 범위 진입 판정 → 생성/재생성. CombatAreaLock이 연결되면 `SpawnForCombatArea`로 기존 적을 재사용하거나 즉시 생성할 수 있으며 Spawn Once는 유지합니다.

기존 Assets/RangedEnemy의 프리팹을 사용합니다. 스크립트를 다시 붙일 필요가 없습니다.

REDACT의 원거리 스포너 구역에는 입장 후 벽이 닫히고 전멸하면 열리는 `Combat Area - Ranged Spawner`를 배치했습니다. 범위/벽/처치 대상 조절은 `Assets/Scripts/World/README.md`를 참고하세요. 전투 중 스포너는 일시 비활성화되며 구역 스크립트가 생성과 적 추적을 담당합니다.

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
- 투사체는 전투 구역/튜토리얼 등의 감지용 Trigger를 통과합니다. 피해 수신자(ICombatDamageable)나 PasteTarget이 연결된 Trigger는 기존처럼 판정하므로 허수아비 피격과 오류 표면 반사는 유지합니다. 일반 벽과 잠금 장벽의 일반 Collider는 계속 충돌합니다.
- 기본 투사체 속도 10, 가속 오류는 이동 속도에 적용.
- 애니메이션용 Sprite 드롭은 SpriteRenderer만 사용하며 projectileSprite는 NotKeyable 유지.
