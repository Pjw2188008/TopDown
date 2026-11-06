using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이 오브젝트와 자식의 일반 Collider2D가 플레이어만 통과시킵니다.
/// Collider의 크기, Trigger, 레이어는 변경하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public sealed class PlayerPassThrough2D : MonoBehaviour
{
    [Tooltip("통과시킬 플레이어입니다. 비우면 씬의 PlayerMove를 자동으로 찾습니다.")]
    [SerializeField] private PlayerMove player;

    private readonly List<Collider2D> objectShapes = new List<Collider2D>();
    private readonly List<Collider2D> playerShapes = new List<Collider2D>();
    private readonly HashSet<(Collider2D, Collider2D)> ownedPairs = new HashSet<(Collider2D, Collider2D)>();

    // 부모와 자식 양쪽에 붙여도 한 컴포넌트를 끄는 것으로 다른 설정이 풀리지 않습니다.
    private sealed class IgnoreState
    {
        public int users;
        public bool originallyIgnored;
    }
    private static readonly Dictionary<(Collider2D, Collider2D), IgnoreState> sharedPairs
        = new Dictionary<(Collider2D, Collider2D), IgnoreState>();

    private void OnEnable() => RefreshPairs();
    private void FixedUpdate() => RefreshPairs();
    private void Update() => RefreshPairs();

    private void RefreshPairs()
    {
        if (player == null) player = FindFirstObjectByType<PlayerMove>();
        if (player == null) return;

        GetComponentsInChildren(false, objectShapes);
        player.GetComponentsInChildren(false, playerShapes);
        foreach (Collider2D objectShape in objectShapes)
        {
            if (!IsSolidActive(objectShape) || objectShape.transform.IsChildOf(player.transform)) continue;
            foreach (Collider2D playerShape in playerShapes)
            {
                if (!IsSolidActive(playerShape) || objectShape == playerShape) continue;
                var pair = (objectShape, playerShape);
                if (ownedPairs.Add(pair))
                {
                    if (!sharedPairs.TryGetValue(pair, out IgnoreState state))
                    {
                        state = new IgnoreState
                        {
                            originallyIgnored = Physics2D.GetIgnoreCollision(objectShape, playerShape)
                        };
                        sharedPairs.Add(pair, state);
                    }
                    state.users++;
                }

                // Collider 재활성화로 Unity의 무시 설정이 초기화된 경우에도 다시 적용합니다.
                if (!Physics2D.GetIgnoreCollision(objectShape, playerShape))
                    Physics2D.IgnoreCollision(objectShape, playerShape, true);
            }
        }
    }

    private static bool IsSolidActive(Collider2D shape)
        => shape != null && shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy
            && (shape.attachedRigidbody == null || shape.attachedRigidbody.simulated);

    private void OnDisable()
    {
        foreach (var pair in ownedPairs)
        {
            if (!sharedPairs.TryGetValue(pair, out IgnoreState state)) continue;
            if (--state.users > 0) continue;
            if (pair.Item1 != null && pair.Item2 != null && !state.originallyIgnored)
                Physics2D.IgnoreCollision(pair.Item1, pair.Item2, false);
            sharedPairs.Remove(pair);
        }
        ownedPairs.Clear();
        objectShapes.Clear();
        playerShapes.Clear();
    }
}
