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
    Rigidbody2D rb;
    BoxCollider2D col;
    ContactFilter2D groundFilter;
    Vector2 standSize;
    Vector2 standOffset;
    Vector3 visualScale;
    Vector3 visualPos;
    Vector3 spawnPosition;
    float jumpBuffer;
    int jumpsUsed;
    bool sliding;

    public bool Grounded { get; private set; }

    void Awake()
    {
        input = GetComponent<RunnerInput>();
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
    }

    void Update()
    {
        if (input.JumpPressed) jumpBuffer = jumpBufferTime;
        else jumpBuffer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        Grounded = col.IsTouching(groundFilter);

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

        float ratio = value ? slideHeightRatio : 1f;
        float drop = standSize.y * (1f - ratio) * 0.5f;

        col.size = new Vector2(standSize.x, standSize.y * ratio);
        col.offset = new Vector2(standOffset.x, standOffset.y - drop);
        visual.localScale = new Vector3(visualScale.x, visualScale.y * ratio, visualScale.z);
        visual.localPosition = new Vector3(visualPos.x, visualPos.y - drop, visualPos.z);
    }

    // 체크포인트 부활 시 호출. 위치와 점프/슬라이드 상태를 시작 상태로 되돌린다.
    public void ResetState()
    {
        rb.linearVelocity = Vector2.zero;
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        jumpBuffer = 0f;
        jumpsUsed = 0;
        SetSliding(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Hazard>() != null)
            RunManager.Instance.Die();
    }
}
