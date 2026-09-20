# 플레이어 스크립트

## Attachable — 오브젝트에 부착
- PlayerMove.cs: 플레이어 루트. Inspector 설정과 전체 실행 순서. 기존 컴포넌트를 그대로 사용합니다.
- CameraFollow.cs: 카메라. 플레이어 위치를 추적합니다.

## Internal — 직접 부착하지 않음
모두 PlayerMove 하나를 나눈 partial 구현이며 같은 필드와 상태를 공유합니다.
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

오류 규칙 모델 자체는 ../Errors, 옮길 수 있는 물체 컴포넌트는 ../Interaction에 있습니다.
