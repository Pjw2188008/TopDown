using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>실습 구간 시작 상태를 메모리에 저장/복구합니다. 직접 부착하지 않습니다.</summary>
public sealed partial class TutorialSequence
{
    [Header("튜토리얼 재도전")]
    [SerializeField, Tooltip("레이저 즉사 시 현재 실습 구간 시작 상태로 복귀합니다. 보관함·사물·오류 상태를 함께 복구하며 도감 기록은 유지합니다.")]
    private bool restartSectionOnInstantDeath = true;
    [SerializeField, Tooltip("자동 검색되는 PasteTarget/이동 사물/오류 원본 외에 위치·크기·활성 상태를 함께 복구할 오브젝트입니다. 생성/파괴나 다른 스크립트의 내부 상태는 복구하지 않습니다.")]
    private GameObject[] additionalCheckpointObjects = Array.Empty<GameObject>();

    private sealed class SectionCheckpoint
    {
        public int StepIndex;
        public PlayerMove Player;
        public Vector3 Position;
        public Action RestorePlayer;
        public TutorialWorldCheckpoint World;
        public int PasteVersion;
        public long Order;
        public string CutNames;
    }

    private SectionCheckpoint checkpoint;
    private static long checkpointOrder;

    private void CaptureSectionCheckpoint()
    {
        if (!restartSectionOnInstantDeath || !running || player == null || CurrentStep == null) return;
        int index = currentStepIndex;
        var step = CurrentStep;
        // Cut 완료 설명 중 미리 Paste해도 오류를 소비한 뒤의 불가능한 상태를 저장하지 않습니다.
        if (step.completionCondition == CompletionCondition.Confirm)
        {
            while (++index < steps.Length && steps[index]?.completionCondition == CompletionCondition.Confirm) { }
            if (index >= steps.Length || steps[index]?.completionCondition != CompletionCondition.EnvironmentPasteSucceeded) return;
            step = steps[index];
        }
        if (checkpoint != null && checkpoint.Player == player)
        {
            if (checkpoint.StepIndex == index) return;
            var previous = index > 0 ? steps[index - 1] : null;
            if (step.completionCondition == CompletionCondition.CutSucceeded
                && previous?.completionCondition == CompletionCondition.EditModeEnabled) return;
            if (IsCombatStep(step) && step.combatDummy != null && previous?.combatDummy == step.combatDummy
                && (IsCombatStep(previous) || previous.completionCondition == CompletionCondition.ArrivalArea)) return;
        }
        var objects = new HashSet<GameObject>();
        foreach (var entry in steps)
        {
            if (entry?.cutTarget != null) objects.Add(entry.cutTarget);
            if (entry?.pasteTarget != null) objects.Add(entry.pasteTarget);
        }
        if (additionalCheckpointObjects != null)
            foreach (var item in additionalCheckpointObjects) if (item != null) objects.Add(item);
        checkpoint = new SectionCheckpoint
        {
            StepIndex = index, Player = player,
            Position = step.restartPoint != null ? step.restartPoint.position : player.transform.position,
            RestorePlayer = player.CaptureTutorialState(),
            World = new TutorialWorldCheckpoint(gameObject.scene, player, objects),
            PasteVersion = player.SuccessfulEnvironmentPasteVersion,
            Order = ++checkpointOrder, CutNames = capturedCutNames
        };
    }

    /// <summary>진행 중인 동일 플레이어의 가장 최근 튜토리얼 구간만 복구합니다.</summary>
    internal static bool TryRestoreCheckpoint(PlayerMove target, out Vector3 position)
    {
        position = default;
        TutorialSequence latest = null;
        foreach (var tutorial in FindObjectsByType<TutorialSequence>(FindObjectsSortMode.None))
        {
            var saved = tutorial.checkpoint;
            if (!tutorial.isActiveAndEnabled || !tutorial.running || !tutorial.restartSectionOnInstantDeath
                || saved == null || saved.Player != target || tutorial.gameObject.scene != target.gameObject.scene) continue;
            if (latest == null || saved.Order > latest.checkpoint.Order) latest = tutorial;
        }
        if (latest == null) return false;
        var snapshot = latest.checkpoint;
        latest.EndCombatPractice(true);
        snapshot.World.Restore();
        snapshot.RestorePlayer();
        latest.currentStepIndex = snapshot.StepIndex;
        latest.hasPreviousPosition = false;
        latest.capturedCutNames = snapshot.CutNames;
        // 성공 기준은 이번 재시도 시작 시점으로 새로 잡습니다.
        snapshot.PasteVersion = target.SuccessfulEnvironmentPasteVersion;
        latest.EnterCurrentStep(false, true);
        position = snapshot.Position;
        return true;
    }
}
