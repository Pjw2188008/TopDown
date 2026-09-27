using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격/패링 교육 전용 고정 허수아비. 일반 적 AI와 중복 부착하지 않습니다.
/// TutorialSequence가 구역 도착 후 활성화합니다. 죽지 않으며 기존 ReflectProjectile의 피해/가드/패링 판정을 사용합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class TutorialTrainingDummy : MonoBehaviour, ICombatDamageable
{
    [Header("연습 투사체")]
    [SerializeField, Tooltip("새 발사 준비 시작 사이의 간격(초). 준비 시간보다 짧으면 준비 시간이 우선합니다."), Min(.1f)] private float shotInterval = 2.5f;
    [SerializeField, Tooltip("연습 시작 후 첫 발사 준비까지 대기 시간(초)."), Min(0f)] private float firstShotDelay = 1.5f;
    [SerializeField, Tooltip("발사 전 노란 표시가 켜지는 시간(초)."), Min(.05f)] private float windupSeconds = .6f;
    [SerializeField, Tooltip("이 거리 안에 플레이어가 있을 때만 발사합니다."), Min(.1f)] private float attackRange = 8f;
    [SerializeField, Tooltip("초당 투사체 이동 거리. 발사 순간 플레이어 위치를 향하며 추적하지 않습니다."), Min(.1f)] private float projectileSpeed = 4f;
    [SerializeField, Tooltip("패링 실패 시 기존 피해/가드 규칙을 따릅니다. 0이면 기존 투사체의 패링 검사가 실행되지 않으므로 양수로 설정합니다."), Min(.1f)] private float projectileDamage = 1f;
    [SerializeField, Tooltip("투사체 충돌 반지름(월드 단위)."), Min(.01f)] private float projectileRadius = .12f;
    [SerializeField, Tooltip("투사체 수명(초)."), Min(.1f)] private float projectileLifetime = 6f;
    [SerializeField, Tooltip("허수아비 중심에서 발사 방향으로 떨어진 거리."), Min(0f)] private float muzzleDistance = .6f;
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("선택적 투사체 이미지. 비우면 임시 사각형을 사용합니다.")] private Sprite projectileSprite;
    [SerializeField, Tooltip("투사체 색상.")] private Color projectileColor = new Color(1f, .65f, .15f);
    [SerializeField, Tooltip("패링에 성공해 허수아비에게 돌아가는 탄환의 색상입니다.")] private Color returnedProjectileColor = new Color(.2f, 1f, 1f);
    [SerializeField, Tooltip("발사 준비 표시 위치(허수아비 로컬 좌표). 직접 조절할 수 있습니다.")] private Vector3 warningOffset = new Vector3(0f, .8f, 0f);

    private PlayerMove practicePlayer;
    private bool practicing;
    private bool charging;
    private bool finishingReturnShots;
    private float nextShotAt;
    private float fireAt;
    private SpriteRenderer visual;
    private SpriteRenderer warning;
    private static Sprite fallbackSprite;
    private readonly List<GameObject> shots = new List<GameObject>();

    public int AttackSuccessCount { get; private set; }
    public int ParrySuccessCount { get; private set; }
    public bool IsPracticingFor(PlayerMove player) => practicing && player != null && practicePlayer == player && isActiveAndEnabled;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        if (visual.sprite == null) visual.sprite = GetFallbackSprite();
    }

    /// <summary>진행 중인 같은 플레이어의 연습은 유지합니다. 다른 플레이어는 기록을 초기화합니다.</summary>
    public void BeginPractice(PlayerMove player)
    {
        if (player == null || IsPracticingFor(player)) return;
        if (practicePlayer != player) { AttackSuccessCount = 0; ParrySuccessCount = 0; }
        practicePlayer = player;
        gameObject.SetActive(true);
        enabled = true;
        practicing = true;
        finishingReturnShots = false;
        charging = false;
        nextShotAt = Time.time + Mathf.Max(0f, firstShotDelay);
    }

    /// <summary>발사를 멈추고 이 허수아비가 만든 탄환만 제거합니다. 일반 적의 탄환은 유지합니다.</summary>
    public void EndPractice(bool hide)
    {
        practicing = false;
        finishingReturnShots = false;
        charging = false;
        if (warning != null) warning.enabled = false;
        foreach (var shot in shots)
            if (shot != null) { shot.SetActive(false); Destroy(shot); }
        shots.Clear();
        if (hide) gameObject.SetActive(false);
    }

    /// <summary>성공 완료: 추가 발사와 적대 탄환만 멈추고 실제 패링된 탄환의 귀환은 끝까지 보여 줍니다.</summary>
    public void FinishPractice()
    {
        practicing = false;
        charging = false;
        if (warning != null) warning.enabled = false;
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var shot = shots[i];
            if (shot != null && shot.TryGetComponent<ReflectProjectile>(out var projectile)
                && projectile.WasParriedBy(practicePlayer)) continue;
            if (shot != null) { shot.SetActive(false); Destroy(shot); }
            shots.RemoveAt(i);
        }
        finishingReturnShots = shots.Count > 0;
    }

    /// <summary>연습용 반사 탄환만 색상을 변경합니다. 일반 적 탄환이나 스프라이트 이미지는 바꾸지 않습니다.</summary>
    public void ShowReturnedProjectile(ReflectProjectile projectile)
    {
        if (projectile != null && projectile.TryGetComponent<SpriteRenderer>(out var renderer))
            renderer.color = returnedProjectileColor;
    }

    public void ResetPractice()
    {
        EndPractice(true);
        practicePlayer = null;
        AttackSuccessCount = 0;
        ParrySuccessCount = 0;
    }

    /// <summary>PlayerMove의 실제 근접 공격 적중 경로에서만 호출합니다. 반환 투사체는 공격 실적으로 세지 않습니다.</summary>
    public void RecordAttack(PlayerMove player) { if (IsPracticingFor(player)) AttackSuccessCount++; }
    /// <summary>이 허수아비의 탄환이 실제로 패링되어 돌아가는 순간에만 호출합니다.</summary>
    public void RecordParry(PlayerMove player) { if (IsPracticingFor(player)) ParrySuccessCount++; }

    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (!isActiveAndEnabled || amount <= 0f) return false;
        if (!practicing && !(finishingReturnShots && practicePlayer != null && source == practicePlayer.gameObject)) return false;
        DamageBlink.Play(gameObject);
        return true; // 반복 연습용이므로 체력 소모/사망 없음.
    }

    private void Update()
    {
        shots.RemoveAll(shot => shot == null);
        if (shots.Count == 0) finishingReturnShots = false;
        if (!practicing || Time.deltaTime <= 0f) return;
        if (practicePlayer == null || !practicePlayer.isActiveAndEnabled || practicePlayer.IsInstantlyDead
            || Vector2.Distance(transform.position, practicePlayer.transform.position) > Mathf.Max(.1f, attackRange))
        { charging = false; SetWarning(false); return; }
        if (!charging && Time.time >= nextShotAt)
        {
            charging = true;
            fireAt = Time.time + Mathf.Max(.05f, windupSeconds);
            nextShotAt = Time.time + Mathf.Max(shotInterval, windupSeconds);
        }
        SetWarning(charging);
        if (charging && Time.time >= fireAt)
        {
            charging = false;
            SetWarning(false);
            Fire();
        }
    }

    private void Fire()
    {
        if (!IsPracticingFor(practicePlayer)) return;
        Vector2 aim = practicePlayer.transform.position - transform.position;
        Vector2 direction = aim.sqrMagnitude > .0001f ? aim.normalized : Vector2.down;
        var shot = new GameObject("Tutorial Dummy Projectile");
        shot.transform.position = transform.position + (Vector3)(direction * Mathf.Max(0f, muzzleDistance));
        var renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite != null ? projectileSprite : GetFallbackSprite();
        renderer.color = projectileColor;
        renderer.sortingLayerID = visual.sortingLayerID;
        renderer.sortingOrder = visual.sortingOrder + 1;
        float radius = Mathf.Max(.01f, projectileRadius);
        Vector2 size = renderer.sprite.bounds.size;
        shot.transform.localScale = new Vector3(2f * radius / Mathf.Max(.001f, size.x), 2f * radius / Mathf.Max(.001f, size.y), 1f);
        // 충돌은 ReflectProjectile의 월드 CircleCast가 담당하며, 투사체끼리는 기존 규칙대로 무시합니다.
        shot.AddComponent<ReflectProjectile>().Initialize(gameObject, direction, Mathf.Max(.1f, projectileSpeed),
            Mathf.Max(.1f, projectileDamage), radius, Mathf.Max(.1f, projectileLifetime), 0, false);
        shots.Add(shot);
    }

    private void SetWarning(bool visible)
    {
        if (!visible && warning == null) return;
        if (warning == null)
        {
            var marker = new GameObject("Shot Warning");
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = new Vector3(.2f, .2f, 1f);
            warning = marker.AddComponent<SpriteRenderer>();
            warning.sprite = GetFallbackSprite(); warning.color = Color.yellow;
            warning.sortingLayerID = visual.sortingLayerID; warning.sortingOrder = visual.sortingOrder + 2;
        }
        warning.transform.localPosition = warningOffset;
        warning.enabled = visible;
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;
        var texture = new Texture2D(1, 1) { name = "Tutorial Dummy Temporary Texture" };
        texture.SetPixel(0, 0, Color.white); texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1f);
        return fallbackSprite;
    }

    private void OnDisable() { EndPractice(false); }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, Mathf.Max(.1f, attackRange));
        Gizmos.DrawWireCube(transform.TransformPoint(warningOffset), Vector3.one * .2f);
    }
}
