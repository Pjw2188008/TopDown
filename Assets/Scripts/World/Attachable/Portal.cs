using System.Collections.Generic;
using UnityEngine;

/// <summary>같은 씬 안의 지정 지점으로 V키 이동. 포탈 오브젝트에 부착하며 Collider는 필요 없습니다.</summary>
[DisallowMultipleComponent]
public sealed class Portal : MonoBehaviour
{
    [SerializeField, Tooltip("이동할 위치의 빈 오브젝트를 연결합니다. 같은 씬에 배치하고 플레이어 몸이 벽/바닥에 겹치지 않게 두세요. 회전/크기/HP는 바꾸지 않습니다.")]
    private Transform destination;
    [SerializeField, Tooltip("사용 가능 범위의 로컬 중심입니다.")] private Vector2 interactionOffset;
    [SerializeField, Tooltip("사용 가능 사각형 범위입니다. 플레이어 중심이 이 범위 안에 있어야 합니다. Transform 스케일/회전도 반영합니다.")]
    private Vector2 interactionSize = new Vector2(2f,3f);
    [SerializeField, Tooltip("근처에 V키 안내를 표시합니다. 포탈 그림은 SpriteRenderer에 직접 넣으세요.")] private bool showPrompt = true;
    [SerializeField, Tooltip("안내 문구를 표시할 로컬 위치입니다.")] private Vector2 promptOffset = new Vector2(0f,1.8f);
    [SerializeField, Tooltip("선택했을 때 범위와 목적지 연결선을 표시합니다.")] private bool showGizmo = true;
    private static readonly HashSet<Portal> activePortals = new HashSet<Portal>();
    public Transform Destination { get => destination; set => destination = value; }

    private void OnEnable() => activePortals.Add(this);
    private void OnDisable() => activePortals.Remove(this);
    private void OnDestroy() => activePortals.Remove(this);
    public bool Contains(PlayerMove player)
    {
        if (player == null || !player.isActiveAndEnabled || !isActiveAndEnabled
            || player.gameObject.scene != gameObject.scene) return false;
        Vector2 delta = (Vector2)transform.InverseTransformPoint(player.transform.position)-interactionOffset;
        return Mathf.Abs(delta.x)<=Mathf.Max(.1f,interactionSize.x)*.5f
            && Mathf.Abs(delta.y)<=Mathf.Max(.1f,interactionSize.y)*.5f;
    }
    private bool HasDestination => destination != null && destination != transform
        && destination.gameObject.scene == gameObject.scene;

    public static Portal FindNearest(PlayerMove player)
    {
        Portal best=null;float distance=float.PositiveInfinity;
        foreach (var portal in activePortals)
        {
            if (portal == null || !portal.HasDestination || !portal.Contains(player)) continue;
            float d=((Vector2)portal.transform.TransformPoint(portal.interactionOffset)-(Vector2)player.transform.position).sqrMagnitude;
            if (d>distance || (d==distance && best!=null && portal.GetInstanceID()>best.GetInstanceID())) continue;
            best=portal;distance=d;
        }
        return best;
    }
    public bool TryTravel(PlayerMove player)
    {
        if (!HasDestination || !Contains(player) || !player.CanUsePortal) return false;
        if (destination.IsChildOf(player.transform)) return false;
        return player.TravelThroughPortal(destination.position);
    }
    private void OnGUI()
    {
        if (!showPrompt || Camera.main==null || !HasDestination) return;
        var player=FindFirstObjectByType<PlayerMove>();
        if (player==null || !player.CanUsePortal || FindNearest(player)!=this) return;
        Vector3 screen=Camera.main.WorldToScreenPoint(transform.TransformPoint(promptOffset));
        if (screen.z<=0f) return;
        using(GameUIFont.UseIMGUI())
            GUI.Box(new Rect(screen.x-65f,Screen.height-screen.y-15f,130f,30f),"[V] 포탈 이동");
    }
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Matrix4x4 previous=Gizmos.matrix;Color old=Gizmos.color;
        Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=new Color(.6f,.25f,1f,.8f);
        Gizmos.DrawWireCube(interactionOffset,new Vector3(Mathf.Max(.1f,interactionSize.x),Mathf.Max(.1f,interactionSize.y),.05f));
        Gizmos.matrix=Matrix4x4.identity;
        if (destination!=null) { Gizmos.DrawLine(transform.position,destination.position);Gizmos.DrawWireSphere(destination.position,.2f); }
        Gizmos.matrix=previous;Gizmos.color=old;
    }
}
