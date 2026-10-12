# Triangle 내려찍기 / 네 방향 충격파 몬스터

`TriangleMeleeMonster`를 사용하는 `mon_1` 및 Triangle 프리팹에 공통으로 적용되는 동작입니다. Enemy 레이어, Rigidbody2D, BoxCollider2D, Animator, TriangleMeleeMonster, EnemyStagger를 사용합니다. 다른 몬스터 스크립트와 보스 공격은 변경하지 않습니다.

pco 씬의 Triangle에 설정이 필요하면 해당 오브젝트를 선택하고 **Tools > CheolO > Monster > Setup Selected Triangle**을 한 번 누른 뒤 씬을 저장하세요. 이 작업은 Undo할 수 있습니다. 잡고 미는 MovableInteractable은 제거됩니다.

## 이미지가 준비되면

1. 이 폴더 안에 Sprites 폴더를 만들고 이미지를 넣습니다. Texture Type은 Sprite (2D and UI), 스프라이트 시트는 Sprite Mode를 Multiple로 설정하고 Sprite Editor에서 Slice/Apply합니다.
2. Triangle을 선택하고 Window > Animation > Animation을 엽니다.
3. 아래 클립을 선택하고 기존 임시 Sprite 키 2개를 삭제한 뒤, 공용 스프라이트들을 순서대로 타임라인에 드래그합니다. SpriteRenderer.Sprite 트랙만 편집하면 됩니다.
4. Samples(FPS)로 재생 속도를 맞춥니다. Idle/Move는 Loop Time ON, Attack/Hit/Death는 OFF로 유지합니다.

| 동작 | 공용 모션 |
|---|---|
| 대기 | Idle |
| 이동 | Move |
| 공격 | Attack |
| 피격 | Hit |
| 죽음 | Death |

모든 클립은 Animations 폴더에 있습니다. 이미 넣어 둔 스프라이트는 유지하며, 내려찍기 이미지는 기존 **Attack** 클립에 넣습니다. 모든 방향은 방향 접미사 없는 클립(Idle, Move, Attack, Hit, Death)을 공유합니다. 오른쪽을 바라보는 이미지를 공용 클립에 넣으면 왼쪽은 SpriteRenderer.flipX로 자동 반전됩니다. 좌우 전환만으로 재생이 처음부터 시작되지 않습니다. 위/아래 전용 모션은 사용하지 않으며, 수직 이동 시 마지막 좌우 반전을 유지합니다.

공격은 **접근 → Attack 내려찍기 → 애니메이션 종료 → 상하좌우 충격파 4발 → 재사용 대기**입니다. 이전의 직접 근접 박스 피해는 사용하지 않습니다. 충격파에 닿아야 피해를 받으며, 발사 후에는 몬스터가 피격되어도 이미 발사한 충격파가 계속 이동합니다. 몬스터가 죽거나 비활성화되면 남은 충격파는 제거합니다. 이번 Triangle 공격은 사이드뷰에서도 네 방향이며, 바닥으로 발사한 충격파는 바닥에 막힐 수 있습니다. 보스의 사이드뷰 좌우 충격파 규칙은 변경하지 않습니다.

Animator 전이는 스크립트에서 직접 선택합니다. 상태 이름을 변경하거나 별도의 자동 전이를 추가하지 마세요. 정지할 때 마지막 방향을 유지하고 공격 중에는 방향을 고정합니다. Hit는 공격을 중단하며 Death가 최우선입니다.

## Inspector 설정

- Player: 비우면 PlayerMove를 자동으로 찾습니다. Detection Range는 추적 감지 반경입니다. 공격 시작 사각형 안에 들어온 플레이어는 이 반경 밖이어도 확인합니다.
- Max Health / Move Speed / Attack Damage / Attack Cooldown: 체력, 속도, 피해량, 공격 후 대기 시간입니다.
- Attack Area Offset / Attack Area Size: 공격 시작 사각형의 중심 오프셋과 가로/세로 크기입니다. 월드 단위이며 몬스터와 함께 이동하지만 좌우 반전·회전·스케일에 따라 뒤집히거나 커지지는 않습니다. 크기 0인 축은 이전 Stop Distance의 지름을 초기값으로 사용합니다.
- 플레이어의 실제 Collider가 사각형과 겹치고 벽에 가려지지 않으면 멈춰 Attack 패턴을 시작합니다. 재사용 대기 중이면 제자리에서 기다립니다. 패턴을 시작한 뒤 플레이어가 벗어나도 공격은 계속됩니다(피격/사망 시 취소 규칙은 유지).
- Triangle을 선택하고 Scene의 Gizmos를 켜면 청록색 사각형이 공격 시작 영역입니다. 중심 화살표를 드래그하면 위치, 테두리 핸들을 드래그하면 크기를 변경합니다. Inspector 값으로도 수정할 수 있으며 Undo/프리팹 Override를 지원합니다. 노란 원은 추적 감지 반경, 하늘색 선과 원은 충격파 경로와 타격 반지름입니다.
- Shockwave Origin Offset: 몬스터 중심 기준 발사 위치입니다. 땅이나 벽 안에서 생성되지 않도록 조정하세요.
- Shockwave Radius / Speed / Distance: 충격파 타격 반지름(그림도 함께 변경), 속도, 최대 비행 거리입니다. Attack Damage가 각 충격파의 피해량입니다.
- Shockwave Effect Prefab: `Assets/Resources/TriangleMonsterEffects/TriangleShockwave.prefab`을 사용합니다. mon_1에 연결되어 있고, 비어 있어도 이 프리팹을 자동으로 사용합니다. 보스의 이펙트/애니메이션과는 별개입니다.
- Release After Animation: 기본 ON. Attack 애니메이션이 끝난 뒤 한 번만 발사합니다. Animation Event를 추가할 필요 없습니다.
- Attack Timing Mode: Release After Animation을 끈 경우에만 사용합니다. NormalizedTime은 진행 비율, AnimationFrame은 공격 클립 프레임 번호로 발사 시점을 정합니다.
- Hit Normalized Time: 0은 시작, 0.5는 중간, 1은 끝입니다. Release After Animation을 끈 경우에만 적용합니다.
- Hit Frame: AnimationFrame 모드에서 사용합니다. Animation 창에서 Attack 클립의 타격 자세가 나오는 프레임 번호를 입력하세요(0부터 시작). 예를 들어 12 FPS 클립의 3번 프레임은 클립 시간 0.25초입니다. 클립 끝을 넘는 값은 끝 시점으로 제한합니다. 재생 속도를 바꿔도 지정한 프레임에 맞춰 판정합니다.
- 두 방식 모두 공격당 한 번만 판정하고, 타격 전에 피격/사망하면 공격이 취소됩니다. Animation Event는 필요 없습니다. Attack Cooldown은 다음 공격까지의 간격이며 타격 프레임 설정과는 별개입니다.
- 플레이 중 Inspector에서 조절한 값은 플레이 종료 시 되돌아갑니다. 값을 기억한 뒤 플레이를 종료하고 mon_1 프리팹에 저장하세요.
- Blocking Layers: 벽/장애물 검사 레이어입니다. 기존 EnemyNavigation/InteractionMotion을 사용합니다.
- Destroy After Death: 켜면 사망 클립이 끝난 뒤 제거하고, 끄면 마지막 프레임을 유지합니다. 사망 즉시 충돌과 공격은 중단합니다.
- Fallback Duration: Animator나 클립이 누락되었을 때만 쓰는 임시 시간입니다. 정상 연결된 클립은 실제 재생 진행률로 판단합니다.

플레이어 공격의 ICombatDamageable 및 기존 가드/패링/반사 피해 흐름과 연결됩니다. EnemyController나 MeleeEnemy를 추가로 붙이지 마세요. 공통 전투/길찾기 코드는 기존 Assets/Scripts 파일을 참조하므로 이 폴더만 별도 프로젝트로 옮기면 해당 공통 코드도 필요합니다.

## 확인 방법

몬스터가 발사한 일반 투사체와 충격파는 다른 적/보스/훈련용 적을 피해 없이 통과합니다. 벽과 사물 충돌은 유지합니다. 플레이어의 일반 공격 및 패링으로 플레이어 소유가 된 투사체는 몬스터에게 피해를 줍니다. 벽에 튕기는 표면 반사만으로는 플레이어 소유가 되지 않습니다. 이 규칙은 공통 ReflectProjectile을 사용하는 공격에 적용됩니다.

충격파 그림은 `img/Effect/earth-left-01.png`부터 `11.png`까지 순서대로 사용합니다. `Animations/TriangleShockwave_Fly.anim`은 12 FPS 반복 재생이며, `TriangleShockwave.controller`의 Fly 상태에 연결되어 있습니다. 프리팹을 열고 Animation 창에서 SpriteRenderer.Sprite 키를 직접 편집할 수 있습니다. 원본이 왼쪽을 향하므로 이펙트의 Sprite Forward Angle은 180도로 설정되어 있으며, 발사/패링 방향에 맞춰 회전합니다. 원본 PNG와 임포트 설정은 변경하지 않았습니다.

Play에서 접근/이탈, 공격 종료 시 네 방향 한 번 발사, 공격 준비 중 피격 취소, 충격파 피격/가드/패링을 확인하세요. 패링은 **플레이어에게 충격파가 닿는 순간** 우클릭하는 방식입니다. 성공하면 플레이어 피해 없이 충격파가 발사자에게 돌아가 실제 충돌 시 몬스터에게 피해를 줍니다(발사 순간 패링이 아닙니다). 패링 자체로 몬스터를 즉시 경직시키지 않고, 돌아간 충격파가 맞으면 기존 Hit 피격 동작이 나옵니다. 플레이어의 일반 공격도 기존 ICombatDamageable 경로로 몬스터에게 피해를 줍니다. 체력 0이면 남은 충격파와 충돌이 해제되고 Death 클립 뒤 제거됩니다.
