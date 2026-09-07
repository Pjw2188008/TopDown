using UnityEngine;

/// <summary>PlayerMove의 오류 보관·교체 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private void BeginStoredErrorReplacement(
        StoredErrorType newErrorType,
        MonoBehaviour source,
        float storedMultiplier)
    {
        if (isReplacingStoredError
            || !isEditMode
            || source == null
            || newErrorType == StoredErrorType.None)
        {
            return;
        }

        if (GetStoredErrorCount() < MaxStoredErrors)
        {
            Debug.LogWarning("보관함에 빈 슬롯이 있어 교체 UI를 열지 않았습니다.", this);
            return;
        }

        pendingReplacementErrorType = newErrorType;
        pendingReplacementSource = source;
        pendingReplacementMultiplier = storedMultiplier;
        selectedReplacementIndex = 0;
        isReplacingStoredError = true;
        isSelectingStoredError = false;

        Debug.Log($"보관함이 가득 찼습니다. {GetStoredErrorDisplayName(newErrorType)}와 교체할 오류를 선택하세요.");
    }

    private void ConfirmStoredErrorReplacement()
    {
        if (!isReplacingStoredError || !isEditMode) return;
        selectedReplacementIndex = Mathf.Clamp(selectedReplacementIndex, 0, storedErrors.Count - 1);
        StoredErrorType discarded = GetStoredErrorTypeAtIndex(selectedReplacementIndex);
        if (!CanCompletePendingCut()
            || !storedErrors.TryReplace(discarded, pendingReplacementErrorType, pendingReplacementMultiplier))
        {
            CancelStoredErrorReplacement("새 오류 원본이 사라졌거나 더 이상 Cut할 수 없어 교체를 취소했습니다.");
            return;
        }
        StoredErrorType incoming = pendingReplacementErrorType;
        ((IErrorSource)pendingReplacementSource).RemoveError();
        CancelStoredErrorReplacement(null);
        selectedStoredErrorIndex = GetStoredErrorIndex(incoming);
        RefreshStoredErrorSelection();
        Debug.Log(GetStoredErrorDisplayName(discarded) + "을(를) 폐기하고 " + GetStoredErrorDisplayName(incoming) + "을(를) 저장했습니다.");
    }

    private bool CanCompletePendingCut()
    {
        return pendingReplacementSource != null
            && pendingReplacementSource is IErrorSource source
            && source.ErrorType == pendingReplacementErrorType && source.CanCut
            && !storedErrors.Contains(source.ErrorType);
    }

    private void CompletePendingErrorCut(StoredErrorType type, MonoBehaviour component, float multiplier)
    {
        if (component == null || !(component is IErrorSource source) || !source.CanCut
            || source.ErrorType != type || !storedErrors.TryStore(type, multiplier)) return;
        source.RemoveError();
        selectedStoredErrorIndex = GetStoredErrorIndex(type);
        RefreshStoredErrorSelection();
        Debug.Log(ErrorRules.DisplayName(type) + " 오류를 Cut해 보관함에 저장했습니다.");
    }

    private void DiscardStoredError(StoredErrorType type)
    {
        storedErrors.Remove(type);
        RefreshStoredErrorSelection();
    }

    private void CancelStoredErrorReplacement(string message)
    {
        isReplacingStoredError = false;
        pendingReplacementErrorType = StoredErrorType.None;
        pendingReplacementSource = null;
        pendingReplacementMultiplier = 0f;
        selectedReplacementIndex = 0;

        if (!string.IsNullOrEmpty(message))
        {
            Debug.Log(message);
        }
    }

    private int GetStoredErrorCount() => storedErrors.Count;

    private StoredErrorType GetStoredErrorTypeAtIndex(int index) => storedErrors.GetTypeAt(index);

    private int GetStoredErrorIndex(StoredErrorType type) => Mathf.Max(0, storedErrors.IndexOf(type));

    private StoredErrorType GetSelectedStoredErrorType()
    {
        ClampSelectedStoredErrorIndex();
        return GetStoredErrorTypeAtIndex(selectedStoredErrorIndex);
    }

    private void ClampSelectedStoredErrorIndex()
    {
        int storedErrorCount = GetStoredErrorCount();
        selectedStoredErrorIndex = storedErrorCount > 0
            ? Mathf.Clamp(selectedStoredErrorIndex, 0, storedErrorCount - 1)
            : 0;
    }

    private static string GetStoredErrorDisplayName(StoredErrorType type) => ErrorRules.DisplayName(type);

    private void ClearStoredAccelerationError() => DiscardStoredError(StoredErrorType.Acceleration);

    private void ClearStoredReflectionError() => DiscardStoredError(StoredErrorType.Reflection);

    private void ClearEnvironmentError() => DiscardStoredError(StoredErrorType.Giant);

    private void RefreshStoredErrorSelection()
    {
        ClampSelectedStoredErrorIndex();

        if (GetStoredErrorCount() < 2)
        {
            isSelectingStoredError = false;
        }
    }
}
