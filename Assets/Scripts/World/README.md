# 전투 구역 잠금

## 적용된 구역

REDACT 씬의 기존 원거리 스포너 위치(-39.43, -23.4), 감지 범위 7.7 × 7.7에 `Combat Area - Ranged Spawner`를 추가했습니다. 기존 맵/카메라/스포너 범위는 바꾸지 않습니다.

- 플레이어 몸 전체가 구역 안으로 들어오면 네 방향의 청록색 임시 벽이 활성화됩니다. 중심만 들어온 순간 닫아서 몸이 입구에 끼는 것을 피합니다.
- 입장 시 범위 안의 EnemyController/MeleeEnemy/RangedEnemy와 지정/범위 내 원거리 스포너를 수집합니다. 지정 스포너는 감지 범위와 무관하게 즉시 생성하므로 아직 생성되지 않은 적 때문에 전멸 판정을 먼저 하지 않습니다.
- 등록된 적의 루트가 모두 Destroy되면 벽이 사라집니다. 현재 적 스크립트의 사망 처리는 Destroy입니다. 비활성화/영역 이탈은 처치로 세지 않습니다. 진행 중인 구역 밖에서 나중에 따로 생성하는 웨이브는 자동 등록하지 않습니다.
- 전투 중 스포너 자동 재생성은 잠시 중지하고 종료/중단 시 원래 enabled 상태를 복원합니다. Spawn Once 규칙은 유지합니다. 같은 스포너를 여러 전투 구역이 공유하지 마세요.
- 클리어한 구역은 이번 씬 실행 중 다시 잠기지 않습니다. 저장 파일에는 기록하지 않으며 씬을 다시 시작하면 초기화합니다.
- 즉사 복귀/텔레포트로 플레이어가 밖으로 이동하거나 컴포넌트를 끄면 벽을 엽니다. 적은 임의로 삭제하지 않습니다. 미클리어 시 재입장하면 남은 적을 다시 추적합니다.
- 적이 없으면 잠그지 않고 클리어합니다. 벽/몬스터 프리팹 등 필수 연결이 잘못되면 경고 후 잠그지 않습니다.

## Inspector 조절

- `Combat Area - Ranged Spawner`의 BoxCollider2D: 입장 감지 범위. Is Trigger ON. 위치/Size를 직접 조정합니다.
- `Combat Area Lock → Entry Area`: 위 감지 Collider 연결. `Spawners`: 기존 RangedEnemySpawner 연결. `Enemies`: 별도로 처치 대상으로 넣을 적의 루트입니다.
- `Barrier Left / Right / Bottom / Top`: 실제 벽입니다. 각 Transform/BoxCollider2D/Renderer를 직접 편집할 수 있습니다. 감지 영역 크기를 바꾸면 네 벽 위치와 크기도 함께 조정하세요. 벽은 감지 영역 바로 바깥쪽에 두세요.
- 벽의 BoxCollider2D는 Is Trigger OFF, Layer는 Default입니다. 걷기/대쉬/상호작용의 Blocking Layers에 해당 레이어가 포함되어야 합니다. 현재 플레이어 설정은 포함되어 있습니다. 장벽 표시 이미지는 SpriteRenderer에서 교체할 수 있습니다.
- 장벽 오브젝트는 시작 시 꺼져 있으므로 편집 중 모습을 보려면 자식을 잠깐 활성화하고, Play 시에는 구역 스크립트가 상태를 관리합니다. Entry Area나 플레이어/구역 루트 자체를 Barriers에 넣지 마세요.
- 입장/클리어 연출은 `On Locked` / `On Cleared` 이벤트로 추가할 수 있습니다. 타이틀/튜토리얼 진행은 자동 변경하지 않습니다.

## 파일

- `Attachable/CombatAreaLock.cs`: 구역 루트에 붙이는 입장·적 추적·벽 상태 관리.
- `Internal/Editor/CombatAreaSetup.cs`: 오브젝트에 붙이지 않는 편집기 도구. 스포너를 선택한 뒤 `Tools > Story > Combat Area > Create Around Selected Spawner`로 다른 구역에도 배치할 수 있습니다. 같은 곳에 중복 생성하지 마세요.
- 벽은 일반 Collider라 기존 이동/투사체 충돌 규칙을 따릅니다. 플레이어 이동 코드와 몬스터 피해 처리는 변경하지 않았습니다.
