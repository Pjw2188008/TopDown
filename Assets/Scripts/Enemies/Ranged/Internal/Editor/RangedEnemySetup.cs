using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>원거리 몬스터의 누락된 프리팹/Animator/편집 가능한 Sprite 클립을 준비하는 Editor 전용 도구입니다. 직접 부착하지 않습니다.</summary>
public static class RangedEnemySetup
{
    public const string Folder = "Assets/RangedEnemy";
    [MenuItem("Tools/Story/Ranged Enemy/Create Missing Assets")]
    public static void CreateAssets()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "RangedEnemy");
        if (!AssetDatabase.IsValidFolder(Folder + "/Animations")) AssetDatabase.CreateFolder(Folder, "Animations");
        Sprite placeholder = GetPlaceholder();
        AnimationClip idle = CreateClip("Ranged_Idle", true, placeholder);
        AnimationClip move = CreateClip("Ranged_Move", true, placeholder);
        AnimationClip attack = CreateClip("Ranged_Attack", false, placeholder);
        string controllerPath = Folder + "/RangedEnemy.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle", new Vector3(250, 100)); idleState.motion = idle;
            var moveState = machine.AddState("Move", new Vector3(500, 100)); moveState.motion = move;
            var attackState = machine.AddState("Attack", new Vector3(350, 270)); attackState.motion = attack;
            machine.defaultState = idleState;
            var transition = idleState.AddTransition(moveState); transition.duration = 0; transition.hasExitTime = false;
            transition.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            transition = moveState.AddTransition(idleState); transition.duration = 0; transition.hasExitTime = false;
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            transition = machine.AddAnyStateTransition(attackState); transition.duration = 0; transition.hasExitTime = false;
            transition.canTransitionToSelf = false; transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            transition = attackState.AddTransition(idleState); transition.duration = 0; transition.hasExitTime = true; transition.exitTime = 1;
            EditorUtility.SetDirty(controller);
        }
        string enemyPath = Folder + "/RangedEnemy.prefab";
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
        if (enemyPrefab == null)
        {
            var go = new GameObject("RangedEnemy");
            try
            {
                int layer = LayerMask.NameToLayer("Enemy"); if (layer >= 0) go.layer = layer;
                go.AddComponent<SpriteRenderer>().sprite = placeholder;
                go.GetComponent<SpriteRenderer>().sortingOrder = 2;
                go.AddComponent<Animator>().runtimeAnimatorController = controller;
                var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.useFullKinematicContacts = true;
                go.AddComponent<BoxCollider2D>().size = new Vector2(.7f, .8f);
                go.AddComponent<RangedEnemy>();
                var paste = new SerializedObject(go.AddComponent<PasteTarget>());
                paste.FindProperty("targetType").intValue = (int)PasteTargetType.Living; paste.ApplyModifiedPropertiesWithoutUndo();
                enemyPrefab = PrefabUtility.SaveAsPrefabAsset(go, enemyPath);
            }
            finally { Object.DestroyImmediate(go); }
        }
        string spawnerPath = Folder + "/RangedEnemySpawner.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(spawnerPath) == null)
        {
            var go = new GameObject("RangedEnemySpawner");
            try
            {
                var spawnPoint = new GameObject("SpawnPoint"); spawnPoint.transform.SetParent(go.transform, false);
                var serialized = new SerializedObject(go.AddComponent<RangedEnemySpawner>());
                serialized.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab.GetComponent<RangedEnemy>();
                serialized.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(go, spawnerPath);
            }
            finally { Object.DestroyImmediate(go); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("원거리 몬스터 프리팹/Animator 준비 완료. 기존 클립과 프리팹은 덮어쓰지 않았습니다.");
    }

    private static Sprite GetPlaceholder()
    {
        string path = Folder + "/Placeholder.asset";
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite) return sprite;
        var texture = new Texture2D(2, 2) { name = "RangedPlaceholder", filterMode = FilterMode.Point };
        texture.SetPixels(new[] { new Color(.85f, .4f, .2f), new Color(.85f, .4f, .2f), new Color(.85f, .4f, .2f), Color.white }); texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        var result = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2);
        result.name = "RangedPlaceholderSprite"; AssetDatabase.AddObjectToAsset(result, path); AssetDatabase.SaveAssets();
        return result;
    }
    private static AnimationClip CreateClip(string name, bool loop, Sprite placeholder)
    {
        string path = Folder + "/Animations/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path); if (clip != null) return clip;
        clip = new AnimationClip { name = name, frameRate = 12 };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
            new[] { new ObjectReferenceKeyframe { time = 0, value = placeholder }, new ObjectReferenceKeyframe { time = .5f, value = placeholder } });
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings); AssetDatabase.CreateAsset(clip, path); return clip;
    }

    [MenuItem("Tools/Story/Ranged Enemy/Create Spawner In Scene")]
    public static void CreateSpawner()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Play를 끄고 생성하세요."); return; }
        CreateAssets();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/RangedEnemySpawner.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(go, "Create ranged enemy spawner");
        var player = Object.FindFirstObjectByType<PlayerMove>();
        go.transform.position = player != null ? player.transform.position + Vector3.right * 8 : Vector3.zero;
        Selection.activeGameObject = go; EditorGUIUtility.PingObject(go);
    }
}
