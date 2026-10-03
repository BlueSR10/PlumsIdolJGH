using UnityEngine;

// 화면에 X 고정된 플레이어. 점프(체공 시간 고정)와 슬라이드(누르는 동안, 지상에서만)를 처리한다.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(RunnerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform visual;
    [Header("점프 (플레이 중에도 바로 반영됨)")]
    [Tooltip("1단 점프 높이 (units)")]
    [SerializeField, Min(0.1f)] float jumpHeight = 1.8f;
    [Tooltip("2단 점프 높이. 공중에서 키를 다시 눌렀을 때 현재 높이에서 추가로 오르는 양")]
    [SerializeField, Min(0.1f)] float airJumpHeight = 1.6f;
    [Tooltip("점프 횟수 (2 = 2단 점프)")]
    [SerializeField, Min(1)] int maxJumps = 2;
    [Tooltip("중력 배율. 클수록 빨리 오르고 빨리 떨어진다 (체공 시간이 짧아짐)")]
    [SerializeField, Min(0.1f)] float gravityScale = 3f;
    [Tooltip("착지 직전에 누른 점프 키를 기억하는 시간 (초)")]
    [SerializeField] float jumpBufferTime = 0.1f;

    [Header("슬라이드")]
    [Tooltip("슬라이드 중 충돌 높이 비율")]
    [SerializeField, Range(0.1f, 1f)] float slideHeightRatio = 0.5f;

    RunnerInput input;
    WitchFlight flight;
    PlayerForm form;
    Rigidbody2D rb;
    BoxCollider2D col;
    ContactFilter2D groundFilter;
    Vector2 standSize;
    Vector2 standOffset;
    Vector3 visualScale;
    Vector3 visualPos;
    Vector3 spawnPosition;
    float visualHalfHeight;
    float jumpBuffer;
    int resetFrame = -1;   // ResetState가 불린 프레임
    float sizeScale = 1f;   // 거대화/소형화 배율 (PlayerForm이 설정)
    int jumpsUsed;
    bool sliding;

    public bool Grounded { get; private set; }
    public bool Flying => flight != null && flight.Active;
    public bool Sliding => sliding;
    public bool DoubleJumped { get; private set; }   // 이번 체공에서 2단 점프를 썼는지 (애니메이션용)

    // 점프를 시작한 순간 발생한다. 인자는 공중에서 쓴 2단 점프인지 여부 (소리용).
    public event System.Action<bool> Jumped;

    void Awake()
    {
        input = GetComponent<RunnerInput>();
        flight = GetComponent<WitchFlight>();
        form = GetComponent<PlayerForm>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        rb.gravityScale = gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        groundFilter = new ContactFilter2D { useTriggers = false };
        groundFilter.SetNormalAngle(45f, 135f);

        spawnPosition = transform.position;
        standSize = col.size;
        standOffset = col.offset;
        visualScale = visual.localScale;
        visualPos = visual.localPosition;

        var sprite = visual.GetComponent<SpriteRenderer>()?.sprite;
        float spriteHeight = sprite != null ? sprite.bounds.size.y : 1f;
        visualHalfHeight = visualScale.y * spriteHeight * 0.5f;
    }

    // 사망(Game Over) 중에는 점프·슬라이드 입력을 받지 않는다
    bool Dead => RunManager.Instance != null && RunManager.Instance.Current == RunManager.State.Dead;

    void Update()
    {
        if (Dead)
        {
            jumpBuffer = 0f;
            return;
        }

        if (Flying)
        {
            jumpBuffer = 0f;   // 비행 중 점프 키는 상승 조작
            return;
        }

        rb.gravityScale = gravityScale;   // Inspector에서 플레이 중 바꾼 값을 반영

        // 재시작 키(Space)가 점프 키이기도 해서, 재시작과 같은 프레임에 눌린 점프는 무시한다
        if (input.JumpPressed && Time.frameCount != resetFrame) jumpBuffer = jumpBufferTime;
        else jumpBuffer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        Grounded = col.IsTouching(groundFilter);
        if (Flying) return;   // 비행 중 이동은 WitchFlight가 담당

        if (Grounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsUsed = 0;
            DoubleJumped = false;
        }
        else if (!Grounded && jumpsUsed == 0) jumpsUsed = 1;   // 점프 없이 공중에 뜬 경우 1회 소모

        if (Dead) return;   // 점프·슬라이드는 건너뛴다 (쓰러진 채로 점프하거나 슬라이드 소리가 나지 않게)

        if (jumpBuffer > 0f && jumpsUsed < maxJumps)
        {
            float height = jumpsUsed == 0 ? jumpHeight : airJumpHeight;
            float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            rb.linearVelocity = new Vector2(0f, Mathf.Sqrt(2f * g * height));
            bool airJump = jumpsUsed > 0;
            if (airJump) DoubleJumped = true;
            jumpsUsed++;
            jumpBuffer = 0f;
            SetSliding(false);
            Jumped?.Invoke(airJump);
            return;
        }

        SetSliding(input.SlideHeld && Grounded);
    }

    void SetSliding(bool value)
    {
        if (sliding == value) return;
        sliding = value;
        ApplyShape();
    }

    // 거대화/소형화 배율 변경. 발 위치(콜라이더 아래쪽)는 고정하고 위쪽으로 늘리거나 줄인다.
    public void SetSizeScale(float scale)
    {
        sizeScale = scale;
        ApplyShape();
    }

    // 배율과 슬라이드 상태를 합쳐 콜라이더 크기를, 배율로 스프라이트 크기를 정한다.
    // 슬라이드 자세는 슬라이드 애니메이션이 직접 그리므로 스프라이트는 눌러서 늘리지 않는다.
    void ApplyShape()
    {
        float sx = sizeScale;
        float sy = sizeScale * (sliding ? slideHeightRatio : 1f);
        float bottom = standOffset.y - standSize.y * 0.5f;
        var size = new Vector2(standSize.x * sx, standSize.y * sy);

        col.size = size;
        col.offset = new Vector2(standOffset.x, bottom + size.y * 0.5f);
        visual.localScale = new Vector3(visualScale.x * sizeScale, visualScale.y * sizeScale, visualScale.z);
        visual.localPosition = new Vector3(visualPos.x, visualPos.y + (sizeScale - 1f) * visualHalfHeight, visualPos.z);
    }

    // 비행 시작/종료 (WitchFlight가 호출). 비행 중에는 중력을 끄고 점프·슬라이드를 막는다.
    public void EnterFlight()
    {
        SetSliding(false);
        jumpBuffer = 0f;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    public void ExitFlight()
    {
        rb.gravityScale = gravityScale;
        jumpsUsed = maxJumps;   // 비행이 끝나면 착지할 때까지 공중 점프 없음
    }

    // 재시작 시 호출. 위치와 점프/슬라이드/비행 상태를 시작 상태로 되돌린다.
    public void ResetState()
    {
        if (flight != null) flight.ResetState();
        if (form != null) form.ResetState();
        rb.linearVelocity = Vector2.zero;
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        jumpBuffer = 0f;
        resetFrame = Time.frameCount;
        jumpsUsed = 0;
        DoubleJumped = false;
        SetSliding(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hazard = other.GetComponentInParent<Hazard>();
        if (hazard == null) return;

        // 거대화 중에는 파괴 가능 장애물을 부수고 지나간다. 파괴 불가 장애물과 벽(Gate)은 그대로 사망.
        if (hazard.Destructible && form != null && form.Giant) hazard.Break(col.bounds);
        else RunManager.Instance.Die();
    }
}
