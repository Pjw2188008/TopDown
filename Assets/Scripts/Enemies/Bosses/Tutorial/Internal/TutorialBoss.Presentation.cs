using UnityEngine;

/// <summary>교체 가능한 보스 그림, 실제 근접 공격 이펙트와 임시 HP/균형 게이지입니다. 별도 경고 표시는 생성하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("교체 가능한 그림")]
    [SerializeField, HideInInspector] private SpriteRenderer bodyVisual;
    // 설정용 Sprite는 애니메이션 대상에서 제외해 프레임 드롭 시 SpriteRenderer만 선택되게 합니다.
    // 인스펙터에서 이미지 교체 및 기존 직렬화 참조는 그대로 유지합니다.
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("공격 이펙트/탄환 이미지가 비었을 때 사용하는 원형 임시 이미지입니다.")] private Sprite circleSprite;
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("보스 SpriteRenderer가 없을 때 사용하는 사각 임시 이미지입니다.")] private Sprite squareSprite;
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("비우면 원형 도형 탄환을 사용합니다.")] private Sprite projectileSprite;
    [SerializeField, Tooltip("보스 머리 위 임시 체력/균형 UI를 표시합니다.")] private bool showStatus = true;
    private SpriteRenderer impactVisual;
    [Header("근접 공격 이펙트")]
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("Slam Hit Frame의 내려찍는 순간 표시할 이미지입니다. 비우면 원형 임시 이펙트를 사용합니다. 중앙 피벗 이미지를 사용하세요.")] private Sprite slamImpactSprite;
    [SerializeField, Min(.01f), Tooltip("근접 이펙트 표시 시간(초)입니다. 피해/패링 판정은 등장 순간 한 번만 발생하며, 크기는 Slam Radius에 맞춥니다.")] private float slamImpactDuration = .18f;
    private float impactRemaining;
    private Sprite runtimeCircle, runtimeSquare;
    private Texture2D runtimeCircleTexture, runtimeSquareTexture;

    private static Sprite Shape(bool circle, out Texture2D texture)
    {
        int n = circle ? 32 : 2;texture = new Texture2D(n,n,TextureFormat.RGBA32,false) { filterMode = FilterMode.Point };
        var pixels = new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            pixels[y*n+x] = !circle || new Vector2(x+.5f-n/2f,y+.5f-n/2f).sqrMagnitude <= n*n*.25f ? Color.white : Color.clear;
        texture.SetPixels(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,n,n),Vector2.one*.5f,n);
    }
    private void PrepareVisuals()
    {
        if (circleSprite == null) { runtimeCircle = Shape(true,out runtimeCircleTexture);circleSprite = runtimeCircle; }
        if (squareSprite == null) { runtimeSquare = Shape(false,out runtimeSquareTexture);squareSprite = runtimeSquare; }
        bodyVisual = GetComponent<SpriteRenderer>();
        if (bodyVisual == null)
        {
            bodyVisual = gameObject.AddComponent<SpriteRenderer>();bodyVisual.sprite = squareSprite;bodyVisual.color = new Color(.5f,.3f,.85f);
            bodyVisual.sortingOrder = 10;
        }
        impactVisual = new GameObject("Boss Slam Impact").AddComponent<SpriteRenderer>();impactVisual.sortingOrder = 21;
        impactVisual.gameObject.SetActive(false);
    }
    private static void Fit(SpriteRenderer renderer, Vector2 size)
    {
        if (renderer.sprite == null) return;
        Vector2 bounds = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x/Mathf.Max(.001f,bounds.x),size.y/Mathf.Max(.001f,bounds.y),1);
    }
    private void ShowImpact(Vector2 point)
    {
        if (impactVisual == null) return;
        impactVisual.sprite = slamImpactSprite != null ? slamImpactSprite : circleSprite;
        impactVisual.color = slamImpactSprite != null ? Color.white : new Color(1,.15f,.05f,.7f);
        impactVisual.transform.position = new Vector3(point.x,point.y,transform.position.z);
        Fit(impactVisual,Vector2.one*Mathf.Max(.1f,slamRadius)*2);
        impactRemaining = Mathf.Max(.01f,slamImpactDuration);impactVisual.gameObject.SetActive(true);
    }
    private void LateUpdate()
    {
        impactRemaining -= Time.deltaTime;
        if (impactRemaining <= 0 && impactVisual != null) impactVisual.gameObject.SetActive(false);
        // 새로 표시한 이펙트는 이번 프레임의 deltaTime으로 즉시 사라지지 않습니다.
        UpdateBossAttackAnimation();
    }
    private void OnGUI()
    {
        if (!showStatus || phase == Phase.Dead || Camera.main == null) return;
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position+Vector3.up*1.8f);if (screen.z <= 0) return;
        float x=screen.x-105,y=Screen.height-screen.y;
        Color old=GUI.color;
        using(GameUIFont.UseIMGUI())
        {
            GUI.Box(new Rect(x-5,y-5,220,76),GUIContent.none);
            GUI.Label(new Rect(x,y,210,22),"튜토리얼 보스  HP "+Mathf.CeilToInt(health)+" / "+Mathf.CeilToInt(maxHealth));
            GUI.color=Color.red;GUI.DrawTexture(new Rect(x,y+23,210*Mathf.Clamp01(health/Mathf.Max(1,maxHealth)),5),Texture2D.whiteTexture);
            GUI.color=old;GUI.Label(new Rect(x,y+30,210,22),IsStaggered ? "균형 붕괴 · 경직!" : "패링 "+parryCount+" / "+ParriesToStagger);
            GUI.color=Color.cyan;GUI.DrawTexture(new Rect(x,y+55,210*(IsStaggered?0:1f-(float)parryCount/ParriesToStagger),8),Texture2D.whiteTexture);
        }
        GUI.color=old;
    }
}
