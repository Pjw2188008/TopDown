# 원거리 몬스터
`Attachable/RangedEnemy.cs` 하나를 사용합니다. 스포너 없이 프리팹을 Hierarchy에 직접 배치합니다.

Aggro Mode와 Chase Area Offset / Size로 추적을 설정하고, Projectile Prefab에 투사체 프리팹을 연결합니다. Muzzle Offset은 오른쪽 기준이며 왼쪽에서 X가 반전됩니다. Projectile Speed로 속도를 조절합니다.

기존 준석 RangedEnemyController 프리팹도 이 AI로 통합되었습니다. 기존 SpriteRenderer/Animator/Projectile 프리팹은 유지합니다. 공격은 단일 투사체이며 기존 부채꼴 패턴은 제거되었습니다.

`Internal/ReflectProjectile.cs`는 탄환의 이동·피해·반사·패링을 담당하는 보조 컴포넌트입니다. 일반 몬스터뿐 아니라 보스와 허수아비도 사용하므로 삭제하지 않습니다. 준석 Projectile.cs는 기존 투사체 프리팹의 이미지 크기/회전을 연결하는 어댑터입니다.

전체 설정은 [몬스터 안내](../README.md)를 참고하세요.
