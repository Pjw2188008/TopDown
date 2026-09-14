using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>PlayerMove의 오류 Cut·환경 Paste 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private bool TryHandleErrorHit(Collider2D hit)
    {
        var candidates = new System.Collections.Generic.List<MonoBehaviour>();
        bool hasActiveError = false;
        foreach (MonoBehaviour component in hit.GetComponents<MonoBehaviour>())
        {
            if (!(component is IErrorSource source) || !component.isActiveAndEnabled || !source.IsActive) continue;
            // 일반 모드의 기존 판정 유지: 반사 오류만 있는 적은 일반 피해/반사 처리를 받습니다.
            if (!isEditMode && source.ErrorType == StoredErrorType.Reflection) continue;
            hasActiveError = true;
            if (!source.CanCut || storedErrors.Contains(source.ErrorType)) continue;
            if (candidates.Exists(item => ((IErrorSource)item).ErrorType == source.ErrorType)) continue;
            candidates.Add(component);
        }
        if (!hasActiveError) return false;
        if (suppressErrorCutForCurrentAttack || isReplacingStoredError) return true;
        if (!isEditMode) { Debug.Log("일반 모드에서는 오류를 Cut할 수 없습니다."); return true; }
        if (candidates.Count == 0)
        {
            Debug.Log("가져올 수 있는 새 오류가 없습니다. 이미 보관한 종류와 Paste한 오류는 원본에 남습니다.");
            return true;
        }
        candidates.Sort((a, b) => ((IErrorSource)a).ErrorType.CompareTo(((IErrorSource)b).ErrorType));
        BeginErrorCutBatch(candidates);
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
        { Debug.Log("전투 기술 Paste는 일반 모드에서 슬롯 번호 1/2를 사용해야 합니다."); return false; }

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
