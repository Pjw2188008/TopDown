# 근접 몬스터

## 배치

`MeleeMonster.prefab`을 씬으로 끌어 넣으세요. 또는 `Tools > Story > Melee Enemy > Create Monster In Scene`으로 플레이어 옆에 생성할 수 있습니다. 기존 맵에는 자동 배치하지 않습니다.

- 순찰 → 사각 감지 범위에서 플레이어 추적 → 근거리 찌르기. 공통 Navigation으로 장애물을 우회합니다.
- 플레이어/몬스터 상호 피해, 가드/패링에 따른 경직, 빨간 피격 표시, 사망 제거는 기존 EnemyController/EnemyStagger를 사용합니다.
- CombatAreaLock 범위 안에 배치하면 입장 시 전멸 대상으로 자동 수집됩니다. 제자리 테스트용 MeleeEnemy나 RangedEnemy를 함께 붙이지 마세요.

## 범위 진입 시 스폰하기

1. `Assets/MeleeEnemy/MeleeEnemySpawner.prefab`을 씬으로 끌어 넣으세요. 또는 `Tools > Story > Melee Enemy > Create Spawner In Scene`을 사용하세요.
2. Inspector의 `Activation Size`로 전체 가로/세로 범위를 조절합니다. 선택 시 청록색 사각형이 표시됩니다. Collider를 추가할 필요는 없습니다.
3. 자식 `SpawnPoint`를 원하는 생성 위치로 옮깁니다. 노란 기즈모가 실제 생성 위치입니다. 벽이나 플레이어와 겹치지 않는 곳에 두세요.
4. `Enemy Prefab`은 MeleeMonster가 연결되어 있습니다. 해당 프리팹의 애니메이션/스프라이트를 바꾸면 스폰되는 몬스터에도 적용됩니다. 씬 인스턴스만 수정했다면 프리팹에 Apply하세요.
5. `Spawn Once`를 켜면 플레이 실행 중 한 번만 생성합니다. 끄면 적 처치 후 범위를 나갔다 다시 들어왔을 때 재생성합니다. 한 스포너당 살아 있는 몬스터는 최대 한 마리입니다.

여러 마리를 원하면 스포너를 복제하고 각각 SpawnPoint를 조절하세요. 몬스터 프리팹 자체를 씬에 함께 배치할 필요는 없습니다.

### 전투 구역 벽과 연결

- 기존 CombatAreaLock 안에 스포너를 배치하면 `Collect Enemies In Area`가 켜져 있을 때 자동 수집합니다. 또는 `Melee Spawners` 목록에 직접 등록하세요.
- 근접/원거리 몬스터를 모두 처치해야 벽이 열립니다. 전투 도중 자동 재생성을 막고 종료/취소 시 스포너의 원래 활성 상태를 복구합니다.
- 새 벽 구역은 Hierarchy에서 스포너 선택 → `Tools > Story > Combat Area > Create Around Selected Spawner`로 생성할 수 있습니다. 이미 벽 구역이 있다면 중복 생성하지 마세요.
- 감지 범위 진입은 플레이어 중심 기준, 벽 닫힘은 몸 전체 진입 기준입니다. 따라서 적이 벽 닫힘보다 조금 먼저 나타날 수 있습니다.
- 한 스포너를 여러 전투 구역이 동시에 공유하지 마세요. 벽 구역 밖에서 생성한 적도 명시적으로 연결되어 있으면 처치해야 하므로 SpawnPoint를 구역 안에 두세요.

## 직접 스프라이트 넣기

1. 시트를 `Sprite (2D and UI)`, `Sprite Mode = Multiple`로 설정하고 Sprite Editor에서 Slice/Apply합니다.
2. 씬의 MeleeMonster를 선택하고 `Ctrl+6`으로 Animation 창을 엽니다.
3. 아래 클립을 골라 `Sprite Renderer → Sprite` 트랙에 잘라낸 Sprite 프레임들을 넣으세요. PNG 파일 전체가 아니라 펼쳐진 개별 Sprite들을 사용합니다. 임시 프레임 2개는 지우고 교체하세요.

| 클립 | 용도 |
| --- | --- |
| Melee_Idle | 대기, 반복 |
| Melee_Move | 걷기, 반복. 오른쪽 기준 이미지이며 왼쪽은 flipX |
| Melee_Attack | 공격, 반복 OFF |
| Melee_Stagger | 패링 경직, 반복. 경직 종료 시 Idle 복귀 |

클립은 일반 `.anim` 파일이며 잠겨 있지 않습니다. 스크립트가 Sprite 프레임을 덮어쓰지 않습니다. 공격 효과용 Sprite 필드는 애니메이션 키 대상에서 제외했습니다.

## 타이밍과 설정

- Animator: IsMoving(bool), Stunned(bool), Attack(trigger), Attack 상태. 공격은 클립 끝까지 재생한 뒤 대기로 돌아옵니다. 패링 경직/사망 시에는 중단합니다.
- `Melee Attack Clip`: 기존 참조 호환용입니다. 타격은 실제 Animator의 Attack 상태 진행률로 판단하므로 클립 교체/길이/속도 변경도 반영됩니다.
- `Melee Hit Normalized Time`: 실제 찌르기 피해/패링 시점, 기본 0.65(65%). Animation Event를 추가하지 마세요. 공격 사각 예고/타격 이펙트는 생성하지 않고 캐릭터의 준비/찌르기 동작으로 보여줍니다.
- 준비/후딜 중에는 추적하지 않고, 찌르기 구간에서만 고정한 방향으로 짧게 전진합니다. 공격 시작 때 플레이어 위치로 상/하/좌/우를 선택하고 끝날 때까지 고정합니다. 대각 위치는 차이가 더 큰 축을 선택하며 같은 경우 좌우 우선입니다.
- `Rotate Attack Sprite`(기본 ON): 기존 오른쪽 공격 이미지를 위 +90도 / 아래 -90도로 회전합니다. 좌우는 원래 이미지/좌우 반전을 사용합니다. 별도 방향별 클립은 필요 없습니다. 위아래는 캐릭터 그림 전체가 돌아가는 방식입니다.
- 실행 중 `MeleeAttackVisual` 자식 Renderer가 같은 애니메이션 프레임을 대신 표시합니다. 새로운 공격 이펙트가 아니며 원본 Renderer는 잠시 렌더링만 숨깁니다. 몸 Transform/Collider는 돌리지 않습니다. 공격 종료·패링·비활성화 시 원래 표시로 복구합니다.
- `Use Thrust Attack`: 기본 공격의 가속 찌르기 연출이며 가속 오류가 필요하지 않습니다. OFF면 전진/속도 변화 없이 실제 애니메이션에 맞춰 타격합니다.
- `Thrust Start Normalized Time` 0.4부터 `Melee Hit Normalized Time` 0.65까지 빠르게 찌릅니다. 준비 속도 0.8배 / 찌르기 속도 2.5배 / 후딜 원래 속도입니다. 찌르는 이미지가 나오는 위치에 맞춰 두 진행률을 조절하세요.
- `Thrust Distance`: 최대 전진 거리(기본 0.8). 공격 시작 시 방향/거리를 고정하므로 옆으로 피할 수 있습니다. 가까운 대상은 거리를 줄이고 벽에는 멈춥니다. 새 기본 프리팹의 Attack Area Size는 3×3입니다.
- 실제 찌르기 타격 직전 `Parry Window`(플레이어 기본 0.2초) 안에 새 우클릭을 누르고 유지하면 패링합니다. 성공 연출 도중에도 우클릭을 놓았다 다시 누르면 다시 패링할 수 있습니다. 누른 채 유지하는 것만으로 재패링되지는 않습니다.
- 기본 테스트 수치: HP 5, 피해 1, 속도 2, 공격 간격 1.2초. Inspector에서 조정할 수 있습니다.
- Patrol Area Size: 감지/순찰 사각 크기. Attack Area Size: 공격 시작 거리. Attack Reach / Attack Hit Size: 실제 타격 위치/크기.
- Sprite를 교체한 뒤 BoxCollider2D 크기를 몸에 맞게 조절하세요. 이전 Attack Effect Sprite/Duration/Show Attack Warning 필드는 직렬화 호환용으로만 남아 있으며 사용하지 않습니다.
- 근접 이동/찌르기는 항상 벽 충돌을 검사합니다. Respect Walls는 기존 Rigidbody 설정 호환용이며, 장애물 범위는 Melee Blocking Layers에서 지정합니다. 공통 Navigation 설정은 Assets/Scripts/Enemies/README.md를 참고하세요.

기존 EnemyController는 Animator 옵션이 기본 OFF라 기존 attackWindup 타이밍을 유지합니다. 생성 메뉴를 다시 실행해도 직접 넣은 스프라이트/클립/프리팹을 덮어쓰지 않습니다.
