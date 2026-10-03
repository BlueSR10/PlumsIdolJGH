using UnityEngine;

// 미리 점프한 플레이어를 잡는 장애물. Hazard와 함께 쓴다.
// 가만히 있다가 플레이어가 holdDistance까지 오면 그 거리를 유지하며 플레이어 앞에 머문다 (화면에서는 멈춰 보인다).
// 플레이어가 2단 점프를 쓰고 착지할 때쯤 뒤로 잠깐 물러났다가 돌진해서 착지 직전에 닿는다.
// 점프하지 않고 maxHoldTime을 버티면 똑같이 물러났다가 돌진하고, 이때는 지상에 있으니 점프로 넘을 수 있다.
// 재시작하면 처음 자리로 돌아간다.
public class SyFeintHazard : MonoBehaviour
{
    enum Phase { Idle, Hold, Retreat, Lunge, Done }

    [Tooltip("플레이어와의 가로 거리가 이 값이 되면 그 거리를 유지하며 머문다")]
    [SerializeField, Min(0.5f)] float holdDistance = 1.5f;
    [Tooltip("점프하지 않아도 이 시간이 지나면 돌진한다 (초). 0이면 2단 점프를 할 때까지 계속 머문다")]
    [SerializeField, Min(0f)] float maxHoldTime = 3f;
    [Tooltip("돌진 전에 뒤로 물러나는 거리")]
    [SerializeField, Min(0f)] float retreatDistance = 1.2f;
    [Tooltip("뒤로 물러나는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0.01f)] float retreatTime = 0.4f;
    [Tooltip("돌진 속도 (화면 기준 units/sec)")]
    [SerializeField, Min(1f)] float lungeSpeed = 7f;
    [Tooltip("착지보다 이만큼 먼저 닿는다 (초). 키우면 더 높은 곳에서 맞는다")]
    [SerializeField, Min(0f)] float landEarly = 0.05f;

    const float ExitDistance = 10f;   // 돌진 후 플레이어 뒤로 이만큼 지나가면 멈춘다 (화면 밖)

    Vector3 origin;
    PlayerController player;
    Rigidbody2D playerBody;
    Phase phase;
    float offset;       // 플레이어 기준 가로 거리
    float timer;
    float groundY;      // 플레이어가 마지막으로 땅에 서 있던 높이
    float contactGap;   // 충돌 상자끼리 닿는 가로 거리

    bool timedOut;      // 미리 점프해서가 아니라 maxHoldTime이 지나서 돌진을 시작했는지

    // 시간이 다 되어 물러나거나 돌진하는 중인지 (SyAmbushBird가 본다). 미리 점프해서 시작한 돌진은 해당하지 않는다.
    public bool TimedOutAttack => timedOut && (phase == Phase.Retreat || phase == Phase.Lunge);

    void Awake()
    {
        origin = transform.localPosition;
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        playerBody = player.GetComponent<Rigidbody2D>();
        groundY = player.transform.position.y;
        contactGap = GetComponent<Collider2D>().bounds.extents.x + player.GetComponent<Collider2D>().bounds.extents.x;
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetState;
    }

    void ResetState()
    {
        phase = Phase.Idle;
        timedOut = false;
        transform.localPosition = origin;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running || player == null) return;

        float dt = Time.deltaTime;
        if (player.Grounded) groundY = player.transform.position.y;

        switch (phase)
        {
            case Phase.Idle:
                if (transform.position.x - player.transform.position.x > holdDistance) return;
                phase = Phase.Hold;
                timer = 0f;
                SetOffset(holdDistance);
                break;

            case Phase.Hold:
                timer += dt;
                SetOffset(holdDistance);
                bool jumpedEarly = player.DoubleJumped && !player.Grounded && TimeToLand() <= LungeLeadTime() + landEarly;
                if (jumpedEarly || (maxHoldTime > 0f && timer >= maxHoldTime))
                {
                    phase = Phase.Retreat;
                    timedOut = !jumpedEarly;
                    timer = 0f;
                }
                break;

            case Phase.Retreat:
                timer += dt;
                SetOffset(holdDistance + retreatDistance * Mathf.Clamp01(timer / retreatTime));
                if (timer >= retreatTime) phase = Phase.Lunge;
                break;

            case Phase.Lunge:
                SetOffset(offset - lungeSpeed * dt);
                if (offset < -ExitDistance) phase = Phase.Done;
                break;
        }
    }

    void SetOffset(float value)
    {
        offset = value;
        var p = transform.position;
        p.x = player.transform.position.x + offset;
        transform.position = p;
    }

    // 물러나기 시작해서 플레이어에게 닿을 때까지 걸리는 시간
    float LungeLeadTime()
    {
        return retreatTime + Mathf.Max(0f, holdDistance + retreatDistance - contactGap) / lungeSpeed;
    }

    // 지금 높이와 속도로 땅에 닿을 때까지 남은 시간
    float TimeToLand()
    {
        float g = -Physics2D.gravity.y * playerBody.gravityScale;
        if (g <= 0f) return float.PositiveInfinity;

        float h = Mathf.Max(0f, player.transform.position.y - groundY);
        float vy = playerBody.linearVelocity.y;
        return (vy + Mathf.Sqrt(vy * vy + 2f * g * h)) / g;
    }
}
