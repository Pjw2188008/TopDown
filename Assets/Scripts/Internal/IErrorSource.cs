/// <summary>오류 종류별 구현을 몰라도 Cut을 처리하기 위한 규약. 직접 부착하지 않습니다.</summary>
public interface IErrorSource
{
    StoredErrorType ErrorType { get; }
    float StoredMultiplier { get; }
    bool IsActive { get; }
    bool CanCut { get; }
    void RemoveError();
}

/// <summary>가속 오류를 전달받는 이동 컴포넌트 규약. 직접 부착하지 않습니다.</summary>
public interface IAccelerationTarget
{
    void SetAccelerationMultiplier(float multiplier);
}
