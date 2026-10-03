using UnityEngine;

// 추격 쿠키가 무작위로 폴짝 뛴다 (그림만 움직이고 잡히는 판정에는 영향이 없다).
// Chaser가 선두와 뒤따르는 쿠키 각각에 자동으로 붙인다.
// 일정하게 보이지 않도록: 쿠키마다 성격(뛰는 빈도)이 다르고, 쉬는 시간은 범위 안에서 매번 다르게 흔들리며,
// 높이는 낮게 뛰는 경우가 많고 가끔 크게 뛰고, 이따금 착지하자마자 한 번 더 뛴다.
// 선두가 뛸 때 자식 쿠키들이 같이 끌려 올라가지 않도록, 부모의 뛴 높이만큼 빼서 각자 뛴다.
public class CookieHop : MonoBehaviour
{
    [Tooltip("다음 점프까지 기다리는 시간 범위 (초). 쿠키마다 성격 배율이 곱해진다")]
    [SerializeField] Vector2 interval = new Vector2(2f, 6.5f);
    [Tooltip("쿠키마다 정해지는 성격 배율 범위. 클수록 그 쿠키는 덜 뛴다")]
    [SerializeField] Vector2 personality = new Vector2(0.7f, 1.6f);
    [Tooltip("점프 높이 범위 (units). 낮은 쪽이 더 자주 나온다")]
    [SerializeField] Vector2 height = new Vector2(0.2f, 0.7f);
    [Tooltip("한 번 뛰어 착지할 때까지 걸리는 시간 범위 (초)")]
    [SerializeField] Vector2 duration = new Vector2(0.35f, 0.6f);
    [Tooltip("착지하고 바로 한 번 더 뛸 확률")]
    [SerializeField, Range(0f, 1f)] float doubleHopChance = 0.18f;

    float baseY;
    float offset;       // 지금 뛰어오른 높이
    float t, dur, h;    // 점프 진행 (0~1), 시간, 높이
    float wait;
    float mood;         // 이 쿠키의 성격 배율
    bool hopping;
    bool chain;         // 연달아 뛰는 두 번째 점프인지
    CookieHop parentHop;

    // 지금 뛰어오른 높이 (Chaser가 몸통 상자를 바닥에 두려고 읽는다)
    public float Offset => offset;

    // 쉬는 시간 범위를 바꾼다 (엔딩처럼 짧은 장면에서 더 자주 뛰게). 다음 점프까지의 대기도 새로 정한다.
    public void SetInterval(Vector2 range)
    {
        interval = range;
        wait = NextWait();
    }

    void Awake()
    {
        baseY = transform.localPosition.y;
        mood = Random.Range(personality.x, personality.y);
        wait = NextWait();
        if (transform.parent != null) parentHop = transform.parent.GetComponentInParent<CookieHop>();
        StageReset.Requested += Snap;
    }

    void OnDestroy()
    {
        StageReset.Requested -= Snap;
    }

    // 쉬는 시간: 범위 안의 무작위 값에 쿠키 성격과 매번 다른 흔들림을 곱해 들쭉날쭉하게 한다
    float NextWait()
    {
        float r = Random.value;
        return Mathf.Lerp(interval.x, interval.y, r) * mood * Random.Range(0.8f, 1.3f);
    }

    // 높이: 낮게 뛰는 경우가 많고 가끔 크게
    float NextHeight()
    {
        float r = Random.value;
        return Mathf.Lerp(height.x, height.y, r * r);
    }

    // 재시작하면 땅으로 돌아온다
    void Snap()
    {
        hopping = false;
        chain = false;
        offset = 0f;
        wait = NextWait();
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
                // 가끔 착지하자마자 한 번 더 뛴다 (연달아 두 번을 넘기지는 않는다)
                if (!chain && Random.value < doubleHopChance) { chain = true; wait = Random.Range(0.02f, 0.12f); }
                else { chain = false; wait = NextWait(); }
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
                h = NextHeight() * (chain ? 0.7f : 1f);   // 연달아 뛰는 점프는 조금 낮게
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
