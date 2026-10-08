# Triangle 근접 몬스터

Triangle.prefab은 Assets/Scenes/pco.unity의 Triangle 전용 몬스터 프리팹입니다. 다른 씬의 Triangle에는 적용하지 않습니다. Enemy 레이어, Rigidbody2D, BoxCollider2D, Animator, TriangleMeleeMonster, EnemyStagger를 사용합니다.

pco 씬의 Triangle에 설정이 필요하면 해당 오브젝트를 선택하고 **Tools > CheolO > Monster > Setup Selected Triangle**을 한 번 누른 뒤 씬을 저장하세요. 이 작업은 Undo할 수 있습니다. 잡고 미는 MovableInteractable은 제거됩니다.

## 이미지가 준비되면

1. 이 폴더 안에 Sprites 폴더를 만들고 이미지를 넣습니다. Texture Type은 Sprite (2D and UI), 스프라이트 시트는 Sprite Mode를 Multiple로 설정하고 Sprite Editor에서 Slice/Apply합니다.
2. Triangle을 선택하고 Window > Animation > Animation을 엽니다.
3. 아래 클립을 선택하고 기존 임시 Sprite 키 2개를 삭제한 뒤, 해당 방향의 스프라이트들을 순서대로 타임라인에 드래그합니다. SpriteRenderer.Sprite 트랙만 편집하면 됩니다.
4. Samples(FPS)로 재생 속도를 맞춥니다. Idle/Move는 Loop Time ON, Attack/Hit/Death는 OFF로 유지합니다.

| 동작 | 좌우 공용 | 위 | 아래 |
|---|---|---|---|
| 대기 | Idle | Idle_Up | Idle_Down |
| 이동 | Move | Move_Up | Move_Down |
| 공격 | Attack | Attack_Up | Attack_Down |
| 피격 | Hit | Hit_Up | Hit_Down |
| 죽음 | Death | Death_Up | Death_Down |

모든 클립은 Animations 폴더에 있고, 현재는 삼각형 이미지 두 키만 들어 있는 자리표시자입니다. 실제 걷기/공격/죽음 그림은 아직 없습니다. 좌우는 방향 접미사 없는 클립(Idle, Move, Attack, Hit, Death)을 공유합니다. 오른쪽을 바라보는 이미지를 공용 클립에 넣으면 왼쪽은 SpriteRenderer.flipX로 자동 반전됩니다. 좌우 전환만으로 재생이 처음부터 시작되지 않습니다. 위/아래로 향할 때는 반전이 해제됩니다. 공격 판정은 실제로 향하는 좌우 방향을 따릅니다.

Animator 전이는 스크립트에서 직접 선택합니다. 상태 이름을 변경하거나 별도의 자동 전이를 추가하지 마세요. 정지할 때 마지막 방향을 유지하고 공격 중에는 방향을 고정합니다. Hit는 공격을 중단하며 Death가 최우선입니다.

## Inspector 설정

- Player: 비우면 PlayerMove를 자동으로 찾습니다. Detection Range 안에서 추적하며 범위를 벗어나면 대기합니다.
- Max Health / Move Speed / Attack Damage / Attack Cooldown: 체력, 속도, 피해량, 공격 후 대기 시간입니다.
- Stop Distance: 플레이어 중심까지 접근한 뒤 멈추는 거리(기본 0.8)입니다. 작게 하면 더 가까이 접근합니다. 실제 공격 박스가 플레이어 Collider에 닿지 않으면 더 접근합니다.
- Triangle을 선택하고 Scene의 Gizmos를 켜면 청록색 원은 정지 거리, 노란 원은 감지 거리, 빨간 사각형은 실제 공격 범위입니다.
- Attack Reach / Attack Size: 실제 타격 중심과 범위입니다. 공격 준비 중 플레이어가 범위 밖으로 피하면 빗나갈 수 있습니다.
- Hit Normalized Time: 공격 클립에서 피해가 발생할 비율(기본 0.6). 애니메이션 이벤트를 넣을 필요가 없습니다. 공격당 한 번만 피해를 줍니다.
- Blocking Layers: 벽/장애물 검사 레이어입니다. 기존 EnemyNavigation/InteractionMotion을 사용합니다.
- Destroy After Death: 켜면 사망 클립이 끝난 뒤 제거하고, 끄면 마지막 프레임을 유지합니다. 사망 즉시 충돌과 공격은 중단합니다.
- Fallback Duration: Animator나 클립이 누락되었을 때만 쓰는 임시 시간입니다. 정상 연결된 클립은 실제 재생 진행률로 판단합니다.

플레이어 공격의 ICombatDamageable 및 기존 가드/패링/반사 피해 흐름과 연결됩니다. EnemyController나 MeleeEnemy를 추가로 붙이지 마세요. 공통 전투/길찾기 코드는 기존 Assets/Scripts 파일을 참조하므로 이 폴더만 별도 프로젝트로 옮기면 해당 공통 코드도 필요합니다.

## 확인 방법

Play에서 접근/이탈, 상하좌우 이동, 공격 한 번당 피해 한 번, 공격 준비 중 피격 취소, 패링 경직, 체력 0 이후 충돌 해제와 사망 클립 종료 후 제거를 확인하세요. 이미지를 넣기 전에는 Animator 창의 활성 상태로 동작 전환을 확인할 수 있습니다.
