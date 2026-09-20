using UnityEngine;

/// <summary>
/// 실제 체력 피해를 받은 플레이어/몬스터의 몸 스프라이트를 잠깐 빨간색으로 표시합니다.
/// 피해 코드가 자동 추가합니다. 미리 부착하면 Inspector에서 연출을 조절할 수 있습니다.
/// 표시/숨김·애니메이션·Collider·피해 판정은 변경하지 않으며 무적 시간을 제공하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DamageBlink : MonoBehaviour
{
    [Tooltip("피격 색상을 유지하는 시간(초)입니다. 0이면 연출하지 않습니다. 연속 피격하면 이 시간부터 다시 시작합니다.")]
    [SerializeField, Min(0f)] private float duration = .35f;
    [Tooltip("피해를 받았을 때 표시할 색상입니다. 기본은 빨간색이며 원래 스프라이트의 투명도는 유지합니다.")]
    [SerializeField] private Color hitColor = Color.red;
    [Tooltip("켜면 편집 모드 슬로모션과 관계없이 실제 시간 기준으로 재생합니다.")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("피격 색상을 표시할 몸 SpriteRenderer입니다. 비우면 같은 오브젝트의 Renderer, 없으면 자식 Renderer들을 찾습니다. 공격 이펙트는 넣지 마세요.")]
    [SerializeField] private SpriteRenderer[] targetRenderers;

    private SpriteRenderer[] activeRenderers;
    private float elapsed;
    private bool playing;

    public bool IsPlaying => playing;

    /// <summary>기존 씬/프리팹에도 수동 연결 없이 적용합니다. 비활성 컴포넌트는 설정대로 연출하지 않습니다.</summary>
    public static void Play(GameObject owner)
    {
        if (owner == null || !owner.activeInHierarchy) return;
        if (!owner.TryGetComponent<DamageBlink>(out var blink)) blink = owner.AddComponent<DamageBlink>();
        blink.Trigger();
    }

    public void Trigger()
    {
        if (!isActiveAndEnabled || duration <= 0f)
        {
            Restore();
            return;
        }

        // 재피격 시 빨간색을 원래 색상으로 잘못 저장하지 않습니다.
        if (!playing)
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                activeRenderers = (SpriteRenderer[])targetRenderers.Clone();
            }
            else
            {
                var rootRenderer = GetComponent<SpriteRenderer>();
                activeRenderers = rootRenderer != null
                    ? new[] { rootRenderer }
                    : GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        elapsed = 0f;
        playing = true;
        ApplyTint();
    }

    private void LateUpdate()
    {
        Advance(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (!playing) return;
        elapsed += Mathf.Max(0f, deltaTime);
        if (elapsed >= duration)
        {
            Restore();
            return;
        }
        ApplyTint();
    }

    private void ApplyTint()
    {
        for (int i = 0; i < activeRenderers.Length; i++)
        {
            SpriteTint.ShowFlash(activeRenderers[i], this, hitColor);
        }
    }

    private void Restore()
    {
        if (!playing) return;
        foreach (var renderer in activeRenderers) SpriteTint.ClearFlash(renderer, this);
        playing = false;
        activeRenderers = null;
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
}
