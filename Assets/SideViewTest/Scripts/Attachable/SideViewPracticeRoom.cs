using UnityEngine;

/// <summary>사이드뷰 테스트 씬의 투사체 허수아비 연습을 연결합니다. 오류/표적은 기존 공통 컴포넌트를 사용합니다.</summary>
public sealed class SideViewPracticeRoom : MonoBehaviour
{
    [SerializeField, Tooltip("사이드뷰 모드를 켠 PlayerMove입니다.")] private PlayerMove player;
    [SerializeField, Tooltip("공격/가드/패링을 반복 시험할 공통 투사체 허수아비입니다.")] private TutorialTrainingDummy dummy;
    private void Start() { if(player!=null && dummy!=null) dummy.BeginPractice(player); }
    private void OnDisable() { if(dummy!=null) dummy.EndPractice(false); }
}
