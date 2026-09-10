using UnityEngine;

/// <summary>적의 패링 경직 시간입니다. 이동/공격 스크립트는 IsStunned를 확인합니다. MeleeEnemy가 자동으로 부착합니다.</summary>
[DisallowMultipleComponent]
public sealed class EnemyStagger : MonoBehaviour
{
    private float stunnedUntil;
    public bool IsStunned => Time.time < stunnedUntil;

    public void Stun(float duration)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + Mathf.Max(0f, duration));
    }
}
