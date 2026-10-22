# 근접 몬스터
`Attachable/MeleeEnemy.cs` 하나를 사용합니다. 기존 EnemyController / TriangleMeleeMonster / MovingEnemy를 함께 붙이지 않습니다.

몬스터 프리팹을 Hierarchy에 직접 배치하고 Aggro Mode, Chase Area Offset / Size를 설정합니다. 타격 위치는 Hit Offset, 크기는 Hit Size로 조절하며 왼쪽에서는 X 위치가 반전됩니다. Attack 상태의 Hit Normalized Time에서 한 번 타격합니다.

기존 Enemy, MeleeMonster, mon_1 프리팹은 이 AI를 사용합니다. 이미지와 Animator 연결은 유지하며 기본 모드는 일반 공격입니다. 보스 공격은 변경하지 않았습니다.

## Attack Mode — Inspector에서 선택

- **일반 공격 (Normal)**: 기존처럼 Hit Offset / Hit Size 박스에서 타격 프레임에 한 번 공격합니다.
- **좌우 파동 (Wave)**: 타격 프레임에 왼쪽·오른쪽 파동 두 개를 발사합니다. 일반 근접 타격을 함께 발생시키지 않습니다. Wave Offset / Speed / Distance / Hit Radius / Visual Size를 조절하세요. 그림 크기와 원형 판정 반지름은 별도입니다. 파동은 직진하며 벽에서 사라집니다(바닥 굴곡을 따라가는 파동이 아닙니다).
- **돌진 찌르기 (Thrust)**: 타격 프레임부터 공격 시작 때 바라본 방향으로 전진합니다. Thrust Distance는 거리, Duration은 시간, Acceleration Power는 가속 느낌(1=일정 속도, 2=점점 빨라짐)입니다. 전진 중 Hit Offset / Hit Size로 한 번만 피해를 주며, 패링/피격/사망/비활성화/플레이어 영역 이탈 시 취소합니다. 벽/영역 경계와 Stop At Ledges 설정을 따릅니다.

### 공통 설정

- **일반 공격**은 플레이어의 실제 몸 Collider가 **Hit Offset / Hit Size** 박스에 들어와야 시작합니다. Attack Range / Attack Vertical Range는 일반 공격에 사용하지 않습니다.
- **파동·찌르기**는 **Attack Range / Attack Vertical Range**로 발동 범위를 조절합니다. 주황색 사각형이 발동 범위입니다.
- 범위 밖에서는 추격하고, 안에서는 멈춰 쿨타임 후 공격합니다. 근접 몬스터는 Stopping Distance 때문에 공격 범위 밖에서 멈추지 않습니다. 벽/낭떠러지 제한은 유지합니다.
- 타격/발사 전에 플레이어가 범위를 벗어나거나 시야가 막히면 공격 준비를 취소하고 추격합니다. 이미 타격한 일반 공격의 후딜, 발사한 파동, 시작한 돌진은 범위 이탈만으로 끊지 않습니다. 추적 영역 이탈·피격·패링·사망에 의한 기존 취소 규칙은 유지합니다.
- 모든 모드는 현재 **Attack State**를 사용합니다. 새 Animator를 코드로 생성하거나 원본 클립을 덮어쓰지 않습니다. 몬스터별 Controller/Attack State에 해당 애니메이션을 연결하세요.
- **Attack Hit Frame**(첫 프레임=1) 또는 **Hit Normalized Time**이 일반 타격 / 파동 발사 / 찌르기 시작 시점입니다. 찌르기가 클립보다 길면 전진 종료까지 공격 상태를 유지합니다. 모드 변경은 다음 공격부터 적용됩니다.
- 일반·찌르기는 Melee Parry Window를 사용합니다. 파동은 기존 투사체 패링으로 되돌릴 수 있고 성공하면 발사 몬스터가 경직됩니다. 적이 발사한 파동은 다른 몬스터에게 피해를 주지 않으며, 패링되어 플레이어 소유가 된 파동은 적에게 피해를 줍니다.
- **Wave Sprite**에 오른쪽을 향하는 이미지를 넣으세요. 이미지가 없으면 임시 사각형입니다. 애니메이션은 **Wave Animation Controller**에 선택적으로 연결합니다. 기본 상태에서 SpriteRenderer.Sprite 프레임을 재생하면 됩니다. 크기와 Flip X는 파동 코드가 제어하므로 해당 속성에 애니메이션 키를 넣지 마세요. 중앙 Pivot 이미지를 권장합니다.
- **Keep Wave Above Feet**는 파동 판정이 몸의 밑면보다 아래에서 생성되지 않게 합니다. 지형 안에서 생성되면 즉시 사라질 수 있으므로 Wave Offset Y / Hit Radius를 조정하세요.
- 선택 시 노랑=근접 타격, 빨강=찌르기 최대 전진 위치, 청록=파동 판정/경로, 파랑=파동 그림 크기입니다.

전체 설정은 [몬스터 안내](../README.md)를 참고하세요.
