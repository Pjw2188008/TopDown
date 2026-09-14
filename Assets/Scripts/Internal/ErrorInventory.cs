using System;

/// <summary>
/// Unity에 의존하지 않는 2칸 오류 보관 모델. 종류 중복 금지, 교체 검증, 배율을 한곳에서 관리합니다.
/// 고정 슬롯은 획득한 순서로 빈칸에 저장합니다. 기존 GetTypeAt은 내부 종류순 조회용입니다. 직접 부착하지 않습니다.
/// </summary>
public sealed class ErrorInventory
{
    private readonly bool[] occupied = new bool[4];
    private readonly float[] multipliers = new float[4];
    private readonly StoredErrorType[] slots;
    public int Capacity { get; }
    public int Count { get; private set; }

    public ErrorInventory(int capacity = 2)
    {
        if (capacity < 1 || capacity > 3) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        slots = new StoredErrorType[capacity];
    }

    private static bool IsValid(StoredErrorType type) => type >= StoredErrorType.Giant && type <= StoredErrorType.Reflection;
    public bool Contains(StoredErrorType type) => IsValid(type) && occupied[(int)type];
    public float GetMultiplier(StoredErrorType type) => Contains(type) ? multipliers[(int)type] : 0f;

    public bool TryStore(StoredErrorType type, float multiplier)
    {
        if (!IsValid(type) || Contains(type) || Count >= Capacity) return false;
        int slot = Array.IndexOf(slots, StoredErrorType.None);
        if (slot < 0) return false;
        slots[slot] = type;
        occupied[(int)type] = true;
        multipliers[(int)type] = multiplier;
        Count++;
        return true;
    }

    public bool Remove(StoredErrorType type)
    {
        if (!Contains(type)) return false;
        slots[SlotIndexOf(type)] = StoredErrorType.None;
        occupied[(int)type] = false;
        multipliers[(int)type] = 0f;
        Count--;
        return true;
    }

    /// <summary>새 오류가 유효할 때만 기존 오류를 교체합니다. 실패하면 보관함은 변하지 않습니다.</summary>
    public bool TryReplace(StoredErrorType discarded, StoredErrorType incoming, float multiplier)
    {
        if (!Contains(discarded) || !IsValid(incoming) || Contains(incoming)) return false;
        int slot = SlotIndexOf(discarded);
        occupied[(int)discarded] = false;
        multipliers[(int)discarded] = 0f;
        occupied[(int)incoming] = true;
        multipliers[(int)incoming] = multiplier;
        slots[slot] = incoming;
        return true;
    }

    /// <summary>숫자 키에 대응하는 고정 슬롯. 소비해도 다른 슬롯이 당겨지지 않습니다.</summary>
    public StoredErrorType GetSlot(int index) => index >= 0 && index < Capacity ? slots[index] : StoredErrorType.None;

    public int SlotIndexOf(StoredErrorType type) => Contains(type) ? Array.IndexOf(slots, type) : -1;

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
