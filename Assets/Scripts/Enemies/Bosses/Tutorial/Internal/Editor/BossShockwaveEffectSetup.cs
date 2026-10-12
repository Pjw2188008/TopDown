using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>충격파 자체의 편집 가능한 Sprite 애니메이션/프리팹을 준비합니다. 기존 프레임은 덮어쓰지 않습니다.</summary>
public static class BossShockwaveEffectSetup
{
    public const string Folder = "Assets/Resources/TutorialBossEffects";
    public const string PrefabPath = Folder + "/ShockwaveEffect.prefab";
    [MenuItem("Tools/Story/Tutorial Boss/Configure Shockwave Effect")]
    public static void Configure()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets","Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources","TutorialBossEffects");
        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/TutorialBoss/Effect/shockwave-01.png").OfType<Sprite>().FirstOrDefault();
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Shockwave_Fly.anim");
        if (clip == null)
        {
            clip = new AnimationClip { name="Shockwave_Fly",frameRate=12 };
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),
                new[] { new ObjectReferenceKeyframe {time=0,value=sprite}, new ObjectReferenceKeyframe {time=11f/12f,value=sprite} });
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,Folder+"/Shockwave_Fly.anim");
        }
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/ShockwaveEffect.controller");
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/ShockwaveEffect.controller");
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Fly");
        if(state==null)state=machine.AddState("Fly");
        if(state.motion==null)state.motion=clip;
        if(machine.defaultState==null)machine.defaultState=state;
        EditorUtility.SetDirty(controller);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null)
        {
            var root = new GameObject("ShockwaveEffect");
            try
            {
                var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=30;
                var animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                root.AddComponent<BossShockwaveEffect>();
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
    }
}
