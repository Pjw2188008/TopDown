using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector2 moveDirection;
    private float moveSpeed;
    private float damage;

    [Tooltip("투사체 생존 시간 (메모리 누수 방지용)")]
    public float lifeTime = 3f;

    //원거리 적 스크립트에서 투사체를 생성할 때 호출하여 값을 넘겨줌
    public void Initialize(Vector2 direction, float speed, float dmg)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        damage = dmg;

        //방향에 맞게 투사체 회전 (오른쪽을 바라보는 스프라이트 기준)
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        //일정 시간 후 자동 소멸
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        //매 프레임 이동 처리
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    //다른 오브젝트(플레이어, 벽 등)와 충돌(Trigger)했을 때
    void OnTriggerEnter2D(Collider2D collision)
    {
        //1. 충돌한 대상이 "Player" 태그를 가지고 있는지 확인
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 투사체에 맞아 {damage} 데미지를 입음!");
            
            //TODO: 플레이어의 피격 함수 호출
            //예: collision.GetComponent<PlayerHealth>()?.TakeDamage(damage);

            //명중 시 투사체 파괴
            Destroy(gameObject);
        }
        //2. 벽(Wall)이나 환경 오브젝트에 닿으면 파괴되도록 추가 가능
        else if (collision.CompareTag("Wall") || collision.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
}