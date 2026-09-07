/// <summary>
/// 편집 모드의 오류 선택/준비/취소 상태입니다. 보관함을 소비하지 않으며 실제 Paste 성공 후에만 종료합니다.
/// Q를 놓는 입력은 처리하지 않습니다. 일반 모드의 전투 Paste와 독립적이며 직접 부착하지 않습니다.
/// </summary>
public sealed class EnvironmentPasteSession
{
    public enum PasteState { Idle, Selecting, Armed }
    public PasteState State { get; private set; }
    public StoredErrorType SelectedError { get; private set; }
    public bool IsBusy => State != PasteState.Idle;
    public bool IsArmed => State == PasteState.Armed;

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
