/// <summary>원본/붙여넣기 출처와 활성 상태를 공통 관리합니다. GameObject에 부착하지 않습니다.</summary>
public struct ErrorEffectState
{
    public bool IsActive { get; private set; }
    public bool IsPasted { get; private set; }
    public bool CanCut => IsActive && !IsPasted;
    public void ActivateOriginal() { IsActive = true; IsPasted = false; }
    public void MarkPasted() { IsPasted = true; }
    public void Deactivate() { IsActive = false; }
}
