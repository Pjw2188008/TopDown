using UnityEngine;
using UnityEngine.UI;

/// <summary>CameraFollow의 구역 전환 연출입니다. 별도 컴포넌트로 부착하지 않습니다.</summary>
public partial class CameraFollow
{
    [Header("구역 전환 페이드")]
    [Tooltip("서로 다른 CameraBoundsArea로 들어가면 암전 후 카메라를 옮깁니다. 시작 구역 배치에는 페이드하지 않습니다.")]
    [SerializeField] private bool fadeBetweenAreas = true;
    [Tooltip("켜면 PlayerMove의 사이드뷰 모드에서만 페이드합니다. 기존 탑다운의 즉시 전환은 유지합니다.")]
    [SerializeField] private bool fadeOnlyInSideView = true;
    [Tooltip("기존 화면이 검게 변하는 실제 시간(초)입니다. 0이면 즉시 암전합니다.")]
    [Min(0f), SerializeField] private float fadeOutDuration = .18f;
    [Tooltip("완전한 암전 유지 시간(초)입니다. 0이어도 카메라 이동을 가릴 한 프레임은 유지합니다.")]
    [Min(0f), SerializeField] private float blackHoldDuration = .08f;
    [Tooltip("새 구역 화면이 밝아지는 실제 시간(초)입니다. 편집 모드 슬로모션에 영향을 받지 않습니다.")]
    [Min(0f), SerializeField] private float fadeInDuration = .22f;

    private enum RoomFadePhase { Idle, FadeOut, Black, FadeIn }
    private RoomFadePhase roomFadePhase;
    private bool hasResolvedRoom;
    private float roomFadeAlpha;
    private float blackElapsed;
    private Canvas roomFadeCanvas;
    private Image roomFadeImage;

    public bool IsChangingCameraArea => roomFadePhase != RoomFadePhase.Idle;

    // 선택은 매 프레임 갱신하지만 카메라의 실제 구역 교체는 완전한 암전 중에만 수행합니다.
    // 밝아지는 도중 또 다른 방으로 이동하면 현재 페이드를 마친 뒤 다음 전환을 시작합니다.
    private void UpdateRoomTransition(CameraBoundsArea nextArea, bool canFade, float deltaTime)
    {
        if (!hasResolvedRoom || !canFade)
        {
            ResetRoomTransition();
            activeArea = nextArea;
            hasResolvedRoom = true;
            return;
        }

        float dt = Mathf.Max(0f, deltaTime);
        switch (roomFadePhase)
        {
            case RoomFadePhase.Idle:
                if (nextArea == activeArea) return;
                // 경계 없는 자유 추적/최초 구역 진입은 기존처럼 즉시 적용합니다.
                if (activeArea == null || nextArea == null)
                {
                    activeArea = nextArea;
                    return;
                }
                roomFadePhase = RoomFadePhase.FadeOut;
                break;
            case RoomFadePhase.Black:
                activeArea = nextArea;
                blackElapsed += dt;
                if (blackElapsed >= Mathf.Max(0f, blackHoldDuration)) roomFadePhase = RoomFadePhase.FadeIn;
                return;
            case RoomFadePhase.FadeIn:
                SetRoomFadeAlpha(fadeInDuration <= 0f ? 0f : Mathf.MoveTowards(roomFadeAlpha, 0f, dt / fadeInDuration));
                if (roomFadeAlpha <= 0f) roomFadePhase = RoomFadePhase.Idle;
                return;
        }

        SetRoomFadeAlpha(fadeOutDuration <= 0f ? 1f : Mathf.MoveTowards(roomFadeAlpha, 1f, dt / fadeOutDuration));
        if (roomFadeAlpha >= 1f)
        {
            activeArea = nextArea;
            blackElapsed = 0f;
            roomFadePhase = RoomFadePhase.Black;
        }
    }

    private void SetRoomFadeAlpha(float alpha)
    {
        roomFadeAlpha = Mathf.Clamp01(alpha);
        if (roomFadeAlpha > 0f && roomFadeImage == null) CreateRoomFadeOverlay();
        if (roomFadeImage != null) roomFadeImage.color = new Color(0f, 0f, 0f, roomFadeAlpha);
        if (roomFadeCanvas != null)
        {
            roomFadeCanvas.targetDisplay = viewCamera != null ? viewCamera.targetDisplay : 0;
            roomFadeCanvas.enabled = roomFadeAlpha > 0f;
        }
    }

    private void CreateRoomFadeOverlay()
    {
        // 검은 화면 한 장만 런타임 생성. 입력/타임스케일/플레이어 위치에는 관여하지 않습니다.
        var root = new GameObject("Camera Room Fade (Runtime)", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        roomFadeCanvas = root.GetComponent<Canvas>();
        roomFadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        roomFadeCanvas.sortingOrder = 32760;
        var panel = new GameObject("Black Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        roomFadeImage = panel.GetComponent<Image>();
        roomFadeImage.raycastTarget = false;
        var rect = roomFadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void ResetRoomTransition()
    {
        roomFadePhase = RoomFadePhase.Idle;
        hasResolvedRoom = false;
        blackElapsed = 0f;
        SetRoomFadeAlpha(0f);
    }

    private void DestroyRoomFadeOverlay()
    {
        if (roomFadeCanvas == null) return;
        if (Application.isPlaying) Destroy(roomFadeCanvas.gameObject);
        else DestroyImmediate(roomFadeCanvas.gameObject);
        roomFadeCanvas = null;
        roomFadeImage = null;
    }
}
