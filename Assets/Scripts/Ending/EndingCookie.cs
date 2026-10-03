using UnityEngine;

// 엔딩에서 마녀를 둘러싸러 달려오는 쿠키 한 마리. 목표 X까지 달려와 서면, 달리는 모션은 계속하고 가만히 선 채로 무작위로 폴짝폴짝 뛴다.
public class EndingCookie : MonoBehaviour
{
    float targetX;
    float speed;
    Vector2 hopInterval;

    public bool Arrived { get; private set; }

    public void Init(float targetX, float speed, Vector2 hopInterval)
    {
        this.targetX = targetX;
        this.speed = speed;
        this.hopInterval = hopInterval;
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
            // CookieHop은 Awake에서 서 있는 높이를 기억하므로 도착해서 높이가 정해진 뒤에 붙인다
            var hop = gameObject.AddComponent<CookieHop>();
            hop.SetInterval(hopInterval);
        }
    }
}
