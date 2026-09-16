using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>상호작용 Animator의 누락된 상태/클립과 테스트 물체를 만드는 Editor 도구입니다. 직접 부착하지 않습니다.</summary>
public static class InteractionSetup
{
    private const string Folder = "Assets/Player/Ani/";
    [MenuItem("Tools/Story/Interaction/Set Up Animator")]
    public static void SetUpAnimator()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "Player.controller");
        if (controller == null) throw new System.InvalidOperationException("Player.controller를 찾지 못했습니다.");
        var machine = controller.layers[0].stateMachine;
        string[] directions = { "Up", "Down", "Right" };
        for (int i = 0; i < directions.Length; i++)
        {
            string direction = directions[i];
            foreach (bool moving in new[] { false, true })
            {
                string name = (moving ? "Interact_Move_" : "Interact_Idle_") + direction;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + name + ".anim");
                if (clip == null)
                {
                    // Copy only the SpriteRenderer track. Never copy animation events or PlayerMove fields.
                    var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "Move_" + direction + ".anim");
                    clip = new AnimationClip { name = name, frameRate = source != null ? source.frameRate : 12f };
                    var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                    var frames = source != null ? AnimationUtility.GetObjectReferenceCurve(source, binding) : null;
                    if (frames != null && frames.Length > 0)
                    {
                        if (!moving) frames = new[] { new ObjectReferenceKeyframe { time = 0, value = frames[0].value },
                            new ObjectReferenceKeyframe { time = 1f / clip.frameRate, value = frames[0].value } };
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
                    }
                    var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    AssetDatabase.CreateAsset(clip, Folder + name + ".anim");
                }
                var state = machine.states.Select(item => item.state).FirstOrDefault(item => item.name == name);
                if (state == null) state = machine.AddState(name, new Vector3(800 + (moving ? 270 : 0), 100 + i * 100, 0));
                if (state.motion == null) state.motion = clip;
                // No Any State transition: script switches these states and returns to Player_Idle on release.
            }
        }
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        Debug.Log("상호작용 6개 상태/클립 준비 완료. 기존 클립 프레임은 덮어쓰지 않았습니다.");
    }

    [MenuItem("Tools/Story/Interaction/Create Test Box")]
    public static void CreateTestBox()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Play를 끈 뒤 테스트 물체를 생성하세요."); return; }
        var box = new GameObject("Movable Interaction Box");
        Undo.RegisterCreatedObjectUndo(box, "Create movable interaction box");
        var player = Object.FindFirstObjectByType<PlayerMove>();
        box.transform.position = player != null ? player.transform.position + Vector3.right * 1.5f : Vector3.zero;
        var renderer = box.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Vector2 spriteSize = renderer.sprite != null ? (Vector2)renderer.sprite.bounds.size : Vector2.one;
        box.transform.localScale = new Vector3(1f / spriteSize.x, 1f / spriteSize.y, 1f);
        renderer.color = new Color(.85f, .63f, .32f);
        box.AddComponent<BoxCollider2D>().size = spriteSize;
        box.AddComponent<MovableInteractable>();
        box.AddComponent<PasteTarget>();
        Selection.activeGameObject = box;
        EditorGUIUtility.PingObject(box);
    }
}
