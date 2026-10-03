using UnityEngine;

// 마녀·게임 흐름에 맞춰 SoundManager로 소리를 낸다: 점프, 2단 점프, 브레이크(처음 누를 때 한 번),
// 아이템 획득, 사망(충돌음 + 조금 뒤 실패음), 클리어, 빗자루 비행 루프, 추격자 발소리 루프(가까울수록 크게).
// 어떤 클립을 쓸지는 SoundManager 프리팹에서 정한다.
[RequireComponent(typeof(PlayerController))]
public class PlayerSounds : MonoBehaviour
{
    [Tooltip("브레이크음을 다시 낼 수 있기까지 최소 간격 (초). 키를 연타하거나 착지하며 다시 눌릴 때 겹치지 않게")]
    [SerializeField, Min(0f)] float brakeCooldown = 0.3f;

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
    }

    void OnJumped(bool airJump) => SoundManager.Play(airJump ? Sound.DoubleJump : Sound.Jump);

    void OnItemPicked(ItemPickup.Kind kind) => SoundManager.Play(Sound.Item);

    // 충돌음 바로 뒤에 실패음이 겹친다 (실패음의 지연은 SoundManager 프리팹의 Delay)
    void OnDied()
    {
        SoundManager.Play(Sound.Collision);
        SoundManager.Play(Sound.Fail);
    }

    void OnCleared() => SoundManager.Play(Sound.Goal);
}
