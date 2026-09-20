using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>PlayerMove의 오류 선택·교체 입력 구현 부분입니다. 별도 컴포넌트가 아니므로 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    private bool HandleStoredErrorReplacementInput()
    {
        if (!isReplacingStoredError)
        {
            return false;
        }

        if (!isEditMode)
        {
            CancelStoredErrorReplacement("편집 모드가 아니므로 오류 교체를 취소했습니다.");
            return true;
        }

        if (!CanCompletePendingCut())
        {
            CancelStoredErrorReplacement("원본 오류 상태가 바뀌어 묶음 Cut을 취소했습니다.");
            return true;
        }
        int storedErrorCount = GetStoredErrorCount();
        if (storedErrorCount == 0)
        {
            CancelStoredErrorReplacement("교체할 기존 오류가 없어 오류 교체를 취소했습니다.");
            return true;
        }

        selectedReplacementIndex = Mathf.Clamp(selectedReplacementIndex, 0, storedErrorCount - 1);

        if (Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;
            selectedReplacementIndex = ErrorInventory.StepSelection(selectedReplacementIndex, storedErrorCount, scrollY);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ConfirmStoredErrorReplacement();
            }
        }

        return true;
    }

    /// <summary>숫자 1/2(또는 숫자패드)로 고정 슬롯을 직접 사용합니다. Q/휠 선택 단계는 없습니다.</summary>
    private void HandleStoredErrorSelectionInput()
    {
        isSelectingStoredError = false;
        if (isReplacingStoredError || Keyboard.current == null) return;
        if (isEditMode) environmentPaste.Validate(storedErrors);

        int slot = Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame ? 0
            : Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame ? 1 : -1;
        if (slot < 0) return;

        StoredErrorType error = storedErrors.GetSlot(slot);
        if (error == StoredErrorType.None)
        {
            Debug.Log((slot + 1) + "번 슬롯이 비어 있습니다.");
            return;
        }

        // 같은 번호를 다시 눌러 취소하는 것은 전투 효과가 활성화되어 있어도 허용합니다.
        if (isEditMode && environmentPaste.IsArmed && environmentPaste.SelectedError == error)
        {
            environmentPaste.Cancel();
            Debug.Log("Paste 준비를 취소했습니다. 오류는 보관함에 유지됩니다.");
            return;
        }
        if (IsAnyCombatErrorActive())
        {
            Debug.Log(GetActiveCombatErrorDisplayName() + " 효과가 유지되는 동안에는 다른 오류를 사용할 수 없습니다.");
            return;
        }

        // 기존 전투 적용 코드는 종류순 인덱스를 사용하므로 고정 슬롯의 오류 종류를 변환해 전달합니다.
        selectedStoredErrorIndex = storedErrors.IndexOf(error);
        if (isEditMode)
            environmentPaste.PressSlot(storedErrors, slot);
        else
            TryActivateStoredCombatError();
    }

    private bool HandleEnvironmentPasteClick()
    {
        if (!isEditMode || isReplacingStoredError || !environmentPaste.IsArmed
            || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return false;

        if (!environmentPaste.Validate(storedErrors)) return true;
        if (IsAnyCombatErrorActive())
        {
            Debug.Log(GetActiveCombatErrorDisplayName() + " 효과가 유지되는 동안에는 다른 오류를 사용할 수 없습니다.");
            return true;
        }

        // 실패하면 준비 상태와 보관함을 유지해 다른 대상에 다시 클릭할 수 있습니다.
        environmentPaste.CompletePaste(TryPasteError(environmentPaste.SelectedError));
        return true;
    }
}
