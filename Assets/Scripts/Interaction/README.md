# 물체 상호작용

## Attachable
- MovableInteractable.cs: 플레이어가 F로 잡고 옮길 수 있는 물체에 부착합니다.

## Internal/Editor
- InteractionSetup.cs: 상호작용 Animator 상태·클립과 테스트 물체를 준비하는 편집기 메뉴 도구. 직접 부착하지 않으며 게임 빌드에 포함되지 않습니다.

플레이어의 상호작용 조작은 ../Player/Internal, 공용 충돌 이동 계산은 ../Shared/Internal/InteractionMotion.cs에 있습니다.
