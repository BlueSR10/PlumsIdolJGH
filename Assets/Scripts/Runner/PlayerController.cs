using UnityEngine;
using UnityEngine.SceneManagement;

// 화면에 X 고정된 플레이어. 점프(체공 시간 고정)와 슬라이드(누르는 동안, 지상에서만)를 처리한다.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(RunnerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform visual;
    [SerializeField] float jumpHeight = 1.8f;
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
    float jumpBuffer;
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

        if (jumpBuffer > 0f && Grounded)
        {
            float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            rb.linearVelocity = new Vector2(0f, Mathf.Sqrt(2f * g * jumpHeight));
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

    void OnTriggerEnter2D(Collider2D other)
    {
        // M3에서 체크포인트 부활로 교체. 지금은 씬을 다시 불러온다.
        if (other.GetComponentInParent<Hazard>() != null)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
