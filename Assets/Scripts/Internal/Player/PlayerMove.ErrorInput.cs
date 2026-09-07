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
                Debug.Log("보관함에 사용할 오류가 없습니다.");
                return;
            }

            ClampSelectedStoredErrorIndex();

            if (storedErrorCount == 1)
            {
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

        if (Keyboard.current.qKey.wasReleasedThisFrame)
        {
            isSelectingStoredError = false;
            ConfirmSelectedStoredError();
        }
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

        if (isEditMode)
        {
            TryPasteError();
        }
        else
        {
            TryActivateStoredCombatError();
        }
    }
}
