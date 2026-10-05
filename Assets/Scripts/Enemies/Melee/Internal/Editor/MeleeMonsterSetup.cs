using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>근접 몬스터 프리팹과 직접 편집하는 Sprite 클립을 생성합니다. 기존 파일은 덮어쓰지 않습니다.</summary>
public static class MeleeMonsterSetup
{
    public const string Folder = "Assets/MeleeEnemy";
    public const string PrefabPath = Folder + "/MeleeMonster.prefab";
    public const string SpawnerPath = Folder + "/MeleeEnemySpawner.prefab";

    [MenuItem("Tools/Story/Melee Enemy/Create Missing Assets")]
    public static void CreateAssets()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "MeleeEnemy");
        if (!AssetDatabase.IsValidFolder(Folder + "/Animations")) AssetDatabase.CreateFolder(Folder, "Animations");
        Sprite placeholder = Placeholder();
        var idle = Clip("Melee_Idle", true, placeholder);
        var move = Clip("Melee_Move", true, placeholder);
        var attack = Clip("Melee_Attack", false, placeholder);
        var stagger = Clip("Melee_Stagger", true, placeholder);
        string controllerPath = Folder + "/MeleeMonster.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Stunned", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle", new Vector3(200,100)); idleState.motion = idle;
            var moveState = machine.AddState("Move", new Vector3(500,100)); moveState.motion = move;
            var attackState = machine.AddState("Attack", new Vector3(350,250)); attackState.motion = attack;
            var stunState = machine.AddState("Stagger", new Vector3(600,250)); stunState.motion = stagger;
            machine.defaultState = idleState;
            var transition = idleState.AddTransition(moveState); Instant(transition);
            transition.AddCondition(AnimatorConditionMode.If,0,"IsMoving");
            transition = moveState.AddTransition(idleState); Instant(transition);
            transition.AddCondition(AnimatorConditionMode.IfNot,0,"IsMoving");
            transition = machine.AddAnyStateTransition(stunState); Instant(transition); transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If,0,"Stunned");
            transition = machine.AddAnyStateTransition(attackState); Instant(transition); transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If,0,"Attack");
            transition.AddCondition(AnimatorConditionMode.IfNot,0,"Stunned");
            transition = attackState.AddTransition(idleState); Instant(transition); transition.hasExitTime = true; transition.exitTime = 1;
            transition = stunState.AddTransition(idleState); Instant(transition);
            transition.AddCondition(AnimatorConditionMode.IfNot,0,"Stunned");
            EditorUtility.SetDirty(controller);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            var go = new GameObject("MeleeMonster");
            try
            {
                int layer = LayerMask.NameToLayer("Enemy"); if (layer >= 0) go.layer = layer;
                var visual = go.AddComponent<SpriteRenderer>(); visual.sprite = placeholder; visual.sortingOrder = 2;
                var animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.useFullKinematicContacts = true;
                go.AddComponent<BoxCollider2D>().size = new Vector2(.7f,.8f);
                var enemy = go.AddComponent<EnemyController>();
                enemy.maxHealth = 5; enemy.attackDamage = 1; enemy.moveSpeed = 2; enemy.attackCooldown = 1.2f;
                enemy.attackAreaSize = new Vector2(3f, 3f);
                var data = new SerializedObject(enemy);
                data.FindProperty("useMeleeAnimator").boolValue = true;
                data.FindProperty("respectWalls").boolValue = true;
                data.FindProperty("meleeAttackClip").objectReferenceValue = attack;
                data.ApplyModifiedPropertiesWithoutUndo();
                var paste = new SerializedObject(go.AddComponent<PasteTarget>());
                paste.FindProperty("targetType").intValue = (int)PasteTargetType.Living;
                paste.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);
            }
            finally { Object.DestroyImmediate(go); }
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SpawnerPath) == null)
        {
            var go = new GameObject("MeleeEnemySpawner");
            try
            {
                var point = new GameObject("SpawnPoint").transform;
                point.SetParent(go.transform, false);
                var data = new SerializedObject(go.AddComponent<MeleeEnemySpawner>());
                data.FindProperty("enemyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<EnemyController>();
                data.FindProperty("spawnPoint").objectReferenceValue = point;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, SpawnerPath);
            }
            finally { Object.DestroyImmediate(go); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("MeleeMonster 프리팹/Animator/편집 가능한 Sprite 클립을 준비했습니다. 기존 파일은 유지합니다.");
    }

    private static void Instant(AnimatorStateTransition transition)
    { transition.duration = 0; transition.hasExitTime = false; }

    private static Sprite Placeholder()
    {
        string path = Folder + "/Placeholder.asset";
        foreach (var item in AssetDatabase.LoadAllAssetsAtPath(path)) if (item is Sprite sprite) return sprite;
        var texture = new Texture2D(2,2) { name = "MeleePlaceholder", filterMode = FilterMode.Point };
        texture.SetPixels(new[] { new Color(.75f,.2f,.2f),Color.white,new Color(.75f,.2f,.2f),new Color(.75f,.2f,.2f) }); texture.Apply();
        AssetDatabase.CreateAsset(texture,path);
        var result = Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,2); result.name = "MeleePlaceholderSprite";
        AssetDatabase.AddObjectToAsset(result,path); AssetDatabase.SaveAssets(); return result;
    }

    private static AnimationClip Clip(string name, bool loop, Sprite sprite)
    {
        string path = Folder + "/Animations/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path); if (clip != null) return clip;
        clip = new AnimationClip { name = name, frameRate = 12 };
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),
            new[] { new ObjectReferenceKeyframe { time = 0,value = sprite },new ObjectReferenceKeyframe { time = .5f,value = sprite } });
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip,settings); AssetDatabase.CreateAsset(clip,path); return clip;
    }

    [MenuItem("Tools/Story/Melee Enemy/Create Monster In Scene")]
    public static void CreateInScene()
    {
        CreateInScene(PrefabPath);
    }

    [MenuItem("Tools/Story/Melee Enemy/Create Spawner In Scene")]
    public static void CreateSpawnerInScene()
    {
        CreateInScene(SpawnerPath);
    }

    private static void CreateInScene(string path)
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Play를 끄고 생성하세요."); return; }
        CreateAssets();
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        var player = Object.FindFirstObjectByType<PlayerMove>();
        go.transform.position = player != null ? player.transform.position + Vector3.right * 4 : Vector3.zero;
        Undo.RegisterCreatedObjectUndo(go,"Create melee monster"); Selection.activeGameObject = go; EditorGUIUtility.PingObject(go);
    }
}
