using UnityEngine;

// 플레이어 뒤에서 일정한 속도로 쫓아오는 추격자. Stage의 자식으로 두면 맵과 함께 움직인다.
// 위치는 스테이지 로컬 X 좌표로 관리한다. 잡히는 판정은 선두(이 오브젝트)의 몸통 상자(바닥에서 위로 bodyHeight)와
// 플레이어의 충돌 상자가 실제로 겹칠 때만 한다: 마녀가 점프로 쿠키 위에 떠 있으면 아래로 지나가도 잡히지 않는다.
// 자식 쿠키들은 군단처럼 보이는 그림일 뿐이다 (프리팹의 Crowd 자식). 몸통 상자는 그림이 폴짝 뛰어도 바닥에 둔다.
public class Chaser : MonoBehaviour
{
    [SerializeField] float speed = 4.5f;            // 스테이지별로 다르게 설정 (units/sec)
    [SerializeField] float startGap = 8f;           // 시작/재시작 시 플레이어와의 거리
    [SerializeField] float catchDistance = 0.6f;    // 발소리(Proximity) 계산용 기준 거리. 잡히는 판정은 아래 몸통 상자로 한다
    [SerializeField] float warningDistance = 5f;    // 이 거리 안으로 들어오면 Proximity가 0보다 커진다 (발소리용)
    [Header("잡히는 판정: 선두 쿠키의 몸통 상자 (바닥 기준)")]
    [Tooltip("몸통 상자의 가로 반폭. 쿠키 그림 앞끝에 맞춘 값")]
    [SerializeField, Min(0.05f)] float bodyHalfWidth = 0.375f;
    [Tooltip("몸통 상자의 높이 (바닥에서 위로). 마녀가 이보다 높이 떠 있으면 닿지 않는다")]
    [SerializeField, Min(0.1f)] float bodyHeight = 0.7f;

    static readonly int RunState = Animator.StringToHash("Run");

    Animator[] animators;   // 선두(이 오브젝트)와 뒤따르는 쿠키들의 달리기 모션
    CookieJostle[] jostles; // animators와 같은 순서 (선두는 null)
    CookieHop leaderHop;
    Collider2D playerBody;
    float localX;

    public float StartGap => startGap;
    public float LocalX => localX;
    public float Gap => RunManager.Instance.PlayerLocalX - localX;

    // 0(멀다) ~ 1(거의 잡힘). 추후 발소리 볼륨에 사용.
    public float Proximity => Mathf.Clamp01(1f - (Gap - catchDistance) / warningDistance);

    public void Configure(float speed, float startGap)
    {
        this.speed = speed;
        this.startGap = startGap;
    }

    public void ResetTo(float x)
    {
        localX = x;
        Place();
    }

    void Awake()
    {
        animators = GetComponentsInChildren<Animator>();

        // 선두와 뒤따르는 쿠키 모두 무작위로 폴짝 뛴다 (그림만, 판정과 무관)
        // 뒤따르는 쿠키(선두 제외)는 서로 앞서거니 뒤서거니 하며 그에 맞춰 달리기 모션도 빨라지고 느려진다
        jostles = new CookieJostle[animators.Length];
        for (int i = 0; i < animators.Length; i++)
        {
            var go = animators[i].gameObject;
            var hop = go.GetComponent<CookieHop>();
            if (hop == null) hop = go.AddComponent<CookieHop>();
            if (go == gameObject)
            {
                leaderHop = hop;
                continue;
            }
            jostles[i] = go.GetComponent<CookieJostle>();
            if (jostles[i] == null) jostles[i] = go.AddComponent<CookieJostle>();
        }
    }

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) playerBody = player.GetComponent<Collider2D>();

        // 뒤따르는 쿠키들은 달리기 모션의 시작 위치를 어긋나게 해서 발이 똑같이 움직이지 않게 한다
        for (int i = 1; i < animators.Length; i++) animators[i].Play(RunState, 0, (i * 0.27f) % 1f);
    }

    void Update()
    {
        // 쫓아오지 않을 때(사망·클리어)는 달리기 모션도 멈춘다
        var run = RunManager.Instance;
        float s = run != null && run.Current == RunManager.State.Running ? 1f : 0f;
        for (int i = 0; i < animators.Length; i++)
            animators[i].speed = s * (jostles[i] != null ? jostles[i].SpeedScale : 1f);
    }

    void FixedUpdate()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running) return;

        localX += speed * Time.fixedDeltaTime;
        Place();
        if (Touching()) run.Die();
    }

    // 선두 쿠키의 몸통 상자 (월드 좌표). 아래는 바닥(그림 아랫면)이고 그림이 뛰어올라도 따라 올라가지 않는다
    Rect BodyRect()
    {
        float hopped = leaderHop != null ? leaderHop.Offset : 0f;
        float bottom = transform.position.y - hopped - 0.5f;   // 그림 한 칸이 1유닛이고 중심이 transform
        return new Rect(transform.position.x - bodyHalfWidth, bottom, bodyHalfWidth * 2f, bodyHeight);
    }

    // 플레이어 충돌 상자와 몸통 상자가 겹치는지 (닿는 순간만 true)
    bool Touching()
    {
        if (playerBody == null) return false;
        var body = BodyRect();
        var b = playerBody.bounds;
        return b.max.x > body.xMin && b.min.x < body.xMax && b.max.y > body.yMin && b.min.y < body.yMax;
    }

    // Scene 뷰: 선택했을 때 잡히는 몸통 상자(빨강)를 보여 준다
    void OnDrawGizmosSelected()
    {
        float bottom = transform.position.y - 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector3(transform.position.x, bottom + bodyHeight * 0.5f, 0f),
            new Vector3(bodyHalfWidth * 2f, bodyHeight, 0f));
    }

    void Place()
    {
        var p = transform.localPosition;
        p.x = localX;
        transform.localPosition = p;
    }
}
