using UnityEngine;

// 마녀·게임 흐름에 맞춰 SoundManager로 소리를 낸다: 점프, 2단 점프, 브레이크(처음 누를 때 한 번),
// 아이템 획득, 사망(충돌음 + 조금 뒤 실패음), 클리어, 빗자루 비행 루프, 추격자 발소리 루프(가까울수록 크게).
// 어떤 클립을 쓸지는 SoundManager 프리팹에서 정한다.
[RequireComponent(typeof(PlayerController))]
public class PlayerSounds : MonoBehaviour
{
    [Tooltip("브레이크음을 다시 낼 수 있기까지 최소 간격 (초). 키를 연타하거나 착지하며 다시 눌릴 때 겹치지 않게")]
    [SerializeField, Min(0f)] float brakeCooldown = 0.3f;
    [Tooltip("이 스크롤 속도에서 달리기 발소리를 원래 속도(피치 1)로 낸다. PlayerAnimator의 Run Speed Reference와 같게 둔다")]
    [SerializeField, Min(0.1f)] float runSpeedReference = 4f;

    PlayerController player;
    SpeedController speed;
    Chaser chaser;
    bool wasBraking;
    float brakeTimer;

    void OnEnable()
    {
        player = GetComponent<PlayerController>();
        player.Jumped += OnJumped;
        RunManager.Died += OnDied;
        RunManager.Cleared += OnCleared;
        ItemPickup.Picked += OnItemPicked;
    }

    void OnDisable()
    {
        player.Jumped -= OnJumped;
        RunManager.Died -= OnDied;
        RunManager.Cleared -= OnCleared;
        ItemPickup.Picked -= OnItemPicked;
        SoundManager.SetLoop(Sound.BroomFly, 0f);
        SoundManager.SetLoop(Sound.ChaserSteps, 0f);
        SoundManager.SetLoop(Sound.WitchRun, 0f);
        SoundManager.StopHold(Sound.Slide);
    }

    void Start()
    {
        speed = FindFirstObjectByType<SpeedController>();
        chaser = FindFirstObjectByType<Chaser>();
    }

    void Update()
    {
        var run = RunManager.Instance;
        bool running = run != null && run.Current == RunManager.State.Running;

        brakeTimer -= Time.deltaTime;
        bool braking = running && speed != null && speed.Braking;
        if (braking && !wasBraking && brakeTimer <= 0f)
        {
            SoundManager.Play(Sound.Brake);
            brakeTimer = brakeCooldown;
        }
        wasBraking = braking;

        SoundManager.SetLoop(Sound.BroomFly, running && player.Flying ? 1f : 0f);
        SoundManager.SetLoop(Sound.ChaserSteps, running && chaser != null ? chaser.Proximity : 0f);

        // 슬라이드: 누르는 동안 이어지고(앞부분 → 중간 반복), 떼면 뒷부분이 난다
        bool sliding = running && player.Grounded && player.Sliding;
        SoundManager.SetHold(Sound.Slide, sliding);

        // 달리기 발소리: 땅에서 그냥 달릴 때만. 속도가 빠를수록 발도 빨라지는 모션(PlayerAnimator)에 맞춰 피치를 올린다
        bool runningOnGround = running && player.Grounded && !player.Sliding && !player.Flying && !braking;
        float scroll = speed != null ? speed.ScrollSpeed : 0f;
        SoundManager.SetLoop(Sound.WitchRun, runningOnGround ? 1f : 0f, Mathf.Clamp(scroll / runSpeedReference, 0.8f, 1.5f));
    }

    void OnJumped(bool airJump) => SoundManager.Play(airJump ? Sound.DoubleJump : Sound.Jump);

    void OnItemPicked(ItemPickup.Kind kind) => SoundManager.Play(Sound.Item);

    // 배경음악은 끄고(재시작할 때 처음부터 다시 켜진다), 충돌음 바로 뒤에 실패음이 겹친다 (실패음의 지연은 SoundManager 프리팹의 Delay)
    void OnDied()
    {
        SoundManager.PauseBgm();
        SoundManager.StopHold(Sound.Slide);   // 슬라이드 중에 죽으면 뒷부분 없이 바로 끈다
        SoundManager.Play(Sound.Collision);
        SoundManager.Play(Sound.Fail);
    }

    void OnCleared() => SoundManager.Play(Sound.Goal);
}
