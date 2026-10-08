using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheolO.Monsters.Editor
{
    /// <summary>저장하지 않은 씬의 Triangle도 선택해서 같은 설정을 적용할 수 있습니다.</summary>
    public static class TriangleMonsterSetup
    {
        private const string Folder = "Assets/철오/monster";
        [MenuItem("Tools/CheolO/Monster/Setup Selected Triangle")]
        public static void SetupSelected()
        {
            GameObject go = Selection.activeGameObject;
            if (EditorApplication.isPlayingOrWillChangePlaymode || go == null || EditorUtility.IsPersistent(go))
            {
                Debug.LogWarning("Play를 끄고 Hierarchy의 Triangle을 선택하세요.");
                return;
            }
            if (go.GetComponent<EnemyController>() != null || go.GetComponent<MeleeEnemy>() != null
                || go.GetComponent<RangedEnemy>() != null)
            {
                Debug.LogWarning("다른 몬스터 AI가 이미 연결되어 있습니다. 중복 AI를 먼저 정리하세요.", go);
                return;
            }
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Folder + "/Animations/TriangleMonster.controller");
            if (controller == null) { Debug.LogError("TriangleMonster.controller를 찾을 수 없습니다."); return; }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Triangle Monster");
            Undo.RegisterFullObjectHierarchyUndo(go, "Setup Triangle Monster");
            var movable = go.GetComponent<MovableInteractable>();
            if (movable != null) Undo.DestroyObjectImmediate(movable);
            var renderer = GetOrAdd<SpriteRenderer>(go);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Triangle.prefab");
            if (renderer.sprite == null && prefab != null) renderer.sprite = prefab.GetComponent<SpriteRenderer>().sprite;
            GetOrAdd<BoxCollider2D>(go);
            var body = GetOrAdd<Rigidbody2D>(go);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.useFullKinematicContacts = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var animator = GetOrAdd<Animator>(go);
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            GetOrAdd<EnemyStagger>(go);
            GetOrAdd<TriangleMeleeMonster>(go);
            int layer = LayerMask.NameToLayer("Enemy");
            if (layer >= 0) go.layer = layer;
            foreach (var component in go.GetComponents<Component>())
            {
                EditorUtility.SetDirty(component);
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Triangle 근접 몬스터 설정 완료. 씬을 저장하세요. 애니메이션 이미지는 monster/Animations 클립에서 교체합니다.", go);
        }
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T result = go.GetComponent<T>();
            return result != null ? result : Undo.AddComponent<T>(go);
        }
    }
}
