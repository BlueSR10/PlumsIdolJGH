using UnityEngine;

// 한 번의 도전(시작 → 사망/클리어)을 관리한다. 스테이지 중간 체크포인트는 없으므로 사망하면 처음부터 다시 시작한다.
public class RunManager : MonoBehaviour
{
    public enum State { Running, Dead, Cleared }

    [SerializeField] Rigidbody2D stage;
    [SerializeField] PlayerController player;
    [SerializeField] SpeedController speed;
    [SerializeField] Chaser chaser;
    [SerializeField] float restartDelay = 0.8f;
    [SerializeField] float clearDelay = 2.5f;
    [SerializeField] float fallDeathDepth = 6f;     // 시작 높이보다 이만큼 아래로 떨어지면 사망

    float stateTimer;
    float spawnY;
    float startSpeed;
    float startBaseSpeed;

    public static RunManager Instance { get; private set; }

    public State Current { get; private set; }
    public float StageTime { get; private set; }
    public float PlayerLocalX => player.transform.position.x - stage.position.x;
    public float StartLocalX { get; private set; }
    public float GoalLocalX { get; private set; }

    void Awake()
    {
        Instance = this;
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
    }

    void Update()
    {
        if (Current == State.Running)
        {
            StageTime += Time.deltaTime;
            if (player.transform.position.y < spawnY - fallDeathDepth) Die();
            return;
        }

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
        Current = State.Running;
    }
}
