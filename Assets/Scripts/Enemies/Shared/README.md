# 몬스터 공통 보조 코드

> 사이드뷰 전용 정리: 플레이어 모드 선택·상하 이동/공격/가드/패링·2.5D 높이 보정 및 몬스터 탑다운 격자 길찾기를 제거했습니다. 기존 시점 플래그 값과 무관하게 좌우 이동·중력을 사용합니다. 사이드뷰 Animator와 그림, 씬/프리팹 참조는 유지합니다.
- Attachable/EnemyStagger: 패링으로 인한 경직 시간을 관리합니다. 일반 몬스터 AI가 자동 확보합니다.
- Internal/CombatParry: 패링 결과를 원래 공격자에게 전달합니다.
- Internal/EnemyNavigation, EnemySideView: 충돌·시야·사이드뷰 중력·발밑 검사 등을 계산합니다. 보스도 사용하므로 유지합니다.

이 폴더에 별도의 일반 몬스터 AI는 없습니다. 기존 MovingEnemy 순찰 AI는 제거했습니다. 일반 몬스터에는 MeleeEnemy 또는 RangedEnemy 중 하나만 사용합니다.
