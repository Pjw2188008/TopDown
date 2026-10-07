using UnityEngine;

/// <summary>보스/낙석 임시 그림과 HP/균형 게이지입니다. 근접 경고는 없으며 낙석은 전용 바닥 경고를 사용합니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("교체 가능한 그림")]
    [SerializeField, HideInInspector] private SpriteRenderer bodyVisual;
    // 설정용 Sprite는 애니메이션 대상에서 제외해 프레임 드롭 시 SpriteRenderer만 선택되게 합니다.
    // 인스펙터에서 이미지 교체 및 기존 직렬화 참조는 그대로 유지합니다.
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("낙석 바닥 경고에 사용할 원형 이미지입니다. 비우면 원형 도형을 생성합니다.")] private Sprite circleSprite;
    [SerializeField, UnityEngine.Animations.NotKeyable, Tooltip("보스 SpriteRenderer가 없을 때 사용하는 사각 임시 이미지입니다.")] private Sprite squareSprite;
    [SerializeField, Tooltip("보스 머리 위 임시 체력/균형 UI를 표시합니다.")] private bool showStatus = true;
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
    }
    private static void Fit(SpriteRenderer renderer, Vector2 size)
    {
        if (renderer.sprite == null) return;
        Vector2 bounds = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x/Mathf.Max(.001f,bounds.x),size.y/Mathf.Max(.001f,bounds.y),1);
    }
    private void LateUpdate()
    {
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
