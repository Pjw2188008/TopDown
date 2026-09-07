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

    private void HandleStoredErrorSelectionInput()
    {
        if (isReplacingStoredError)
        {
            return;
        }

        if (isEditMode)
        {
            HandleEnvironmentPasteSelectionInput();
            return;
        }

        int storedErrorCount = GetStoredErrorCount();

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (IsAnyCombatErrorActive())
            {
                isSelectingStoredError = false;
                Debug.Log($"{GetActiveCombatErrorDisplayName()} 효과가 유지되는 동안에는 다른 오류를 사용할 수 없습니다.");
                return;
            }

            if (storedErrorCount == 0)
            {
                isSelectingStoredError = false;
                Debug.Log("보관함에 사용할 오류가 없습니다.");
                return;
            }

            ClampSelectedStoredErrorIndex();

            // 하나는 즉시 적용, 둘은 선택 중 다시 Q를 눌렀을 때만 적용합니다.
            if (storedErrorCount == 1 || isSelectingStoredError)
            {
                isSelectingStoredError = false;
                ConfirmSelectedStoredError();
                return;
            }

            isSelectingStoredError = true;
            Debug.Log($"오류 선택 시작: {GetStoredErrorDisplayName(GetSelectedStoredErrorType())}");
        }

        if (!isSelectingStoredError)
        {
            return;
        }

        if (Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;
            int previousIndex = selectedStoredErrorIndex;
            selectedStoredErrorIndex = ErrorInventory.StepSelection(selectedStoredErrorIndex, storedErrorCount, scrollY);
            if (previousIndex != selectedStoredErrorIndex)
                Debug.Log($"오류 선택: {GetStoredErrorDisplayName(GetSelectedStoredErrorType())}");
        }

        // Q를 놓아도 선택 상태를 유지합니다. 다음 Q 누름이 전투 Paste를 확정합니다.
    }

    private void ConfirmSelectedStoredError()
    {
        if (IsAnyCombatErrorActive())
        {
            Debug.Log($"{GetActiveCombatErrorDisplayName()} 효과가 유지되는 동안에는 다른 오류를 사용할 수 없습니다.");
            return;
        }

        if (GetSelectedStoredErrorType() == StoredErrorType.None)
        {
            Debug.Log("선택할 수 있는 오류가 없습니다.");
            return;
        }

        if (!isEditMode)
            TryActivateStoredCombatError();
    }

    private void HandleEnvironmentPasteSelectionInput()
    {
        isSelectingStoredError = false;
        environmentPaste.Validate(storedErrors);
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            // 준비 취소는 전투 오류의 활성 상태와 관계없이 허용합니다.
            if (!environmentPaste.IsArmed && IsAnyCombatErrorActive())
            {
                Debug.Log(GetActiveCombatErrorDisplayName() + " 효과가 유지되는 동안에는 다른 오류를 사용할 수 없습니다.");
                return;
            }
            if (storedErrors.Count == 0)
            {
                Debug.Log("보관함에 사용할 오류가 없습니다.");
                return;
            }
            environmentPaste.PressQ(storedErrors, selectedStoredErrorIndex);
            if (!environmentPaste.IsBusy) Debug.Log("Paste 준비를 취소했습니다. 오류는 보관함에 유지됩니다.");
        }

        // Q를 놓아도 소비/확정하지 않습니다. 다음 Q 누름이 준비를 확정합니다.
        if (Mouse.current != null)
            environmentPaste.Scroll(storedErrors, Mouse.current.scroll.ReadValue().y);

        if (environmentPaste.IsBusy)
            selectedStoredErrorIndex = GetStoredErrorIndex(environmentPaste.SelectedError);
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
