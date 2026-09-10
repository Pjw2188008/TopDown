using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 달리는 플레이어의 현재 프레임을 짧게 남기는 시각 효과입니다. PlayerMove partial로 별도 부착하지 않습니다.
/// 12개 SpriteRenderer를 재사용하며, 충돌/오류/공격 기능은 없습니다. 스프라이트나 머티리얼을 복제하지 않습니다.
/// </summary>
public partial class PlayerMove
{
    private const int RunAfterimageCapacity = 12;
    private sealed class RunAfterimage
    {
        public SpriteRenderer renderer;
        public float remaining;
        public float lifetime;
        public Color initialColor;
    }

    private readonly RunAfterimage[] runAfterimages = new RunAfterimage[RunAfterimageCapacity];
    private GameObject runAfterimageRoot;
    private int nextRunAfterimage;
    private float runAfterimageCooldown;
    private bool didRunThisFrame;
    private Vector2 runAfterimageDirection;

    // Animator가 평가된 LateUpdate에서 현재 프레임/뒤집기/크기를 복사합니다.
    private void UpdateRunAfterimages(float deltaTime)
    {
        if (!showRunAfterimages)
        {
            ClearRunAfterimages();
            return;
        }
        deltaTime = Mathf.Max(0f, deltaTime);
        foreach (RunAfterimage image in runAfterimages)
        {
            if (image == null || image.renderer == null || !image.renderer.enabled) continue;
            image.remaining = Mathf.Max(0f, image.remaining - deltaTime);
            Color color = image.initialColor;
            color.a *= image.remaining / image.lifetime;
            image.renderer.color = color;
            if (image.remaining <= 0f) image.renderer.enabled = false;
        }

        bool canEmit = didRunThisFrame && guardHasFocus && !isGuarding && !isPlayingParry
            && !isErrorCodexOpen && spriteRenderer != null && spriteRenderer.enabled
            && spriteRenderer.gameObject.activeInHierarchy && spriteRenderer.sprite != null
            && runAfterimageOpacity > 0f;
        if (!canEmit)
        {
            runAfterimageCooldown = 0f;
            return;
        }
        if (deltaTime <= 0f) return;
        runAfterimageCooldown -= deltaTime;
        if (runAfterimageCooldown > 0f) return;
        EmitRunAfterimage();
        // 저프레임에서 같은 위치에 여러 잔상이 한꺼번에 생기지 않도록 프레임당 최대 1개 생성합니다.
        runAfterimageCooldown = Mathf.Max(0.02f, runAfterimageInterval);
    }

    private void EmitRunAfterimage()
    {
        EnsureRunAfterimagePool();
        RunAfterimage image = runAfterimages[nextRunAfterimage];
        nextRunAfterimage = (nextRunAfterimage + 1) % RunAfterimageCapacity;
        SpriteRenderer target = image.renderer;
        Transform sourceTransform = spriteRenderer.transform;
        target.transform.SetPositionAndRotation(sourceTransform.position
            - (Vector3)runAfterimageDirection * Mathf.Max(0f, runAfterimageOffset), sourceTransform.rotation);
        target.transform.localScale = sourceTransform.lossyScale;
        target.sprite = spriteRenderer.sprite;
        target.flipX = spriteRenderer.flipX;
        target.flipY = spriteRenderer.flipY;
        target.drawMode = spriteRenderer.drawMode;
        target.size = spriteRenderer.size;
        target.sharedMaterial = spriteRenderer.sharedMaterial;
        target.sortingLayerID = spriteRenderer.sortingLayerID;
        target.sortingOrder = Mathf.Max(-32768, spriteRenderer.sortingOrder - 1);
        target.spriteSortPoint = spriteRenderer.spriteSortPoint;
        target.maskInteraction = spriteRenderer.maskInteraction;
        image.lifetime = Mathf.Max(0.01f, runAfterimageLifetime);
        image.remaining = image.lifetime;
        image.initialColor = spriteRenderer.color;
        image.initialColor.a *= Mathf.Clamp01(runAfterimageOpacity);
        target.color = image.initialColor;
        target.enabled = true;
    }

    private void EnsureRunAfterimagePool()
    {
        if (runAfterimageRoot != null) return;
        runAfterimageRoot = new GameObject("Player Run Afterimages");
        // 플레이어의 자식으로 두지 않아 이동 후에도 잔상이 원래 월드 위치에 남습니다.
        SceneManager.MoveGameObjectToScene(runAfterimageRoot, gameObject.scene);
        for (int i = 0; i < RunAfterimageCapacity; i++)
        {
            GameObject imageObject = new GameObject("Run Afterimage " + i);
            imageObject.transform.SetParent(runAfterimageRoot.transform, false);
            // 카메라의 기존 플레이어 레이어 표시 설정을 그대로 따릅니다. Collider는 추가하지 않습니다.
            imageObject.layer = spriteRenderer.gameObject.layer;
            SpriteRenderer renderer = imageObject.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            runAfterimages[i] = new RunAfterimage { renderer = renderer };
        }
        nextRunAfterimage = 0;
    }

    private bool IsRunAfterimage(SpriteRenderer renderer)
    {
        return renderer != null && runAfterimageRoot != null
            && renderer.transform.IsChildOf(runAfterimageRoot.transform);
    }

    private void ClearRunAfterimages()
    {
        didRunThisFrame = false;
        runAfterimageCooldown = 0f;
        foreach (RunAfterimage image in runAfterimages)
        {
            if (image == null || image.renderer == null) continue;
            image.remaining = 0f;
            image.renderer.enabled = false;
        }
    }

    private void DestroyRunAfterimages()
    {
        ClearRunAfterimages();
        if (runAfterimageRoot != null) Destroy(runAfterimageRoot);
    }
}
