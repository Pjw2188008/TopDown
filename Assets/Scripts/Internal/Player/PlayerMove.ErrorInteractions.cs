using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>PlayerMove의 오류 Cut·환경 Paste 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private bool TryHandleErrorHit(Collider2D hit)
    {
        // 기존 우선순위/일반모드 동작을 유지합니다: 반사(편집만) → 가속 → 거대화.
        IErrorSource source = null;
        MonoBehaviour component = null;
        if (isEditMode && hit.TryGetComponent<ReflectionErrorEffect>(out var reflection) && reflection.IsActive)
        { source = reflection; component = reflection; }
        else if (hit.TryGetComponent<AccelerationErrorEffect>(out var acceleration) && acceleration.IsActive)
        { source = acceleration; component = acceleration; }
        else if (hit.TryGetComponent<GiantErrorEffect>(out var giant) && giant.IsActive)
        { source = giant; component = giant; }

        if (source == null) return false;
        // Paste 준비 전에 시작했던 공격의 지연 타격으로 오류가 추가 Cut되는 것을 방지합니다.
        if (suppressErrorCutForCurrentAttack) return true;
        string errorName = ErrorRules.DisplayName(source.ErrorType);
        if (!isEditMode) { Debug.Log("일반 상태에서는 " + errorName + " 오류를 Cut할 수 없습니다."); return true; }
        if (!source.CanCut) { Debug.Log("다른 대상에 Paste한 " + errorName + " 오류는 다시 Cut할 수 없습니다."); return true; }
        if (storedErrors.Contains(source.ErrorType)) { Debug.Log(errorName + " 오류가 이미 보관되어 있습니다."); return true; }

        if (storedErrors.Count >= storedErrors.Capacity)
            BeginStoredErrorReplacement(source.ErrorType, component, source.StoredMultiplier);
        else
            CompletePendingErrorCut(source.ErrorType, component, source.StoredMultiplier);
        return true;
    }

    private bool TryPasteError(StoredErrorType selected)
    {
        if (!isEditMode || !storedErrors.Contains(selected))
        { Debug.Log("붙여넣을 오류가 없습니다."); return false; }

        if (!TryFindPasteTarget(out PasteTarget target)) return false;
        if (!ErrorRules.CanPasteTo(selected, target.TargetType))
        { Debug.Log(ErrorRules.DisplayName(selected) + " 오류는 " + target.TargetType + " 대상에 Paste할 수 없습니다."); return false; }
        if (target.TargetType == PasteTargetType.CombatSkill)
        { Debug.Log("전투 기술 Paste는 일반 모드에서 Q를 사용해야 합니다."); return false; }

        float multiplier = storedErrors.GetMultiplier(selected);
        switch (selected)
        {
            case StoredErrorType.Giant:
                GetOrAddEffect<GiantErrorEffect>(target.gameObject).ApplyPaste(multiplier);
                break;
            case StoredErrorType.Acceleration:
                GetOrAddEffect<AccelerationErrorEffect>(target.gameObject).ApplyPaste(multiplier);
                break;
            case StoredErrorType.Reflection:
                GetOrAddEffect<ReflectionErrorEffect>(target.gameObject).ApplyPaste();
                break;
            default: return false;
        }
        DiscardStoredError(selected);
        Debug.Log(ErrorRules.DisplayName(selected) + " 오류를 " + target.name + "에 Paste했습니다.");
        return true;
    }

    private bool TryFindPasteTarget(out PasteTarget target)
    {
        target = null;
        if (Mouse.current == null || Camera.main == null) return false;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        worldPosition.z = 0f;
        Collider2D collider = Physics2D.OverlapPoint(worldPosition);
        if (collider == null)
        {
            GameObject clicked = GetSpriteObjectAtMousePosition(worldPosition);
            if (clicked == null) { Debug.Log("커서 위치에 붙여넣기 대상이 없습니다."); return false; }
            collider = clicked.GetComponent<Collider2D>();
            if (collider == null)
            {
                BoxCollider2D box = clicked.AddComponent<BoxCollider2D>();
                SpriteRenderer renderer = clicked.GetComponent<SpriteRenderer>();
                if (renderer != null) box.size = renderer.bounds.size;
                collider = box;
            }
        }
        target = collider.GetComponentInParent<PasteTarget>();
        if (target == null) Debug.Log("PasteTarget이 없는 대상에는 오류를 Paste할 수 없습니다.");
        return target != null;
    }

    private static T GetOrAddEffect<T>(GameObject target) where T : Component
    {
        T effect = target.GetComponent<T>();
        return effect != null ? effect : target.AddComponent<T>();
    }

    private GameObject GetSpriteObjectAtMousePosition(Vector3 mouseWorldPos)
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        foreach (SpriteRenderer renderer in renderers)
        {
            // 시각 효과인 잔상을 Collider 없는 환경 Paste 대상으로 취급하지 않습니다.
            if (IsRunAfterimage(renderer)) continue;
            if (renderer == null || renderer.bounds.Contains(mouseWorldPos) == false)
            {
                continue;
            }

            return renderer.gameObject;
        }

        return null;
    }
}
