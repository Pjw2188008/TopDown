# 근접 몬스터

## Attachable
- EnemyController.cs: 순찰·추적·근접 공격·체력 설정을 가진 적 루트 컴포넌트.
- MeleeEnemySpawner.cs: 빈 스포너에 부착합니다. 사각 범위 진입 시 MeleeMonster 또는 mon_1을 생성하고 CombatAreaLock의 전멸 판정과 연동합니다. `Assets/MeleeEnemy/MeleeEnemySpawner.prefab`에 기본 연결이 되어 있습니다.
- MeleeEnemy.cs: 제자리 근접 공격 테스트용 컴포넌트. 순찰이 필요하면 Shared/Attachable/MovingEnemy와 조합합니다.
- 두 공격/체력 컴포넌트를 같은 적에 중복 부착하지 않습니다.

## Internal
EnemyController.*.cs는 본체를 나눈 partial 코드이며 직접 부착하지 않습니다.
- Movement: 순찰·추적·공통 Navigation을 통한 장애물 우회.
- Combat: 공격 타이밍·피해·패링 대응.
- Effects: 공격 취소와 편집기 타격 기즈모. 런타임 공격 이펙트는 생성하지 않습니다.
- Collisions: 플레이어 몸 충돌 제외와 복구.
- Animation: 선택형 Animator 연동, 공격 클립 타이밍, 좌우 반전, 새 프리팹의 벽 충돌 이동.
- Editor/MeleeMonsterSetup: `Assets/MeleeEnemy`의 새 몬스터 프리팹/Animator/편집 가능한 클립 생성. 기존 에셋은 덮어쓰지 않습니다.

새 몬스터는 `Assets/MeleeEnemy/MeleeMonster.prefab`을 씬에 배치하세요. 제자리 테스트용 `MeleeEnemy` 스크립트를 추가하지 않습니다. 새 프리팹에는 `EnemyController`의 Use Melee Animator / Respect Walls를 켰고, 기존 씬/프리팹에는 기본 OFF로 이전 동작을 유지합니다. 스프라이트 입력은 `Assets/MeleeEnemy/README.md`를 참고하세요.

경직은 ../Shared/Attachable/EnemyStagger.cs를 공통 사용합니다.

## mon_1 연결

1. Hierarchy의 근접 스포너를 선택합니다.
2. `Melee Enemy Spawner > Enemy Prefab`에 Project의 `Assets/철오/monster/mon_1.prefab`을 넣습니다.
3. `Spawn Point`와 `Activation Size`를 원하는 위치/범위로 조절합니다. Player를 비우면 PlayerMove를 자동 검색합니다.
4. 전투 구역에서는 기존처럼 `CombatAreaLock > Melee Spawners`에 스포너를 연결하거나 자동 수집을 사용합니다.

프리팹 루트에 EnemyController 또는 TriangleMeleeMonster 중 하나만 필요합니다. 두 AI를 동시에 붙이지 마세요. 기존 EnemyController 프리팹 연결은 숨겨진 이전 참조에서 자동 이전되며, 씬/프리팹을 저장하면 새 GameObject 참조로 저장됩니다. SpawnedEnemy / SpawnForCombatArea 반환 타입도 GameObject입니다.

한 스포너는 프리팹 하나를 생성합니다. 두 종류를 함께 등장시키려면 스포너를 두 개 배치하고 각각 다른 프리팹을 넣으세요. 실제 씬의 선택을 임의 변경하지 않으므로 mon_1 연결은 위와 같이 직접 지정합니다.

TriangleMeleeMonster는 IsDead가 되면 처치로 인식합니다. 사망 애니메이션 재생 중이거나 Destroy After Death를 꺼 시체가 남아 있어도 전멸 시 문이 열립니다. Spawn Once가 OFF라면 처치 후 범위 밖으로 나갔다 다시 들어올 때 재생성합니다. 단순 비활성화는 처치로 취급하지 않습니다. TriangleMeleeMonster의 공격/애니메이션 코드는 변경하지 않습니다.
