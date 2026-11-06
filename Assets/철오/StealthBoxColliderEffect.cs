using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns off BoxCollider2D components on this GameObject while its stealth error is active.
/// Add this component alongside StealthErrorEffect.
/// </summary>
[DisallowMultipleComponent]
public sealed class StealthBoxColliderEffect : MonoBehaviour
{
    private struct ColliderState
    {
        public BoxCollider2D collider;
        public bool wasEnabled;
    }

    private readonly List<ColliderState> savedStates = new List<ColliderState>();
    private bool applied;

    private void OnEnable() => SyncWithStealth();

    private void Update() => SyncWithStealth();

    private void SyncWithStealth()
    {
        StealthErrorEffect stealth = GetComponent<StealthErrorEffect>();
        bool shouldDisableColliders = stealth != null
            && stealth.isActiveAndEnabled
            && stealth.IsActive;

        if (shouldDisableColliders == applied) return;

        if (shouldDisableColliders) DisableColliders();
        else RestoreColliders();
    }

    private void DisableColliders()
    {
        savedStates.Clear();
        foreach (BoxCollider2D box in GetComponents<BoxCollider2D>())
        {
            if (box == null) continue;
            savedStates.Add(new ColliderState { collider = box, wasEnabled = box.enabled });
            box.enabled = false;
        }
        applied = true;
    }

    private void RestoreColliders()
    {
        foreach (ColliderState saved in savedStates)
        {
            if (saved.collider != null) saved.collider.enabled = saved.wasEnabled;
        }
        savedStates.Clear();
        applied = false;
    }

    private void OnDisable() => RestoreColliders();

    private void OnDestroy() => RestoreColliders();
}
