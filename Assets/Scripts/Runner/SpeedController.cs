using UnityEngine;

// 스테이지 스크롤 속도. 기준 속도는 시간이 지나며 계속 빨라지고(상한 없음),
// 감속 키를 누르는 동안만 느려진다. 키를 떼면 기준 속도로 복귀한다.
public class SpeedController : MonoBehaviour
{
    [SerializeField] RunnerInput input;
    [SerializeField] PlayerController player;
    [SerializeField] float startSpeed = 4f;          // units/sec
    [SerializeField] float minSpeed = 1.5f;          // 감속 하한
    [SerializeField] float acceleration = 0.1f;      // 기준 속도 증가량 (units/sec²)
    [SerializeField] float brakeDeceleration = 6f;   // 감속 키를 누를 때
    [SerializeField] float recoveryRate = 4f;        // 키를 뗐을 때 기준 속도로 복귀하는 속도

    float baseSpeed;

    public float Speed { get; private set; }

    // 사망/클리어 중에는 스크롤을 멈춘다
    public bool Frozen { get; set; }
    public float ScrollSpeed => Frozen ? 0f : Speed;
    public float BaseSpeed => baseSpeed;

    // 스테이지별 설정(StageSettings)이 시작 시 적용한다. Awake보다 먼저/나중에 불려도 결과가 같다.
    public void Configure(float startSpeed, float acceleration)
    {
        this.startSpeed = startSpeed;
        this.acceleration = acceleration;
        baseSpeed = startSpeed;
        Speed = startSpeed;
    }

    public void Restore(float speed, float baseSpeed)
    {
        Speed = speed;
        this.baseSpeed = baseSpeed;
    }

    void Awake()
    {
        baseSpeed = startSpeed;
        Speed = startSpeed;
    }

    void Update()
    {
        if (Frozen) return;

        float dt = Time.deltaTime;
        baseSpeed += acceleration * dt;

        if (!player.Grounded && !player.Flying) return;   // 공중에서는 속도 유지 (마녀 비행 중에만 조절 가능)

        if (input.BrakeHeld)
            Speed = Mathf.Max(minSpeed, Speed - brakeDeceleration * dt);
        else
            Speed = Mathf.MoveTowards(Speed, baseSpeed, recoveryRate * dt);
    }
}
