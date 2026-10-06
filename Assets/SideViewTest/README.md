# 사이드뷰 — 공통 플레이어 기능 테스트

현재 씬 작업을 저장한 뒤 `Scenes/SideViewTest.unity`를 열고 Play → Game 창 클릭.
`SideViewPlayer` 오브젝트에는 이제 **PlayerMove 하나**가 붙습니다.

## 모드 체크

PlayerMove 인스펙터 맨 위의 **사이드뷰 모드**를 Play 전에 설정합니다.

- 켜기: 좌우 이동/공격/가드/패링/대시, 중력/점프, 좌우 밀고 당기기. 중력/최대 낙하 속도/점프 높이/점프 키 설정을 추가 표시합니다.
- 끄기: 기존 WASD 탑다운 이동/방향별 전투. 세로 공격 방향 각도 설정을 표시합니다.
- HP, 대시 스태미나, 가드 게이지, 공격, 오류, 도감, 상호작용, UI 설정은 공통입니다.
- 모드 변경은 카메라/맵/Animator를 자동 변환하지 않습니다. 이 테스트 프리팹에는 사이드뷰 Animator를 연결했습니다. 탑다운에서는 기존 Player Animator/프리팹을 사용하세요.
- 기존 탑다운 프리팹의 체크박스 기본값은 꺼짐이며 수정하지 않았습니다.

중력은 선행 충돌 검사 기반 Kinematic 이동과 통합했습니다. Rigidbody Gravity Scale은 0으로 유지하고 **Side View Gravity**를 조절합니다.
점프 높이는 기본 2 월드 단위이며, 바닥에 있을 때 키를 새로 눌러 1회 점프합니다. 길게 눌러 자동 반복하거나 공중에서 재점프하지 않습니다. 천장에 닿으면 상승을 멈춥니다.
공격/가드/패링/대시/물체 잡기/오류 선택·교체·Paste 준비 중에는 점프를 시작하지 않습니다. 편집 모드에서는 슬로모션 속도로 점프할 수 있습니다.
점프 높이 또는 중력을 0으로 하면 점프하지 않습니다. 점프 키는 기존 기능과 겹치지 않게 지정하세요.

## 카메라

테스트 씬의 `Main Camera`에 공통 `CameraFollow`를 연결했습니다. 좌우 이동과 점프/낙하를 지연 없이 따라가며, 기본 Offset은 (0, 0, -10)으로 플레이어를 화면 중앙에 둡니다.
`Main Camera → Camera Follow → Offset`으로 화면 내 위치를, Camera의 Orthographic Size로 확대 정도를 조절하세요. 기존 Size 6은 유지했습니다.
테스트 씬에서는 `Use Area Bounds`를 껐으므로 맵 끝을 지나도 자유롭게 따라갑니다. 나중에 CameraBoundsArea를 배치하고 이 옵션을 켜면 구역 제한도 재사용할 수 있습니다.
다른 사이드뷰 씬에서는 카메라에 `CameraFollow`를 붙이고 Player에 해당 플레이어 Transform을 연결하면 됩니다. 탑다운 카메라 스크립트/씬은 변경하지 않았습니다.

## 조작

| 입력 | 동작 |
| --- | --- |
| A/D, ←/→ | 좌우 이동, 왼쪽은 반전 |
| W / ↑ 누르기 | 사이드뷰 전용 점프 (인스펙터에서 키/높이 변경 가능) |
| Space 누르기 | 스태미나를 소비하는 좌우 대시 |
| Space 유지 + 이동 | 대시 이후 달리기, 잔상 |
| 좌클릭 | 바라보는 방향 공격; 편집 모드에서는 공격 범위 내 오류 Cut |
| 우클릭 유지 | 가드, 피해 감소; 피격마다 가드 게이지 10 소모 |
| 공격 순간 우클릭 | 패링: 근접 경직 / 투사체 되돌리기 |
| E | 편집 모드, 슬로모션 |
| 1 / 2 | 일반 모드: 해당 슬롯 오류를 전투 기술에 적용 |
| 편집 모드에서 1 / 2 → 좌클릭 | 환경 Paste. 같은 번호 재입력은 취소, 보관함 유지 |
| 휠 → 좌클릭 | 보관함 2칸이 꽉 찼을 때 교체 선택/확정 |
| B | 발견한 오류 도감 열기/닫기 |
| F | 가까운 상자 잡기/놓기, A/D로 밀고 당기기 |

일반 공격은 마지막 이동 방향, 편집 모드의 Cut 방향은 커서의 좌우를 사용합니다. 환경 Paste는 커서로 대상을 직접 지정합니다.
공격/가드/패링 중 이동은 잠깁니다. 공격은 마지막에서 세 번째 프레임에 판정과 이펙트를 생성합니다.
상호작용 중에는 물체 반대편으로 상하 우회하지 않고 잡은 위치에서 좌우로 밀거나 당깁니다. 낙하하면 물체를 놓습니다.

## 테스트 배치

- 기존 `SideView Target`: 공격 시 빨간 피격 표시와 HP 감소, 자동 회복.
- `Giant Source`, `Acceleration Source`, `Reflection Source`: 오류 원본 3종. 발견/Cut/2칸 교체 시험.
- `Moving Paste Target`: 움직이는 물체에 오류 적용 시험. 가속 시 이동 속도 변화.
- `Movable Paste Box`: F로 옮기는 물체 겸 환경 Paste 대상. 바닥 피벗이라 거대화 시 위로 확장.
- `Right Wall`: Surface 분류, 반사 Paste 가능.
- `Projectile Practice`: 4월드 단위 안에 접근하면 발사하는 공통 허수아비. 가드/패링/피격 확인용.
- 레이저 즉사/리스폰은 공통 PlayerMove API로 기존 기믹과 연결할 수 있습니다. 테스트 씬에 레이저는 추가하지 않았습니다.

도감은 이번 테스트 프리팹에서 저장하지 않고 매 실행 빈 상태로 시작합니다. 탑다운의 기존 기록을 덮어쓰지 않습니다.
현재 구현을 재사용하며, 기획서에만 있는 먹물 비용/새 튜토리얼은 추가하지 않았습니다.

## 애니메이션과 코드

루트 선택 → Ctrl+6으로 `Animations`의 Sprite 트랙을 편집합니다. Transform 키는 사용하지 마세요.
상태: Player_Idle / Move_Right / Jump_Right / Attack_Right / Guard_Right / Parry_Right / Interact_Idle_Right / Interact_Move_Right.
점프 클립은 `Animations/SideView_Jump_Right.anim`입니다. 플레이어 루트를 선택하고 Ctrl+6 → 해당 클립 → 기존 `Sprite Renderer → Sprite` 트랙의 임시 프레임을 직접 교체하세요. 스프라이트 시트는 Sprite (2D and UI)/Multiple로 잘라진 개별 Sprite 프레임을 사용합니다.
오른쪽 점프 프레임만 넣으면 왼쪽은 Flip X로 반전합니다. 점프/낙하 중 한 번 재생하고 마지막 그림을 유지하며, 착지하면 대기/이동으로 즉시 복귀합니다. 별도 낙하 클립은 없습니다.
공격/가드/패링/상호작용 애니메이션을 점프가 끊지 않습니다. 공중 공격이 끝나면 다시 점프 상태로 돌아갑니다. Jump_Right는 코드로 진입/종료하므로 Any State 전환이나 Exit Time 전환을 추가할 필요가 없습니다. Loop Time은 꺼두세요.
기존 프레임을 별도 클립으로 복사했습니다. 오른쪽 클립만 쓰며 왼쪽은 반전합니다. 원본 PNG는 공유합니다.
공통 구현은 `Assets/Scripts/Player`, 연결부는 `PlayerMove.SideView.cs`, 인스펙터는 `Internal/Editor/PlayerMoveEditor.cs`입니다.
초기 `SideViewPlayer.cs`와 `SideViewPlayer.Attack.cs`는 이전 미니 프로토타입용으로 보존했지만 현재 프리팹에는 붙이지 않습니다. PlayerMove와 동시에 붙이지 마세요.
`SideViewPracticeRoom`은 허수아비 연습 연결, `SideViewTargetDummy`는 연습 표적입니다.

기존 타이틀 시작 씬, Build Settings, 프로젝트 입력/물리 설정은 변경하지 않았습니다.
