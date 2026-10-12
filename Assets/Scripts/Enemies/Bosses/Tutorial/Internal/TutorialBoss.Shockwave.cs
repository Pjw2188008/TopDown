using System.Collections.Generic;
using UnityEngine;

/// <summary>보스 중심에서 네 방향으로 발사하는 충격파. TutorialBoss partial이며 직접 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("네 갈래 충격파")]
    [SerializeField, Tooltip("거리와 무관하게 충격파 패턴을 사용합니다. 기존 근접/낙석 공격이 끝난 뒤 시작합니다.")] private bool enableShockwave = true;
    [SerializeField, Min(0f), Tooltip("방 입장 후 첫 충격파 준비까지의 최소 시간입니다.")] private float shockwaveFirstDelay = 5f;
    [SerializeField, Min(.1f), Tooltip("충격파 공격 시작 사이의 최소 간격입니다. 다른 공격 중이면 끝날 때까지 기다립니다.")] private float shockwaveInterval = 9f;
    [SerializeField, Min(.1f), Tooltip("충격파 애니메이션 시작부터 발사 프레임까지의 시간입니다. 애니메이션이 없어도 이 시간 뒤 발사합니다.")] private float shockwaveWindupTime = 1f;
    [SerializeField, Min(2), Tooltip("Boss_Shockwave 클립의 발사 프레임 번호(첫 프레임=1). 이후 후딜도 끝까지 재생합니다.")] private int shockwaveReleaseFrame = 9;
    [SerializeField, Tooltip("보스 중심 기준 발사 위치입니다. 월드 단위이며 보스의 좌우 반전과 무관합니다.")] private Vector2 shockwaveOriginOffset;
    [SerializeField, Range(0f,90f), Tooltip("0이면 상하좌우(+), 45이면 대각선(X)입니다. 기본은 0입니다.")] private float shockwaveAngle;
    [SerializeField, Min(.1f), Tooltip("충격파의 초당 이동 거리입니다.")] private float shockwaveSpeed = 5f;
    [SerializeField, Min(.1f), Tooltip("충격파의 최대 이동 거리입니다. 수명=거리/속도로 계산합니다. 벽에 닿거나 방 밖으로 나가면 먼저 사라집니다.")] private float shockwaveDistance = 10f;
    [SerializeField, Min(.05f), Tooltip("충격파의 기본 원형 타격 반지름입니다. 실제 판정과 기즈모에는 Shockwave Visual Scale 배율을 곱합니다.")] private float shockwaveRadius = .35f;
    [SerializeField, Min(.1f), Tooltip("충격파 한 번의 피해량입니다. 기존 투사체 가드/패링/반사 규칙을 사용합니다.")] private float shockwaveDamage = 2f;
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("충격파 이미지입니다. 비우면 밝은 원형 임시 그림을 사용합니다. 중앙 피벗 이미지를 권장합니다.")] private Sprite shockwaveSprite;
    [SerializeField, Tooltip("충격파 색상입니다.")] private Color shockwaveColor = new Color(.5f,.9f,1f,1f);
    [SerializeField, Tooltip("충격파 표시 순서입니다. Sorting Layer는 보스와 같습니다.")] private int shockwaveSortingOrder = 30;
    [SerializeField, Tooltip("충격파 이펙트 프리팹입니다. 비우면 Resources/TutorialBossEffects/ShockwaveEffect를 자동 사용합니다. 프레임은 프리팹의 Shockwave_Fly 클립에서 편집하세요.")] private GameObject shockwaveEffectPrefab;
    [SerializeField, Min(.01f), Tooltip("충격파 그림과 타격 반지름을 함께 확대/축소합니다. 예: Radius 0.35 × Scale 3 = 실제 반지름 1.05. 발사된 충격파는 생성 당시 크기를 유지합니다.")] private float shockwaveVisualScale = 1f;

    private float shockwaveRemaining;
    private readonly List<GameObject> shockwaveShots = new List<GameObject>();
    private Vector2 ShockwaveOrigin => (Vector2)transform.position + shockwaveOriginOffset;
    // 공통 투사체의 최소 반지름(0.01)까지 맞춰 그림/판정/기즈모가 같은 크기 기준을 씁니다.
    private float ShockwaveHitRadius => Mathf.Max(.01f, Mathf.Max(.05f, shockwaveRadius) * Mathf.Max(.01f, shockwaveVisualScale));
    private Vector2 ShockwaveDirection(int index)
    {
        float angle = (shockwaveAngle + index * 90f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private void BeginShockwave()
    {
        shockwaveRemaining = Mathf.Max(.1f, shockwaveInterval);
        FaceAttackDirection(player.transform.position-transform.position);
        BeginPatternError(PatternKind.Shockwave);
        SetPhase(Phase.Shockwave, Mathf.Max(.1f, shockwaveWindupTime));
        BeginBossAttackAnimation();
    }

    private void ReleaseShockwaves()
    {
        if (!IsPlayerInRoom() || phase == Phase.Dead || IsStaggered) return;
        float radius = ShockwaveHitRadius;
        float speed = Mathf.Max(.1f, shockwaveSpeed);
        var pattern = activePatternError;
        var prefab = shockwaveEffectPrefab != null ? shockwaveEffectPrefab
            : Resources.Load<GameObject>("TutorialBossEffects/ShockwaveEffect");
        for (int i = 0; i < 4; i++)
        {
            var shot = prefab != null ? Instantiate(prefab) : new GameObject("Boss Shockwave");
            shot.name = "Boss Shockwave " + (i + 1);
            shot.SetActive(true);
            Vector2 direction = ShockwaveDirection(i);
            shot.transform.position = new Vector3(ShockwaveOrigin.x, ShockwaveOrigin.y, transform.position.z);
            shot.transform.rotation = Quaternion.Euler(0f, 0f, shockwaveAngle + i * 90f);
            var visual = shot.GetComponent<SpriteRenderer>();
            if (visual == null) visual = shot.AddComponent<SpriteRenderer>();
            if (visual.sprite == null) visual.sprite = shockwaveSprite != null ? shockwaveSprite : circleSprite;
            visual.color = shockwaveColor;
            visual.sortingLayerID = bodyVisual != null ? bodyVisual.sortingLayerID : 0;
            visual.sortingOrder = shockwaveSortingOrder;
            if (visual.sprite != null)
            {
                // 가로세로 비율은 보존하고, 그림과 판정에 크기 배율을 한 번만 적용합니다.
                Vector2 bounds = visual.sprite.bounds.size;
                float diameter = radius * 2f;
                shot.transform.localScale = Vector3.one * (diameter / Mathf.Max(.001f, bounds.x, bounds.y));
            }
            // 공통 투사체의 연속 CircleCast로 빠른 이동도 판정합니다. 서로 충돌하지 않습니다.
            var projectile = shot.GetComponent<ReflectProjectile>();
            if (projectile == null) projectile = shot.AddComponent<ReflectProjectile>();
            projectile.PreserveParryReturnLifetime = true;
            projectile.Initialize(gameObject, direction, speed,
                Mathf.Max(.1f, shockwaveDamage), radius, Mathf.Max(.1f, shockwaveDistance) / speed, 1, false);
            BindShockwaveError(projectile, pattern);
            if (shot.TryGetComponent<BossShockwaveEffect>(out var effect))
            {
                effect.MatchVisualToHitRadius();
                effect.RefreshDirection();
            }
            shockwaveShots.Add(shot);
        }
    }

    private void UpdateShockwaveShots()
    {
        for (int i = shockwaveShots.Count - 1; i >= 0; i--)
        {
            var shot = shockwaveShots[i];
            if (shot != null && IsInsideRoom(shot.transform.position)) continue;
            if (shot != null) { shot.SetActive(false); Destroy(shot); }
            shockwaveShots.RemoveAt(i);
        }
        PrunePatternErrors();
    }

    private void ClearShockwaves()
        => ClearShockwaveShots(false);

    private void ClearHostileShockwaves()
        => ClearShockwaveShots(true);

    private void ClearShockwaveShots(bool keepParried)
    {
        for (int i = shockwaveShots.Count - 1; i >= 0; i--)
        {
            var shot = shockwaveShots[i];
            if (keepParried && shot != null && shot.TryGetComponent<ReflectProjectile>(out var projectile)
                && projectile.WasParriedBy(player)) continue;
            if (shot != null) { shot.SetActive(false); Destroy(shot); }
            shockwaveShots.RemoveAt(i);
        }
    }

    private void ResetShockwaveAttack()
    {
        ClearShockwaves();
        shockwaveRemaining = Mathf.Max(0f, shockwaveFirstDelay);
    }

    private void DrawShockwaveGizmo()
    {
        if (!enableShockwave) return;
        Gizmos.color = new Color(.5f,.9f,1f,.65f);
        float radius = ShockwaveHitRadius;
        if (activePatternError != null && activePatternError.kind == PatternKind.Shockwave && !activePatternError.removed
            && activePatternError.type == StoredErrorType.Giant) radius *= activePatternError.multiplier;
        Gizmos.DrawWireSphere(ShockwaveOrigin, radius);
        for (int i = 0; i < 4; i++)
        {
            Vector2 end = ShockwaveOrigin + ShockwaveDirection(i) * Mathf.Max(.1f,shockwaveDistance);
            Gizmos.DrawLine(ShockwaveOrigin, end);
            Gizmos.DrawWireSphere(end, radius);
        }
    }
}
