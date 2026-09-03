using UnityEngine;

/// <summary>
/// 플레이어나 적처럼 전투 피해를 받을 수 있는 컴포넌트가 구현하는 공통 규약입니다.
/// 인터페이스이므로 GameObject에 직접 부착하지 않습니다.
/// </summary>
public interface ICombatDamageable
{
    bool ReceiveDamage(float amount, GameObject source, bool canReflect);
}

/// <summary>
/// 충돌한 GameObject와 부모에서 ICombatDamageable 구현을 찾아 피해를 전달하는 공용 기능입니다.
/// 정적 유틸리티이므로 GameObject에 직접 부착하지 않습니다.
/// </summary>
public static class CombatDamageUtility
{
    /// <summary>
    /// 대상에 피해를 전달하고 실제 피해 처리 컴포넌트를 찾았는지 반환합니다.
    /// </summary>
    public static bool TryApplyDamage(GameObject target, float amount, GameObject source, bool canReflect = true)
    {
        if (target == null || amount <= 0f)
        {
            return false;
        }

        MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ICombatDamageable damageable)
            {
                return damageable.ReceiveDamage(amount, source, canReflect);
            }
        }

        return false;
    }
}
