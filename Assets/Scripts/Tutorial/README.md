# 이동 / 편집 모드 / Cut 튜토리얼

## 이동 다음에 편집 모드와 Cut 안내 추가

1. Play를 끄고 기존 **Movement Tutorial**을 선택합니다.
2. Inspector의 **편집 모드 / Cut 안내 3단계 추가**를 한 번 누릅니다.
3. 처음 생성한 기본 '이동 연습 완료 / 다음 안내를 설정하세요' 대기 문구는 교체하고, 기존 이동 단계 뒤에 아래 세 단계를 붙입니다. 직접 작성한 문구/이벤트는 삭제하지 않습니다. 사용자 정의 대기 단계가 남아 있다면 원하는 순서로 옮기거나 직접 정리하세요.
4. 기존 Canvas UI와 위치/폰트/색상은 그대로 사용합니다. 길어진 설명이 잘리면 Message 영역이나 패널 높이를 직접 늘려 주세요.
5. 이동 목적지 근처에 **Cut 가능한 오류 원본**을 직접 배치합니다. 예: Collider2D와 GiantErrorEffect(Trigger On Start 켜짐)가 있는 사물. 이 버튼은 오류 사물이나 보관함을 자동 생성/초기화하지 않습니다.

| 단계 | 완료 조건 | 기본 안내 |
| --- | --- | --- |
| 편집 모드 | EditModeEnabled | E로 편집 모드 켜기 |
| Cut | CutSucceeded | 오류 원본에 가까이 가서 조준 후 좌클릭 |
| 보관함 설명 | Confirm | 실제 Cut한 오류 이름과 보관함 저장/원본 제거 설명, Enter 확인 |

- 각 단계의 Message는 자유롭게 수정합니다. **{error}**는 이 튜토리얼에서 성공한 Cut의 오류 이름으로 치환됩니다. 두 오류를 함께 Cut했다면 두 이름이 표시됩니다.
- **Minimum Display Seconds**는 안내를 읽을 최소 실제 시간입니다. 기본 1초 / 0.5초 / 2초이며 편집 모드 슬로모션과 무관합니다. 일시 정지/도감/즉사 프레임에서는 시간이 흐르지 않습니다.
- 마지막 안내는 자동으로 사라지지 않습니다. 최소 시간 후 Enter(또는 숫자패드 Enter)를 눌러야 완료됩니다. 확인 키는 게임 조작을 바꾸지 않으며 패널 클릭을 요구하지 않습니다.
- 편집 모드가 이미 켜져 있으면 첫 안내를 최소 시간만큼 보여준 뒤 진행합니다. 편집 안내 중 이미 Cut에 성공했어도 다음 Cut 단계에서 성공을 놓치지 않습니다.
- 과거에 가지고 있던 오류나 튜토리얼 진입 전 Cut은 성공 조건이 아닙니다. 실제 Cut 저장이 성공해야 합니다. 실패/헛클릭/교체 창 열기/교체 취소/Paste/도감 발견은 진행시키지 않습니다.
- 보관함이 가득 찼다면 교체를 확정해 실제 저장한 시점에 성공합니다. 같은 종류를 이미 보유하거나 Paste된 오류는 기존 규칙상 Cut할 수 없습니다. 첫 교육 구간에는 가져올 수 있는 원본을 준비하세요.
- Cut 이후 자동으로 Paste하거나 입력을 잠그지 않습니다. 이번 교육은 Cut과 보관함 설명까지만입니다.
- 안내가 편집 모드 어두운 화면 뒤에 있으면 Tutorial Canvas의 **Sort Order를 150 이상**으로 직접 설정하세요(기존 편집 오버레이는 100). 사용자가 편집한 Canvas 설정은 자동 변경하지 않습니다.
- 이미 CutSucceeded 단계가 있으면 추가 버튼을 다시 눌러도 중복 추가하지 않습니다. 기존 단계 자체를 수정하세요. 추가 작업은 Undo 가능합니다.

## 단계 완료 조건

### 어떤 대상을 Cut해야 하는지 지정하기

1. Movement Tutorial → Steps에서 **Completion Condition = CutSucceeded**인 단계를 펼칩니다.
2. **Cut Target**에 Hierarchy의 오류 원본 오브젝트를 드래그합니다. 자식에 오류가 있다면 그 부모를 연결해도 됩니다. 모든 오류를 담은 맵 전체 루트보다는 실제 교육용 사물 하나를 지정하세요.
3. **Cut Target Display Name**에 플레이어용 이름을 적습니다(예: 거대한 상자). 비우면 오브젝트 이름을 사용합니다.
4. Message 예: **{target}에 가까이 가서 조준하세요. / 편집 모드에서 좌클릭해 Cut하세요.** 기존 문구에 {target}이 없어도 대상이 지정돼 있으면 'Cut 대상: 이름'을 앞에 붙입니다.
5. 기존 **Destination Marker** UI가 대상 위를 따라가며 Text라면 'Cut 대상 ↓'를 표시합니다. Show Destination Label / Follow Destination을 켜세요. Image 표식도 사용 가능하며 이미지 모양은 바꾸지 않습니다.
6. 위치를 정확히 지정하려면 대상 위에 빈 자식을 만들고 **Cut Target Marker Anchor**에 연결하세요. 비워두면 대상 Renderer/Collider 위쪽, 둘 다 없으면 Transform 위치를 사용합니다. 기존 Destination Offset도 적용됩니다.

- 다른 오브젝트의 같은 종류 오류를 Cut해도 이 단계는 완료되지 않습니다. 선택한 오브젝트 또는 자식에서 실제 저장이 성공해야 합니다.
- Cut Target을 비워두면 이전처럼 어떤 원본의 Cut도 인정합니다. 기존 씬에는 대상을 자동 지정하지 않습니다.
- 실패/교체 취소/보관함의 기존 오류는 인정하지 않습니다. 대상 Cut 후 최소 표시 시간이 끝나기 전에 다른 오류를 Cut해도 지정 대상의 저장 기록/이름으로 설명합니다.
- Cut 단계 진입 전에 이미 원본을 제거했다면 다시 가져올 수 없으므로 교육용 원본을 준비한 상태에서 시작하세요. 바로 앞 편집 모드 안내 중 먼저 Cut한 경우는 기존대로 인정합니다.
- 대상이 Cut 후 제거돼도 단계 진입 시 기억한 대상 ID로 성공을 판정합니다. Cut하지 않고 대상만 파괴/비활성화하면 완료되지 않습니다. 대상 참조가 없어지면 표식은 숨깁니다.
- 표식은 화면 안의 대상만 따라갑니다. 카메라 밖의 방향 안내나 벽 너머 강조는 추가하지 않았습니다.
- 패널 배치/텍스트 폰트·색상은 유지합니다. Cut 단계에서만 표식 Text 문구를 임시 변경하며 다른 단계에서는 원래 문구로 복구합니다.

### 조건 종류

- **ArrivalArea (기본값)**: 기존 구역 도착. Arrival Area 미연결이면 대기합니다.
- **EditModeEnabled**: 지정한 플레이어가 편집 모드일 때 진행합니다.
- **CutSucceeded**: 해당 단계 진입 후(바로 앞이 편집 모드 안내라면 그 안내 진입 후) 실제 Cut 저장 성공을 기다립니다.
- **Manual**: CompleteCurrentStep 외부 호출로만 진행합니다.
- **Confirm**: 최소 표시 시간 후 Enter로 진행합니다.
- Arrival Area와 목표 표식은 ArrivalArea 조건에서만 사용합니다. 기능 안내 단계에는 도착 구역이 필요 없습니다.

## 빠른 배치

1. Play를 끈 상태에서 Hierarchy 우클릭 → **Story → Movement Tutorial**을 선택합니다.
2. Movement Tutorial 오브젝트에 TutorialSequence가 추가되고, 자식 Canvas UI와 Arrival Area의 BoxCollider2D(Is Trigger)가 만들어지고 구역이 첫 단계에 연결됩니다. 기존 씬은 자동으로 수정하지 않으며 이 메뉴를 실행할 때만 생성합니다.
3. **Arrival Area - Move Here**를 원하는 위치로 옮기고 **Edit Collider / Size / Offset**으로 도착 범위를 조절합니다. 기본 위치는 현재 플레이어의 위쪽 4, 구역 크기는 3×2입니다. 실제 맵에서 갈 수 있는 곳으로 옮겨 주세요.
4. TutorialSequence의 **Player**를 연결합니다. 비워두면 활성 PlayerMove를 검색합니다.
5. Play하면 첫 안내를 표시하고 플레이어 중심이 도착 구역에 들어가면 두 번째 안내로 넘어갑니다.

## 안내 / 다음 단계 편집

- **Steps → Element 0 → Message**: WASD 이동, Space 유지 달리기, 목표 도착 안내가 기본입니다.
- **Steps → Element 1 → Message**: 다음 튜토리얼 문구를 넣습니다. 현재는 '이동 연습 완료 / 다음 안내를 설정하세요'라는 임시 문구입니다. 다음 조작 교육 내용은 아직 정하지 않았습니다.
- 각 단계의 **Arrival Area**에 직접 만든 BoxCollider2D를 연결하면 그 구역 도착 시 다음 단계로 갑니다. 콜라이더를 실행 중 자동 검색/생성/수정하지 않습니다.
- **Arrival Area가 비어 있으면 해당 안내에서 대기**합니다. 다른 시스템의 UnityEvent에서 TutorialSequence.CompleteCurrentStep을 호출해 진행할 수도 있습니다.
- **On Entered**는 해당 단계 진입 때 한 번 실행됩니다. 다음 기믹/적 오브젝트 활성화 등에 연결하세요. 이 이벤트에서 자기 자신의 단계 전환을 재귀 호출하지 마세요.
- 마지막 단계 완료 시 안내를 숨기고 **On Completed**를 한 번 호출합니다. 다음 별도 튜토리얼의 BeginTutorial 등에 연결할 수 있습니다.
- 첫 구역 도착 후 바로 안내를 끝내고 외부 튜토리얼을 시작하려면 Steps를 1개로 줄이고 On Completed를 연결하세요.
- Begin On Start는 기본 켜짐입니다. BeginTutorial은 처음부터 다시 시작, StopTutorial은 중단합니다. Play마다 처음부터 시작하며 PlayerPrefs/저장 파일은 사용하지 않습니다.

## 판정 규칙

- 지정한 플레이어의 **Transform 중심**이 현재 단계 구역에 도착하는 것으로 완료합니다. 꼭 달리기를 사용해야 하는 조건은 아닙니다.
- Is Trigger가 켜진 활성 구역만 검사합니다. 적/사물/다른 단계의 구역은 진행 조건이 아닙니다.
- 대시로 좁은 구역을 프레임 사이에 지나쳐도 선분 검사를 통해 완료합니다. 레이저 리스폰/NotifyHazardTeleport로 알린 워프 경로는 제외합니다.
- 처음부터 구역 안에 있으면 바로 완료합니다. 출발 위치와 목표 구역을 겹치지 않게 배치하세요. 연속 단계가 같은 영역이면 다음 프레임에 다음 단계도 완료될 수 있습니다.
- 한 프레임에 최대 한 단계만 완료합니다. 도감 등 Time.timeScale=0 상태 및 즉사 프레임에서는 진행하지 않습니다.
- 진행 중 컴포넌트를 껐다 켜면 현재 단계에서 재개합니다. 처음부터 하려면 BeginTutorial을 호출하세요.
- 입력/Animator/속도는 유지합니다. PlayerMove에는 읽기 전용 편집 모드 상태와 Cut 성공 기록만 추가했습니다. 현재 Space는 첫 입력에 대시, 누른 채 유지하면 달리기로 이어지는 기존 동작입니다.

## 직접 편집하는 Canvas UI

기존 튜토리얼은 Play를 끄고 Movement Tutorial 선택 → Inspector 아래 **편집 가능한 UI 생성/연결**을 한 번 누르세요. 기존 IMGUI는 없어졌으므로 UI가 연결되지 않으면 안내는 표시되지 않습니다. 진행 설정과 도착 구역은 유지됩니다.

새로 Story → Movement Tutorial을 생성하면 Canvas UI도 함께 연결됩니다. 생성은 Undo 가능하며 사용자가 편집한 기존 패널을 다시 생성하거나 덮어쓰지 않습니다.

- **Tutorial Canvas / Instruction Panel**: Rect Tool(T) 또는 RectTransform의 Anchors / Pos X,Y / Width,Height로 배치합니다. 기본값은 오른쪽 위이며, 플레이 중 위치나 크기를 강제로 고정하지 않습니다. 이동 후 HP/보관함과 겹치는지는 직접 확인하세요.
- **Instruction Panel → Image**: 배경 색상/투명도/Source Image를 바꿉니다.
- **Title → Text**: 단계 번호 제목의 글꼴/크기/색상/정렬을 바꿉니다.
- **Message → Text**: 안내 본문의 글꼴/크기/색상/줄바꿈을 바꿉니다. 실제 문구는 Steps → Message를 수정합니다. 플레이 중에는 Text 내용만 현재 단계의 문구로 갱신됩니다.
- **Destination Marker → Text**: 목표 표식의 문구/색상/폰트/크기를 직접 바꿉니다. Text 대신 Image로 바꿔도 RectTransform 연결을 유지하면 됩니다.
- **Follow Destination**: 켜면 목표 표식의 위치만 구역을 따라갑니다. Destination Offset으로 간격을 조절합니다. 끄면 표식의 위치도 RectTransform에서 수동 조절합니다. 이 옵션은 안내 패널과 무관합니다.
- **Canvas Scaler**: 기본 1280×720, Scale With Screen Size / Expand입니다. 원하는 화면 대응 방식으로 바꿀 수 있습니다.
- 한글이 네모로 나오면 **Title / Message / Destination Marker의 Text → Font**에 한글 지원 Font를 연결하세요. 기본은 Unity 내장 폰트입니다.
- 사용하는 텍스트 컴포넌트는 **UI Text(Legacy)**이며 TMP가 아닙니다. 직접 만든 패널도 Instruction Panel / Message Text / Title Text / Destination Marker 슬롯에 연결할 수 있습니다.
- 패널은 튜토리얼 관리자 자신이나 부모가 아닌 별도 UI 오브젝트를 연결하세요. 코드는 안내 패널과 목표 표식의 표시 여부만 관리합니다.
- 기본 UI는 Raycast Target을 꺼서 클릭/공격을 가로채지 않습니다. 안내를 위해 EventSystem을 새로 만들지 않습니다.
- 편집은 Play를 끈 상태에서 하고 씬/프리팹을 저장하세요. 단계 진입/완료, Show Instructions, 일시 정지에 따라 UI 표시가 바뀝니다.
- 긴 문구는 패널 높이/본문 영역을 늘리세요. 편집 중의 예시 문구와 실제 게임 화면에서 줄바꿈을 확인하세요.
- 과거 Panel Position / Panel Size / Pin Panel To Top Right / Font Size 설정은 최초 Canvas 생성 시에만 이전합니다. 생성 후에는 RectTransform과 Text를 사용합니다.

## 파일 역할

- Attachable/TutorialSequence.cs: 오브젝트에 붙이는 진행 관리자, 단계/목표/이벤트 설정.
- Internal/TutorialSequence.UI.cs: 동일 컴포넌트의 Canvas 표시 연결 부분. 별도 부착하지 않습니다.
- Internal/Editor/TutorialSetup.cs: 편집기 생성 메뉴. 별도 부착하지 않습니다.

## 확인

- 시작 즉시 첫 문구 → 구역 밖에서는 유지 → 지정 구역 도착 시 다음 문구.
- 다음 단계에 구역을 연결하지 않으면 대기 → 연결하면 해당 구역 도착 후 진행.
- 마지막 완료 이벤트는 한 번만 실행, Play 재시작 시 첫 단계부터.
- 기존 이동/달리기/대시/가드/전투/레이저 기능이 변경되지 않았는지 확인.
