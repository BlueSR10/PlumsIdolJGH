using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 한 번의 도전(시작 → 사망/클리어)을 관리한다. 스테이지 중간 체크포인트는 없으므로 사망하면 처음부터 다시 시작한다.
// 클리어하면 맵의 StageSettings.nextStage가 있을 때 전환 연출(8.4) 후 그 스테이지로 넘어가고, 없으면 같은 스테이지를 다시 시작한다.
public class RunManager : MonoBehaviour
{
    public enum State { Running, Dead, Cleared, Intro }   // Intro = 스테이지 전환 직후 페이드 인 중 (아직 출발 전)

    [SerializeField] Rigidbody2D stage;
    [SerializeField] PlayerController player;
    [SerializeField] SpeedController speed;
    [SerializeField] Chaser chaser;
    [SerializeField] float restartDelay = 0.8f;
    [SerializeField] float clearDelay = 2.5f;
    [SerializeField] float fallDeathDepth = 6f;     // 시작 높이보다 이만큼 아래로 떨어지면 사망

    [Header("스테이지 전환 연출")]
    [SerializeField] float transitionDelay = 0.6f;  // 클리어 후 어두워지기 시작할 때까지
    [SerializeField] float fadeOutTime = 0.9f;
    [SerializeField] float darkHoldTime = 0.6f;     // 완전히 어두운 채로 마녀만 보이는 시간
    [SerializeField] float fadeInTime = 0.9f;

    static bool fadeInPending;   // 전환으로 막 들어온 씬이면 어두운 상태에서 시작한다

    float stateTimer;
    float spawnY;
    float startSpeed;
    float startBaseSpeed;
    ScreenFade fade;

    public static RunManager Instance { get; private set; }

    public State Current { get; private set; }
    public bool Transitioning { get; private set; }   // 클리어 후 어두워지는 중
    public bool HideHud => Transitioning || Current == State.Intro;   // 전환 연출 중에는 HUD를 숨긴다
    public float StageTime { get; private set; }
    public float PlayerLocalX => player.transform.position.x - stage.position.x;
    public float StartLocalX { get; private set; }
    public float GoalLocalX { get; private set; }

    void Awake()
    {
        Instance = this;
        fade = gameObject.AddComponent<ScreenFade>();
        fade.Init(Camera.main, player.GetComponentInChildren<SpriteRenderer>());
    }

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
            StartCoroutine(Intro());
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

        if (Current == State.Intro || Transitioning) return;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) Restart();
    }

    public float ToLocalX(float worldX) => worldX - stage.position.x;

    public void Die()
    {
        if (Current != State.Running) return;
        Current = State.Dead;
        stateTimer = restartDelay;
        speed.Frozen = true;
    }

    public void Clear()
    {
        if (Current != State.Running) return;
        Current = State.Cleared;
        stateTimer = clearDelay;
        speed.Frozen = true;

        var settings = FindFirstObjectByType<StageSettings>();
        if (settings != null && !string.IsNullOrEmpty(settings.nextStage))
            StartCoroutine(TransitionTo(settings.nextStage));
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
        SceneManager.LoadScene(sceneName);
    }

    // 전환으로 들어온 스테이지: 어두운 채로 마녀만 보이다가 밝아지면 출발한다
    IEnumerator Intro()
    {
        Current = State.Intro;
        speed.Frozen = true;
        fade.SetAlpha(1f);
        fade.LiftWitch(true);
        yield return new WaitForSeconds(darkHoldTime);
        yield return fade.FadeTo(0f, fadeInTime);
        fade.LiftWitch(false);
        speed.Frozen = false;
        Current = State.Running;
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
        Current = State.Running;
    }
}
