using UnityEngine;

public enum MeleeAttackMode
{
    [InspectorName("일반 공격")] Normal = 0,
    [InspectorName("좌우 파동")] Wave = 1,
    [InspectorName("돌진 찌르기")] Thrust = 2
}

/// <summary>MeleeEnemy의 공격 모드 구현입니다. partial이므로 별도로 붙이지 않습니다.</summary>
public sealed partial class MeleeEnemy
{
    [Header("공격 모드")]
    [Tooltip("일반: 기존 박스 타격. 파동: 좌우 두 방향 발사. 찌르기: 공격 시점부터 전진하며 한 번 타격. 모두 기존 Attack 상태를 사용합니다. 변경은 다음 공격부터 적용됩니다.")]
    public MeleeAttackMode attackMode = MeleeAttackMode.Normal;

    [Header("좌우 파동 — Wave 모드")]
    [Tooltip("몬스터 중심에서 좌우 발사 위치까지의 거리 X와 높이 Y입니다. X는 절댓값을 양쪽에 적용합니다.")]
    public Vector2 waveOffset = new Vector2(.4f,0f);
    [Min(.1f), Tooltip("파동 이동 속도(월드 단위/초).")]
    public float waveSpeed = 6f;
    [Min(.1f), Tooltip("파동 최대 이동 거리. 벽 충돌 시에는 먼저 사라집니다.")]
    public float waveDistance = 8f;
    [Min(.01f), Tooltip("파동의 원형 타격 반지름입니다. 청록색 원으로 표시합니다.")]
    public float waveHitRadius = .3f;
    [Tooltip("파동 그림 가로/세로의 월드 크기입니다. 판정 반지름은 별도입니다.")]
    public Vector2 waveVisualSize = new Vector2(1f,1f);
    [UnityEngine.Animations.NotKeyable, Tooltip("오른쪽으로 진행하는 이미지. 비우면 임시 사각형을 표시합니다.")]
    public Sprite waveSprite;
    [Tooltip("선택 사항: 파동 그림의 Animator Controller. 같은 오브젝트의 SpriteRenderer.Sprite 프레임을 편집하세요. 몬스터의 Animator는 변경하지 않습니다.")]
    public RuntimeAnimatorController waveAnimationController;
    public Color waveColor = new Color(.5f,.9f,1f,1f);
    [Tooltip("체형 밑면보다 파동 판정이 낮게 생성되어 바닥에 즉시 사라지는 것을 방지합니다. Y 보정은 이 최소 높이보다 위로 조정할 수 있습니다.")]
    public bool keepWaveAboveFeet = true;

    [Header("돌진 찌르기 — Thrust 모드")]
    [Min(0f), Tooltip("타격 프레임부터 앞으로 전진할 총 거리. 실제 이동은 벽/낭떠러지/영역 끝에서 제한됩니다.")]
    public float thrustDistance = 2f;
    [Min(.02f), Tooltip("전진 시간(초). 짧을수록 빠릅니다. 가속 오류가 있으면 공격 애니메이션과 함께 빨라집니다.")]
    public float thrustDuration = .25f;
    [Range(1f,4f), Tooltip("1은 일정 속도, 2 이상은 출발 후 점점 빨라지는 찌르기입니다.")]
    public float thrustAccelerationPower = 2f;

    private MeleeAttackMode activeAttackMode;
    private bool thrustActive, thrustHit;
    private float thrustElapsed, thrustTravelled, thrustFacing;
    private static Sprite fallbackWaveSprite;
    protected override bool RequiresTargetInRangeOnRelease => activeAttackMode == MeleeAttackMode.Normal;
    protected override bool HasActiveAttackAction => thrustActive;
    protected override void OnAttackStarted()
    {
        activeAttackMode = attackMode;
        thrustActive = thrustHit = false;
        thrustElapsed = thrustTravelled = 0f;
        thrustFacing = FacingSign;
    }
    protected override void OnAttackEnded() => thrustActive = false;

    protected override void TickAttackAction(float dt)
    {
        if (!thrustActive || !IsAttacking || dt <= 0f) return;
        thrustElapsed = Mathf.Min(Mathf.Max(.02f,thrustDuration),thrustElapsed+dt);
        float t = thrustElapsed / Mathf.Max(.02f,thrustDuration);
        float desired = Mathf.Max(0f,thrustDistance) * Mathf.Pow(t,Mathf.Clamp(thrustAccelerationPower,1f,4f));
        float remaining = Mathf.Max(0f,desired-thrustTravelled);
        // 짧은 구간별 연속 박스 검사: 저프레임에도 플레이어를 건너뛰지 않고 패링 직후 전진을 멈춥니다.
        TryThrustHit(Vector2.zero);
        while (remaining > .00001f && thrustActive && IsAttacking)
        {
            float requested = Mathf.Min(.1f,remaining);
            Vector2 before = transform.position;
            Vector2 moved = MoveAttackStep(thrustFacing*requested);
            Vector2 delta = (Vector2)transform.position-before;
            TryThrustHit(delta);
            thrustTravelled += Mathf.Abs(moved.x);
            remaining -= requested;
            if (Mathf.Abs(moved.x)+.00001f < requested) { thrustActive = false; break; }
        }
        if (t >= 1f) thrustActive = false;
    }

    private void TryThrustHit(Vector2 moved)
    {
        if (thrustHit || !thrustActive || Target == null) return;
        Vector2 end = (Vector2)transform.position + new Vector2(hitOffset.x*thrustFacing,hitOffset.y);
        Vector2 start = end-moved;
        foreach (var hit in Physics2D.BoxCastAll(start,SafeSize(hitSize),0f,moved.sqrMagnitude>.000001f?moved.normalized:Vector2.right,moved.magnitude))
        {
            if (hit.collider == null || hit.collider.GetComponentInParent<PlayerMove>() != Target) continue;
            thrustHit = true; // 피해/패링 콜백보다 먼저 기록해 중복 타격을 방지합니다.
            Target.ReceiveMeleeAttack(attackDamage,gameObject,Mathf.Max(0f,meleeParryWindow));
            break;
        }
    }

    private Vector3 WaveOrigin(float sign)
    {
        Vector3 origin = transform.position + new Vector3(Mathf.Abs(waveOffset.x)*sign,waveOffset.y,0f);
        if (keepWaveAboveFeet && EnemySideView.TryBounds(transform,out var bounds))
            origin.y = Mathf.Max(origin.y,bounds.min.y+Mathf.Max(.01f,waveHitRadius)+.04f);
        return origin;
    }
    private void ReleaseWaves()
    {
        for (int i=0;i<2;i++)
        {
            float sign = i==0?1f:-1f;
            var shot = new GameObject("Melee Wave " + (sign>0?"Right":"Left"));
            shot.transform.position = WaveOrigin(sign);
            var visual = shot.AddComponent<SpriteRenderer>();
            if (fallbackWaveSprite == null)
                fallbackWaveSprite = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            visual.sprite = waveSprite != null ? waveSprite : fallbackWaveSprite;
            visual.color = waveColor;
            visual.sortingLayerID = Visual != null ? Visual.sortingLayerID : 0;
            visual.sortingOrder = Visual != null ? Visual.sortingOrder+1 : 1;
            if (waveAnimationController != null)
            {
                var animation = shot.AddComponent<Animator>();
                animation.runtimeAnimatorController = waveAnimationController;
                animation.Update(0f);
            }
            var projectile = shot.AddComponent<ReflectProjectile>();
            projectile.Initialize(gameObject,Vector2.right*sign,Mathf.Max(.1f,waveSpeed),attackDamage,
                Mathf.Max(.01f,waveHitRadius),Mathf.Max(.1f,waveDistance)/Mathf.Max(.1f,waveSpeed),0,false);
            projectile.HorizontalOnly = true;
            projectile.PreserveParryReturnLifetime = true;
            shot.AddComponent<MeleeWaveVisual>().Initialize(projectile,waveVisualSize);
        }
    }
    private void DrawWaveGizmos()
    {
        for(int i=0;i<2;i++)
        {
            float sign = i==0?1f:-1f; Vector3 origin = WaveOrigin(sign);
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(origin,Mathf.Max(.01f,waveHitRadius));
            Gizmos.DrawLine(origin,origin+Vector3.right*sign*Mathf.Max(.1f,waveDistance));
            Gizmos.color = Color.blue; Gizmos.DrawWireCube(origin,SafeSize(waveVisualSize));
        }
    }
}
