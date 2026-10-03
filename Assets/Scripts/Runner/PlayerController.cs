using UnityEngine;

// 화면에 X 고정된 플레이어. 점프(체공 시간 고정)와 슬라이드(누르는 동안, 지상에서만)를 처리한다.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(RunnerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform visual;
    [SerializeField] float jumpHeight = 1.8f;
    [SerializeField] float airJumpHeight = 1.2f;   // 2단 점프 높이
    [SerializeField] int maxJumps = 2;
    [SerializeField] float gravityScale = 3f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float slideHeightRatio = 0.5f;

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
    float sizeScale = 1f;   // 거대화/소형화 배율 (PlayerForm이 설정)
    int jumpsUsed;
    bool sliding;

    public bool Grounded { get; private set; }
    public bool Flying => flight != null && flight.Active;

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

    void Update()
    {
        if (Flying)
        {
            jumpBuffer = 0f;   // 비행 중 점프 키는 상승 조작
            return;
        }

        if (input.JumpPressed) jumpBuffer = jumpBufferTime;
        else jumpBuffer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        Grounded = col.IsTouching(groundFilter);
        if (Flying) return;   // 비행 중 이동은 WitchFlight가 담당

        if (Grounded && rb.linearVelocity.y <= 0.1f) jumpsUsed = 0;
        else if (!Grounded && jumpsUsed == 0) jumpsUsed = 1;   // 점프 없이 공중에 뜬 경우 1회 소모

        if (jumpBuffer > 0f && jumpsUsed < maxJumps)
        {
            float height = jumpsUsed == 0 ? jumpHeight : airJumpHeight;
            float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            rb.linearVelocity = new Vector2(0f, Mathf.Sqrt(2f * g * height));
            jumpsUsed++;
            jumpBuffer = 0f;
            SetSliding(false);
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

    // 배율과 슬라이드 상태를 합쳐 콜라이더와 스프라이트 크기를 정한다.
    void ApplyShape()
    {
        float sx = sizeScale;
        float sy = sizeScale * (sliding ? slideHeightRatio : 1f);
        float bottom = standOffset.y - standSize.y * 0.5f;
        var size = new Vector2(standSize.x * sx, standSize.y * sy);

        col.size = size;
        col.offset = new Vector2(standOffset.x, bottom + size.y * 0.5f);
        visual.localScale = new Vector3(visualScale.x * sx, visualScale.y * sy, visualScale.z);
        visual.localPosition = new Vector3(visualPos.x, visualPos.y + (sy - 1f) * visualHalfHeight, visualPos.z);
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
        jumpsUsed = 0;
        SetSliding(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hazard = other.GetComponentInParent<Hazard>();
        if (hazard == null) return;

        // 거대화 중에는 파괴 가능 장애물을 부수고 지나간다. 파괴 불가 장애물과 벽(Gate)은 그대로 사망.
        if (hazard.Destructible && form != null && form.Giant) hazard.Break();
        else RunManager.Instance.Die();
    }
}
