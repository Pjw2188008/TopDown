using System.Collections.Generic;
using UnityEngine;

/// <summary>EnemyController의 탐색/순찰/추적 부분입니다. partial 구현이므로 직접 부착하지 않습니다.</summary>
public partial class EnemyController
{
    private void ResolvePlayer()
    {
        // 대부분의 프레임에서는 기존 참조를 재사용합니다. 대상 교체/삭제 때만 다시 탐색합니다.
        if (target != null && player == target.transform) return;
        target = player != null ? player.GetComponentInParent<PlayerMove>() : null;
        if (target == null) target = FindFirstObjectByType<PlayerMove>();
        if (target != null) player = target.transform;
    }

    private void SetNewPatrolPoint()
    {
        patrolCenter = transform.position; // 기존 순찰처럼 현재 위치에서 다음 목적지를 정합니다.
        targetPatrolPoint = patrolCenter + new Vector2(Random.Range(-Mathf.Abs(patrolAreaSize.x)/2f, Mathf.Abs(patrolAreaSize.x)/2f),
            Random.Range(-Mathf.Abs(patrolAreaSize.y)/2f, Mathf.Abs(patrolAreaSize.y)/2f));
    }

    private void MoveTowards(Vector2 point)
    {
        Vector2 next = Vector2.MoveTowards(transform.position, point, Mathf.Max(0f, moveSpeed) * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    private void PatrolLogic()
    {
        if (IsInsideBox(player.position, patrolAreaSize)) { currentState = State.Chase; return; }
        MoveTowards(targetPatrolPoint);
        if (Vector2.Distance(transform.position, targetPatrolPoint) < .1f) SetNewPatrolPoint();
    }

    private void ChaseLogic()
    {
        if (!IsInsideBox(player.position, patrolAreaSize)) { currentState = State.Patrol; SetNewPatrolPoint(); return; }
        if (IsInsideBox(player.position, attackAreaSize)) { currentState = State.Attack; return; }
        MoveTowards(player.position);
    }

    private bool IsInsideBox(Vector3 position, Vector2 size)
    {
        Vector2 delta = position - transform.position;
        return Mathf.Abs(delta.x) <= Mathf.Abs(size.x)/2f && Mathf.Abs(delta.y) <= Mathf.Abs(size.y)/2f;
    }
}
