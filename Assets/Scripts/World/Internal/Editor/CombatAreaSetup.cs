#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>기존 맵/콜라이더를 수정하지 않고 전투 구역과 네 방향 임시 장벽을 만듭니다. Editor 전용입니다.</summary>
public static class CombatAreaSetup
{
    [MenuItem("Tools/Story/Combat Area/Create Around Selected Spawner")]
    public static void CreateAroundSelectedSpawner()
    {
        var selected = Selection.activeGameObject;
        Component spawner = selected != null ? selected.GetComponent<RangedEnemySpawner>() : null;
        if (spawner == null && selected != null) spawner = selected.GetComponent<MeleeEnemySpawner>();
        if (spawner == null || !spawner.gameObject.scene.IsValid())
        { Debug.LogWarning("Hierarchy에서 RangedEnemySpawner 또는 MeleeEnemySpawner를 선택하세요."); return; }
        var size = new SerializedObject(spawner).FindProperty("activationSize").vector2Value;
        var area = Build(spawner.transform.position, size);
        var data = new SerializedObject(area);
        string property = spawner is MeleeEnemySpawner ? "meleeSpawners" : "spawners";
        if (spawner is MeleeEnemySpawner) area.gameObject.name = "Combat Area - Melee Spawner";
        data.FindProperty(property).arraySize = 1;
        data.FindProperty(property).GetArrayElementAtIndex(0).objectReferenceValue = spawner;
        data.ApplyModifiedPropertiesWithoutUndo();
        Undo.RegisterCreatedObjectUndo(area.gameObject, "Create Combat Area");
        Selection.activeGameObject = area.gameObject;
    }

    public static CombatAreaLock Build(Vector3 center, Vector2 size)
    {
        size = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
        var root = new GameObject("Combat Area - Ranged Spawner");
        root.transform.position = center;
        var trigger = root.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = size;
        var area = root.AddComponent<CombatAreaLock>();
        var data = new SerializedObject(area);
        data.FindProperty("entryArea").objectReferenceValue = trigger;
        var walls = data.FindProperty("barriers"); walls.arraySize = 4;
        const float thickness = .25f;
        walls.GetArrayElementAtIndex(0).objectReferenceValue = Wall(root.transform, "Barrier Left", new Vector2(-(size.x + thickness) / 2, 0), new Vector2(thickness, size.y + thickness * 2));
        walls.GetArrayElementAtIndex(1).objectReferenceValue = Wall(root.transform, "Barrier Right", new Vector2((size.x + thickness) / 2, 0), new Vector2(thickness, size.y + thickness * 2));
        walls.GetArrayElementAtIndex(2).objectReferenceValue = Wall(root.transform, "Barrier Bottom", new Vector2(0, -(size.y + thickness) / 2), new Vector2(size.x, thickness));
        walls.GetArrayElementAtIndex(3).objectReferenceValue = Wall(root.transform, "Barrier Top", new Vector2(0, (size.y + thickness) / 2), new Vector2(size.x, thickness));
        data.ApplyModifiedPropertiesWithoutUndo();
        return area;
    }

    private static GameObject Wall(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var wall = new GameObject(name); wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.layer = 0; // 현재 플레이어의 걷기/대쉬/상호작용 충돌 마스크에 포함되는 Default입니다.
        var body = wall.AddComponent<BoxCollider2D>(); body.size = size; body.isTrigger = false;
        var visual = wall.AddComponent<SpriteRenderer>();
        visual.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        visual.drawMode = SpriteDrawMode.Sliced; visual.size = size;
        visual.color = new Color(.25f, .7f, 1f, .65f); visual.sortingOrder = 20;
        wall.SetActive(false);
        return wall;
    }
}
#endif
