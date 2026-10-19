# 일반 몬스터 — 사이드뷰 통합
일반 몬스터 AI는 두 개만 사용합니다.

- 근접: `Melee/Attachable/MeleeEnemy.cs`
- 원거리: `Ranged/Attachable/RangedEnemy.cs`

## 배치
1. 몬스터 프리팹을 씬 Hierarchy로 직접 드래그합니다. 스포너는 사용하지 않습니다.
2. 한 오브젝트에 위 AI 중 하나만 붙입니다.
3. 루트 SpriteRenderer, Animator, Collider2D, Rigidbody2D를 사용합니다. 새로 AI를 붙이면 필수 컴포넌트가 함께 추가됩니다.
4. SpriteRenderer와 Animator의 기존 편집 가능한 클립에 직접 이미지를 넣습니다. 코드가 Animator Controller나 클립을 만들거나 교체하지 않습니다.
5. 스프라이트 원본이 왼쪽을 보면 `Sprite Faces Left`를 켭니다.

## 추적과 복귀
- `Aggro Mode / When Damaged By Player`: 영역 안의 플레이어에게 실제로 맞으면 추적합니다. 다른 몬스터의 공격은 피해를 주지 않습니다.
- `When Player Enters Area`: 플레이어가 영역 안에 들어오면 추적합니다.
- `Chase Area Offset / Size`: 최초 배치 위치 기준 고정 사각형 영역입니다. 선택 시 청록색 기즈모로 표시됩니다. 몬스터를 따라 이동하지 않습니다.
- 플레이어가 영역을 벗어나면 공격을 취소하고 최초 X 위치로 복귀합니다. 복귀 도중에는 다시 추적하지 않습니다.
- 피격형은 복귀 완료 후 다시 맞아야 추적하고, 진입형은 영역 안의 플레이어를 다시 감지합니다.
- `Target`은 비워 두면 같은 씬의 PlayerMove를 검색합니다.
- 좌우 이동만 합니다. `Use Gravity`는 같은 씬 플레이어의 Side View ON / Depth Movement OFF일 때 작동합니다.
- `Movement Blocking Layers`에 지형 레이어를 포함하세요. `Stop At Ledges`는 낭떠러지 앞에서 멈춥니다. 자동 점프나 다른 발판으로 가는 경로 탐색은 하지 않습니다. 막힌 귀환 경로를 순간이동으로 통과하지 않습니다.

## 공격과 애니메이션

- 비전투 중에는 `Patrol Enabled`를 켜면 추적 영역 안을 좌우 순찰합니다(기본 ON). `Patrol Speed Multiplier`, `Patrol Wait Time`, `Patrol Edge Padding`으로 속도·방향 전환 대기·경계 여유를 조절합니다. 벽/낭떠러지를 만나면 대기 후 반대 방향으로 이동합니다. 범위가 몸보다 좁으면 정지합니다.
- 순찰은 추적 조건을 바꾸지 않습니다. 피격형은 순찰 중에도 플레이어에게 맞아야 추적합니다. 영역 이탈 시 원위치로 복귀한 후 순찰을 재개합니다.
- 근접 `Attack Hit Frame`은 1부터 시작하는 클립 프레임 번호입니다. 0이면 기존 `Hit Normalized Time`을 사용합니다. MeleeMonster는 찌르기가 펼쳐진 16번째 프레임으로 맞췄습니다(24 FPS 기준 약 0.625초). 애니메이션을 교체하면 해당 이미지에 맞춰 다시 설정하세요.
- `Melee Parry Window`는 해당 근접 공격에만 적용되는 우클릭 타이밍 허용 시간입니다(기본 0.3초). 오래 유지한 가드는 패링이 되지 않습니다. 보스/원거리 패링 설정은 바꾸지 않습니다.
- 타격은 Attack 재생 중 한 번만 발생합니다. 애니메이션이 중간에 취소되면 뒤늦은 타격은 발생하지 않습니다. 기존 Animator의 IsMoving/Stunned Bool이 있으면 함께 갱신하고, Hit 상태가 없는 MeleeMonster는 기존 Stagger를 사용합니다.
- `Attack Range / Attack Vertical Range`: 공격을 시작할 가로 거리와 높이 차이.
- `Hit Normalized Time`: Attack 애니메이션 중 타격/발사 시점(0~1). 클립이 없으면 `Fallback Attack Duration`을 사용합니다.
- `Attack Cooldown`: 한 번의 공격 종료 후 다음 공격까지 대기 시간.
- 기본 상태 이름은 Idle / Move / Attack / Hit / Death입니다. Inspector에서 기존 Animator 상태 이름에 맞춰 바꿀 수 있습니다.
- 근접은 `Hit Offset / Hit Size`로 타격 박스를 조정합니다. 오른쪽 기준 X 오프셋이 왼쪽에서 자동 반전됩니다.
- 원거리는 `Projectile Prefab`, `Muzzle Offset`, `Projectile Speed`를 설정합니다. Projectile 어댑터 프리팹은 기존 이미지/애니메이션/크기 설정을 그대로 사용합니다.
- 플레이어 공격으로 피해를 받고, 근접·투사체 모두 패링할 수 있습니다. 일반 몬스터는 성공 즉시 경직합니다.
- 적 탄환은 다른 적을 통과합니다. 패링한 탄환은 플레이어 소유가 되어 적에게 피해를 줍니다.

## 이번 정리 범위
기존 EnemyController / TriangleMeleeMonster / RangedEnemyController / MovingEnemy, 일반 몬스터 스포너와 CombatAreaLock 웨이브를 제거했습니다. 해당 프리팹과 직접 배치된 일반 몬스터는 위 두 AI로 연결을 변경했습니다. 기존 일반 몬스터의 별도 돌진·충격파·부채꼴 패턴은 단일 근접 타격/단일 투사체로 통합되었습니다.

보스 전용 AI, 튜토리얼 허수아비, 플레이어 기능, 맵, 이미지, 애니메이션은 유지합니다. Shared의 경직/이동 계산과 Ranged/Internal의 투사체 처리는 AI를 따로 붙이는 기능이 아니라 공통 보조 코드이며 보스도 사용하므로 유지합니다.

REDACT의 기존 웨이브 장벽은 비활성 상태로 남겼습니다. 스포너 프리팹 인스턴스는 제거했고, 직접 구성했던 빈 구역/Spawn Point 오브젝트는 배치 참고용으로 남을 수 있습니다. 몬스터를 자동 생성하지 않습니다.
