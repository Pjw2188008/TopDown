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

## 거대화의 바닥 기준 확대

- `GiantErrorEffect → Growth Anchor` 기본값은 **AutoSideView**입니다. 2D 사이드뷰에서 원본 거대화/Paste 시 몸체 밑면의 월드 Y 위치를 유지하며 위로 커집니다. 탑다운과 2.5D 바닥 이동은 기존 피벗 기준 확대를 유지합니다.
- **Bottom**: 시점과 무관하게 항상 밑면 기준. **Pivot**: 기존처럼 Transform 피벗 기준.
- `Bottom Anchor Collider`를 비우면 자신과 자식의 활성 비트리거 Collider2D 중 최하단을 사용합니다. 감지 Trigger는 제외합니다. 복잡한 프리팹은 몸체 Collider를 직접 연결하세요. Collider가 전혀 없으면 SpriteRenderer 밑면을 기준으로 합니다.
- 확대 매 단계마다 현재 밑면을 보정하므로 크기 변화 도중 물체를 옮겨도 최초 위치로 끌어당기지 않습니다. Cut/Reset으로 작아질 때도 현재 밑면을 유지합니다. Rigidbody2D 위치와 물리 판정도 함께 갱신합니다.
- 바닥을 검색해 공중 물체를 아래로 떨어뜨리는 기능은 아닙니다. 현재 밑면 높이를 유지하는 기능이며 벽/천장으로의 확대를 막는 공간 검사는 포함하지 않습니다.
- 튜토리얼 체크포인트는 크기 변화 시작값과 확대 기준도 복원합니다. Paste한 오류 재획득 금지 규칙은 그대로입니다.
