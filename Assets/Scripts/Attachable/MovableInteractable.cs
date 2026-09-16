using UnityEngine;

/// <summary>
/// F로 잡아 플레이어와 함께 밀고 당기는 물체입니다. 옮길 물체 루트에 직접 부착합니다.
/// 위치 관계를 유지해 이동하며 부모/크기를 바꾸지 않습니다. 단독 이동 스크립트와 함께 사용하지 마세요.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MovableInteractable : MonoBehaviour
{
    [Tooltip("플레이어 가까이에서 표시할 물체 이름입니다.")]
    [SerializeField] private string interactionName = "물체";
    [Tooltip("이 물체를 잡고 이동할 때 걷기 속도에 곱할 배율입니다. 달리기/대쉬는 사용하지 않습니다.")]
    [SerializeField, Range(0.1f, 1f)] private float moveSpeedMultiplier = 0.6f;
    private Transform holder;
    private Rigidbody2D body;
    private RigidbodyType2D previousBodyType;
    private bool changedBody;
    public string InteractionName => string.IsNullOrWhiteSpace(interactionName) ? name : interactionName;
    public float MoveSpeedMultiplier => Mathf.Clamp(moveSpeedMultiplier, 0.1f, 1f);
    public bool IsHeld => holder != null;
    public bool IsHeldBy(Transform candidate) => holder == candidate && candidate != null;
    public bool IsAvailable => isActiveAndEnabled && !IsHeld && GetComponent<Collider2D>() is Collider2D shape
        && shape.enabled && !shape.isTrigger && (GetComponent<Rigidbody2D>() == null || GetComponent<Rigidbody2D>().simulated);

    public bool TryGrab(Transform candidate)
    {
        if (candidate == null || !IsAvailable || transform.IsChildOf(candidate) || candidate.IsChildOf(transform)) return false;
        // A child Rigidbody or an independently moving child needs its own interaction implementation.
        foreach (Rigidbody2D child in GetComponentsInChildren<Rigidbody2D>())
            if (child.transform != transform) return false;
        holder = candidate;
        body = GetComponent<Rigidbody2D>();
        changedBody = body != null;
        if (changedBody)
        {
            previousBodyType = body.bodyType;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0f;
        }
        return true;
    }

    public void Release(Transform candidate)
    {
        if (holder != candidate) return;
        ReleaseInternal();
    }

    public void MoveBy(Transform candidate, Vector2 displacement)
    {
        if (!IsHeldBy(candidate) || !isActiveAndEnabled) return;
        Vector3 next = transform.position + (Vector3)displacement;
        if (body != null) body.position = next;
        transform.position = next;
    }

    private void ReleaseInternal()
    {
        holder = null;
        if (changedBody && body != null)
        {
            body.bodyType = previousBodyType;
            // Do not relaunch the object with velocity from before it was grabbed.
            if (body.bodyType != RigidbodyType2D.Static)
            { body.linearVelocity = Vector2.zero; body.angularVelocity = 0f; }
        }
        changedBody = false; body = null;
    }
    private void Update() { if (changedBody && holder == null) ReleaseInternal(); }
    private void OnDisable() => ReleaseInternal();
}
