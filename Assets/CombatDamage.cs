using UnityEngine;

public interface ICombatDamageable
{
    bool ReceiveDamage(float amount, GameObject source, bool canReflect);
}

public static class CombatDamageUtility
{
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
