/// <summary>
/// 편집 모드의 오류 선택/준비/취소 상태입니다. 보관함을 소비하지 않으며 실제 Paste 성공 후에만 종료합니다.
/// 현재 조작은 PressSlot(1/2)로 직접 준비합니다. PressQ/Scroll은 기존 모델 호환용이며 플레이어 입력에 연결하지 않습니다.
/// </summary>
public sealed class EnvironmentPasteSession
{
    public enum PasteState { Idle, Selecting, Armed }
    public PasteState State { get; private set; }
    public StoredErrorType SelectedError { get; private set; }
    public bool IsBusy => State != PasteState.Idle;
    public bool IsArmed => State == PasteState.Armed;

    /// <summary>숫자 키로 해당 슬롯을 즉시 준비합니다. 같은 슬롯은 취소, 다른 슬롯은 변경합니다.</summary>
    public void PressSlot(ErrorInventory inventory, int slot)
    {
        Validate(inventory);
        StoredErrorType error = inventory.GetSlot(slot);
        if (error == StoredErrorType.None) return;
        if (IsArmed && SelectedError == error) { Cancel(); return; }
        SelectedError = error;
        State = PasteState.Armed;
    }

    public void PressQ(ErrorInventory inventory, int preferredIndex)
    {
        if (IsArmed) { Cancel(); return; }
        if (inventory.Count == 0) { Cancel(); return; }
        if (State == PasteState.Selecting)
        {
            if (!Validate(inventory)) return;
            State = PasteState.Armed;
            return;
        }

        SelectedError = inventory.GetTypeAt(preferredIndex);
        if (SelectedError == StoredErrorType.None) SelectedError = inventory.GetTypeAt(0);
        State = inventory.Count == 1 ? PasteState.Armed : PasteState.Selecting;
    }

    public void Scroll(ErrorInventory inventory, float scroll)
    {
        if (State != PasteState.Selecting || !Validate(inventory)) return;
        int index = ErrorInventory.StepSelection(inventory.IndexOf(SelectedError), inventory.Count, scroll);
        SelectedError = inventory.GetTypeAt(index);
    }

    public bool Validate(ErrorInventory inventory)
    {
        if (!IsBusy) return false;
        if (inventory.Contains(SelectedError)) return true;
        Cancel();
        return false;
    }

    public void CompletePaste(bool succeeded)
    {
        if (succeeded) Cancel();
    }

    public void Cancel()
    {
        State = PasteState.Idle;
        SelectedError = StoredErrorType.None;
    }
}
