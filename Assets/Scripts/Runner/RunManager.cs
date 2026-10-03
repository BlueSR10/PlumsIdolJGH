using System.Collections.Generic;
using UnityEngine;

// 한 번의 도전(시작 → 사망/클리어)을 관리한다. 사망하면 마지막 체크포인트 상태로 되돌린다.
public class RunManager : MonoBehaviour
{
    public enum State { Running, Dead, Cleared }

    [SerializeField] Rigidbody2D stage;
    [SerializeField] PlayerController player;
    [SerializeField] SpeedController speed;
    [SerializeField] Chaser chaser;
    [SerializeField] float respawnDelay = 0.8f;
    [SerializeField] float clearDelay = 2.5f;
    [SerializeField] float respawnChaserGap = 7f;   // 체크포인트 부활 시 추격자와의 거리
    [SerializeField] float fallDeathDepth = 6f;     // 시작 높이보다 이만큼 아래로 떨어지면 사망

    struct Snapshot
    {
        public float localX, speed, baseSpeed, stageTime;
    }

    Snapshot initial;
    Snapshot checkpoint;
    float stateTimer;
    float spawnY;

    public static RunManager Instance { get; private set; }

    public State Current { get; private set; }
    public float StageTime { get; private set; }
    public float PlayerLocalX => player.transform.position.x - stage.position.x;
    public float StartLocalX { get; private set; }
    public float GoalLocalX { get; private set; }
    public IReadOnlyList<float> CheckpointXs => checkpointXs;

    readonly List<float> checkpointXs = new List<float>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        spawnY = player.transform.position.y;
        StartLocalX = PlayerLocalX;
        initial = new Snapshot { localX = StartLocalX, speed = speed.Speed, baseSpeed = speed.BaseSpeed, stageTime = 0f };
        checkpoint = initial;

        var goal = FindFirstObjectByType<Goal>();
        GoalLocalX = goal != null ? ToLocalX(goal.transform.position.x) : StartLocalX + 100f;
        foreach (var c in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
            checkpointXs.Add(ToLocalX(c.transform.position.x));

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
        if (stateTimer > 0f) return;

        if (Current == State.Cleared) checkpoint = initial;
        Respawn();
    }

    public float ToLocalX(float worldX) => worldX - stage.position.x;

    public void Die()
    {
        if (Current != State.Running) return;
        Current = State.Dead;
        stateTimer = respawnDelay;
        speed.Frozen = true;
    }

    public void ReachCheckpoint(float localX)
    {
        if (Current != State.Running || localX <= checkpoint.localX + 0.01f) return;
        checkpoint = new Snapshot { localX = localX, speed = speed.Speed, baseSpeed = speed.BaseSpeed, stageTime = StageTime };
    }

    public void Clear()
    {
        if (Current != State.Running) return;
        Current = State.Cleared;
        stateTimer = clearDelay;
        speed.Frozen = true;
    }

    void Respawn()
    {
        var pos = new Vector2(player.transform.position.x - checkpoint.localX, stage.position.y);
        stage.position = pos;
        stage.transform.position = pos;

        speed.Restore(checkpoint.speed, checkpoint.baseSpeed);
        speed.Frozen = false;
        StageTime = checkpoint.stageTime;
        player.ResetState();
        chaser.ResetTo(checkpoint.localX - respawnChaserGap);
        Current = State.Running;
    }
}
