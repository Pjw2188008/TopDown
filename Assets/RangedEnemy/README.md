# 원거리 몬스터 사용법

## 씬에 배치

`RangedEnemySpawner.prefab`을 Hierarchy로 끌어 넣고 위치를 정합니다. 또는 `Tools > Story > Ranged Enemy > Create Spawner In Scene` 메뉴를 사용합니다. 기존 씬에는 자동으로 추가하지 않습니다.

- 플레이어가 청록색 감지 원(Activation Radius, 기본 7) 안에 들어오면 한 마리를 생성합니다.
- 스포너의 SpawnPoint 자식을 옮기면 감지 중심과 실제 생성 위치를 분리할 수 있습니다.
- 기본은 한 번만 생성합니다. Spawn Once를 끄면 처치 후 범위 밖으로 나갔다 다시 들어올 때 재생성합니다. 안에 계속 서 있다고 연속 생성되지 않습니다.
- 스포너가 아니라 `RangedEnemy.prefab`을 배치하면 처음부터 몬스터가 존재합니다. 두 프리팹을 같은 자리에 동시에 놓지 마세요.

## 이동과 공격 설정

`RangedEnemy.prefab`의 Ranged Enemy 컴포넌트에서 수정합니다.

| 설정 | 기본값 / 용도 |
| --- | --- |
| Move Speed | 1.8, 이동 속도 |
| Stopping Distance | 3, 접근을 멈추는 거리 |
| Attack Range | 5, 공격 가능 거리 |
| Attack Interval | 1.5초, 공격 시작 사이 최소 간격 N |
| First Attack Delay | 1초, 생성 직후 첫 공격 유예 |
| Release Normalized Time | 0.65, 공격 클립의 65%에서 한 발 발사 |
| Projectile Speed / Damage / Lifetime | 10 / 1 / 6초 |
| Max Health | 5 |

공격 애니메이션 중에는 이동하지 않습니다. 공격 클립이 N초보다 길면 클립을 끝낸 후 다음 공격을 시작합니다. 프레임이 느려져도 한 공격에 한 발만 나갑니다. 투사체는 몬스터의 발사 위치에서 생성되어 **실제 발사 순간에 플레이어가 있던 위치**를 향해 직진합니다. 준비 동작 중 플레이어가 움직이면 발사 순간의 위치로 조준을 갱신하지만, 이미 발사한 탄환은 플레이어를 따라 방향을 바꾸지 않습니다(벽 반사/패링은 예외). 발사 시점에 플레이어가 공격 범위 밖으로 나가면 발사하지 않습니다. 공격 간격과 발사 시간은 게임 시간(Time.time)을 사용하므로 편집 모드 슬로모션에도 함께 느려집니다.

장애물 충돌은 처리하지만 복잡한 길찾기나 벽 너머 시야 검사는 없습니다. 몬스터/플레이어의 몸 충돌은 제외하고 투사체와 공격의 피해 판정은 유지합니다. 가속 오류는 이동 속도에 적용됩니다. 이전 테스트용 발사 컴포넌트는 제거했으며, 원거리 몬스터는 RangedEnemy와 RangedEnemySpawner를 사용합니다. ReflectProjectile은 현재 몬스터에도 필요한 공용 투사체 코드이므로 유지합니다.

## 스프라이트 프레임 넣기

1. `RangedEnemy.prefab`을 더블클릭해 Prefab Mode로 엽니다.
2. 루트를 선택하고 `Ctrl+6`으로 Animation 창을 엽니다.
3. 아래 클립을 골라 기존 임시 Sprite 키프레임을 지우고, 잘라 놓은 Sprite 프레임들을 **Sprite Renderer > Sprite 트랙**에 넣습니다.

- `Animations/Ranged_Idle.anim`: 정지, 반복.
- `Animations/Ranged_Move.anim`: 오른쪽 걷기, 반복. 위아래/대각선 이동도 같은 클립을 사용합니다. 왼쪽 이동 시 SpriteRenderer.flipX만 켭니다.
- `Animations/Ranged_Attack.anim`: 오른쪽 기준 공격, 반복 안 함. 이름을 유지해야 코드가 실제 클립 길이를 읽습니다.

원본 이미지 전체가 아닌 Import Settings에서 Sprite로 설정/분할한 프레임을 넣으세요. 세 클립에는 SpriteRenderer의 Sprite 트랙만 있으며 PlayerMove 필드 트랙은 없습니다. Flip X, Transform Scale/Position 키는 추가하지 않는 것이 좋습니다. 오른쪽 원본 프레임만 넣으면 왼쪽은 코드가 반전합니다. 세로 방향은 별도 클립 없이 직전 좌우 방향을 유지합니다.

Animator는 `Idle ↔ Move`, `Any State → Attack → Idle`로 연결되어 있습니다. 파라미터는 `IsMoving`(Bool), `Attack`(Trigger)입니다. 공격 클립은 이동 상태로 중간에 끊기지 않습니다. 스프라이트를 교체한 뒤 BoxCollider2D의 Size/Offset과 Muzzle Distance를 그림에 맞게 조절하세요. 애니메이션 이벤트 없이 발사 비율을 Inspector에서 정합니다.

투사체는 기존 ReflectProjectile을 사용하므로 투사체끼리 통과하며, 플레이어 가드/패링과 반사 표면을 계속 지원합니다. 이 몬스터 자체에 반사 오류를 기본 부여하지는 않습니다.

## 관련 스크립트

- `Assets/Scripts/Enemies/Ranged/Attachable/RangedEnemy.cs`: 적 루트에 부착. 이동, 발사 타이밍, 체력, 좌우 반전.
- `Assets/Scripts/Enemies/Ranged/Attachable/RangedEnemySpawner.cs`: 감지 지점에 부착. 범위 진입 시 프리팹 생성.
- `Assets/Scripts/Enemies/Ranged/Internal/Editor/RangedEnemySetup.cs`: Editor 메뉴 도구. 오브젝트에 부착하지 않습니다. 기존 아트/클립을 덮어쓰지 않습니다.

EnemyController, MeleeEnemy, MovingEnemy를 RangedEnemy 루트에 중복 부착하지 마세요.

## 코드 구성

Inspector 설정은 `Assets/Scripts/Enemies/Ranged/Attachable/RangedEnemy.cs`에서 바꿉니다. 이동·공격 타이밍·탄환 생성·충돌은 `Internal/RangedEnemy.*.cs`로 나누었으며 별도 부착하지 않습니다. 파일별 설명은 `Assets/Scripts/Enemies/Ranged/README.md`를 참고하세요. 프리팹과 애니메이션 위치는 그대로입니다.
