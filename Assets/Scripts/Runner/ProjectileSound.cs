using UnityEngine;

// 날아오는 장애물(투사체: 새 등)의 소리. 종류와 상관없이 모두 SoundManager의 Wind를 쓴다.
// 위치에 따라 좌우로 들리고 가까울수록 크게 들리며, 다가올 때는 높게 지나간 뒤에는 낮게 들리는 도플러 효과를 적용한다.
// 도플러는 Unity 내장 대신 직접 계산한다: 스테이지 스크롤(FixedUpdate)과 새의 이동(Update)이 섞여 있어 내장 속도 계산이 떨리기 때문이다.
//
// 쓰는 법: 날아오는 오브젝트에 이 컴포넌트를 붙인다 (다른 스크립트를 고칠 필요 없음).
//  - 기본(Detect Movement 켜짐): 오브젝트가 스테이지 안에서 실제로 움직이기 시작하면 소리가 나고, 멈추면 꺼진다.
//    가만히 놓여 있다가 조건이 맞으면 날아오는 방식이면 어떤 스크립트든 그대로 된다.
//  - 날기 시작하는 시점을 코드가 아는 경우(FlyingHazard): Detect Movement를 끄고 Begin()/End()를 부른다.
public class ProjectileSound : MonoBehaviour
{
    [Tooltip("켜면 움직이기 시작할 때 자동으로 소리를 낸다. 끄면 코드가 Begin()/End()를 부를 때만")]
    [SerializeField] bool detectMovement = true;
    [Tooltip("스테이지 안에서 이 속도(units/sec) 이상으로 움직이면 날고 있는 것으로 본다")]
    [SerializeField, Min(0.1f)] float moveThreshold = 1f;
    [Tooltip("플레이어보다 이만큼 왼쪽으로 지나가면 소리를 끈다 (화면 밖에서 계속 나지 않게)")]
    [SerializeField, Min(0f)] float stopBehind = 8f;
    [SerializeField, Min(0.01f)] float fadeTime = 0.2f;

    const float TeleportDistance = 2f;   // 한 프레임에 이만큼 넘게 움직이면 순간이동으로 보고 속도 계산에서 뺀다

    AudioSource src;
    AudioListener listener;
    Transform player;
    SpeedController speed;
    float baseVolume;
    float speedOfSound = 30f;
    float level;
    float idleTime;
    bool active;
    bool ready;
    bool finished;          // 지나가서 끈 뒤에는 재시작할 때까지 다시 켜지 않는다
    Vector3 lastLocal;
    Vector3 localVelocity;

    // FlyingHazard처럼 날기 시작하는 시점을 아는 쪽이 직접 제어할 때 쓴다
    public bool DetectMovement
    {
        get => detectMovement;
        set => detectMovement = value;
    }

    void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        ready = SoundManager.TryConfigureProjectile(src, out baseVolume, out speedOfSound);
        lastLocal = transform.localPosition;
        StageReset.Requested += OnReset;
    }

    void Start()
    {
        var p = FindFirstObjectByType<PlayerController>();
        if (p != null) player = p.transform;
        speed = FindFirstObjectByType<SpeedController>();
        listener = FindFirstObjectByType<AudioListener>();
    }

    void OnDestroy()
    {
        StageReset.Requested -= OnReset;
    }

    public void Begin()
    {
        if (ready) active = true;
    }

    public void End()
    {
        active = false;
    }

    void OnReset()
    {
        active = false;
        finished = false;
        idleTime = 0f;
        lastLocal = transform.localPosition;
        localVelocity = Vector3.zero;
    }

    void Update()
    {
        if (!ready) return;

        float dt = Time.deltaTime;
        var run = RunManager.Instance;
        bool running = run != null && run.Current == RunManager.State.Running;

        UpdateVelocity(dt);

        if (detectMovement && running)
        {
            float moving = localVelocity.magnitude;
            if (!active && !finished && moving >= moveThreshold)
            {
                active = true;
                idleTime = 0f;
            }
            else if (active && moving < moveThreshold * 0.3f)
            {
                idleTime += dt;
                if (idleTime > 0.3f) active = false;   // 멈췄다
            }
            else idleTime = 0f;
        }

        // 마녀 뒤로 충분히 지나갔으면 끈다
        if (active && player != null && transform.position.x < player.position.x - stopBehind)
        {
            active = false;
            finished = true;
        }

        level = Mathf.MoveTowards(level, active && running ? 1f : 0f, dt / fadeTime);
        src.volume = baseVolume * level;
        if (level > 0f && !src.isPlaying) src.Play();
        else if (level <= 0f && src.isPlaying) src.Stop();

        ApplyDoppler();
    }

    // 스테이지 안에서의 이동 속도(내 Update로 움직이는 만큼, 부드럽게)
    void UpdateVelocity(float dt)
    {
        var local = transform.localPosition;
        if (dt > 0f)
        {
            var delta = local - lastLocal;
            if (delta.magnitude < TeleportDistance)
                localVelocity = Vector3.Lerp(localVelocity, delta / dt, 0.3f);
        }
        lastLocal = local;
    }

    // 소리 원천이 듣는 위치(카메라)에 다가오는 속도로 피치를 정한다: 피치 = c / (c - 다가오는 속도)
    void ApplyDoppler()
    {
        float scroll = speed != null ? speed.ScrollSpeed : 0f;
        var velocity = localVelocity + Vector3.left * scroll;   // 세계 기준 속도: 스테이지가 왼쪽으로 흐르는 만큼 더한다

        var listenerPos = listener != null ? listener.transform.position : (Camera.main != null ? Camera.main.transform.position : transform.position);
        var toListener = listenerPos - transform.position;
        if (toListener.sqrMagnitude < 0.0001f) return;

        float closing = Vector3.Dot(velocity, toListener.normalized);   // 양수 = 다가오는 중
        src.pitch = Mathf.Clamp(speedOfSound / Mathf.Max(0.01f, speedOfSound - closing), 0.5f, 2f);
    }
}
