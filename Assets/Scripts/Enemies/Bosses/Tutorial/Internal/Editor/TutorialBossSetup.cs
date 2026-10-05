using UnityEditor;
using UnityEngine;

/// <summary>도형 이미지와 튜토리얼 보스 프리팹 생성 도구입니다. 기존 에셋/그림은 덮어쓰지 않습니다.</summary>
public static class TutorialBossSetup
{
    public const string Folder = "Assets/TutorialBoss";
    public const string PrefabPath = Folder + "/TutorialBoss.prefab";
    [MenuItem("Tools/Story/Tutorial Boss/Create Missing Assets")]
    public static void CreateAssets()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets","TutorialBoss");
        Sprite square = Shape("Square",false), circle = Shape("Circle",true);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            var root = new GameObject("TutorialBoss");
            try
            {
                int layer = LayerMask.NameToLayer("Enemy");if (layer >= 0) root.layer = layer;
                var body = new GameObject("Body");body.transform.SetParent(root.transform,false);body.transform.localScale = Vector3.one*1.5f;
                var visual = body.AddComponent<SpriteRenderer>();visual.sprite = square;visual.color = new Color(.5f,.3f,.85f);visual.sortingOrder = 10;
                root.AddComponent<BoxCollider2D>().size = Vector2.one*1.5f;root.GetComponent<BoxCollider2D>().isTrigger = true;
                var physics = root.AddComponent<Rigidbody2D>();physics.bodyType = RigidbodyType2D.Kinematic;physics.gravityScale = 0;physics.constraints = RigidbodyConstraints2D.FreezeRotation;
                var data = new SerializedObject(root.AddComponent<TutorialBoss>());
                data.FindProperty("bodyVisual").objectReferenceValue = visual;data.FindProperty("circleSprite").objectReferenceValue = circle;
                data.FindProperty("squareSprite").objectReferenceValue = square;data.ApplyModifiedPropertiesWithoutUndo();
                var blink = new SerializedObject(root.AddComponent<DamageBlink>());
                blink.FindProperty("targetRenderers").arraySize = 1;blink.FindProperty("targetRenderers").GetArrayElementAtIndex(0).objectReferenceValue = visual;blink.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
    }
    private static Sprite Shape(string name,bool circle)
    {
        string path = Folder+"/"+name+".asset";
        foreach(var item in AssetDatabase.LoadAllAssetsAtPath(path)) if(item is Sprite sprite) return sprite;
        const int n=32;var texture = new Texture2D(n,n,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Point};
        var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)pixels[y*n+x]=!circle||new Vector2(x+.5f-n/2f,y+.5f-n/2f).sqrMagnitude<=n*n*.25f?Color.white:Color.clear;
        texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,path);
        var result=Sprite.Create(texture,new Rect(0,0,n,n),Vector2.one*.5f,n);result.name=name;AssetDatabase.AddObjectToAsset(result,path);return result;
    }
    [MenuItem("Tools/Story/Tutorial Boss/Create Boss In Scene")]
    public static void CreateInScene()
    {
        if(EditorApplication.isPlaying){Debug.LogWarning("Play를 끄고 생성하세요.");return;}
        CreateAssets();var boss=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var player=Object.FindFirstObjectByType<PlayerMove>();boss.transform.position=player!=null?player.transform.position+Vector3.right*4:Vector3.zero;
        Undo.RegisterCreatedObjectUndo(boss,"Create Tutorial Boss");Selection.activeGameObject=boss;EditorGUIUtility.PingObject(boss);
    }
}
