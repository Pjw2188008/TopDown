# 몬스터 스크립트

- Melee: 근접 몬스터. Attachable에 본체, Internal에 EnemyController 분할 구현.
- Ranged: 원거리 몬스터와 스포너. Attachable에 본체, Internal에 이동·공격·탄환·충돌 구현과 편집기 도구.
- Shared: 적 공통 순찰과 경직 컴포넌트.
- Bosses/Tutorial: 고정형 튜토리얼 보스. 근접 내리찍기/원거리 투사체 경고, N회 패링 누적 경직. `Assets/TutorialBoss/TutorialBoss.prefab`으로 배치합니다.

EnemyController / MeleeEnemy / RangedEnemy는 각각 체력과 공격을 소유하므로 같은 적에 중복 부착하지 않습니다.
기존 테스트용 발사 스크립트는 제거된 상태를 유지합니다. 새 원거리 적은 RangedEnemySpawner 프리팹으로 배치합니다.

## 공통 장애물 우회

이동형 근접 EnemyController, 원거리 RangedEnemy, 기존 순찰 MovingEnemy는 공통 `Shared/Internal/EnemyNavigation`을 사용합니다. 따로 컴포넌트를 붙이거나 NavMesh를 베이크할 필요 없습니다.

- 일반 Collider2D를 피하고 Trigger는 무시합니다. 각 적의 Blocking Layers와 Unity 레이어 충돌 설정에 장애물 레이어가 포함되어 있어야 합니다.
- 몸 Collider 크기로 검사합니다. 스프라이트보다 Collider가 크면 좁은 길로 들어가지 못하므로 몸에 맞춰 주세요.
- Inspector의 Navigation: Cell Size(격자 간격, 기본 0.5), Search Radius(주변 탐색 반경, 10), Repath Interval(재탐색 간격, 0.5초), Max Search Nodes(한 번의 탐색 상한, 512).
- 벽 뒤 플레이어에게 제자리 공격하지 않고 우회합니다. 원거리 적은 사격 시야가 확보되면 원래 공격/정지 거리로 돌아갑니다.
- 벽, 모서리와 U자 장애물을 돌아갈 수 있지만 제한된 지역 탐색입니다. 큰 미로/몸보다 좁은 통로/완전히 닫힌 방에는 경로를 보장하지 않으며 벽을 뚫거나 순간이동하지 않습니다. 많은 몬스터가 동시에 탐색하는 맵에서는 성능을 확인하세요.
- 움직이는 장애물은 다음 재탐색 때 반영됩니다. 길이 막히면 충돌 검사로 멈추고, 직선 경로가 열리면 즉시 접근합니다.
- 제자리 테스트용 MeleeEnemy 및 튜토리얼 허수아비에 새로운 추적 이동은 추가하지 않습니다. MeleeEnemy에 MovingEnemy를 함께 붙인 경우 기존 순찰에 우회가 적용됩니다.
