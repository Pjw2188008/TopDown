# 튜토리얼 보스 — 기본 패턴 프로토타입

`TutorialBoss.prefab`을 씬에 넣으세요. 또는 `Tools > Story > Tutorial Boss > Create Boss In Scene`을 사용합니다. 기존 씬에는 자동 배치하지 않습니다. 일반 근접/원거리 적 스크립트를 함께 붙이지 마세요.

## 기본 규칙

- 제자리 보스입니다. 플레이어가 Engagement Range(12) 안에 오면 공격을 시작하며, 이동/추적/다음 페이즈는 아직 없습니다.
- Melee Range(2.5) 안: 준비 시작 시 플레이어 위치를 고정 → 원형 경고(0.9초) → 타격 시점에 해당 원 안에 한 번 피해. 준비 중 밖으로 피하거나 타격 순간 패링할 수 있습니다.
- Melee Range 밖: 방향을 고정 → 직선 경고(1초) → 경고 방향으로 탄환 1발. 발사 후 플레이어를 추적하지 않습니다. 패링하면 기존 투사체 규칙대로 보스 쪽으로 돌아갑니다.
- 경고가 시작된 뒤 거리가 바뀌어도 그 패턴을 끝까지 사용합니다. 다음 패턴 선택 때 거리를 다시 확인합니다. 공격 클립의 후딜까지 끝난 뒤 기본 1.2초 쉬고 다음 패턴을 고릅니다.
- 경고의 색이 주황에서 빨강으로 변합니다. 원형 경고 크기는 실제 내리찍기 반경과 같고, 직선 경고 폭은 탄환 지름입니다. 직선은 조준 방향 안내이며 자체 피해 판정이 아닙니다. 발사 후 탄환은 수명 동안 직진하고 벽에 막힙니다.
- 벽에 가려진 플레이어에게 새 패턴을 시작하지 않습니다. Blocking Layers에 벽 레이어를 포함하세요.
- 보스 몸은 피격용 Trigger입니다. 플레이어와 밀지 않으며 몸에 닿았다는 이유만으로 피해를 주지 않습니다.

## 패링 누적 / 균형

- Parries To Stagger: 필요 횟수 N, 기본 3. 근접과 원거리 패링을 합산합니다.
- 패링할 때마다 플레이어 피해/가드 게이지 소모는 기존처럼 무효화됩니다. N회 미만이면 보스는 경직되지 않습니다.
- 원거리는 탄환을 튕겨낸 순간 1회 인정합니다. 돌아온 탄환이 보스에 명중하면 일반 피해만 주고 패링 횟수를 중복 계산하지 않습니다.
- N회가 되면 준비 중인 공격을 취소하고 Stagger Duration(2.5초) 동안 경직합니다. 경직 시작 시 누적 횟수를 0으로 초기화합니다. 경직 중 추가 패링은 다음 경직에 누적하지 않습니다.
- 누적 횟수는 시간에 따라 자동 감소하지 않습니다. 이미 발사한 탄환은 경직 중에도 남아 있습니다. 보스가 사망/비활성화되면 남은 보스 탄환은 정리합니다.
- 머리 위에 HP와 패링 횟수/남은 균형 막대를 임시 표시합니다. Show Status로 끌 수 있습니다. 일반 적의 한 번 패링 경직은 그대로 유지합니다.

## 그림 교체

- 기본 이미지는 `golem-upper-left-128.png`입니다. 원본 PNG/슬라이스 설정은 변경하지 않습니다.
- `TutorialBoss` 하나에 SpriteRenderer, Animator, BoxCollider2D, Rigidbody2D, 보스 스크립트가 있습니다. 별도 `Body`나 `VisualRig`는 필요 없습니다. SpriteRenderer의 Sprite에서 그림을 교체하세요.
- 루트 Transform의 Scale을 바꾸면 그림과 Collider 크기가 함께 변합니다. 피격 범위만 바꾸려면 BoxCollider2D의 Size/Offset을 조절하세요. 이번 통합에서는 기존 화면상 크기와 월드 충돌 범위를 유지했습니다.
- `Projectile Sprite`에 탄환 이미지를 넣습니다. 비우면 원형 도형입니다. 그림 크기는 Projectile Radius에 맞추고 피격 원형 크기도 같이 바뀝니다.
- `Circle Sprite`, `Square Sprite`는 경고/임시 효과 이미지입니다. 정확한 경고를 위해 원/사각 전체를 채운 중앙 피벗 이미지를 사용하세요.
- 지금 에셋은 골렘 그림 1장이므로 각 클립에는 같은 Sprite를 유지하는 키가 들어 있습니다. 기존 임시 확대/낙하/흔들림 Transform 키는 제거했습니다. 실제 동작 프레임은 이후 Sprite 트랙에 넣으세요. 공격 경고와 피해 판정은 그대로 작동합니다.

## Animator / 애니메이션 편집

- `TutorialBoss` 루트에 Animator가 연결되어 있습니다. 컨트롤러는 `TutorialBoss.controller`이며 Idle(대기), Slam(내리찍기), Shoot(발사), Stagger(경직) 네 상태입니다. 스크립트가 직접 전환하므로 별도 Trigger 입력은 없습니다.
- 씬의 **TutorialBoss**를 선택 → Ctrl+6 → `Boss_Idle / Boss_Slam / Boss_Shoot / Boss_Stagger`를 선택합니다. `Sprite Renderer → Sprite` 트랙에 애니메이션 프레임을 넣으세요.
- 애니메이션에는 Sprite 키만 사용하세요. 루트 Transform의 위치/크기/회전 키를 넣으면 Collider까지 움직입니다.
- `Slam Impact Time` / `Shot Release Time`: 클립 진행률 0~1, 기본 0.65. 해당 지점에서 실제 타격/탄환 발사가 한 번 발생합니다. Animation Event는 추가하지 마세요.
- Slam Warning Time / Shot Warning Time에 위 타격 지점에 도달하도록 재생 속도를 자동 조정합니다. 클립 길이를 바꾸어도 경고 시간이 유지됩니다. Animator State의 Speed는 양수로 두세요.
- 패링 N회 경직은 진행 중인 공격을 취소하고 Stagger로 전환합니다. 경직 종료 후 Idle로 복귀합니다. 보스 Transform을 코드로 들어올리는 임시 연출도 제거했습니다.
- Animator가 없는 기존 오브젝트는 이전 타이머 방식으로 작동합니다. 재생 중 Animator를 끄면 진행 중인 공격을 안전하게 취소합니다.
- `Tools > Story > Tutorial Boss > Configure Animator`로 프리팹을 연결할 수 있습니다. 이전 Body/VisualRig 구조는 단일 루트로 통합하고, 루트 Transform 애니메이션 키는 제거합니다. 기존 Sprite 키와 상태 Motion, 에셋 GUID는 유지합니다. 사용자 컴포넌트나 추가 자식이 있으면 자동 제거하지 않고 중지합니다.

## 전투 구역 / 튜토리얼 연동

CombatAreaLock의 자동 수집이 켜져 있으면 구역 안 보스도 전멸 대상으로 포함됩니다. 또는 Enemies 목록에 보스 루트를 넣으세요. 죽으면 루트가 제거되어 벽 해제 조건에 반영됩니다. On Staggered / On Defeated 이벤트에 후속 연출을 연결할 수 있습니다. 기존 튜토리얼 단계는 자동 변경하지 않습니다.

## 코드 위치

- Scripts/Enemies/Bosses/Tutorial/Attachable/TutorialBoss.cs: 패턴·체력·패링 누적.
- Internal/TutorialBoss.Presentation.cs: 도형/경고/UI. 별도 부착하지 않습니다.
- Internal/Editor/TutorialBossSetup.cs: 프리팹 생성. 기존 파일은 덮어쓰지 않습니다.
- Scripts/Enemies/Shared/Internal/CombatParry.cs: 공격자에게 패링 성공 전달. 플레이어와 보스 탄환이 공통 사용합니다.
