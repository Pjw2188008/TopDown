using System.Collections.Generic;
using UnityEngine;

/// <summary>보스 방 입장/출입구 잠금. TutorialBoss의 일부이며 따로 부착하지 않습니다.</summary>
public sealed partial class TutorialBoss
{
    [Header("보스 방 출입구")]
    [SerializeField, Tooltip("플레이어가 방 안에 들어오면 출입구를 닫고 보스 처치 시 엽니다. 비활성화/삭제 시에도 안전하게 해제합니다.")]
    private bool lockRoomOnEntry = true;
    [SerializeField, Tooltip("닫을 문들의 BoxCollider2D입니다. 보스/방 범위와 별개인 고정 오브젝트를 연결하세요. 콜라이더와 같은 오브젝트의 SpriteRenderer도 함께 켜고 끕니다. 비우면 방 좌우에 임시 벽을 만듭니다. 모든 출입구를 연결하세요.")]
    private BoxCollider2D[] roomGates = new BoxCollider2D[0];
    [SerializeField, Min(.05f), Tooltip("문을 지정하지 않았을 때 자동 생성하는 좌우 차단벽의 두께입니다. 방 전체 높이를 막습니다.")]
    private float roomGateThickness = .4f;
    [SerializeField, Tooltip("자동 생성 차단벽 색상입니다. 직접 지정한 문의 이미지는 바꾸지 않습니다.")]
    private Color roomGateColor = new Color(.65f,.12f,.18f,.85f);
    private readonly List<BoxCollider2D> activeRoomGates = new List<BoxCollider2D>();
    private GameObject generatedRoomGates;
    private PlayerMove roomTrackedPlayer;
    private int roomTeleportVersion;
    public bool IsRoomLocked { get; private set; }

    private Bounds RoomWorldBounds => roomArea != null ? BoxWorldBounds(roomArea)
        : new Bounds((Application.isPlaying ? roomOrigin : (Vector2)transform.position) + roomOffset,
            new Vector3(Mathf.Max(.1f,roomSize.x),Mathf.Max(.1f,roomSize.y),1f));

    // Collider.bounds는 비활성 상태에서 비어 있으므로 편집/열린 문에도 사용할 수 있도록 계산합니다.
    private static Bounds BoxWorldBounds(BoxCollider2D box)
    {
        var result = new Bounds(box.transform.TransformPoint(box.offset),Vector3.zero);
        for (int x=-1;x<=1;x+=2) for (int y=-1;y<=1;y+=2)
            result.Encapsulate(box.transform.TransformPoint(box.offset + Vector2.Scale(box.size,new Vector2(x,y))*.5f));
        return result;
    }

    private void PrepareRoomGates()
    {
        if (!lockRoomOnEntry) return;
        if (roomGates != null) foreach (var gate in roomGates)
        {
            if (gate == null || activeRoomGates.Contains(gate)) continue;
            if (gate == roomArea || gate.transform.IsChildOf(transform) || transform.IsChildOf(gate.transform)
                || gate.GetComponentInParent<PlayerMove>() != null || gate.attachedRigidbody != null)
            {
                Debug.LogWarning("보스 문은 보스/플레이어/방 감지 영역과 분리된 고정 BoxCollider2D로 지정하세요.",this);
                continue;
            }
            gate.isTrigger = false;
            activeRoomGates.Add(gate);
        }
        if (activeRoomGates.Count == 0)
        {
            generatedRoomGates = new GameObject(name + " Room Gates (Runtime)");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(generatedRoomGates,gameObject.scene);
            var bounds = RoomWorldBounds;
            float width = Mathf.Max(.05f,roomGateThickness);
            for (int i=0;i<2;i++)
            {
                var gateObject = new GameObject(i == 0 ? "Left Gate" : "Right Gate");
                gateObject.transform.SetParent(generatedRoomGates.transform,false);
                // 방 바깥쪽에 두어 방 크기/사용 가능한 이동 폭을 줄이지 않습니다.
                gateObject.transform.position = new Vector3(i == 0 ? bounds.min.x-width*.5f : bounds.max.x+width*.5f,
                    bounds.center.y,transform.position.z);
                var gate = gateObject.AddComponent<BoxCollider2D>();
                gate.size = new Vector2(width,bounds.size.y);
                // 그림 크기만 별도 자식으로 맞춰 콜라이더 크기를 중복 확대하지 않습니다.
                var visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(gateObject.transform,false);
                var visual = visualObject.AddComponent<SpriteRenderer>();
                visual.sprite = squareSprite;visual.color = roomGateColor;
                visual.sortingLayerID = bodyVisual != null ? bodyVisual.sortingLayerID : 0;
                visual.sortingOrder = 40;
                Fit(visual,gate.size);
                activeRoomGates.Add(gate);
            }
        }
        SetRoomGatesClosed(false);
    }

    private void TickRoomGates()
    {
        if (!lockRoomOnEntry || phase == Phase.Dead) { SetRoomGatesClosed(false);return; }
        if (roomTrackedPlayer != player)
        {
            roomTrackedPlayer = player;
            roomTeleportVersion = player != null ? player.HazardTeleportVersion : 0;
        }
        if (player == null) return;
        if (roomTeleportVersion != player.HazardTeleportVersion)
        {
            roomTeleportVersion = player.HazardTeleportVersion;
            // 사망 후 방 밖으로 리스폰하면 재입장이 가능해야 합니다. 단순한 방 이탈로는 열지 않습니다.
            if (IsRoomLocked)
            {
                SetRoomGatesClosed(false);
                ClearAllPatternErrors();ResetShockwaveAttack();StopAttackAnimation();ClearRockfall();
                health = Mathf.Max(1f,maxHealth);parryCount = 0;
                rockfallRemaining = Mathf.Max(0f,rockfallFirstDelay);
                SetPhase(Phase.Recovery,attackCooldown);
            }
            return;
        }
        if (IsRoomLocked || !IsPlayerInRoom() || player.IsInstantlyDead) return;
        Physics2D.SyncTransforms();
        if (EnemySideView.TryBounds(player.transform,out var body))
        {
            var room = RoomWorldBounds;
            // 몸이 문에 걸친 상태에서 닫혀 끼이는 일을 방지합니다.
            if (body.min.x < room.min.x+.02f || body.max.x > room.max.x-.02f) return;
            foreach (var gate in activeRoomGates)
            {
                if (gate == null) continue;
                var gateBounds = BoxWorldBounds(gate);
                if (body.min.x <= gateBounds.max.x && body.max.x >= gateBounds.min.x
                    && body.min.y <= gateBounds.max.y && body.max.y >= gateBounds.min.y) return;
            }
        }
        SetRoomGatesClosed(true);
    }

    private void SetRoomGatesClosed(bool closed)
    {
        IsRoomLocked = closed;
        foreach (var gate in activeRoomGates)
        {
            if (gate == null) continue;
            // 지정 문 오브젝트는 활성 상태로 두고 충돌/그림만 전환합니다.
            gate.enabled = closed;
            foreach (var visual in gate.GetComponentsInChildren<SpriteRenderer>(true)) visual.enabled = closed;
        }
        Physics2D.SyncTransforms();
    }

    private void DisposeRoomGates()
    {
        SetRoomGatesClosed(false);
        if (generatedRoomGates != null) Destroy(generatedRoomGates);
        activeRoomGates.Clear();
    }

    private void DrawRoomGatesGizmo()
    {
        if (!lockRoomOnEntry) return;
        Gizmos.color = IsRoomLocked ? Color.red : new Color(1f,.35f,.1f,.8f);
        bool specified = false;
        if (roomGates != null) foreach (var gate in roomGates)
        {
            if (gate == null) continue;
            specified = true;
            var previous = Gizmos.matrix;Gizmos.matrix = gate.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(gate.offset,gate.size);Gizmos.matrix = previous;
        }
        if (specified) return;
        var room = RoomWorldBounds;float width = Mathf.Max(.05f,roomGateThickness);
        Gizmos.DrawWireCube(new Vector3(room.min.x-width*.5f,room.center.y,transform.position.z),new Vector3(width,room.size.y,0));
        Gizmos.DrawWireCube(new Vector3(room.max.x+width*.5f,room.center.y,transform.position.z),new Vector3(width,room.size.y,0));
    }
}
