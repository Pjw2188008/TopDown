# 플레이어 스크립트

## Attachable — 오브젝트에 부착
- PlayerMove.cs: 플레이어 루트. Inspector 설정과 전체 실행 순서. 기존 컴포넌트를 그대로 사용합니다.
- CameraFollow.cs: 카메라. 플레이어 즉시 추적과 구역별 화면 경계를 처리합니다.
- CameraBoundsArea.cs: 맵 구역의 빈 오브젝트. 카메라가 보여줄 수 있는 사각형 범위를 지정합니다.

## Internal — 직접 부착하지 않음
PlayerMove.* 파일은 PlayerMove 하나를 나눈 partial 구현이며 같은 필드와 상태를 공유합니다.
- Movement: 이동·조준·방향 애니메이션.
- Physics: Rigidbody2D와 몸 충돌 이동.
- Combat: 공격·타격·피해·이펙트·기즈모.
- Guard / Parry: 가드 게이지, 방어와 패링.
- Dash / RunAfterimages: 대시·스태미나·달리기 잔상.
- Interaction / InteractionPositioning: F 상호작용과 물체를 옮기는 위치.
- Editing: 편집 모드.
- ErrorInput / ErrorInventory / ErrorInteractions: 오류 슬롯 입력·획득·교체·환경 Paste.
- CombatErrors: 전투 오류 적용.
- ErrorCodex: 발견·도감 입력·저장·UI.
- Hud: 임시 HUD 표시.
- InstantDeath: 레이저 즉사 후 동작 정리, 최초 생성 위치(또는 Instant Death Respawn Point)로 복귀, HP 회복. 일반 피해 처리와 별개입니다.

오류 규칙 모델 자체는 ../Errors, 옮길 수 있는 물체 컴포넌트는 ../Interaction에 있습니다.

## 구역별 카메라 설정

1. Main Camera의 기존 CameraFollow에서 Player를 연결하고 Use Area Bounds를 켭니다(기본 켜짐). Orthographic 카메라로 사용합니다.
2. Hierarchy 우클릭 → Story → Camera Bounds Area로 구역 오브젝트를 만듭니다. 또는 빈 오브젝트에 CameraBoundsArea를 붙입니다.
3. 구역을 선택하고 Scene 뷰의 사각형 핸들을 드래그해 맵 끝에 맞춥니다. Center/Size를 입력하거나 Transform 위치/스케일로도 조절할 수 있습니다. Scene의 Gizmos를 켜세요.
4. 구역 수만큼 복제합니다. 플레이어 중심이 들어간 구역을 자동 선택합니다. 카메라에 구역 목록을 일일이 연결하거나 Collider를 붙일 필요가 없습니다.
5. 시작 위치가 구역 밖이면 CameraFollow의 Default Area를 지정할 수 있습니다.

- 화면 가장자리를 기준으로 제한하므로 끝에 접근하면 플레이어만 움직이고 카메라는 멈춥니다. 플레이어가 항상 화면 중앙에 있는 것은 아닙니다.
- 다음 구역에 진입하면 즉시 경계가 전환됩니다. 겹치면 높은 Priority 우선, 같은 값에서는 현재 구역 유지, 현재 구역이 없으면 먼저 등록된 구역을 사용합니다.
- Keep Last Area Outside는 기본 켜짐입니다. 구역 사이 빈 공간에서는 마지막 경계를 유지합니다. 끄면 Default Area로 돌아가며 그것도 없으면 자유 추적합니다.
- 구역이 전혀 없고 Default Area도 없으면 기존 자유 추적입니다. 맵의 구획은 자동 추측하지 않으므로 직접 구역을 배치해야 합니다.
- Fit Small Areas는 기본 켜짐입니다. 구역이 화면보다 작으면 Orthographic Size를 줄여 화면 전체를 넣고, 큰 구역으로 가면 원래 Size로 돌아옵니다. 끄면 작은 축의 중심에 고정하지만 화면 일부가 경계 밖에 보일 수 있습니다.
- 화면 비율/해상도/Size 변경을 매 프레임 반영합니다. 외부에서 바꾼 Size는 새로운 기본 줌으로 사용합니다.
- 경계는 월드 XY축 사각형이며 구역 회전/다각형은 지원하지 않습니다. 카메라는 XY 평면을 보는 Orthographic이어야 하며 Z축 회전은 지원합니다.
- 이 기능은 카메라만 제한합니다. 플레이어가 맵 밖으로 나가지 못하게 하려면 별도의 벽 Collider가 필요합니다.
- Internal/CameraBoundsMath.cs: 화면 반경을 고려한 경계/줌 계산. 직접 부착하지 않습니다.
- Internal/Editor/CameraBoundsAreaEditor.cs: 구역 생성 메뉴와 Scene 편집 핸들. Editor 전용이며 빌드에는 포함되지 않습니다.
