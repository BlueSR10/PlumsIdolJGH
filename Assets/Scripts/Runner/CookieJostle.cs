using UnityEngine;

// 뒤따르는 쿠키들이 서로 앞서거니 뒤서거니 한다 (그림만, 선두와 잡히는 판정에는 영향이 없다).
// 쿠키마다 자기 자리(프리팹의 위치)를 기준으로 천천히 불규칙하게 앞뒤로 움직이고(Perlin 노이즈), 앞으로 치고 나갈 때는
// 달리기 모션도 빨라지고 뒤처질 때는 느려진다. 앞에 선 쿠키가 위에 그려지도록 위치에 따라 깊이(z)를 바꾼다.
// 사망·클리어로 멈추면 그 자리에서 멈춘다. Chaser가 뒤따르는 쿠키에 자동으로 붙인다.
public class CookieJostle : MonoBehaviour
{
    [Tooltip("자기 자리 기준으로 움직이는 범위 (units): 뒤쪽 한계(음수) ~ 앞쪽 한계(양수). 죽을 때 맨 뒤 쿠키도 화면에 남게 뒤쪽은 작게")]
    [SerializeField] Vector2 range = new Vector2(-0.25f, 0.6f);
    [Tooltip("선두 앞으로 나가지 않게 하는 한계 (Chaser 기준 x)")]
    [SerializeField] float maxFront = -0.3f;
    [Tooltip("위치가 바뀌는 빠르기. 클수록 앞뒤가 빨리 바뀐다 (쿠키마다 배율이 달라진다)")]
    [SerializeField, Min(0.01f)] float tempo = 0.6f;
    [Tooltip("앞뒤로 움직이는 속도만큼 달리기 모션이 빨라지고 느려지는 정도")]
    [SerializeField, Min(0f)] float animationFollow = 0.9f;

    float baseX;
    float seed;
    float pace;
    float clock;
    float lastX;
    float speedScale = 1f;

    // 달리기 모션 배속 (Chaser가 곱한다)
    public float SpeedScale => speedScale;

    void Awake()
    {
        baseX = transform.localPosition.x;
        lastX = baseX;
        seed = Random.value * 100f;
        pace = Random.Range(0.7f, 1.4f);
        clock = Random.value * 10f;   // 쿠키마다 시작 지점이 다르게
    }

    void Update()
    {
        var run = RunManager.Instance;
        bool running = run != null && run.Current == RunManager.State.Running;
        float dt = Time.deltaTime;
        if (!running || dt <= 0f)
        {
            speedScale = Mathf.Lerp(speedScale, 1f, 0.2f);
            return;
        }

        clock += dt * tempo * pace;
        // 큰 흐름과 작은 떨림을 섞는다. Perlin은 0.5 근처에 몰리므로 가운데 구간만 0~1로 넓혀 쓴다
        float n = 0.7f * Mathf.PerlinNoise(seed, clock) + 0.3f * Mathf.PerlinNoise(seed + 41f, clock * 2.3f);
        n = Mathf.InverseLerp(0.25f, 0.75f, n);
        float x = Mathf.Min(baseX + Mathf.Lerp(range.x, range.y, n), maxFront);

        var p = transform.localPosition;
        p.x = x;
        p.z = -x * 0.01f;   // 앞(오른쪽)에 선 쿠키가 카메라에 가까워 위에 그려진다
        transform.localPosition = p;

        float velocity = (x - lastX) / dt;
        lastX = x;
        speedScale = Mathf.Lerp(speedScale, Mathf.Clamp(1f + velocity * animationFollow, 0.6f, 1.6f), 0.2f);
    }
}
