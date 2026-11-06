using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 발판 위 감지 영역에 들어온 Rigidbody2D의 질량을 합산합니다.
/// 요구 무게를 만족하면 지정한 물체를 시작 위치에서 위로 이동시킵니다.
/// 발판의 단단한 바닥과 별도로, 바로 위의 얇은 감지 영역에 붙이세요.
/// </summary>
[DisallowMultipleComponent]
public class balpan : MonoBehaviour
{
    [Header("무게 감지")]
    [Tooltip("작동에 필요한 Rigidbody2D.mass의 합계입니다. 예: 기본 질량 1인 물체가 3배 커지면 질량은 9입니다.")]
    [SerializeField, Min(0.01f)] private float requiredWeight = 5f;
    [Tooltip("무게를 감지할 물체의 레이어입니다. Trigger Collider와 Static Rigidbody2D는 제외합니다.")]
    [SerializeField] private LayerMask weightLayers = ~0;

    [Header("올릴 물체")]
    [Tooltip("버튼을 누르면 위로 이동할 물체입니다. Rigidbody2D가 있다면 Kinematic 설정을 권장합니다.")]
    [SerializeField] private Transform targetObject;
    [Tooltip("대상의 시작 위치에서 월드 Y축 방향으로 올라갈 거리입니다.")]
    [SerializeField, Min(0f)] private float riseHeight = 3f;
    [Tooltip("초당 이동 거리입니다. 올라갈 때와 내려올 때 동일하게 적용됩니다.")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;
    [Tooltip("켜면 무게가 부족해졌을 때 원위치로 내려옵니다. 끄면 한 번 작동한 뒤 올라간 상태를 유지합니다.")]
    [SerializeField] private bool returnWhenReleased = true;

    [Header("발판 눌림")]
    [Tooltip("아래로 눌릴 발판을 연결하세요. 비워 두면 이 오브젝트가 움직입니다. 실제 받침도 움직이려면 일반 Collider2D를 가진 발판을 연결하세요.")]
    [SerializeField] private Transform plateVisual;
    [Tooltip("무게 1 이하에서는 원래 위치를 유지합니다. 1을 초과한 무게에 비례해 내려가며 요구 무게에서 최대 거리에 도달합니다. 부모 기준 로컬 Y축을 사용합니다.")]
    [SerializeField, Min(0f)] private float pressDepth = 0.15f;
    [Tooltip("발판 그림이 눌리고 복귀하는 속도입니다.")]
    [SerializeField, Min(0.01f)] private float pressSpeed = 1f;
    [Tooltip("물체를 받치는 일반 Collider2D입니다(Is Trigger 끄기). Plate Visual 자신 또는 자식에 있어야 함께 움직입니다. 비우면 기존 일반 Collider만 찾습니다. 새 Collider를 생성하거나 크기를 변경하지 않습니다.")]
    [SerializeField] private Collider2D plateSurface;

    [Header("현재 상태 (실행 중 자동 갱신)")]
    [SerializeField] private float currentWeight;
    [SerializeField] private bool isPressed;

    public float CurrentWeight => currentWeight;
    public bool IsPressed => isPressed;

    private Collider2D sensor;
    private Rigidbody2D targetBody;
    private Vector3 targetStartPosition;
    private bool hasActivated;
    private Vector3 plateStartLocalPosition;
    private bool plateVisualReady;
    private Rigidbody2D plateBody;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<Rigidbody2D> countedBodies = new HashSet<Rigidbody2D>();
    private void Awake()
    {
        // 사용자가 만든 Collider를 감지 범위로 사용하며 Trigger 설정은 바꾸지 않습니다.
        sensor = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (plateVisual == null) plateVisual = transform;
        if (plateVisual != null)
        {
            if (plateVisual == targetObject)
            {
                Debug.LogWarning("balpan: 내려갈 발판과 위로 올릴 Target Object는 서로 다른 물체여야 합니다.", this);
            }
            else
            {
                plateStartLocalPosition = plateVisual.localPosition;
                plateBody = plateVisual.GetComponent<Rigidbody2D>();
                plateVisualReady = true;
                FindPlateSurface();
            }
        }

        if (targetObject == null)
        {
            Debug.LogWarning("balpan: Inspector의 Target Object에 올라갈 물체를 연결하세요.", this);
            return;
        }

        targetStartPosition = targetObject.position;
        targetBody = targetObject.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        UpdateWeight();
        isPressed = currentWeight >= Mathf.Max(0.01f, requiredWeight);
        if (isPressed) hasActivated = true;
        UpdatePlateVisual();

        if (targetObject == null) return;

        bool shouldRise = isPressed || (!returnWhenReleased && hasActivated);
        Vector3 destination = targetStartPosition;
        if (shouldRise) destination.y += Mathf.Max(0f, riseHeight);
        float step = Mathf.Max(0.01f, moveSpeed) * Time.fixedDeltaTime;

        if (targetBody != null)
        {
            Vector2 nextPosition = Vector2.MoveTowards(targetBody.position, destination, step);
            targetBody.MovePosition(nextPosition);
        }
        else
        {
            targetObject.position = Vector3.MoveTowards(targetObject.position, destination, step);
        }
    }

    private void UpdatePlateVisual()
    {
        if (!plateVisualReady || plateVisual == null) return;

        // 최초 위치에서 정해진 거리만 내려가므로 누르고 있어도 계속 가라앉지 않습니다.
        Vector3 destination = plateStartLocalPosition;
        // 기본 무게 1은 눌림에서 제외하고, 추가된 무게만 반영합니다.
        float pressure = Mathf.Clamp01((currentWeight - 1f) / Mathf.Max(0.01f, requiredWeight - 1f));
        destination.y -= Mathf.Max(0f, pressDepth) * pressure;
        // 물리 스텝에서 발판을 이동시켜 접촉 중인 Dynamic 물체를 밀어냅니다.
        Vector3 currentLocal = plateVisual.parent != null
            ? plateVisual.parent.InverseTransformPoint(new Vector3(plateBody.position.x, plateBody.position.y, plateVisual.position.z))
            : new Vector3(plateBody.position.x, plateBody.position.y, plateVisual.position.z);
        Vector3 nextPosition = Vector3.MoveTowards(currentLocal, destination,
            Mathf.Max(0.01f, pressSpeed) * Time.fixedDeltaTime);
        plateBody.MovePosition(PlateLocalToWorld(nextPosition));
    }

    private void FindPlateSurface()
    {
        if (plateBody == null) plateBody = plateVisual.gameObject.AddComponent<Rigidbody2D>();
        plateBody.bodyType = RigidbodyType2D.Kinematic;
        plateBody.simulated = true;
        plateBody.gravityScale = 0f;
        plateBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        plateBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (!IsPlateSurface(plateSurface))
        {
            plateSurface = null;
            foreach (Collider2D shape in plateVisual.GetComponentsInChildren<Collider2D>())
            {
                if (!IsPlateSurface(shape)) continue;
                plateSurface = shape;
                break;
            }
        }

        if (plateSurface == null)
        {
            plateVisualReady = false;
            Debug.LogWarning("balpan: 기존 받침 Collider2D를 Plate Surface에 연결하세요. Plate Visual 자신 또는 자식에 있고 Is Trigger가 꺼져 있어야 합니다. Collider는 자동 생성하지 않습니다.", this);
            return;
        }
        if (sensor == null) sensor = plateSurface;
        Physics2D.SyncTransforms();
    }

    private bool IsPlateSurface(Collider2D shape)
        => shape != null && !shape.isTrigger && shape.enabled
            && (shape.transform == plateVisual || shape.transform.IsChildOf(plateVisual))
            && shape.attachedRigidbody == plateBody;

    private Vector3 PlateLocalToWorld(Vector3 localPosition)
        => plateVisual.parent != null ? plateVisual.parent.TransformPoint(localPosition) : localPosition;

    private void UpdateWeight()
    {
        currentWeight = 0f;
        overlaps.Clear();
        countedBodies.Clear();
        if (sensor == null || !sensor.enabled || (sensor.attachedRigidbody != null && !sensor.attachedRigidbody.simulated)) return;

        // GiantErrorEffect_2가 Transform 크기를 변경한 결과도 현재 감지에 반영합니다.
        Physics2D.SyncTransforms();
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(weightLayers);
        // 감지 영역이 발판과 함께 내려가도 원래 위치부터 최대 눌림 위치까지 감지합니다.
        // 받침이 내려가거나 상자가 작아지는 동안 감지가 끊겨 발판이 떨리는 것을 방지합니다.
        Bounds detectionBounds = sensor.bounds;
        // 일반 받침을 감지에도 사용할 때는 표면 바로 위의 접촉 물체까지 검색합니다.
        // 검색 범위만 늘리며 실제 Collider의 크기나 충돌 간격은 변경하지 않습니다.
        if (!sensor.isTrigger) detectionBounds.Expand(new Vector3(0f, 0.1f, 0f));
        if (plateVisualReady && plateVisual != null)
        {
            if (sensor.transform == plateVisual || sensor.transform.IsChildOf(plateVisual))
                detectionBounds.center -= plateVisual.position - PlateLocalToWorld(plateStartLocalPosition);

            Vector3 travel = Vector3.down * Mathf.Max(0f, pressDepth);
            if (plateVisual.parent != null) travel = plateVisual.parent.TransformVector(travel);
            Bounds loweredBounds = detectionBounds;
            loweredBounds.center += travel;
            detectionBounds.Encapsulate(loweredBounds);
        }
        Physics2D.OverlapBox(detectionBounds.center, detectionBounds.size, 0f, filter, overlaps);

        foreach (Collider2D shape in overlaps)
        {
            // 플레이어 통과 설정으로 받침과 충돌하지 않는 물체는 무게에도 포함하지 않습니다.
            if (plateSurface != null && Physics2D.GetIgnoreCollision(shape, plateSurface)) continue;
            // 발판 자신의 받침 Collider는 누르는 물체로 세지 않습니다.
            if (plateVisualReady && plateVisual != null
                && (shape.transform == plateVisual || shape.transform.IsChildOf(plateVisual))) continue;
            Rigidbody2D body = shape.attachedRigidbody;
            if (body == null || !body.simulated || body.bodyType == RigidbodyType2D.Static
                || body == sensor.attachedRigidbody) continue;

            // 한 물체에 Collider가 여러 개 있어도 질량은 한 번만 더합니다.
            // 질량을 매번 다시 읽으므로 발판 위에서 거대화/원상복구해도 반영됩니다.
            if (countedBodies.Add(body)) currentWeight += body.mass;
        }
    }

    private void OnDisable()
    {
        // 비활성화할 때 발판을 순간이동시키면 위 물체와 겹칠 수 있으므로 현 위치를 유지합니다.
        if (plateBody != null) plateBody.linearVelocity = Vector2.zero;
        currentWeight = 0f;
        isPressed = false;
        overlaps.Clear();
        countedBodies.Clear();
    }
}
