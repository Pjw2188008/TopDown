using UnityEngine;

/// <summary>사이드뷰 공격 확인용 표적에 붙입니다. 피격 시 빨갛게 변하고 체력 소진 후 자동 회복합니다.</summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class SideViewTargetDummy : MonoBehaviour, ICombatDamageable
{
    [SerializeField, Min(1f), Tooltip("연습용 표적 최대 HP입니다.")] private float maxHealth = 5f;
    [SerializeField, Min(.1f), Tooltip("HP가 0이 되면 이 시간 뒤 자동 회복합니다.")] private float resetDelay = 1f;
    private SpriteRenderer visual;
    private Color baseColor;
    private float health, flashUntil, resetAt;
    public float Health => health;
    private void Awake() { visual=GetComponent<SpriteRenderer>();baseColor=visual.color;health=maxHealth; }
    public bool ReceiveDamage(float amount, GameObject source, bool canReflect)
    {
        if (!isActiveAndEnabled || health<=0 || amount<=0) return false;
        health=Mathf.Max(0,health-amount);flashUntil=Time.time+.15f;
        visual.color=Color.red;if(health<=0)resetAt=Time.time+resetDelay;return true;
    }
    private void Update()
    {
        if(health<=0 && Time.time>=resetAt)health=maxHealth;
        visual.color=health<=0?Color.gray:Time.time<flashUntil?Color.red:baseColor;
    }
    private void OnGUI()
    {
        if(Camera.main==null)return;
        Vector3 p=Camera.main.WorldToScreenPoint(visual.bounds.max+Vector3.up*.15f);
        if(p.z>0)GUI.Label(new Rect(p.x-70,Screen.height-p.y,150,24),"Target HP "+health+" / "+maxHealth);
    }
}
