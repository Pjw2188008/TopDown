using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>투명한 시작 메뉴 버튼의 포커스 장식, 글자 강조, 클릭 반응을 담당합니다.</summary>
[RequireComponent(typeof(Button))]
public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("강조할 글자. 버튼의 클릭 영역은 확대하지 않고 이 글자만 확대합니다.")]
    public Text label;
    [Tooltip("선택 시 나타나는 양옆 장식 묶음입니다. 위치와 모양은 자식 오브젝트에서 편집합니다.")]
    public CanvasGroup ornaments;
    [Tooltip("평소 글자색입니다.")] public Color normalColor = new Color(.9f, .86f, .76f);
    [Tooltip("마우스/키보드 선택 시 글자색입니다.")] public Color selectedColor = new Color(1f, .94f, .76f);
    [Tooltip("클릭이 접수된 순간의 글자색입니다.")] public Color clickedColor = Color.white;
    [Tooltip("선택 시 글자의 확대 배율입니다.")] [Range(1f, 1.2f)] public float selectedScale = 1.07f;
    [Tooltip("클릭 강조와 화면 전환 전 대기 시간입니다. 게임 시간 정지와 무관합니다.")]
    [Range(.05f, .5f)] public float clickDuration = .18f;
    [Tooltip("선택 강조가 부드럽게 변하는 속도입니다.")] [Min(1f)] public float responseSpeed = 18f;
    private Button button;
    private Vector3 restingScale;
    private bool hovered, held;
    private float flashUntil;

    private void Awake() => button = GetComponent<Button>();

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (label != null) restingScale = label.rectTransform.localScale;
        hovered = held = false;
        flashUntil = float.NegativeInfinity;
        Refresh(true);
    }

    private void LateUpdate() => Refresh(false);

    private void Refresh(bool immediate)
    {
        if (label == null || button == null) return;
        bool available = button.IsInteractable();
        bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        bool focused = available && (hovered || selected);
        bool flashing = Time.unscaledTime < flashUntil;
        Color targetColor = flashing ? clickedColor : focused ? selectedColor : normalColor;
        if (!available && !flashing) targetColor.a *= .45f;
        float scale = flashing ? selectedScale + .03f : held && available ? .97f : focused ? selectedScale : 1f;
        float blend = immediate ? 1f : 1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime);
        label.color = Color.Lerp(label.color, targetColor, blend);
        label.rectTransform.localScale = Vector3.Lerp(label.rectTransform.localScale, restingScale * scale, blend);
        if (ornaments != null) ornaments.alpha = Mathf.Lerp(ornaments.alpha, focused || flashing ? 1f : 0f, blend);
    }

    /// <summary>Button.onClick이 실제로 접수된 뒤 호출합니다. 취소된 드래그에는 반응하지 않습니다.</summary>
    public float PlayClick()
    {
        flashUntil = Time.unscaledTime + clickDuration;
        held = false;
        Refresh(true);
        return clickDuration;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.IsInteractable()) return;
        hovered = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }
    public void OnPointerExit(PointerEventData eventData) { hovered = held = false; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && button.IsInteractable()) held = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) held = false;
    }
    public void OnSelect(BaseEventData eventData) => Refresh(false);
    public void OnDeselect(BaseEventData eventData) { hovered = held = false; }
    private void OnDisable()
    {
        if (label != null) { label.rectTransform.localScale = restingScale; label.color = normalColor; }
        if (ornaments != null) ornaments.alpha = 0;
        hovered = held = false;
        flashUntil = float.NegativeInfinity;
    }
}
