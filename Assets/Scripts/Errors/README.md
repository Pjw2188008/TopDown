# 오류 스크립트

> 사이드뷰 전용 정리: 플레이어 모드 선택·상하 이동/공격/가드/패링·2.5D 높이 보정 및 몬스터 탑다운 격자 길찾기를 제거했습니다. 기존 시점 플래그 값과 무관하게 좌우 이동·중력을 사용합니다. 사이드뷰 Animator와 그림, 씬/프리팹 참조는 유지합니다.

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

- `GiantErrorEffect → Growth Anchor` 기본값은 **AutoBottom**입니다. 원본 거대화/Paste 시 밑면을 유지하며 위로 커집니다. 중심 확대가 필요한 개별 사물만 Pivot을 직접 선택하세요.
- **Bottom**: 시점과 무관하게 항상 밑면 기준. **Pivot**: 기존처럼 Transform 피벗 기준.
- `Bottom Anchor Collider`를 비우면 자신과 자식의 활성 비트리거 Collider2D 중 최하단을 사용합니다. 감지 Trigger는 제외합니다. 복잡한 프리팹은 몸체 Collider를 직접 연결하세요. Collider가 전혀 없으면 SpriteRenderer 밑면을 기준으로 합니다.
- 확대 매 단계마다 현재 밑면을 보정하므로 크기 변화 도중 물체를 옮겨도 최초 위치로 끌어당기지 않습니다. Cut/Reset으로 작아질 때도 현재 밑면을 유지합니다. Rigidbody2D 위치와 물리 판정도 함께 갱신합니다.
- 바닥을 검색해 공중 물체를 아래로 떨어뜨리는 기능은 아닙니다. 현재 밑면 높이를 유지하는 기능이며 벽/천장으로의 확대를 막는 공간 검사는 포함하지 않습니다.
- 튜토리얼 체크포인트는 크기 변화 시작값과 확대 기준도 복원합니다. Paste한 오류 재획득 금지 규칙은 그대로입니다.

## 은신 오류

- 원본에는 `StealthErrorEffect`를 붙이고 Trigger On Start를 켭니다. Cut 대상의 Layer는 Error로 지정하세요. Cut하면 원래 그림으로 복구하며 도감/2칸 보관함에서도 [은신]을 사용합니다.
- 생명체 / 물체 / 투사체 / 표면 / 전투 기술 모두 호환됩니다. 환경 대상에는 기존 PasteTarget을 붙이세요. 환경 Paste한 오류는 다시 Cut할 수 없습니다.
- 환경 Paste는 대상을 희미하게 만들고 움직이는 노이즈를 표시합니다. Collider, 이동, 피해 판정은 유지합니다.
- 일반 상태에서 오류 슬롯 1/2로 전투 Paste하면 플레이어가 설정 시간 동안 은신합니다. PlayerMove의 **Stealth Combat Duration**(기본 5초), **Stealth Combat Opacity**, **Stealth Combat Noise Strength**로 조절합니다.
- 은신 중 근접/원거리 적은 새 추적·공격을 멈추고 복귀/순찰합니다. 보스는 새 추적/표적 패턴을 시작하지 않습니다. 이미 발사된 투사체·진행 중인 보스 공격/낙석과 레이저의 판정은 유지되어 피해를 받습니다. 공격하거나 피격돼도 은신은 시간 만료 전까지 유지합니다.
- 원본/환경 효과는 `Visible Opacity`(기본 0.22), `Noise Strength`, `Noise Pixel Size`, `Noise Rate`, `Target Renderers`로 그림을 조절합니다. Target Renderers가 비어 있으면 자신/자식 SpriteRenderer를 사용합니다. 원래 Sprite Atlas/Animator의 프레임은 그대로 표시하며 해제/비활성화 시 기존 Material로 복구합니다.
- Resources/ErrorEffects/StealthNoise.shader는 런타임 노이즈 표현입니다. 원본 이미지/애니메이션을 새로 만들 필요 없습니다.
