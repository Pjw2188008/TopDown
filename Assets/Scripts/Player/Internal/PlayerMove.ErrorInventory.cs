using System.Collections.Generic;
using UnityEngine;

/// <summary>PlayerMove의 오류 보관·교체 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    // 튜토리얼은 입력 클릭이나 보관함 개수 대신 실제 저장 완료 기록을 읽습니다.
    public bool IsInEditMode => isEditMode;
    public int SuccessfulCutVersion { get; private set; }
    public string LastSuccessfulCutNames { get; private set; } = string.Empty;
    private struct CutReceipt { public int version; public string names; }
    private readonly Dictionary<int, CutReceipt> successfulCutsByTarget = new Dictionary<int, CutReceipt>();

    /// <summary>지정 오브젝트(또는 자식)의 원본을 실제 저장한 기록만 조회합니다. 실패/취소는 기록되지 않습니다.</summary>
    public bool TryGetTargetCutAfter(int targetInstanceId, int afterVersion, out string names)
    {
        if (successfulCutsByTarget.TryGetValue(targetInstanceId, out CutReceipt record) && record.version > afterVersion)
        { names = record.names; return true; }
        names = string.Empty; return false;
    }
    private readonly List<MonoBehaviour> pendingCutSources = new List<MonoBehaviour>();
    private readonly List<StoredErrorType> pendingCutTypes = new List<StoredErrorType>();

    private void BeginErrorCutBatch(List<MonoBehaviour> sources)
    {
        if (isReplacingStoredError || !isEditMode || sources.Count == 0) return;
        if (sources.Count > storedErrors.Capacity)
        {
            Debug.Log("보관함은 2칸입니다. 3종의 새 오류는 한 번에 Cut할 수 없습니다.");
            return;
        }
        pendingCutSources.Clear();
        pendingCutTypes.Clear();
        foreach (MonoBehaviour component in sources)
        {
            pendingCutSources.Add(component);
            pendingCutTypes.Add(((IErrorSource)component).ErrorType);
        }
        if (GetPendingCutReplacementCount() == 0)
        {
            CommitPendingErrorCut(new StoredErrorType[0]);
            return;
        }
        isReplacingStoredError = true;
        isSelectingStoredError = false;
        selectedReplacementIndex = 0;
        Debug.Log("묶음 Cut: " + GetPendingCutDisplayName() + " · 교체를 확정하기 전에는 원본/보관함을 변경하지 않습니다.");
    }

    private int GetPendingCutReplacementCount()
        => Mathf.Max(0, pendingCutSources.Count - (storedErrors.Capacity - storedErrors.Count));

    private string GetPendingCutDisplayName()
    {
        string names = "";
        foreach (StoredErrorType type in pendingCutTypes)
        {
            if (names.Length > 0) names += " + ";
            names += GetStoredErrorDisplayName(type);
        }
        return names;
    }

    private bool CanCompletePendingCut()
    {
        if (!isEditMode || pendingCutSources.Count == 0 || pendingCutSources.Count != pendingCutTypes.Count) return false;
        for (int i = 0; i < pendingCutSources.Count; i++)
        {
            MonoBehaviour component = pendingCutSources[i];
            if (component == null || !component.isActiveAndEnabled || !(component is IErrorSource source)
                || !source.IsActive || !source.CanCut || source.ErrorType != pendingCutTypes[i]
                || storedErrors.Contains(source.ErrorType)) return false;
        }
        return true;
    }

    private void ConfirmStoredErrorReplacement()
    {
        if (!isReplacingStoredError || !isEditMode) return;
        int needed = GetPendingCutReplacementCount();
        var discarded = new List<StoredErrorType>();
        if (needed > 0)
        {
            selectedReplacementIndex = Mathf.Clamp(selectedReplacementIndex, 0, storedErrors.Count - 1);
            discarded.Add(GetStoredErrorTypeAtIndex(selectedReplacementIndex));
            for (int i = 0; discarded.Count < needed && i < storedErrors.Count; i++)
            {
                StoredErrorType type = GetStoredErrorTypeAtIndex(i);
                if (!discarded.Contains(type)) discarded.Add(type);
            }
        }
        CommitPendingErrorCut(discarded.ToArray());
    }

    private void CommitPendingErrorCut(StoredErrorType[] discarded)
    {
        if (!CanCompletePendingCut())
        {
            CancelStoredErrorReplacement("묶음 오류 중 일부가 사라졌거나 더 이상 Cut할 수 없어 전체 획득을 취소했습니다.");
            return;
        }
        var values = new float[pendingCutSources.Count];
        for (int i = 0; i < values.Length; i++) values[i] = ((IErrorSource)pendingCutSources[i]).StoredMultiplier;
        if (!storedErrors.TryStoreBatch(pendingCutTypes.ToArray(), values, discarded))
        {
            CancelStoredErrorReplacement("모든 오류를 저장할 수 없어 전체 획득을 취소했습니다.");
            return;
        }
        string names = GetPendingCutDisplayName();
        // 원본 제거 과정에서 오브젝트가 사라져도 어느 대상에서 가져왔는지 보존합니다.
        var targetIds = new HashSet<int>();
        foreach (MonoBehaviour component in pendingCutSources)
            for (Transform source = component.transform; source != null; source = source.parent)
                targetIds.Add(source.gameObject.GetInstanceID());
        StoredErrorType first = pendingCutTypes[0];
        // 보관함 전체 반영에 성공한 경우에만 함께 선택한 원본 오류들을 제거합니다.
        foreach (MonoBehaviour component in pendingCutSources) ((IErrorSource)component).RemoveError();
        CancelStoredErrorReplacement(null);
        selectedStoredErrorIndex = GetStoredErrorIndex(first);
        RefreshStoredErrorSelection();
        LastSuccessfulCutNames = names;
        SuccessfulCutVersion++;
        foreach (int id in targetIds) successfulCutsByTarget[id] = new CutReceipt { version = SuccessfulCutVersion, names = names };
        Debug.Log(names + " 오류를 한 번에 Cut해 보관함에 저장했습니다.");
    }

    private void DiscardStoredError(StoredErrorType type)
    {
        storedErrors.Remove(type);
        RefreshStoredErrorSelection();
    }

    private void CancelStoredErrorReplacement(string message)
    {
        isReplacingStoredError = false;
        pendingCutSources.Clear();
        pendingCutTypes.Clear();
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
