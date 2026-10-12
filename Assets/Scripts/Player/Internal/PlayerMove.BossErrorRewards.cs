using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>보스 패링 보상과 전투 중 2칸 교체 UI. Cut 원본/먹물/튜토리얼 Cut 기록과 독립적이며 직접 부착하지 않습니다.</summary>
public partial class PlayerMove
{
    [Header("보스 패링 보상 / 교체 UI")]
    [SerializeField, Min(.1f), Tooltip("보관함이 가득 차서 교체 UI가 열린 뒤 자동으로 획득을 취소할 시간(초)입니다. 기본 5초. 휠 선택으로 연장되지 않으며 도감/일시 정지 중에는 흐르지 않는 게임 시간입니다. 기존 보관함은 유지합니다.")]
    private float bossErrorReplacementTimeout = 5f;
    private struct BossErrorReward
    {
        public StoredErrorType type;
        public float multiplier;
    }
    private readonly List<BossErrorReward> pendingBossErrorRewards = new List<BossErrorReward>(3);
    private int bossRewardSlot, bossRewardOpenedFrame;
    private float bossRewardTimeRemaining;

    /// <summary>패링으로 이미 해제된 오류를 획득합니다. 가득 차면 전투 모드에서도 교체를 기다립니다.</summary>
    public void ReceiveBossPatternError(StoredErrorType type, float multiplier)
    {
        if (!isActiveAndEnabled || IsInstantlyDead || type < StoredErrorType.Giant || type > StoredErrorType.Reflection) return;
        if (errorCodex.TryDiscover(type))
        {
            if (selectedCodexError == StoredErrorType.None) selectedCodexError = type;
            if (usePersistentErrorDiscoveries)
            {
                PlayerPrefs.SetInt(ErrorCodexSavePrefix + (int)type, 1);
                PlayerPrefs.Save();
            }
        }
        if (storedErrors.Contains(type))
        {
            ShowBossRewardNotice(ErrorRules.DisplayName(type) + " 제거! 이미 보관 중인 오류는 중복 저장하지 않습니다.");
            return;
        }
        foreach (var reward in pendingBossErrorRewards) if (reward.type == type) return;
        if (storedErrors.TryStore(type, multiplier))
        {
            RefreshStoredErrorSelection();
            ShowBossRewardNotice("첨삭 성공! " + ErrorRules.DisplayName(type) + " 보관함 획득");
            return;
        }
        if (pendingBossErrorRewards.Count == 0)
        {
            bossRewardSlot = 0;
            bossRewardOpenedFrame = Time.frameCount;
            bossRewardTimeRemaining = Mathf.Max(.1f, bossErrorReplacementTimeout);
            environmentPaste.Cancel();
            isSelectingStoredError = false;
            if (isReplacingStoredError) CancelStoredErrorReplacement(null);
            if (isAttacking) suppressErrorCutForCurrentAttack = true;
        }
        pendingBossErrorRewards.Add(new BossErrorReward { type = type, multiplier = multiplier });
    }

    private void ShowBossRewardNotice(string message)
    {
        discoveryNotice = message;
        discoveryNoticeUntil = Time.unscaledTime + 4f;
    }

    // 이동/가드/대시는 계속 처리하되 같은 좌클릭이 공격/Cut/Paste까지 실행되지 않게 소비합니다.
    private bool HandleBossErrorRewardInput()
    {
        if (pendingBossErrorRewards.Count == 0) return false;
        // 열린 프레임에는 시간을 차감하지 않습니다. 만료된 프레임의 클릭도 소비하며 획득하지 않습니다.
        if (Time.frameCount > bossRewardOpenedFrame && AdvanceBossRewardTimer(Time.deltaTime)) return true;
        var reward = pendingBossErrorRewards[0];
        if (storedErrors.Contains(reward.type) || storedErrors.TryStore(reward.type, reward.multiplier))
        {
            FinishBossErrorReward();
            return true;
        }
        if (Time.frameCount <= bossRewardOpenedFrame) return true;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowBossRewardNotice("획득을 취소했습니다. 보관함은 유지되며 패턴의 오류는 제거된 상태입니다.");
            FinishBossErrorReward();
            return true;
        }
        if (Mouse.current != null)
        {
            bossRewardSlot = ErrorInventory.StepSelection(bossRewardSlot, storedErrors.Capacity, Mouse.current.scroll.ReadValue().y);
            if (Mouse.current.leftButton.wasPressedThisFrame) ConfirmBossErrorReward();
        }
        return true;
    }

    private bool AdvanceBossRewardTimer(float deltaTime)
    {
        if (pendingBossErrorRewards.Count == 0) return false;
        bossRewardTimeRemaining = Mathf.Max(0f, bossRewardTimeRemaining - Mathf.Max(0f, deltaTime));
        if (bossRewardTimeRemaining > 0f) return false;
        ShowBossRewardNotice("교체 시간이 지나 획득을 취소했습니다. 기존 보관함은 유지됩니다.");
        FinishBossErrorReward();
        return true;
    }

    private void ConfirmBossErrorReward()
    {
        if (pendingBossErrorRewards.Count == 0) return;
        var reward = pendingBossErrorRewards[0];
        if (!storedErrors.Contains(reward.type) && !storedErrors.TryStore(reward.type, reward.multiplier)
            && !storedErrors.TryReplace(storedErrors.GetSlot(bossRewardSlot), reward.type, reward.multiplier)) return;
        ShowBossRewardNotice(ErrorRules.DisplayName(reward.type) + " 보관함 획득!");
        FinishBossErrorReward();
    }

    private void FinishBossErrorReward()
    {
        pendingBossErrorRewards.RemoveAt(0);
        bossRewardSlot = 0;
        bossRewardOpenedFrame = Time.frameCount;
        bossRewardTimeRemaining = pendingBossErrorRewards.Count > 0 ? Mathf.Max(.1f, bossErrorReplacementTimeout) : 0f;
        RefreshStoredErrorSelection();
    }

    private void ClearBossErrorRewards()
    {
        pendingBossErrorRewards.Clear();
        bossRewardSlot = 0;
        bossRewardTimeRemaining = 0f;
    }

    private void DrawBossErrorReward()
    {
        if (!Application.isPlaying || pendingBossErrorRewards.Count == 0) return;
        var oldMatrix = GUI.matrix;
        int oldDepth = GUI.depth;
        GUI.depth = -90;
        float scale = Mathf.Max(.01f, Mathf.Min(1f, Screen.width / 540f, Screen.height / 300f));
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 520f * scale) * .5f,
            Mathf.Max(0f, Screen.height - 245f * scale), 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        GUI.Box(new Rect(0, 0, 520, 155), GUIContent.none);
        GUI.Label(new Rect(18, 10, 484, 26), "새 오류 " + ErrorRules.DisplayName(pendingBossErrorRewards[0].type) + "  >>  교체할 오류 선택");
        for (int slot = 0; slot < storedErrors.Capacity; slot++)
            GUI.Box(new Rect(18 + slot * 246, 46, 238, 40), (slot == bossRewardSlot ? "▶ " : "")
                + (slot + 1) + ". " + ErrorRules.DisplayName(storedErrors.GetSlot(slot)));
        GUI.Label(new Rect(18, 96, 484, 24), "마우스 휠: 선택  /  좌클릭: 교체 확정  /  Esc: 획득 취소");
        GUI.Label(new Rect(18, 122, 484, 24), $"{bossRewardTimeRemaining:0.0}초 후 자동 취소 · 이동/가드/대시 가능");
        GUI.matrix = oldMatrix;
        GUI.depth = oldDepth;
    }
}
