using UnityEngine;

// 플레이어 뒤에서 일정한 속도로 쫓아오는 추격자. Stage의 자식으로 두면 맵과 함께 움직인다.
// 위치는 스테이지 로컬 X 좌표로 관리하고, 플레이어와의 거리가 catchDistance 이하가 되면 잡힌 것으로 본다.
// 잡히는 판정은 선두(이 오브젝트)만 한다. 자식 쿠키들은 군단처럼 보이는 그림일 뿐이다 (프리팹의 Crowd 자식).
public class Chaser : MonoBehaviour
{
    [SerializeField] float speed = 4.5f;            // 스테이지별로 다르게 설정 (units/sec)
    [SerializeField] float startGap = 8f;           // 시작/재시작 시 플레이어와의 거리
    [SerializeField] float catchDistance = 1.25f;   // 중심 간 거리가 이 이하이면 사망
    [SerializeField] float warningDistance = 5f;    // 이 거리 안으로 들어오면 Proximity가 0보다 커진다 (발소리용)

    static readonly int RunState = Animator.StringToHash("Run");

    Animator[] animators;   // 선두(이 오브젝트)와 뒤따르는 쿠키들의 달리기 모션
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
    }

    void Start()
    {
        // 뒤따르는 쿠키들은 달리기 모션의 시작 위치를 어긋나게 해서 발이 똑같이 움직이지 않게 한다
        for (int i = 1; i < animators.Length; i++) animators[i].Play(RunState, 0, (i * 0.27f) % 1f);
    }

    void Update()
    {
        // 쫓아오지 않을 때(사망·클리어)는 달리기 모션도 멈춘다
        var run = RunManager.Instance;
        float s = run != null && run.Current == RunManager.State.Running ? 1f : 0f;
        foreach (var a in animators) a.speed = s;
    }

    void FixedUpdate()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running) return;

        localX += speed * Time.fixedDeltaTime;
        Place();
        if (Gap <= catchDistance) run.Die();
    }

    void Place()
    {
        var p = transform.localPosition;
        p.x = localX;
        transform.localPosition = p;
    }
}
