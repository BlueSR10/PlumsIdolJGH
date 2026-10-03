using UnityEngine;

// 스테이지 스크롤 속도. 기준 속도는 시간이 지나며 계속 빨라지고(상한 없음),
// 감속 키를 누르는 동안만 느려진다. 키를 떼면 기준 속도로 복귀한다.
public class SpeedController : MonoBehaviour
{
    [SerializeField] RunnerInput input;
    [SerializeField] float startSpeed = 4f;          // units/sec
    [SerializeField] float minSpeed = 1.5f;          // 감속 하한
    [SerializeField] float acceleration = 0.1f;      // 기준 속도 증가량 (units/sec²)
    [SerializeField] float brakeDeceleration = 6f;   // 감속 키를 누를 때
    [SerializeField] float recoveryRate = 4f;        // 키를 뗐을 때 기준 속도로 복귀하는 속도

    float baseSpeed;

    public float Speed { get; private set; }

    void Awake()
    {
        baseSpeed = startSpeed;
        Speed = startSpeed;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        baseSpeed += acceleration * dt;

        if (input.BrakeHeld)
            Speed = Mathf.Max(minSpeed, Speed - brakeDeceleration * dt);
        else
            Speed = Mathf.MoveTowards(Speed, baseSpeed, recoveryRate * dt);
    }
}
