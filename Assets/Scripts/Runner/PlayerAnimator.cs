using UnityEngine;

// 플레이어 상태(지상/슬라이드/점프/2단 점프/비행/착지)에 맞춰 마녀 애니메이션을 재생한다.
// 아티스트의 Witch.controller에는 전환이 없어서, 상태 이름으로 직접 Play 한다.
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float landTime = 0.08f;          // 착지 자세를 보여 주는 시간
    [SerializeField] float runSpeedReference = 4f;    // 이 스크롤 속도에서 달리기를 1배속으로 재생한다

    static readonly int Run = Animator.StringToHash("Run");
    static readonly int Sit = Animator.StringToHash("Sit");
    static readonly int Slide = Animator.StringToHash("Slide");
    static readonly int Jump = Animator.StringToHash("Jump");
    static readonly int DJump = Animator.StringToHash("DJump");
    static readonly int Broom = Animator.StringToHash("Broom");

    PlayerController player;
    SpeedController speed;
    int current;
    bool wasGrounded = true;
    float landTimer;

    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Start()
    {
        speed = FindFirstObjectByType<SpeedController>();
    }

    void LateUpdate()
    {
        if (player.Grounded && !wasGrounded && !player.Flying) landTimer = landTime;
        wasGrounded = player.Grounded;
        if (landTimer > 0f) landTimer -= Time.deltaTime;

        int target;
        if (player.Flying) target = Broom;
        else if (!player.Grounded) target = player.DoubleJumped ? DJump : Jump;
        else if (player.Sliding) target = Slide;
        else if (landTimer > 0f) target = Sit;
        else target = Run;

        if (target != current)
        {
            current = target;
            animator.Play(target, 0, 0f);
        }
        else if (target == Run && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            animator.Play(Run, 0, 0f);   // 달리기 클립은 반복 설정이 아니라 직접 되감는다
        }

        // 달리는 속도가 빨라지면 발도 빠르게
        animator.speed = target == Run && speed != null
            ? Mathf.Clamp(speed.ScrollSpeed / runSpeedReference, 0.5f, 2f)
            : 1f;
    }
}
