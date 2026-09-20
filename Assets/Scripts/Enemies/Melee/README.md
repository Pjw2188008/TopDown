# 근접 몬스터

## Attachable
- EnemyController.cs: 순찰·추적·근접 공격·체력 설정을 가진 적 루트 컴포넌트.
- MeleeEnemy.cs: 제자리 근접 공격 테스트용 컴포넌트. 순찰이 필요하면 Shared/Attachable/MovingEnemy와 조합합니다.
- 두 공격/체력 컴포넌트를 같은 적에 중복 부착하지 않습니다.

## Internal
EnemyController.*.cs는 본체를 나눈 partial 코드이며 직접 부착하지 않습니다.
- Movement: 순찰·추적·이동.
- Combat: 공격 타이밍·피해·패링 대응.
- Effects: 예고와 공격 이펙트.
- Collisions: 플레이어 몸 충돌 제외와 복구.

경직은 ../Shared/Attachable/EnemyStagger.cs를 공통 사용합니다.
