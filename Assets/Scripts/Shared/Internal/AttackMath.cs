using UnityEngine;

/// <summary>입력/Animator 없이 검증할 수 있는 공격 타격 시점 계산. 직접 부착하지 않습니다.</summary>
public static class AttackMath
{
    public static float ImpactTime(float length, float frameRate, int framesFromEnd = 3)
    {
        if (length <= 0f || frameRate <= 0f) return 1f;
        return Mathf.Clamp01((length - framesFromEnd / frameRate) / length);
    }
}
