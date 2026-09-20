# 오류 스크립트

## Attachable — 오류 대상 오브젝트에 부착
- GiantErrorEffect.cs: 거대화 효과와 원본/붙여넣기 상태.
- AccelerationErrorEffect.cs: 가속 효과와 속도 배율.
- ReflectionErrorEffect.cs: 반사 오류의 활성 상태와 효과.
- PasteTarget.cs: 생명체·물체·투사체·표면 등 Paste 대상군.

## Internal — 직접 부착하지 않음
- ErrorDefinitions.cs: 오류 종류·표시 이름·호환 규칙.
- ErrorEffectState.cs: 원본/붙여넣기 출처와 활성 상태.
- ErrorInventory.cs: 최대 2칸 보관과 교체 규칙.
- ErrorCodex.cs: 발견 정보와 오류 설명.
- IErrorSource.cs: 오류 원본 제거 및 가속 대상 인터페이스.
- EnvironmentPasteSession.cs: 환경 Paste 선택·준비 상태.

플레이어 키 입력과 UI 연결은 ../Player/Internal/PlayerMove.Error*.cs에 있습니다.
