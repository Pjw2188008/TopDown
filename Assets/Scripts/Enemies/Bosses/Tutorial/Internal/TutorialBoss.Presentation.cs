using UnityEngine;

/// <summary>교체 가능한 보스 그림, 원형/직선 경고, 내리찍기와 임시 HP/균형 게이지입니다. 별도로 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("교체 가능한 그림")]
    [SerializeField, Tooltip("보스 그림 전용 자식 Renderer입니다. Sprite를 교체하세요. 몸 Collider와 분리되어 준비 중 그림만 들립니다.")] private SpriteRenderer bodyVisual;
    [SerializeField, Tooltip("원형 바닥 경고/기본 탄환 이미지입니다.")] private Sprite circleSprite;
    [SerializeField, Tooltip("직선 조준 경고에 늘려 쓸 사각 이미지입니다.")] private Sprite squareSprite;
    [SerializeField, Tooltip("비우면 원형 도형 탄환을 사용합니다.")] private Sprite projectileSprite;
    [SerializeField, Min(0f), Tooltip("내리찍기 준비 중 그림을 들어올리는 높이입니다.")] private float slamLiftHeight = .6f;
    [SerializeField, Tooltip("보스 머리 위 임시 체력/균형 UI를 표시합니다.")] private bool showStatus = true;
    private SpriteRenderer warningVisual, impactVisual;
    private Vector3 bodyRestPosition;
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
        if (bodyVisual == null)
        {
            var visual = new GameObject("Body");visual.transform.SetParent(transform,false);
            bodyVisual = visual.AddComponent<SpriteRenderer>();bodyVisual.sprite = squareSprite;bodyVisual.color = new Color(.5f,.3f,.85f);
            bodyVisual.sortingOrder = 10;Fit(bodyVisual,Vector2.one*1.5f);
        }
        bodyRestPosition = bodyVisual.transform.localPosition;
        warningVisual = new GameObject("Boss Attack Warning").AddComponent<SpriteRenderer>();warningVisual.sortingOrder = 20;
        impactVisual = new GameObject("Boss Slam Impact").AddComponent<SpriteRenderer>();impactVisual.sortingOrder = 21;
        HideWarning();impactVisual.gameObject.SetActive(false);
    }
    private static void Fit(SpriteRenderer renderer, Vector2 size)
    {
        if (renderer.sprite == null) return;
        Vector2 bounds = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x/Mathf.Max(.001f,bounds.x),size.y/Mathf.Max(.001f,bounds.y),1);
    }
    private void ShowWarning()
    {
        if (warningVisual == null) return;
        warningVisual.gameObject.SetActive(true);warningVisual.color = new Color(1f,.55f,.1f,.45f);
        if (phase == Phase.SlamWarning)
        {
            warningVisual.sprite = circleSprite;warningVisual.transform.position = new Vector3(lockedPoint.x,lockedPoint.y,transform.position.z);
            warningVisual.transform.rotation = Quaternion.identity;Fit(warningVisual,Vector2.one*Mathf.Max(.1f,slamRadius)*2);
        }
        else
        {
            float length = Mathf.Max(meleeRange,engagementRange);
            Vector2 center = (Vector2)transform.position + shotDirection * length*.5f;
            warningVisual.sprite = squareSprite;warningVisual.transform.position = new Vector3(center.x,center.y,transform.position.z);
            warningVisual.transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(shotDirection.y,shotDirection.x)*Mathf.Rad2Deg);
            Fit(warningVisual,new Vector2(length,Mathf.Max(.03f,projectileRadius)*2));
        }
    }
    private void UpdateWarningAndPose()
    {
        if (phase != Phase.SlamWarning && phase != Phase.ShotWarning) return;
        float progress = 1f-Mathf.Clamp01(phaseRemaining/Mathf.Max(.01f,phaseDuration));
        if (warningVisual != null) warningVisual.color = new Color(1f,Mathf.Lerp(.65f,.1f,progress),.05f,Mathf.Lerp(.35f,.8f,progress));
        if (phase == Phase.SlamWarning && bodyVisual != null && bodyVisual.transform != transform)
            bodyVisual.transform.localPosition = bodyRestPosition + Vector3.up * Mathf.Max(0,slamLiftHeight)*progress;
    }
    private void RestoreBodyPose() { if (bodyVisual != null && bodyVisual.transform != transform) bodyVisual.transform.localPosition = bodyRestPosition; }
    private void HideWarning() { if (warningVisual != null) warningVisual.gameObject.SetActive(false); }
    private void ShowImpact(Vector2 point)
    {
        if (impactVisual == null) return;
        impactVisual.sprite = circleSprite;impactVisual.color = new Color(1,.15f,.05f,.7f);
        impactVisual.transform.position = new Vector3(point.x,point.y,transform.position.z);
        Fit(impactVisual,Vector2.one*Mathf.Max(.1f,slamRadius)*2);impactRemaining = .18f;impactVisual.gameObject.SetActive(true);
    }
    private void LateUpdate()
    {
        impactRemaining -= Time.deltaTime;
        if (impactRemaining <= 0 && impactVisual != null) impactVisual.gameObject.SetActive(false);
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
