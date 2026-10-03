using UnityEngine;

// 추격 쿠키가 무작위 간격으로 폴짝 뛴다 (그림만 움직이고 잡히는 판정에는 영향이 없다).
// Chaser가 선두와 뒤따르는 쿠키 각각에 자동으로 붙인다. 쿠키마다 간격과 높이가 달라서 군단이 제각각 뛴다.
// 선두가 뛸 때 자식 쿠키들이 같이 끌려 올라가지 않도록, 부모의 뛴 높이만큼 빼서 각자 뛴다.
public class CookieHop : MonoBehaviour
{
    [Tooltip("다음 점프까지 기다리는 시간 범위 (초)")]
    [SerializeField] Vector2 interval = new Vector2(0.4f, 2f);
    [Tooltip("점프 높이 범위 (units)")]
    [SerializeField] Vector2 height = new Vector2(0.25f, 0.6f);
    [Tooltip("한 번 뛰어 착지할 때까지 걸리는 시간 범위 (초)")]
    [SerializeField] Vector2 duration = new Vector2(0.35f, 0.55f);

    float baseY;
    float offset;       // 지금 뛰어오른 높이
    float t, dur, h;    // 점프 진행 (0~1), 시간, 높이
    float wait;
    bool hopping;
    CookieHop parentHop;

    void Awake()
    {
        baseY = transform.localPosition.y;
        wait = Random.Range(interval.x, interval.y);
        if (transform.parent != null) parentHop = transform.parent.GetComponentInParent<CookieHop>();
        StageReset.Requested += Snap;
    }

    void OnDestroy()
    {
        StageReset.Requested -= Snap;
    }

    // 재시작하면 땅으로 돌아온다
    void Snap()
    {
        hopping = false;
        offset = 0f;
        wait = Random.Range(interval.x, interval.y);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (hopping)
        {
            t += dt / dur;
            if (t >= 1f)
            {
                hopping = false;
                offset = 0f;
                wait = Random.Range(interval.x, interval.y);
            }
            else offset = h * 4f * t * (1f - t);   // 포물선
        }
        else
        {
            // 쫓아오는 중에만 새로 뛴다 (사망·클리어로 멈추면 뛰던 점프만 마저 끝낸다)
            var run = RunManager.Instance;
            if (run == null || run.Current != RunManager.State.Running) return;

            wait -= dt;
            if (wait <= 0f)
            {
                hopping = true;
                t = 0f;
                dur = Random.Range(duration.x, duration.y);
                h = Random.Range(height.x, height.y);
            }
        }
    }

    // 부모가 같은 프레임에 움직인 뒤에 위치를 정하도록 LateUpdate에서 적용한다
    void LateUpdate()
    {
        var p = transform.localPosition;
        p.y = baseY + offset - (parentHop != null ? parentHop.offset : 0f);
        transform.localPosition = p;
    }
}
