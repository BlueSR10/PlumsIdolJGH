using UnityEngine;

// 플레이어가 도착하는 순간에 맞춰 열리는 문. Gate 조각에 붙이고 MovingHazard는 끈다 (위아래 움직임을 이 스크립트가 대신한다).
// 화면에 들어오기 직전(lockDistance)에 플레이어의 지금 속도와 앞으로의 가속을 보고 도착 시각을 계산해서,
// 그 순간 문 아래 끝이 플레이어 머리(requireSlide면 슬라이드한 머리)보다 clearance만큼만 위에 있도록 타이밍을 맞춘다.
// 맞춘 뒤에는 MovingHazard처럼 스테이지 시간 기준으로 왕복하므로, 그 뒤에 감속하면 늦게 도착해서 문이 더 열려 있다.
// 재시작하면 다시 계산한다.
public class SyArrivalGate : MonoBehaviour
{
    [SerializeField] float amplitude = 1.1f;   // 위아래 이동 폭 (유닛)
    [SerializeField] float period = 3f;        // 왕복 주기 (초)
    [Tooltip("켜면 슬라이드해야 겨우 지나가고, 끄면 서서 겨우 지나간다")]
    [SerializeField] bool requireSlide = true;
    [Tooltip("도착하는 순간 머리 위로 남는 틈 (유닛). 작을수록 아슬아슬하다")]
    [SerializeField, Min(0f)] float clearance = 0.1f;
    [Tooltip("계산한 도착 시각보다 이만큼 늦게 열린다 (초). 0보다 크면 그대로 달려서는 닫혀 있고, 감속해서 늦게 와야 지나간다")]
    [SerializeField, Min(0f)] float arrivalDelay;
    [Tooltip("플레이어와의 가로 거리가 이 값 이하가 되면 타이밍을 계산한다. 화면 오른쪽 끝은 플레이어에서 약 7")]
    [SerializeField, Min(0f)] float lockDistance = 8f;
    [Tooltip("슬라이드 중 충돌 높이 비율. PlayerController의 값과 같게 둔다")]
    [SerializeField, Range(0.1f, 1f)] float slideHeightRatio = 0.5f;
    [Tooltip("감속 키를 뗐을 때 속도가 돌아오는 양. SpeedController의 값과 같게 둔다")]
    [SerializeField, Min(0f)] float recoveryRate = 4f;

    Vector3 origin;
    Collider2D box;
    Collider2D playerBox;
    SpeedController speed;
    float acceleration;
    float bottomDrop;     // 문 위치(피벗)에서 충돌 상자 아래 끝까지의 거리
    float playerBottom;   // 땅에 서 있을 때 충돌 상자 아래 끝
    float playerHeight;   // 서 있을 때 충돌 상자 높이
    float phase;
    bool locked;

    void Awake()
    {
        origin = transform.localPosition;
        box = GetComponent<Collider2D>();
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        bottomDrop = transform.position.y - box.bounds.min.y;
        speed = FindFirstObjectByType<SpeedController>();
        var settings = FindFirstObjectByType<StageSettings>();
        if (settings != null) acceleration = settings.acceleration;

        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        playerBox = player.GetComponent<Collider2D>();
        playerBottom = playerBox.bounds.min.y;
        playerHeight = playerBox.bounds.size.y;
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetState;
    }

    void ResetState()
    {
        locked = false;
        phase = 0f;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null) return;

        if (!locked && run.Current == RunManager.State.Running && playerBox != null && speed != null)
        {
            float distance = box.bounds.min.x - playerBox.bounds.max.x;
            if (distance <= lockDistance) Lock(run.StageTime + TimeToCover(Mathf.Max(0f, distance)) + arrivalDelay);
        }

        float s = Mathf.Sin((run.StageTime / period + phase) * Mathf.PI * 2f);
        transform.localPosition = origin + Vector3.up * (s * amplitude);
    }

    // arrival 시각에 문 아래 끝이 목표 높이를 지나며 올라가도록 phase를 정한다
    void Lock(float arrival)
    {
        locked = true;
        float headY = playerBottom + playerHeight * (requireSlide ? slideHeightRatio : 1f);
        float rest = transform.parent != null ? transform.parent.TransformPoint(origin).y : origin.y;
        float s = Mathf.Clamp((headY + clearance + bottomDrop - rest) / amplitude, -1f, 1f);
        phase = Mathf.Repeat(Mathf.Asin(s) / (Mathf.PI * 2f) - arrival / period, 1f);
    }

    // 지금 속도에서 distance를 가는 데 걸리는 시간. 감속에서 회복 중이면 기준 속도까지는 recoveryRate로,
    // 그 뒤로는 스테이지 가속으로 빨라진다고 본다.
    float TimeToCover(float distance)
    {
        float v = speed.Speed;
        float gap = speed.BaseSpeed - v;
        float elapsed = 0f;

        if (gap > 0.001f && recoveryRate > acceleration)
        {
            float catchUp = gap / (recoveryRate - acceleration);
            float covered = v * catchUp + 0.5f * recoveryRate * catchUp * catchUp;
            if (covered >= distance) return Solve(v, recoveryRate, distance);

            elapsed = catchUp;
            distance -= covered;
            v += recoveryRate * catchUp;
        }

        return elapsed + Solve(v, acceleration, distance);
    }

    // distance = v·t + a·t²/2 를 t에 대해 푼다
    static float Solve(float v, float a, float distance)
    {
        if (a <= 0.0001f) return v > 0f ? distance / v : 0f;
        return (-v + Mathf.Sqrt(v * v + 2f * a * distance)) / a;
    }
}
