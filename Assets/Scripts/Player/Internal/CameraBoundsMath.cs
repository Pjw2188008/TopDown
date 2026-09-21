using UnityEngine;

/// <summary>카메라 화면 반경을 고려한 사각형 경계 계산입니다. 직접 부착하지 않습니다.</summary>
public static class CameraBoundsMath
{
    public static Vector3 ClampPosition(Vector3 position, Bounds bounds, Vector2 halfView)
    {
        position.x = ClampAxis(position.x, bounds.min.x, bounds.max.x, halfView.x);
        position.y = ClampAxis(position.y, bounds.min.y, bounds.max.y, halfView.y);
        return position; // Z와 플레이어 Offset은 경계 계산으로 바꾸지 않습니다.
    }

    private static float ClampAxis(float value, float min, float max, float halfView)
    {
        float lower = min + halfView;
        float upper = max - halfView;
        return lower <= upper ? Mathf.Clamp(value, lower, upper) : (min + max) * .5f;
    }

    public static float FitOrthographicSize(float requestedSize, Bounds bounds, Vector2 unitHalfView)
    {
        float fitX = bounds.extents.x / Mathf.Max(.0001f, unitHalfView.x);
        float fitY = bounds.extents.y / Mathf.Max(.0001f, unitHalfView.y);
        return Mathf.Min(requestedSize, Mathf.Min(fitX, fitY));
    }
}
