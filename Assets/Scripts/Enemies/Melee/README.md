# 근접 몬스터

## Attachable
- EnemyController.cs: 순찰·추적·근접 공격·체력 설정을 가진 적 루트 컴포넌트.
- MeleeEnemySpawner.cs: 빈 스포너에 부착합니다. 사각 범위 진입 시 MeleeMonster를 생성하고 CombatAreaLock의 전멸 판정과 연동합니다. `Assets/MeleeEnemy/MeleeEnemySpawner.prefab`에 기본 연결이 되어 있습니다.
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
