using UnityEngine;

/// <summary>입력/Animator 없이 검증할 수 있는 공격 방향과 타격 시점 계산. 직접 부착하지 않습니다.</summary>
public static class AttackMath
{
    public static Vector2 CardinalDirection(Vector2 direction, float verticalHalfAngle)
    {
        if (direction.sqrMagnitude < 0.0001f) return Vector2.right;
        float angle = Mathf.Atan2(Mathf.Abs(direction.x), Mathf.Abs(direction.y)) * Mathf.Rad2Deg;
        if (angle < Mathf.Clamp(verticalHalfAngle, 1f, 45f))
            return direction.y > 0f ? Vector2.up : Vector2.down;
        return direction.x < 0f ? Vector2.left : Vector2.right;
    }

    public static float ImpactTime(float length, float frameRate, int framesFromEnd = 3)
    {
        if (length <= 0f || frameRate <= 0f) return 1f;
        return Mathf.Clamp01((length - framesFromEnd / frameRate) / length);
    }
}
