using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>보스 루트 하나에 Renderer/Animator/게임플레이를 연결합니다. 기존 Sprite 키와 클립 GUID는 보존합니다.</summary>
public static class TutorialBossAnimationSetup
{
    private const string Folder = "Assets/TutorialBoss";
    [MenuItem("Tools/Story/Tutorial Boss/Configure Animator")]
    public static void Configure()
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(Folder+"/golem-upper-left-128.png").OfType<Sprite>().FirstOrDefault();
        if(sprite==null){Debug.LogWarning("골렘 Sprite가 없어 기존 보스를 유지합니다.");return;}
        if(!AssetDatabase.IsValidFolder(Folder+"/Animations"))AssetDatabase.CreateFolder(Folder,"Animations");
        var idle=Clip("Boss_Idle",sprite,1.2f,true);var slam=Clip("Boss_Slam",sprite,1.4f,false);
        var shoot=Clip("Boss_Shoot",sprite,1.4f,false);var stagger=Clip("Boss_Stagger",sprite,.6f,true);
        string controllerPath=Folder+"/TutorialBoss.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=controller.layers[0].stateMachine;
        var idleState=State(machine,"Idle",idle,new Vector3(200,100));State(machine,"Slam",slam,new Vector3(450,100));
        State(machine,"Shoot",shoot,new Vector3(450,240));State(machine,"Stagger",stagger,new Vector3(200,240));
        if(machine.defaultState==null)machine.defaultState=idleState;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(TutorialBossSetup.PrefabPath)==null)return;
        var root=PrefabUtility.LoadPrefabContents(TutorialBossSetup.PrefabPath);
        try
        {
            var data=new SerializedObject(root.GetComponent<TutorialBoss>());
            var old=data.FindProperty("bodyVisual").objectReferenceValue as SpriteRenderer;
            var renderer=root.GetComponent<SpriteRenderer>();
            var animator=root.GetComponent<Animator>();
            if(old!=null&&old.gameObject!=root)
            {
                var rig=root.transform.Find("VisualRig");var removal=rig!=null?rig:old.transform;
                // 사용자가 추가한 컴포넌트/다른 자식은 지우지 않고 알립니다.
                foreach(var component in removal.GetComponentsInChildren<Component>(true))
                    if(!(component is Transform)&&component!=old&&!(component is Animator))
                        throw new InvalidOperationException("Body/VisualRig에 사용자 컴포넌트가 있습니다. 자동 통합을 중지합니다.");
                if(removal.GetComponentsInChildren<Transform>(true).Any(t=>t!=removal&&t!=old.transform))
                    throw new InvalidOperationException("Body/VisualRig에 추가 자식이 있습니다. 자동 통합을 중지합니다.");
                if(Vector3.Distance(old.transform.position,root.transform.position)>.001f||Quaternion.Angle(old.transform.rotation,root.transform.rotation)>.01f)
                    throw new InvalidOperationException("그림에 별도 위치/회전 설정이 있습니다. 보존 방법 확인이 필요합니다.");
                var boxes=root.GetComponents<Collider2D>();
                if(boxes.Length!=1||!(boxes[0] is BoxCollider2D))throw new InvalidOperationException("추가 Collider가 있어 자동 크기 보정을 중지합니다.");
                var box=(BoxCollider2D)boxes[0];
                Vector3 a=old.transform.lossyScale,b=root.transform.lossyScale;
                Vector3 ratio=new Vector3(a.x/b.x,a.y/b.y,a.z/b.z);
                if(Mathf.Abs(ratio.x)<.0001f||Mathf.Abs(ratio.y)<.0001f)throw new InvalidOperationException("그림 Scale이 0입니다.");
                root.transform.localScale=Vector3.Scale(root.transform.localScale,ratio);
                box.size=new Vector2(box.size.x/Mathf.Abs(ratio.x),box.size.y/Mathf.Abs(ratio.y));
                box.offset=new Vector2(box.offset.x/ratio.x,box.offset.y/ratio.y);
                if(renderer==null)renderer=root.AddComponent<SpriteRenderer>();EditorUtility.CopySerialized(old,renderer);
                var oldAnimator=old.GetComponent<Animator>();
                if(animator==null)animator=root.AddComponent<Animator>();if(oldAnimator!=null)EditorUtility.CopySerialized(oldAnimator,animator);
                Object.DestroyImmediate(removal.gameObject); // 본 작업에서 제거하도록 지정된 그림용 컨테이너만 제거합니다.
            }
            if(renderer==null)renderer=root.AddComponent<SpriteRenderer>();
            if(renderer.sprite==null||renderer.sprite==data.FindProperty("squareSprite").objectReferenceValue)
            {renderer.sprite=sprite;renderer.color=Color.white;renderer.sortingOrder=10;}
            if(animator==null)animator=root.AddComponent<Animator>();
            if(animator.runtimeAnimatorController==null)animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            data.Update();data.FindProperty("bodyVisual").objectReferenceValue=renderer;data.FindProperty("bossAnimator").objectReferenceValue=animator;data.ApplyModifiedPropertiesWithoutUndo();
            var damage=root.GetComponent<DamageBlink>();
            if(damage!=null){var d=new SerializedObject(damage);var refs=d.FindProperty("targetRenderers");refs.arraySize=1;refs.GetArrayElementAtIndex(0).objectReferenceValue=renderer;d.ApplyModifiedPropertiesWithoutUndo();}
            PrefabUtility.SaveAsPrefabAsset(root,TutorialBossSetup.PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    private static AnimatorState State(AnimatorStateMachine machine,string name,AnimationClip clip,Vector3 position)
    {
        foreach(var state in machine.states)if(state.state.name==name)return state.state;
        var added=machine.AddState(name,position);added.motion=clip;return added;
    }
    private static AnimationClip Clip(string name,Sprite sprite,float duration,bool loop)
    {
        string path=Folder+"/Animations/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool created=clip==null;
        if(created)clip=new AnimationClip{name=name,frameRate=12};
        float length=created?duration:clip.length;
        var spriteBinding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
        var frames=AnimationUtility.GetObjectReferenceCurve(clip,spriteBinding);
        if(frames==null||frames.Length==0)frames=new[]{new ObjectReferenceKeyframe{time=0,value=sprite}};
        bool remove=false;
        foreach(var binding in AnimationUtility.GetCurveBindings(clip))
            if(binding.type==typeof(Transform)&&binding.path==""){AnimationUtility.SetEditorCurve(clip,binding,null);remove=true;}
        if(created||remove)
        {
            float end=Mathf.Max(0,length-1f/Mathf.Max(1,clip.frameRate));
            if(frames[frames.Length-1].time<end-.0001f)
                frames=frames.Concat(new[]{new ObjectReferenceKeyframe{time=end,value=frames[frames.Length-1].value}}).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip,spriteBinding,frames);
            if(created){var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,path);}
            else EditorUtility.SetDirty(clip);
        }
        return clip;
    }
}
