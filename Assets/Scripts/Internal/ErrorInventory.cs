using System;

/// <summary>
/// Unity에 의존하지 않는 2칸 오류 보관 모델. 종류 중복 금지, 교체 검증, 배율을 한곳에서 관리합니다.
/// 표시 순서는 기존과 동일한 거대화→가속→반사입니다. 직접 부착하지 않습니다.
/// </summary>
public sealed class ErrorInventory
{
    private readonly bool[] occupied = new bool[4];
    private readonly float[] multipliers = new float[4];
    public int Capacity { get; }
    public int Count { get; private set; }

    public ErrorInventory(int capacity = 2)
    {
        if (capacity < 1 || capacity > 3) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }

    private static bool IsValid(StoredErrorType type) => type >= StoredErrorType.Giant && type <= StoredErrorType.Reflection;
    public bool Contains(StoredErrorType type) => IsValid(type) && occupied[(int)type];
    public float GetMultiplier(StoredErrorType type) => Contains(type) ? multipliers[(int)type] : 0f;

    public bool TryStore(StoredErrorType type, float multiplier)
    {
        if (!IsValid(type) || Contains(type) || Count >= Capacity) return false;
        occupied[(int)type] = true;
        multipliers[(int)type] = multiplier;
        Count++;
        return true;
    }

    public bool Remove(StoredErrorType type)
    {
        if (!Contains(type)) return false;
        occupied[(int)type] = false;
        multipliers[(int)type] = 0f;
        Count--;
        return true;
    }

    /// <summary>새 오류가 유효할 때만 기존 오류를 교체합니다. 실패하면 보관함은 변하지 않습니다.</summary>
    public bool TryReplace(StoredErrorType discarded, StoredErrorType incoming, float multiplier)
    {
        if (!Contains(discarded) || !IsValid(incoming) || Contains(incoming)) return false;
        Remove(discarded);
        return TryStore(incoming, multiplier);
    }

    public StoredErrorType GetTypeAt(int index)
    {
        if (index < 0 || index >= Count) return StoredErrorType.None;
        for (int kind = 1; kind < occupied.Length; kind++)
            if (occupied[kind] && index-- == 0) return (StoredErrorType)kind;
        return StoredErrorType.None;
    }

    public int IndexOf(StoredErrorType type)
    {
        for (int index = 0; index < Count; index++)
            if (GetTypeAt(index) == type) return index;
        return -1;
    }

    public static int StepSelection(int index, int count, float scroll)
    {
        if (count <= 0) return 0;
        int step = scroll < 0f ? 1 : scroll > 0f ? -1 : 0;
        return ((index + step) % count + count) % count;
    }
}
