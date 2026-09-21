using UnityEngine;

/// <summary>레이저 전체를 일시 정지시키는 사물 표식입니다. 실제 충돌 영역을 가진 Collider2D도 필요합니다.</summary>
[DisallowMultipleComponent]
public sealed class LaserBlocker : MonoBehaviour
{
    [Tooltip("켜져 있는 동안 이 물체의 일반 Collider2D가 레이저를 차단합니다. Trigger Collider는 차단하지 않습니다.")]
    [SerializeField] private bool blocksLaser = true;
    public bool BlocksLaser => isActiveAndEnabled && blocksLaser;
}
