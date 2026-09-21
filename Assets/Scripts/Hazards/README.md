# 즉사 레이저 — 지정한 BoxCollider2D로 판정

## 새 레이저 배치

1. Hierarchy 우클릭 → Story → Instant Death Laser로 생성합니다. LineRenderer는 사용하지 않습니다.
2. 같은 오브젝트의 **SpriteRenderer → Sprite**에 빔 이미지를 넣습니다. 이미지 Import Settings는 Sprite (2D and UI)여야 합니다.
3. **Transform → Scale X/Y**로 이미지 길이/두께를 직접 조절합니다. 세로 빔 이미지라면 Y가 길이, X가 두께입니다. **Z Rotation**으로 방향을 바꿉니다.
4. SpriteRenderer의 Color, Material, Flip X/Y, Sorting Layer, Order in Layer는 직접 설정합니다. 스크립트가 매 프레임 덮어쓰지 않습니다.
5. Draw Mode가 Sliced/Tiled라면 SpriteRenderer의 Size로 조절할 수도 있습니다. 해당 모드에 맞게 Sprite Editor의 Border/Mesh를 준비하세요.
6. 자식 오브젝트에 SpriteRenderer를 두려면 LaserHazard의 **Beam Renderer**에 연결합니다. 상하 이동을 같이 해야 하므로 자기 자신 또는 자식만 지원합니다.
7. Player는 비우면 활성 PlayerMove를 자동 검색합니다. 차단 사물에는 일반 Collider2D와 MovableInteractable / PasteTarget(Object) / LaserBlocker 중 하나가 필요합니다.
8. 레이저 자신 또는 자식에 **BoxCollider2D**를 직접 추가하고 **Is Trigger**를 켭니다. **Edit Collider / Size / Offset**으로 판정 범위를 조절합니다.
9. 해당 BoxCollider2D 컴포넌트를 **LaserHazard → Beam Collider** 칸에 직접 드래그합니다. 같은 오브젝트에 있어도 연결하지 않으면 사용하지 않습니다.
10. 기존 투사체가 판정 상자에 부딪혀 사라지는 것을 막으려면 판정 오브젝트의 **Layer를 Ignore Raycast**로 지정하세요. 코드는 Layer를 변경하지 않습니다.

이미지는 편집 화면에서 바로 보이며 별도의 실행 중 표시 오브젝트를 생성하지 않습니다.
빔 이미지와 판정 범위는 별개입니다. 이미지가 없거나 SpriteRenderer를 꺼도 연결한 판정 콜라이더가 유효하면 즉사합니다. 판정을 끄려면 LaserHazard 또는 연결한 Collider를 끄세요.

권장 구성: Laser 루트에는 LaserHazard, 자식 Visual에는 SpriteRenderer, 자식 Hitbox에는 BoxCollider2D(Is Trigger, Ignore Raycast). 각각 Beam Renderer / Beam Collider에 연결하면 이미지와 판정을 독립적으로 조절할 수 있습니다. 부모 Transform Scale을 바꾸면 Unity 특성상 자식 이미지와 콜라이더가 함께 커집니다.

## 기존 레이저 전환

- 기존 오브젝트 선택 → LaserHazard Inspector 아래 **기존 레이저를 SpriteRenderer로 전환**을 누릅니다(Play를 끈 상태).
- 해당 오브젝트의 LineRenderer를 제거합니다. 이전 Beam Sprite가 있으면 편집 가능한 자식 **Beam Sprite**로 옮기고 기존 길이/두께/방향을 가능한 한 보존합니다.
- 이미 SpriteRenderer에 이미지를 직접 설정했다면 그 설정을 덮어쓰지 않습니다.
- 전환은 Undo 가능합니다. 씬/프리팹은 확인 후 저장하세요. 프로젝트의 모든 씬을 자동으로 변경하지 않습니다.
- 기존 Length / Width / End Point / Beam Sprite / Sprite Is Vertical은 전환을 위한 숨김 데이터일 뿐, 새 실행 방식에서는 사용하지 않습니다.
- 새 구조에서 길이는 Transform Scale 또는 SpriteRenderer Size로 조절합니다. 외부 End Point를 따라 자동으로 늘리지 않습니다.
- 전환하지 않은 이전 LineRenderer는 자동으로 삭제하지 않으므로 기존 오브젝트에서 전환 버튼을 한 번 눌러 주세요.
- 이전 자동 판정 상자는 더 이상 생성되지 않습니다. 기존 레이저도 직접 BoxCollider2D를 만들고 Beam Collider에 연결해야 합니다. 전환 버튼은 콜라이더를 생성하거나 연결하지 않습니다.

## 판정과 표시

- **Beam Collider에 연결한 실제 콜라이더만** 사용합니다. 자동 생성/자동 검색/이미지 크기 기반 보정은 하지 않습니다.
- Size, Offset, Edge Radius, 회전/Scale은 Unity의 실제 Collider 모양을 따릅니다. Sprite나 이미지 크기를 바꾸어도 Collider의 Size/Offset을 덮어쓰지 않습니다.
- 콜라이더의 Enabled / Is Trigger / Layer / Transform을 스크립트가 변경하지 않으며, LaserHazard를 제거해도 지정 콜라이더를 삭제하지 않습니다. 루트의 상하 이동은 기존대로 동작합니다.
- 미연결, 비활성, Is Trigger 꺼짐, 외부 오브젝트의 콜라이더, Rigidbody2D의 Simulated 꺼짐은 판정하지 않습니다. 자신 또는 자식에 연결하세요.
- Scene Gizmos의 빨간 사각형이 지정 상자의 범위, 청록색 사각형이 왕복 이동 양 끝입니다. Edge Radius의 둥근 모서리까지 확인하려면 Unity의 Edit Collider를 사용하세요.
- 사물이 빔 어디든 겹치면 **전체 빔을 숨기고 즉사 판정을 끕니다**. 일부 구간만 차단하는 방식은 아닙니다.
- 마지막 사물이 빠지면 Reactivation Delay(기본 0.3초, 게임 시간) 후 다시 표시/활성화합니다.
- Color/Enabled 값을 변경하는 대신 forceRenderingOff로 일시 숨깁니다. 해당 속성은 레이저가 관리하므로 다른 스크립트에서 동시에 제어하지 마세요.
- 기존 회색 안내선/비활성 색상은 사용하지 않습니다. SpriteRenderer의 활성 색상은 그대로 보존합니다.

## 상하 왕복 이동

- Move Vertically는 기본 켜짐입니다. 고정 레이저가 필요하면 끄세요.
- Upper Travel / Lower Travel: Play 시작 위치에서 위/아래 이동 거리. 기본 각각 2입니다.
- Vertical Speed: 초당 이동 거리, 기본 1. Start Moving Up을 끄면 아래로 먼저 움직입니다.
- 월드 Y축 왕복입니다. 빔이 막혀 숨겨진 동안에도 이동은 계속됩니다.
- 편집 모드 슬로모션과 일시 정지를 따릅니다. 속도 0 또는 Move Vertically 해제 시 현재 위치에서 멈춥니다.
- 빠르게 움직이는 빔의 플레이어/사물 통과도 이동 경로로 검사합니다.
- 다른 스크립트나 Animator로 레이저 루트의 Y 위치를 동시에 제어하지 마세요.

## 즉사 / 재시작

- 활성 빔 접촉: 가드/패링/반사/HP 수치와 무관하게 즉사 → 최초 생성 위치로 즉시 복귀 → HP 전부 회복.
- PlayerMove의 Instant Death Respawn Point가 있으면 해당 위치로 돌아갑니다. 반드시 안전한 위치에 두세요.
- 잡은 물체를 놓고 공격/대시/가드/편집 모드/도감을 종료합니다. 보관함·오류 발견 기록·맵 사물 배치는 유지합니다.
- 같은 프레임에는 재즉사/추가 피해를 막습니다. 장시간 무적은 없으므로 시작 지점이 레이저 위면 다음 프레임에 다시 죽을 수 있습니다.
- Trigger Collider, 플레이어, 일반 생명체 적은 차단 사물이 아닙니다. Blocker Layers에 사물 레이어가 포함돼야 합니다.
- LaserBlocker가 있으면 표식 설정이 우선입니다. 표식을 끄면 MovableInteractable/PasteTarget이 있어도 차단하지 않습니다.
- 외부 워프 후에는 PlayerMove.NotifyHazardTeleport()를 호출하여 워프 경로를 빔 통과로 오인하지 않게 합니다.
- Time.timeScale=0에서는 즉사 판정을 하지 않습니다. On Player Killed는 위치/HP 복구 후 호출합니다.
- 기존 일반 전투 피해 처리와 테스트용 HP 회복 옵션은 변경하지 않습니다.

## 파일 역할

- Attachable/LaserHazard.cs: 즉사·차단·재활성화·접촉/이동 경로 판정.
- Attachable/LaserBlocker.cs: 사물 차단 표식.
- Internal/LaserHazard.Visuals.cs: 지정 콜라이더 유효성 확인, SpriteRenderer 임시 숨김, 판정 기즈모. 별도 부착하지 않습니다.
- Internal/LaserHazard.Movement.cs: 상하 왕복 이동과 범위 기즈모. 별도 부착하지 않습니다.
- Internal/Editor/LaserHazardEditor.cs: 생성 메뉴, Inspector 안내, 기존 레이저 전환(Undo 지원).
- ../Player/Internal/PlayerMove.InstantDeath.cs: 즉사 정리와 시작 위치 복귀.
