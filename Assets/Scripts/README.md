# 스크립트 구조

## 기능별 폴더 안내

```text
Assets/Scripts/
├─ Player/             플레이어 입력·행동·HUD·카메라
│  ├─ Attachable/      PlayerMove, CameraFollow
│  └─ Internal/        PlayerMove.* partial 구현
├─ Errors/             오류 효과·호환성·보관함·도감
│  ├─ Attachable/      오류 효과 3종, PasteTarget
│  └─ Internal/        오류 규칙·모델·인터페이스
├─ Enemies/
│  ├─ Melee/
│  │  ├─ Attachable/   EnemyController, MeleeEnemy
│  │  └─ Internal/     EnemyController.* partial 구현
│  ├─ Ranged/
│  │  ├─ Attachable/   RangedEnemy, RangedEnemySpawner
│  │  └─ Internal/     RangedEnemy.*, ReflectProjectile, Editor 도구
│  └─ Shared/
│     └─ Attachable/   MovingEnemy, EnemyStagger
├─ Interaction/
│  ├─ Attachable/      MovableInteractable
│  └─ Internal/Editor/ 상호작용 Animator 설정 도구
└─ Shared/
   ├─ Attachable/      공용 빨간 피격 표시(DamageBlink)
   └─ Internal/        공용 피해 전달·공격 계산·충돌 이동
```

- **Attachable**: 씬/프리팹에 붙이는 컴포넌트입니다. 모두 필수라는 뜻은 아닙니다. EnemyStagger처럼 다른 컴포넌트가 자동 추가하는 경우도 있습니다.
- **Internal**: 직접 붙이지 않는 partial 구현, 모델, 유틸리티 또는 런타임 자동 생성 컴포넌트입니다. 삭제해도 된다는 뜻은 아닙니다.
- **Internal/Editor**: Unity 편집기 메뉴 도구입니다. Editor 폴더 아래에 두어 빌드에서 제외하며 오브젝트에 부착하지 않습니다.
- PlayerMove.*의 오류 입력·HUD는 플레이어의 일부이므로 Player/Internal에 둡니다. 재사용 가능한 오류 효과와 규칙은 Errors에 둡니다.
- 각 기능 폴더의 README에 파일별 역할과 부착 위치를 정리했습니다.
- 기존 스크립트의 .meta GUID, 클래스명, 직렬화 필드명과 기본값을 유지합니다. 기존 씬/프리팹에 스크립트를 다시 붙이지 마세요.
- 프리팹·애니메이션·스프라이트는 기존 위치를 유지합니다. 이번 정리는 스크립트 및 문서에만 적용합니다.

## 현재 원거리 몬스터 구성

피격 시 플레이어·근접·원거리 몬스터는 공용 DamageBlink로 약 0.35초 빨간색으로 표시된 뒤 원래 색상으로 돌아옵니다. 캐릭터를 숨기지 않습니다. 실제 HP 감소 때만 자동 실행되며 무적 시간은 없습니다. 세부 설정은 Shared/README.md를 참고하세요.

- `Enemies/Ranged/Attachable/RangedEnemy.cs`: 이동, 공격 애니메이션, 주기적 발사와 체력. 원거리 적 루트에 부착합니다.
- `Enemies/Ranged/Attachable/RangedEnemySpawner.cs`: 플레이어가 감지 범위에 진입하면 적 프리팹을 생성합니다.
- `Enemies/Ranged/Internal/ReflectProjectile.cs`: 탄환 한 발의 이동·충돌·피해·패링/반사. 적이 자동 추가하므로 직접 부착하지 않습니다.
- 씬에는 `Assets/RangedEnemy/RangedEnemySpawner.prefab`을 배치합니다. 세부 설정은 `Assets/RangedEnemy/README.md`를 확인하세요.
- 이전 테스트용 발사 컴포넌트와 씬 연결은 제거했습니다. `REDACT`의 `Reflection Error Test Source`는 반사·거대화 오류 원본만 남긴 정적 테스트 오브젝트이며 공격하지 않습니다.

## 플레이어 Rigidbody2D / 투사체 충돌

- ReflectProjectile의 충돌 검색은 다른 ReflectProjectile(자식 Collider 포함)을 제외합니다. 서로 지나가며, 그 뒤의 벽/플레이어/적 타격과 반사/패링은 계속 검사합니다.
- Player 프리팹에 Rigidbody2D와 BoxCollider2D를 추가했습니다. 씬에 따로 만든 PlayerMove도 실행 시 누락된 몸 Collider와 Rigidbody2D를 준비합니다. 기존 Collider 크기/오프셋은 유지하며 몸 Collider는 Trigger를 끕니다.
- Rigidbody2D는 Kinematic, Gravity Scale 0, Freeze Rotation, Continuous, Interpolate None입니다. 플레이어를 물리 반동으로 밀지 않고, 이동 전 충돌 검사를 한 뒤 Rigidbody 위치를 갱신합니다. 코드가 실행 시 이 설정을 보장하므로 Dynamic으로 변경하지 마세요.
- 걷기/달리기는 Movement Blocking Layers에서 벽을 검사하며 약 0.02의 접촉 여유를 유지합니다. 벽으로 대각선 이동하면 열린 축으로 미끄러집니다. 대쉬와 상호작용은 기존 전용 충돌 검사를 유지하되 같은 Rigidbody 위치 적용 함수를 사용합니다.
- PlayerMove.Physics.cs는 별도 부착하지 않습니다. 이 구조에는 외력 기반 넉백을 추가하지 않았습니다. 기존 근접 적의 플레이어 몸 충돌 제외 설정은 유지됩니다.

## 2026-09-16 리팩토링

- 씬에 붙이는 `EnemyController.cs`의 GUID와 Inspector 필드는 유지했습니다. 본체 경로는 `Enemies/Melee/Attachable`이며 내부 구현은 `Enemies/Melee/Internal/EnemyController.Movement.cs`, `.Combat.cs`, `.Collisions.cs`, `.Effects.cs`로 분리했습니다. 이 partial 파일들은 별도로 부착하지 않습니다.
- `PlayerMove.Interaction.cs`는 F 입력/안내/애니메이션, `PlayerMove.InteractionPositioning.cs`는 잡기 간격/방향별 자리 이동을 담당합니다. PlayerMove 연결은 그대로입니다.
- 몬스터 충돌 검색은 재사용 List로 처리하고 플레이어 Collider를 한 프레임에 한 번만 수집합니다. 대상이 바뀌지 않으면 PlayerMove 참조도 재사용합니다. 동적으로 추가/활성화된 Collider는 계속 반영합니다.
- 상호작용 충돌/경계 계산과 피해 수신자 탐색은 Unity ListPool을 사용하고 finally에서 반환합니다. 공격 중복 방지 HashSet도 재사용합니다. 기존 피해 순서/패링/충돌 필터는 유지합니다.
- 코드 분리는 유지보수 목적이며 빌드 크기가 크게 줄어드는 변경은 아닙니다. 원본 이미지의 해상도/압축/메타 설정은 변경하지 않았습니다.

## EnemyController — 순찰/추적 근접 몬스터

- `Assets/Scripts/Enemies/Melee/Attachable/EnemyController.cs`를 적 루트에 부착합니다. 기존 Stat/Range/Player 필드와 수치는 유지했습니다. EnemyStagger는 자동 추가하므로 MeleeEnemy/RangedEnemy를 중복 부착하지 마세요.
- Player를 비우면 활성 PlayerMove를 찾습니다. 감지 → 추적 → 공격 준비(기본 0.4초) → 한 번 타격 → 쿨타임 순서입니다. 예고 시작에 방향/타격 위치를 고정하므로 범위 밖으로 피할 수 있습니다.
- Attack Area Size는 공격 시작 범위, Attack Reach는 앞쪽 타격 중심 거리, Attack Hit Size는 실제 타격 크기입니다. 기즈모는 노란색=감지, 빨간색=공격 시작, 하늘색=실제 타격입니다.
- 주황색 사각 예고가 끝나면 공격 이펙트와 타격이 함께 발생합니다. Attack Effect Sprite에 오른쪽 방향 이미지를 넣을 수 있습니다. 비우면 플레이어 공격 이미지, 그것도 없으면 빨간 임시 사각형을 사용합니다. 유지 시간 동안 피해는 반복되지 않습니다.
- 기존 근접 패링/가드/반사 피해 처리를 사용합니다. 패링 성공 시 피해/가드 게이지 소모 없이 EnemyStagger 경직으로 이동/공격을 중단합니다. 경직 시간은 PlayerMove 패링 설정을 따릅니다.
- 플레이어 공격은 ICombatDamageable을 통해 적 체력을 감소시킵니다. 여러 자식 Collider가 있어도 한 공격당 한 번만 피해를 줍니다. 체력 0이면 사망합니다.
- Ignore Player Body Collision은 이 적과 플레이어 몸 충돌만 제외합니다. Collider를 끄거나 전역 충돌표를 바꾸지 않아 공격 검색/투사체 판정은 유지합니다. 비활성화 시 이 컴포넌트가 설정한 충돌 제외만 해제합니다.
- Enemy 프리팹은 Enemy 레이어로 연결했습니다. Auto Assign Enemy Layer가 켜져 있으면 실행 중 Default Collider도 Enemy로 지정합니다. 커스텀 레이어는 PlayerMove의 Enemy Layer 마스크에 포함하세요.
- 기존 적 피해 10 / 플레이어 최대 HP 10 / 테스트용 Restore Health On Defeat 설정은 그대로입니다. HP가 바로 가득 차면 자동 회복 옵션을 끄거나 테스트 피해량을 낮추세요.

## F 상호작용 — 물체 잡고 옮기기

- 가까운 물체에서 **F → 잡기**, **WASD → 함께 이동**, **F → 놓기**입니다. 누른 채 유지하지 않습니다.
- 이동 입력에 맞춰 물체 반대편에 자리 잡은 뒤 함께 밀어 움직입니다. D는 물체 왼쪽, A는 오른쪽, W는 아래, S는 위에 섭니다. 대각선은 좌우 배치를 우선하며 실제 물체 이동은 대각선입니다. 가까운 Collider부터 선택하고 벽 너머의 대상은 잡지 않습니다.
- 방향 전환 시 캐릭터가 걷기 속도로 물체 둘레의 모서리를 돌아갑니다. 자리 이동 중 물체는 멈추며, 도착한 뒤 함께 이동합니다. 키를 놓으면 자리 이동도 멈춥니다. 벽/장애물을 통과하지 않고, 양쪽 모서리 경로가 모두 막히면 멈추므로 다른 방향을 누르거나 F로 놓으세요. 범용 길찾기가 아니라 물체 주변의 짧은 경로만 검사합니다.
- PlayerMove > 상호작용 > **잡기 간격**(기본 0.3)은 Collider 경계 사이의 여유 공간입니다. 잡는 순간에는 물체만 가능한 만큼 벌리며 이미 충분히 떨어져 있으면 그대로 둡니다. 이동 입력 시 캐릭터가 이 간격에 맞춰 자리를 잡습니다. 0은 최초 간격 보정을 끄지만 자리 이동에는 최소 충돌 여유 0.04를 둡니다. 스프라이트 투명 여백이 아닌 Collider 월드 사각 경계를 기준으로 합니다.
- 옮길 물체 루트에 **MovableInteractable**을 붙이세요. BoxCollider2D가 자동으로 추가됩니다. Collider는 활성 상태이고 Is Trigger가 꺼져 있어야 합니다.
- 빠른 테스트: Play 종료 → **Tools > Story > Interaction > Create Test Box**. 플레이어 오른쪽에 임시 상자를 만듭니다. 기존 씬에는 자동으로 상자를 추가하지 않습니다.
- PlayerMove의 상호작용 설정: 키(F), 검색 거리(기본 1.5), 대상 레이어, 장애물 레이어, 안내 UI/범위 기즈모. 선택한 Player의 노란 원으로 범위를 확인합니다.
- MovableInteractable의 Move Speed Multiplier(기본 0.6)는 잡은 동안 걷기 속도에 곱합니다. Space 달리기/대쉬, 공격, 가드, E 편집 전환, 1/2 오류 사용은 잡고 있는 동안 차단합니다. 놓은 뒤 정상 조작으로 돌아갑니다.
- B 도감은 열 수 있고 잡은 상태로 일시 정지됩니다. 도감을 닫고 계속 옮기거나 F로 놓으세요.
- 플레이어와 물체를 같은 거리만큼 이동하며 둘 중 하나라도 장애물에 닿으면 함께 멈춥니다. Trigger/레이어 충돌 제외 설정은 유지합니다. 검사 방식은 Collider 월드 사각 경계 기반이라 회전/복잡한 모양에서는 보수적으로 멈출 수 있습니다.
- Rigidbody2D는 필수가 아닙니다. 있다면 잡는 동안 Kinematic으로 전환하고 놓을 때 기존 Body Type을 복구합니다. 잡기 전의 속도는 되살리지 않습니다. 자식에 별도 Rigidbody2D가 있는 복합 물체는 지원하지 않습니다.
- 물체 또는 Player 비활성화/삭제, 앱 포커스 이탈 시 잡기가 해제됩니다. MovingEnemy처럼 독립적으로 Transform을 움직이는 스크립트와 같은 물체에 붙이지 마세요.
- 환경 오류로 크기가 변한 물체도 현재 크기의 Collider를 기준으로 처리합니다. MovableInteractable만으로 오류 호환성이 생기는 것은 아닙니다. Paste가 필요하면 PasteTarget을 별도로 설정합니다.

### 상호작용 애니메이션

- `Assets/Player/Ani/Interact_Idle_Up/Down/Right.anim`: 잡은 채 정지.
- `Assets/Player/Ani/Interact_Move_Up/Down/Right.anim`: 잡은 채 이동.
- 기존 이동 스프라이트를 임시 프레임으로 사용합니다. 기존 Move/Attack/Guard/Parry 클립은 변경하지 않습니다.
- Player를 선택하고 Ctrl+6 → 위 Interact 클립 선택 → **SpriteRenderer > Sprite** 트랙 프레임을 직접 교체하세요. Animator의 Motion에 다른 클립을 연결해도 됩니다.
- 이동 입력에 따라 방향을 바꿉니다. W는 Up, S는 Down, D는 Right, A는 Right 클립 flipX입니다. 대각선은 일반 이동처럼 좌우를 우선하며, 키를 놓으면 마지막 방향의 Interact_Idle 클립을 사용합니다. 자리 이동 중에도 해당 방향의 Interact_Move 클립을 사용합니다.
- 스크립트가 6개 상태를 직접 재생하며 놓으면 Player_Idle로 복귀합니다. Any State 전환이나 별도 입력 파라미터는 필요하지 않습니다.
- `Tools > Story > Interaction > Set Up Animator`는 누락된 상태/클립만 복구하며 이미 편집한 프레임이나 연결된 Motion은 덮어쓰지 않습니다.
- `Player/Internal/PlayerMove.Interaction.cs`, `Shared/Internal/InteractionMotion.cs`, `Interaction/Internal/Editor/InteractionSetup.cs`는 별도 부착하지 않습니다.

## 같은 대상의 오류 2종 한 번에 Cut

- 동일 GameObject에 서로 다른 오류 컴포넌트(예: GiantErrorEffect + AccelerationErrorEffect)를 활성화하면 한 번의 편집 모드 Cut으로 둘 다 획득합니다. 별도 묶음 컴포넌트는 필요하지 않습니다.
- 빈 보관함이면 1/2 슬롯에 각각 저장하고 원본의 두 오류를 함께 제거합니다. 저장 순서는 거대화 → 가속 → 반사이며 사용은 기존 1/2 키로 각각 합니다.
- 자리가 부족하면 교체 UI가 먼저 표시됩니다. 이름에 획득할 오류가 모두 나오며, 필요한 기존 오류를 교체 확정한 뒤 전체 획득합니다.
- 예: [반사][빈칸]에서 거대화+가속 Cut → 반사를 교체할지 확인 → 확정 시 [거대화][가속]. 취소하면 반사와 원본 오류 모두 유지합니다.
- 교체 대기 중 원본이 사라지거나 비활성화/다른 상태로 바뀌면 전체 획득을 취소합니다. 일부만 제거/저장하지 않습니다.
- 이미 보관 중인 종류, Paste로 적용한 오류는 재획득하지 않고 해당 원본에 남겨둡니다. 가져올 수 있는 새 오류만 함께 획득합니다.
- 보관함은 여전히 2칸이며 중복 종류를 저장하지 않습니다. 3종 모두 새 오류인 대상은 용량 초과로 묶음 Cut을 거절합니다.
- 같은 오브젝트의 여러 Collider가 같은 공격에 맞아도 한 번만 처리합니다. 기존 전투 수치/먹물 규칙/씬 배치는 변경하지 않습니다.

## 임시 플레이어 HUD

- 좌측 상단에 HP(빨강), 대쉬 스태미나(노랑), 가드(파랑)를 세로로 표시합니다. 각 게이지에 현재/최대 수치가 나타납니다.
- 그 아래 네모 슬롯 2개에 단축키 1/2와 저장된 오류 이름을 표시합니다. 비어 있으면 `빈 슬롯`이며 별도 아이콘 이미지가 필요하지 않습니다.
- 환경 Paste 준비 슬롯은 하늘색, Full 교체 대상 슬롯은 주황색 테두리로 강조합니다. 기존 1/2, 좌클릭, 휠 교체 조작은 유지합니다.
- 기존 좌측 하단 게이지와 텍스트형 보관함은 제거해 중복 표시하지 않습니다. 교체 안내는 슬롯 아래, 오류 발견 알림은 화면 하단에 표시합니다.
- PlayerMove > 임시 플레이어 HUD의 `Show Player Hud`로 전체 표시를 끄고 `Player Hud Scale`로 크기를 조절할 수 있습니다.
- 작은 Game 뷰에서는 자동 축소합니다. 카메라 Size에 영향받지 않으며 별도 Canvas/프리팹/이미지 연결 없이 Play하면 나타납니다.
- `Player/Internal/PlayerMove.Hud.cs`는 표시 전용 partial 구현이므로 직접 부착하지 않습니다. HP/회복/스태미나 비용 등 게임 규칙은 변경하지 않습니다.

## 원거리 몬스터 공격 범위

- `RangedEnemy → Attack Range` 안에 플레이어가 있을 때만 공격을 시작합니다(기본 반경 5 월드 단위).
- 몬스터 중심과 플레이어 중심의 XY 거리로 판정하며 경계도 포함합니다. 벽/시야 검사는 추가하지 않습니다.
- 범위 밖으로 나가면 새 발사를 멈춥니다. 이미 발사된 투사체는 기존 수명/충돌 규칙대로 남습니다.
- `Attack Interval`은 공격 시작 사이 최소 간격이며, `First Attack Delay`는 생성 직후 대기 시간입니다. 경직 중에는 공격을 취소합니다.
- 몬스터 선택 → Scene의 Gizmos 켜기: 빨간색은 공격 범위, 노란색은 접근을 멈추는 거리입니다. 스포너의 청록색 원은 생성 감지 범위입니다.
- 공격 클립의 설정된 진행률에 한 발을 발사하며, 발사 순간 플레이어가 있던 위치를 향해 직진합니다. 발사 후 추적하지 않고, 기본 속도는 10입니다.

## 오류 슬롯 단축키 (1 / 2)

- 숫자열 1/2 또는 숫자패드 1/2로 해당 고정 슬롯의 오류를 사용합니다. Q와 휠은 더 이상 오류 사용/선택에 사용하지 않습니다.
- 일반 전투 모드: 번호 1회 → 해당 오류를 전투 기술에 즉시 적용하고 보관함에서 소비합니다.
- 환경 편집 모드: 번호 1회 → 즉시 Paste 준비 → 대상 좌클릭으로 적용합니다. 같은 번호를 다시 누르면 소비 없이 취소하고 다른 번호는 준비 오류를 바꿉니다.
- 비어 있는 번호는 사용하지 않습니다. 잘못된 환경 대상에 Paste하면 소비하지 않고 준비 상태를 유지합니다.
- 화면 왼쪽 위에 [1]/[2] 슬롯과 오류 이름을 항상 표시합니다. 한 슬롯을 사용해도 다른 슬롯의 번호가 바뀌지 않습니다.
- 새 오류는 첫 번째 빈칸에 저장하고, Full 교체는 선택한 오류가 있던 슬롯을 그대로 사용합니다.
- 보관함이 가득 찼을 때만 기존대로 휠로 교체 대상을 선택하고 좌클릭으로 확정합니다. 이 동작은 Paste와 별개입니다.
- 전투 오류 유지 중 추가 사용 제한, 도감/대쉬 중 입력 차단은 기존대로 유지합니다.

## 대쉬 / 스태미나

- Space를 **새로 누를 때 1회** 대쉬합니다. 유지하면 대쉬 종료 후 기존 달리기로 이어지며 자동 연속 대쉬하지 않습니다.
- WASD 방향으로 대쉬합니다. 이동 입력이 없으면 마지막 이동 방향이며 8방향 속도/거리는 같습니다. 대쉬 중 방향은 고정됩니다.
- 초기 스태미나 100, 대쉬당 30 소모. 부족하면 소비 없이 실패하고 HUD에 `스태미나 부족`이 표시됩니다.
- 기본 속도 18, 시간 0.16초(장애물 없을 때 2.88 단위), 종료 후 재사용 대기 0.25초입니다.
- 대쉬 중 회복하지 않으며 종료 후 0.8초부터 초당 25 회복합니다. 달리기는 스태미나를 소모하지 않습니다.
- 모두 PlayerMove Inspector의 **대쉬 / 스태미나**에서 조절합니다. 스태미나는 좌측 상단 HP 아래에 표시됩니다.
- 공격/가드/패링/오류 교체·Paste 준비 중에는 대쉬를 시작하지 않습니다. 대쉬 중에는 공격·가드와 1/2/E 편집 입력을 받지 않습니다.
- 편집 모드에서도 대쉬할 수 있고 게임 시간 배율을 따릅니다. 도감을 열면 일시 정지하고 닫은 뒤 이어집니다. 포커스 이탈/컴포넌트 비활성화는 대쉬를 중단하며 비용을 환불하지 않습니다.
- 대쉬는 플레이어 Collider2D의 경로를 검사하여 `Dash Blocking Layers`의 장애물 앞에서 멈춥니다. Trigger와 충돌 무시 대상은 통과합니다. 기존 걷기/달리기의 이동 충돌 방식은 변경하지 않습니다.
- 무적/피해 감소/새 애니메이션은 추가하지 않습니다. 기존 이동 클립을 사용하고 대쉬 중에도 피해를 받을 수 있습니다.
- 대쉬 잔상은 기존 잔상 풀을 공유하고 기본 0.035초 간격으로 생성합니다. `Show Run Afterimages`를 끄면 달리기/대쉬 잔상 모두 꺼집니다.
- `Player/Internal/PlayerMove.Dash.cs`는 PlayerMove partial이므로 별도로 부착하지 않습니다.

## 달리기 (Space 유지)

- **달리기 잔상:** 실제로 달리는 동안 현재 스프라이트의 옅은 복사본이 이동 뒤쪽에 잠깐 남습니다.
  걷기/정지/가드/패링 중에는 새 잔상이 생기지 않고 남아 있는 잔상만 서서히 사라집니다.
- PlayerMove의 `Show Run Afterimages`로 끌 수 있습니다. `Run Afterimage Interval`(기본 0.07초),
  `Lifetime`(0.2초), `Opacity`(0.22), `Offset`(0.1)으로 간격·유지 시간·불투명도·뒤쪽 간격을 조절합니다.
- 잔상은 원래 애니메이션 프레임/좌우 반전/색/월드 크기를 따르며 플레이어보다 한 단계 뒤에 표시됩니다.
  기존 Sprite/Material을 공유하여 Sprite Atlas도 그대로 사용합니다. 별도 이미지/프리팹을 넣을 필요가 없습니다.
- 최대 12개 렌더러를 재사용합니다. 잔상에는 충돌/공격/오류가 없으며 환경 Paste 대상 검색에서도 제외합니다.
- 게임 시간에 따라 사라져 편집 모드에서는 느려지고 도감에서는 정지합니다. Player 비활성화 시 숨기고 파괴 시 정리합니다.
- `Player/Internal/PlayerMove.RunAfterimages.cs`는 partial 구현이므로 따로 부착하지 않습니다.

- WASD 이동 중 Space를 누르고 있으면 이동 속도만 증가하고, 놓으면 즉시 걷기 속도로 복귀합니다.
- PlayerMove의 `Run Speed Multiplier`로 배율을 조절합니다(기본 1.5배). Move Speed가 5이면 달리기는 7.5입니다.
- 기존 걷기 애니메이션/재생 속도를 그대로 사용합니다. Animator나 스프라이트를 추가할 필요가 없습니다.
- 방향키 없이 Space만 누르면 움직이지 않으며, 대각선도 정규화되어 같은 속도로 이동합니다.
- 가드/패링 중 이동 제한과 도감 일시 정지는 그대로 유지합니다. 편집 모드에서도 달리며 기존 슬로모션 배율을 따릅니다.
- 달리기는 스태미나를 소비하지 않습니다. Space 단발 대쉬/스태미나는 위 대쉬 항목을 참고하세요.

## 오류 발견 / 도감

- PlayerMove에 자동 통합되어 있습니다. 씬에 새 컴포넌트나 UI를 직접 붙이지 않아도 됩니다.
- PlayerMove Inspector의 **오류 발견 / 도감**에서 `Error Discovery Radius`를 조절합니다(기본 3 월드 단위).
- 플레이어 중심과 활성 오류 대상 중심의 XY 거리가 반경 이내이면 자동 발견합니다. 벽/시야 검사는 하지 않습니다.
- Player를 선택하고 Scene Gizmos를 켜면 초록색 원으로 범위를 확인합니다. `Show Error Discovery Gizmo`로 끌 수 있습니다.
- 0.2초마다 거대화/반사/가속 컴포넌트를 확인합니다. 비활성 GameObject, 꺼진 컴포넌트, 효과가 비활성인 오류는 제외합니다.
- 새로 생성되는 대상도 확인하며 같은 오류 종류는 중복 등록/알림하지 않습니다. 세 종류를 모두 발견하면 검사를 중단합니다.
- **B**: 도감 열기/닫기. 목록에서 발견한 오류를 클릭하면 설명과 Paste 가능/불가 대상군이 나타납니다.
- 미발견 항목은 ???로 표시하고 선택할 수 없습니다. 호환 표시는 실제 `ErrorRules.CanPasteTo` 규칙을 사용합니다.
- 도감을 읽는 동안 일시 정지하고 이동/공격/Cut/Paste 입력을 차단합니다. B, Esc, 닫기 버튼으로 닫으면 원래 시간 배율(편집 모드 포함)을 복원합니다.
- 도감을 열 때 진행 중인 오류 선택/교체/Paste 준비는 소비 없이 취소합니다. 이미 시작한 공격은 일시 정지했다가 닫은 뒤 이어집니다.
- **Editor 테스트 기본값:** `Start With Empty Error Codex In Editor`가 켜져 있어 Play 시작 시 빈 도감으로 시작합니다.
  이 실행에서는 기존 PlayerPrefs를 읽거나 덮어쓰지 않습니다. 기존 저장 기록 자체는 삭제하지 않습니다.
  실행 중에도 가까이 가면 정상 등록되며 다음 Play에서 다시 빈 상태로 시작합니다.
  시작 지점의 발견 반경 안에 오류가 있으면 첫 검사에서 바로 등록됩니다. 순차 발견을 테스트하려면 오류 원본을 초록색 범위 밖에 배치하세요.
- `Persist Error Discoveries` 기본 켜짐: 빌드에서는 이 기기의 PlayerPrefs에 발견 종류만 저장합니다. 씬 이동/재실행 후에도 유지하며 보관함 슬롯을 차지하지 않습니다.
  Editor에서 저장 유지도 테스트하려면 Play를 끈 뒤 `Start With Empty Error Codex In Editor`를 해제하세요. 저장 사용 여부는 실행 시작 시 결정됩니다.
- 도감은 정보 기록이지 오류 지급 장치가 아닙니다. 발견해도 원본에서 오류가 제거되거나 보관함에 자동 저장되지 않습니다.
- **이번 구현은 발견/도감만 추가합니다. 기존 Cut/Paste 조건은 유지하며 패링·퍼즐을 통한 별도 노출 조건은 아직 추가하지 않았습니다.**
- 테스트 초기화: PlayerMove 컴포넌트 컨텍스트 메뉴 → `오류 도감/발견 기록 초기화 (저장 기록 포함)`.
  오류 도감 키 3개만 삭제하며 다른 세이브/설정은 유지합니다. 실행 중 범위 안에 있으면 다음 검사에서 다시 발견하므로 멀리 이동한 뒤 초기화하세요.
- `Errors/Internal/ErrorCodex.cs`: 도감 모델/오류 설명. `Player/Internal/PlayerMove.ErrorCodex.cs`: 발견/저장/입력/UI. 둘 다 별도 부착하지 않습니다.

### 도감 확인 순서

1. 발견 기록 초기화 후 오류 원본에서 멀리 떨어진 상태로 Play: B를 눌러 미발견 항목을 확인합니다.
2. 활성 오류 원본 가까이 접근: 최초 발견 알림과 도감 등록을 확인합니다. 동일 종류에 재접근해도 알림은 반복하지 않습니다.
3. B → 등록된 항목 클릭: 설명과 호환 대상군을 확인합니다. 기존 오류가 보관함으로 자동 들어오지 않는지 확인합니다.
4. 편집 모드 E → B → 항목 클릭 → 닫기: 클릭으로 Cut/Paste되지 않고 편집 모드 슬로모션으로 돌아오는지 확인합니다.
5. 기본 테스트 모드에서는 Play 종료 후 다시 실행하면 도감이 빈 상태로 시작하는지 확인합니다.
   저장 유지 테스트는 위 Editor 테스트 옵션을 끄고 진행합니다. 보관함은 기존 규칙을 따릅니다.

기존 컴포넌트 클래스명, .meta GUID, Inspector 직렬화 필드와 기본값을 유지한 리팩토링입니다.
씬/프리팹/Animator/스프라이트는 수정하지 않았습니다.

## 부착할 파일
각 기능 폴더의 Attachable에 있는 기존 컴포넌트를 그대로 사용합니다. 새 파일을 다시 붙일 필요는 없습니다.
- PlayerMove.cs: Inspector 설정 및 Start/Update 실행 순서.
- CameraFollow.cs: LateUpdate에서 플레이어 위치 + Offset으로 즉시 추적합니다. 보간 지연과 Smooth Speed 설정은 제거했습니다.
  Offset X/Y가 0이면 플레이어 Transform을 화면 중앙에 유지합니다. 카메라 Size와 기존 Offset은 변경하지 않습니다.
- MovingEnemy.cs: 왕복 이동과 가속 적용.
- RangedEnemy.cs: 원거리 적 이동·공격 애니메이션·발사 주기·체력·투사체 생성.
- RangedEnemySpawner.cs: 감지 범위 진입 시 원거리 적 생성.
- GiantErrorEffect.cs / AccelerationErrorEffect.cs / ReflectionErrorEffect.cs: 대상의 실제 효과.
- PasteTarget.cs: Paste 대상 분류.

## 직접 부착하지 않는 파일
Player/Internal의 PlayerMove.*.cs는 하나의 PlayerMove 클래스를 나눈 partial 구현입니다.
컴포넌트가 추가된 것이 아니며 같은 필드와 상태를 공유합니다.
- Movement: 이동/조준.
- Combat: 공격 애니메이션/판정/이펙트/피해/기즈모.
- Guard: 우클릭 유지 가드/상하좌우 방향/포커스 해제 처리. 별도 부착하지 않습니다.
- Editing: 편집 모드와 UI.
- ErrorInput: 1/2 슬롯 사용 및 Full 교체 입력.
- ErrorInventory: 획득/교체와 보관 모델 연결.
- ErrorInteractions: Cut와 환경 Paste.
- CombatErrors: 전투 Paste와 버프 시간.

각 기능 폴더의 Internal에 있는 독립 규칙:
- ErrorInventory: 최대 2개, 종류 중복 금지, 배율 보관, 검증 후 교체.
- ErrorDefinitions / ErrorRules: 오류 종류, 표시 이름, 호환 대상 표.
- ErrorEffectState: 원본/붙여넣기 출처 및 활성 여부.
- IErrorSource: 오류를 공통 방식으로 제거하는 규약. IAccelerationTarget도 이 파일에 있습니다.
- AttackMath: 4방향 공격과 끝에서 세 번째 프레임 시점 계산.
- CombatDamage: 피해 수신자 탐색/전달.
- ReflectProjectile: RangedEnemy가 자동 부착하는 공용 투사체 컴포넌트.

## 유지한 게임 규칙
- 이동은 8방향, 대각선 이동 이미지는 좌우 클립.
- 공격은 상하좌우. 대각선 커서는 좌우 영역으로 분류.
- 기본 타격은 끝에서 세 번째 프레임 시작 시점, 공격 자체는 끝까지 재생.
- 일반 모드: 1/2 번호를 누르면 해당 고정 슬롯의 오류를 즉시 전투 Paste.
- 편집 모드: 1/2 번호로 즉시 준비 → 좌클릭 적용. 같은 번호 또는 편집 모드 종료(E)로 소비 없이 취소.
- 환경 Paste 실패(빈 공간/비호환 대상)는 오류를 소비하지 않고 준비 상태를 유지.
- 보관함 표시: 고정 1/2 슬롯. 종류순 정렬하지 않으며 소비 후 남은 슬롯을 당기지 않음.
- 오류 2개 보관 시 교체 UI, E로 편집 모드 종료하면 취소.
- 다른 원본의 같은 오류는 획득 가능. Paste한 대상의 오류는 재획득 불가.
- 환경 Paste가 활성화된 것만으로 전투 Paste를 막지 않음.
- 전투 오류 활성 중에는 기존처럼 다른 오류 사용을 제한.
- 기존 일반모드에서 가속/거대화 원본 공격이 Cut 불가 처리로 끝나는 동작,
  Collider가 없는 환경 대상에 Collider를 추가하는 동작 등은 이번에 별도로 변경하지 않음.

## 검증 및 실행
### 가드 설정 및 프레임 넣기
- PlayerMove의 `Guard Damage Reduction Percent`는 받는 피해 감소율(0~100%)입니다. 초기값 50%.
- 일반 모드에서 우클릭을 누르는 동안 가드합니다. 가드 중 WASD 이동과 공격 불가.
- 가드 게이지 최대치 기본 100, 가드 피격 1회당 10 소모. 피해 감소율이 100%여도 소모합니다.
- 0이 되면 즉시 가드와 프레임 고정을 해제합니다. 마지막 타격에는 가드 감소율이 적용되고 다음 타격부터 일반 피해입니다.
- 소진 후 우클릭 유지로 재가드하지 않습니다. 한 번 놓고 게이지가 0보다 큰 상태에서 다시 누르세요.
- 가드하지 않을 때 초당 20 자동 회복(기본값), 가드 중 회복 없음. 최대치와 회복량은 Inspector에서 조절합니다.
- 좌측 상단에 가드 게이지 표시. Show Guard Gauge로 표시를 끌 수 있습니다.
- 반사 오류로 되돌린 공격은 가드로 받은 공격이 아니므로 게이지를 소모하지 않습니다(기존 반사 우선 규칙 유지).
- 이미 시작한 공격은 끝까지 재생한 뒤, 우클릭을 유지 중이면 가드로 이어집니다.
- 현재 가드와 패링은 방향 제한 없이 판정합니다.
- 반사 오류 활성 시 기존 전량 반사가 먼저 처리됩니다. 반사가 적용되지 않는 피해에 가드 감소율을 적용합니다.
- `Assets/Player/Ani/Guard_Up.anim`, `Guard_Down.anim`, `Guard_Right.anim`에 Ctrl+6으로 스프라이트 프레임을 넣으세요.
  Player를 선택한 상태에서 해당 클립을 선택하고 SpriteRenderer > Sprite 트랙에 넣습니다. 빈 클립은 이미지가 바뀌지 않는 것이 정상입니다.
- 가드 클립은 끝에서 세 번째 프레임에 도달하면 정지하고, 우클릭을 놓을 때까지 그 이미지를 유지합니다.
  클립 길이와 Samples로 자동 계산합니다(등간격 프레임 기준). 3프레임 이하이면 첫 이미지를 유지합니다.
  위/아래/좌우 클립을 바꾸면 처음부터 재생 후 해당 클립의 끝에서 세 번째 프레임을 유지합니다.
  왼쪽은 Guard_Right를 flipX로 뒤집으며 좌↔우 전환만으로 재생을 다시 시작하지 않습니다.
  우클릭 해제/편집 모드 전환/포커스 이탈 시 Animator 속도를 원래대로 복구합니다.
- Animator의 Guard 3개 상태는 공격과 동일하게 스크립트가 직접 진입/종료합니다. Any State 전환은 추가하지 않았습니다.
- 가드 해제/편집 모드 전환/앱 포커스 이탈 시 가드가 종료됩니다.
- 확인: 50%에서 피해 2→1, 0%에서 2→2, 100%에서 2→0. 우클릭 해제/공격 중/편집 모드에서는 원래 피해.

Tools/ScriptTests/Run.ps1에 Unity.exe 경로를 전달하면 독립 로직 테스트를 실행합니다.
테스트의 Unity 대체 타입은 Assets 바깥에 있어 게임 빌드에 포함되지 않습니다.
이 테스트는 실제 Physics2D 충돌이나 Animator 재생을 검증하는 Play Mode 테스트가 아닙니다.

Unity에서 Play를 끈 상태로 변경분을 가져오고 컴파일 후 다음 항목을 확인하세요.
- Player의 기존 Inspector 참조/수치, Missing Script 여부.
- 8방향 이동, 4방향 공격, 가속 중 타격 프레임.
- 거대화/가속/반사 획득, 일반 모드 1/2 즉시 사용, 편집 모드 1/2 준비/취소와 좌클릭 Paste.
- 가득 찬 보관함 교체 확정/취소 및 원본이 사라진 경우.
- Paste한 대상 재획득 차단, 다른 원본에서 동일 오류 재획득.
- 적 순찰, 투사체 발사/피해/반사, 카메라 추적.

## 패링 / 첨삭
- 우클릭을 새로 누른 시점부터 Parry Window(기본 0.2초) 이내의 근접 공격/투사체 충돌을 패링합니다.
- 우클릭 1회당 한 공격만 패링합니다. 계속 누르거나 방향/편집 모드를 바꿔도 창이 갱신되지 않습니다.
- 성공 시 체력 및 가드 게이지 소모 없음. 창이 지나거나 같은 입력의 두 번째 공격은 기존 가드 규칙(게이지 -10).
- 공격 중/편집 모드/포커스 없음/게이지 0/가드 브레이크 재입력 대기 중에는 패링 불가.
- 근접: MeleeEnemy가 ReceiveMeleeAttack으로 피해 전달 → 패링 성공 → EnemyStagger에 Parry Stun Duration(기본 1초) 적용.
- EnemyStagger가 켜진 동안 MovingEnemy의 순찰과 MeleeEnemy/RangedEnemy의 공격이 중단됩니다. 가속 오류와 별개입니다.
- 원거리: ReflectProjectile의 실제 탄환을 발사자 방향으로 되돌리고 소유자를 플레이어로 변경합니다.
  벽 반사 횟수와 관계없이 패링할 수 있고, 기존 속도/피해/남은 수명은 유지합니다.
  패링한 탄환도 명중 대상의 반사 오류를 따릅니다. 반사 오류 활성 적에게 맞으면 새 소유자인 플레이어에게 피해가 돌아옵니다.
  반사 오류가 제거된 적은 그대로 피해를 받습니다. 플레이어에게 돌아온 피해는 재패링/재반사되지 않으며, 일반 가드의 피해 감소/게이지 소모는 기존대로 적용됩니다.
- 반사 오류 자체로 돌아오는 간접 피해는 패링 대상이 아닙니다. 패링 실패 시 기존 반사 오류 처리는 유지됩니다.
- 플레이어 스프라이트 발밑에 '첨삭 성공!'을 0.45초 표시하고 방향별 Parry 애니메이션을 재생합니다.
  문구는 카메라/플레이어 이동을 따라가며, 가드 게이지 표시를 꺼도 나타납니다. 가드 HUD에는 중복 표시하지 않습니다.
- PlayerMove > 패링 성공 이미지 > Parry Feedback Sprite에 직접 만든 글씨 Sprite를 넣으면 기본 문구 대신 표시합니다.
  이미지는 Sprite (2D and UI)로 임포트하세요. 표시 크기/발밑 간격은 Parry Feedback Image Size / Offset에서 조절합니다.
  원본 비율과 Sprite Atlas를 지원하며 빈 슬롯은 기존 문구로 돌아갑니다. 런타임 Canvas는 자동 생성·정리되므로 씬에 UI를 추가할 필요가 없습니다.
- Player를 선택하고 Scene 뷰 Gizmos를 켜면 패링 이미지 위치/크기를 미리 볼 수 있습니다.
  하늘색은 설정한 UI 영역, 노란색은 원본 이미지 비율을 유지한 영역입니다(이미지 미지정 시 하늘색만 표시).
  Show Parry Feedback Gizmo로 끌 수 있으며 Image Size / Offset 변경을 즉시 반영합니다.
  화면 픽셀 기반 UI이므로 MainCamera의 투영과 현재 Game 뷰 해상도를 기준으로 표시합니다. MainCamera가 없으면 표시하지 않습니다.
- `Assets/Player/Ani/Parry_Up.anim`, `Parry_Down.anim`, `Parry_Right.anim`에 스프라이트를 직접 넣으세요.
  Player 선택 → Ctrl+6 → 해당 클립 선택 → SpriteRenderer > Sprite 프레임 추가. 왼쪽은 오른쪽 클립을 뒤집습니다.
- 패링 성공 순간 커서 방향으로 클립을 결정합니다. 성공 연출 도중 커서를 바꿔도 방향은 유지됩니다.
- 성공 클립은 끝까지 1회 재생하며 그동안 이동/공격/새 패링은 불가합니다. 우클릭 유지 시 일반 가드는 계속 적용됩니다.
- 끝난 뒤 우클릭 유지 → 가드로 복귀(끝에서 세 번째 프레임 유지), 해제 상태 → 기본 상태 복귀.
- 편집 모드 진입/포커스 이탈/비활성화/가드 게이지 소진 시 성공 연출을 취소하고 속도를 복구합니다.
- 빈 클립은 Empty Parry Duration(기본 0.25초) 뒤 종료합니다. 실제 프레임을 넣으면 클립 길이를 사용합니다.
- Animator의 Parry 3개 상태는 스크립트가 직접 전환합니다. 기존 가드/공격/이동 연결과 이미지에는 변경이 없습니다.

### 근접 패링 테스트 적 구성
씬/기존 적 구성은 자동 변경하지 않았습니다. 별도 적 GameObject에 SpriteRenderer와 MeleeEnemy를 붙이세요.
EnemyStagger와 BoxCollider2D는 자동 추가됩니다. 순찰하려면 같은 루트에 MovingEnemy도 추가하세요.
RangedEnemy와 MeleeEnemy를 같은 오브젝트에 함께 붙이지 마세요(체력 수신자 중복).
공격 범위에 들어오면 '! 근접 공격 준비' 표시 후 타격합니다. Windup Duration을 조절해서 타이밍을 시험하세요.
미래의 근접 공격 스크립트도 PlayerMove.ReceiveMeleeAttack을 호출하고 EnemyStagger.IsStunned 동안 공격을 중단해야 합니다.
