using UnityEngine;

/// <summary>게임 UI의 공용 폰트 에셋입니다. 오브젝트에 붙이지 않고 Resources/GameUIFont에서 설정합니다.</summary>
[CreateAssetMenu(menuName = "Story/Game UI Fonts")]
public sealed class GameUIFontSettings : ScriptableObject
{
    [Tooltip("본문·버튼·안내에 사용하는 일반 폰트입니다.")]
    public Font regular;
    [Tooltip("제목·보관함 이름 등 강조에 사용하는 실제 Bold 폰트입니다.")]
    public Font bold;
}
