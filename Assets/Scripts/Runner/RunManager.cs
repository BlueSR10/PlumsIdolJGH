using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 한 번의 도전(시작 → 사망/클리어)을 관리한다. 스테이지 중간 체크포인트는 없으므로 사망하면 Game Over 화면에서
// Space를 눌러 처음부터 다시 시작한다 (재시작하면 배경음악도 처음부터).
// 클리어하면 맵의 StageSettings.nextStage가 있을 때 전환 연출(8.4) 후 그 스테이지로 넘어가고, 없으면 같은 스테이지를 다시 시작한다.
public class RunManager : MonoBehaviour
{
    public enum State { Running, Dead, Cleared }

    [SerializeField] Rigidbody2D stage;
    [SerializeField] PlayerController player;
    [SerializeField] SpeedController speed;
    [SerializeField] Chaser chaser;
    [Tooltip("사망 후 Game Over 문구가 나타나고 재시작 키를 받기 시작하는 시간 (초). 점프 키를 연타하다 바로 재시작되는 것을 막는다")]
    [SerializeField] float restartDelay = 0.8f;
    [SerializeField] float clearDelay = 2.5f;
    [SerializeField] float fallDeathDepth = 6f;     // 시작 높이보다 이만큼 아래로 떨어지면 사망

    [Header("스테이지 전환 연출")]
    [SerializeField] float transitionDelay = 0.6f;  // 클리어 후 어두워지기 시작할 때까지
    [SerializeField] float fadeOutTime = 0.9f;
    [SerializeField] float darkHoldTime = 0.6f;     // 완전히 어두운 채로 마녀만 보이는 시간
    [SerializeField] float fadeInTime = 0.9f;       // 다음 스테이지에서 밝아지는 시간

    static bool fadeInPending;   // 전환으로 막 들어온 씬이면 어두운 상태에서 시작한다

    bool fadingIn;
    InputAction retry;
    float stateTimer;
    float spawnY;
    float startSpeed;
    float startBaseSpeed;
    ScreenFade fade;

    public static RunManager Instance { get; private set; }

    // 사망했을 때(장애물·추격자·추락 공통) 한 번 발생한다. 이펙트·소리가 구독한다.
    public static event System.Action Died;

    // 골에 닿아 클리어했을 때 한 번 발생한다.
    public static event System.Action Cleared;

    public State Current { get; private set; }
    public bool Transitioning { get; private set; }   // 클리어 후 어두워지는 중
    public bool HideHud => Transitioning || fadingIn;   // 전환 연출 중에는 HUD를 숨긴다
    public bool CanRetry => Current == State.Dead && !Transitioning && stateTimer <= 0f;   // Game Over 문구를 보여 주고 재시작을 받는 중
    public float StageTime { get; private set; }
    public float PlayerLocalX => player.transform.position.x - stage.position.x;
    public float StartLocalX { get; private set; }
    public float GoalLocalX { get; private set; }

    void Awake()
    {
        Instance = this;
        fade = gameObject.AddComponent<ScreenFade>();
        fade.Init(Camera.main, player.GetComponentInChildren<SpriteRenderer>());
        gameObject.AddComponent<GameOverHUD>();

        retry = new InputAction("Retry", InputActionType.Button);
        retry.AddBinding("<Keyboard>/space");
        retry.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable() => retry.Enable();

    void OnDisable() => retry.Disable();

    void Start()
    {
        spawnY = player.transform.position.y;
        StartLocalX = PlayerLocalX;
        startSpeed = speed.Speed;
        startBaseSpeed = speed.BaseSpeed;

        var goal = FindFirstObjectByType<Goal>();
        GoalLocalX = goal != null ? ToLocalX(goal.transform.position.x) : StartLocalX + 100f;

        chaser.ResetTo(StartLocalX - chaser.StartGap);

        if (fadeInPending)
        {
            fadeInPending = false;
            StartCoroutine(FadeIn());
        }
    }

    void Update()
    {
        if (Current == State.Running)
        {
            StageTime += Time.deltaTime;
            if (player.transform.position.y < spawnY - fallDeathDepth) Die();
            return;
        }

        if (Transitioning) return;

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        // 사망하면 키를 눌러야 다시 시작하고, 클리어 후 반복(다음 스테이지 없음)은 자동으로 다시 시작한다
        if (Current == State.Cleared || retry.WasPressedThisFrame()) Restart();
    }

    public float ToLocalX(float worldX) => worldX - stage.position.x;

    public void Die()
    {
        if (Current != State.Running) return;
        Current = State.Dead;
        stateTimer = restartDelay;
        speed.Frozen = true;
        Died?.Invoke();
    }

    public void Clear()
    {
        if (Current != State.Running) return;
        Current = State.Cleared;
        stateTimer = clearDelay;
        Cleared?.Invoke();

        // 다음 스테이지가 있으면 멈추지 않고 계속 달린 채로 어두워진다. 없으면 멈추고 같은 스테이지를 다시 시작한다.
        var settings = FindFirstObjectByType<StageSettings>();
        if (settings != null && !string.IsNullOrEmpty(settings.nextStage))
        {
            StartCoroutine(TransitionTo(settings.nextStage));
            return;
        }
        speed.Frozen = true;
    }

    // 마녀만 남기고 어두워진 뒤 다음 스테이지 씬을 불러온다
    IEnumerator TransitionTo(string sceneName)
    {
        Transitioning = true;
        yield return new WaitForSeconds(transitionDelay);
        fade.LiftWitch(true);
        yield return fade.FadeTo(1f, fadeOutTime);
        yield return new WaitForSeconds(darkHoldTime);
        fadeInPending = true;
        StageLoader.Load(sceneName);
    }

    // 전환으로 들어온 스테이지: 이전 스테이지에서 이어 달리는 채로 어두운 화면에서 마녀만 보이다가 밝아진다.
    // 멈추지 않고 바로 Running이다 (맵 시작 부분은 비어 있어서 밝아지는 동안 위험하지 않다).
    IEnumerator FadeIn()
    {
        fadingIn = true;
        fade.SetAlpha(1f);
        fade.LiftWitch(true);
        yield return fade.FadeTo(0f, fadeInTime);
        fade.LiftWitch(false);
        fadingIn = false;
    }

    void Restart()
    {
        var pos = new Vector2(player.transform.position.x - StartLocalX, stage.position.y);
        stage.position = pos;
        stage.transform.position = pos;

        speed.Restore(startSpeed, startBaseSpeed);
        speed.Frozen = false;
        StageTime = 0f;
        player.ResetState();
        chaser.ResetTo(StartLocalX - chaser.StartGap);
        StageReset.Raise();   // 부서진 장애물, 먹은 아이템 복구
        SoundManager.RestartBgm();
        Current = State.Running;
    }
}
