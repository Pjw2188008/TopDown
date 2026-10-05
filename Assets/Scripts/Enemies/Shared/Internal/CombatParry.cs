using UnityEngine;

/// <summary>패링의 결과를 직접 관리하는 적입니다. 일반 적의 즉시 경직을 보스 누적 경직 등으로 대체합니다.</summary>
public interface ICombatParryReceiver
{
    void ReceiveParry(PlayerMove player);
}

/// <summary>근접/투사체 패링 성공을 원래 공격자에게 한 번 알립니다. 직접 부착하지 않습니다.</summary>
public static class CombatParry
{
    public static bool Notify(GameObject attacker, PlayerMove player)
    {
        if (attacker == null) return false;
        foreach (var component in attacker.GetComponentsInParent<MonoBehaviour>(true))
            if (component is ICombatParryReceiver receiver)
            { receiver.ReceiveParry(player); return true; }
        return false;
    }
}
