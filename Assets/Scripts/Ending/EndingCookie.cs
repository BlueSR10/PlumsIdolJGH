using UnityEngine;

// 엔딩에서 마녀를 둘러싸러 달려오는 쿠키 한 마리. 목표 X까지 달려와 서면 달리기 모션을 멈춘다.
public class EndingCookie : MonoBehaviour
{
    float targetX;
    float speed;
    Animator animator;

    public bool Arrived { get; private set; }

    public void Init(float targetX, float speed)
    {
        this.targetX = targetX;
        this.speed = speed;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Arrived) return;
        var p = transform.position;
        p.x = Mathf.MoveTowards(p.x, targetX, speed * Time.deltaTime);
        transform.position = p;
        if (Mathf.Approximately(p.x, targetX))
        {
            Arrived = true;
            if (animator != null) animator.speed = 0f;   // 서서 마녀를 노려본다
        }
    }
}
