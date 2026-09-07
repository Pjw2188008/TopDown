# 스크립트 구조

기존 컴포넌트 클래스명, .meta GUID, Inspector 직렬화 필드와 기본값을 유지한 리팩토링입니다.
씬/프리팹/Animator/스프라이트는 수정하지 않았습니다.

## 부착할 파일
Attachable의 기존 컴포넌트를 그대로 사용합니다. 새 파일을 다시 붙일 필요는 없습니다.
- PlayerMove.cs: Inspector 설정 및 Start/Update 실행 순서.
- CameraFollow.cs: 카메라 추적.
- MovingEnemy.cs: 왕복 이동과 가속 적용.
- ProjectileEnemy.cs: 발사 주기·적 체력·투사체 생성.
- GiantErrorEffect.cs / AccelerationErrorEffect.cs / ReflectionErrorEffect.cs: 대상의 실제 효과.
- PasteTarget.cs: Paste 대상 분류.

## 직접 부착하지 않는 파일
Internal/Player의 PlayerMove.*.cs는 하나의 PlayerMove 클래스를 나눈 partial 구현입니다.
컴포넌트가 추가된 것이 아니며 같은 필드와 상태를 공유합니다.
- Movement: 이동/조준.
- Combat: 공격 애니메이션/판정/이펙트/피해/기즈모.
- Editing: 편집 모드와 UI.
- ErrorInput: Q/휠 선택 및 교체 입력.
- ErrorInventory: 획득/교체와 보관 모델 연결.
- ErrorInteractions: Cut와 환경 Paste.
- CombatErrors: 전투 Paste와 버프 시간.

Internal의 독립 규칙:
- ErrorInventory: 최대 2개, 종류 중복 금지, 배율 보관, 검증 후 교체.
- ErrorDefinitions / ErrorRules: 오류 종류, 표시 이름, 호환 대상 표.
- ErrorEffectState: 원본/붙여넣기 출처 및 활성 여부.
- IErrorSource: 오류를 공통 방식으로 제거하는 규약. IAccelerationTarget도 이 파일에 있습니다.
- AttackMath: 4방향 공격과 끝에서 세 번째 프레임 시점 계산.
- CombatDamage: 피해 수신자 탐색/전달.
- ReflectProjectile: ProjectileEnemy가 자동 부착하는 투사체 컴포넌트.

## 유지한 게임 규칙
- 이동은 8방향, 대각선 이동 이미지는 좌우 클립.
- 공격은 상하좌우. 대각선 커서는 좌우 영역으로 분류.
- 기본 타격은 끝에서 세 번째 프레임 시작 시점, 공격 자체는 끝까지 재생.
- 일반 모드: 오류 1개는 Q 즉시 전투 Paste, 2개는 Q를 누른 상태에서 휠 선택 후 놓으면 확정.
- 편집 모드: 오류 1개는 Q로 Paste 준비, 좌클릭으로 적용. 준비 중 Q를 다시 누르면 소비 없이 취소.
- 편집 모드 오류 2개: Q로 선택 시작 → 휠 변경 → Q를 다시 눌러 준비 → 좌클릭 적용.
  Q를 놓는 것만으로 준비/적용되지 않음. 준비 중 Q 또는 편집 모드 종료(E)로 취소.
- 환경 Paste 실패(빈 공간/비호환 대상)는 오류를 소비하지 않고 준비 상태를 유지.
- 보관함 표시 순서: 거대화 → 가속 → 반사.
- 오류 2개 보관 시 교체 UI, E로 편집 모드 종료하면 취소.
- 다른 원본의 같은 오류는 획득 가능. Paste한 대상의 오류는 재획득 불가.
- 환경 Paste가 활성화된 것만으로 전투 Paste를 막지 않음.
- 전투 오류 활성 중에는 기존처럼 다른 오류 사용을 제한.
- 기존 일반모드에서 가속/거대화 원본 공격이 Cut 불가 처리로 끝나는 동작,
  Collider가 없는 환경 대상에 Collider를 추가하는 동작 등은 이번에 별도로 변경하지 않음.

## 검증 및 실행
Tools/ScriptTests/Run.ps1에 Unity.exe 경로를 전달하면 독립 로직 테스트를 실행합니다.
테스트의 Unity 대체 타입은 Assets 바깥에 있어 게임 빌드에 포함되지 않습니다.
이 테스트는 실제 Physics2D 충돌이나 Animator 재생을 검증하는 Play Mode 테스트가 아닙니다.

Unity에서 Play를 끈 상태로 변경분을 가져오고 컴파일 후 다음 항목을 확인하세요.
- Player의 기존 Inspector 참조/수치, Missing Script 여부.
- 8방향 이동, 4방향 공격, 가속 중 타격 프레임.
- 거대화/가속/반사 획득, 일반 모드 Q+휠 선택, 편집 모드 Q 준비/취소와 좌클릭 Paste.
- 가득 찬 보관함 교체 확정/취소 및 원본이 사라진 경우.
- Paste한 대상 재획득 차단, 다른 원본에서 동일 오류 재획득.
- 적 순찰, 투사체 발사/피해/반사, 카메라 추적.
