using UnityEngine;

// 함정 가시(SyFeintHazard)가 시간이 다 되어 돌진하는 동안 2단 점프를 쓰면 화면 오른쪽 밖에서 날아와 맞히는 새. Hazard와 함께 쓴다.
// 가시가 멈춰 있을 때 미리 2단 점프한 경우는 가시가 처리하므로 나타나지 않는다.
// 평소에는 놓은 자리(화면 위쪽 밖에 둔다)에 가만히 있고, 날기 시작하면 플레이어 높이를 따라가며 왼쪽으로 지나간다.
// 1단 점프로 가시를 넘으면 나타나지 않는다. 재시작하면 처음 자리로 돌아간다.
public class SyAmbushBird : MonoBehaviour
{
    [Tooltip("이 가시가 시간이 다 되어 돌진하는 동안의 2단 점프에만 반응한다")]
    [SerializeField] SyFeintHazard trap;
    [Tooltip("날기 시작하는 위치. 플레이어보다 이만큼 앞 (화면 오른쪽 끝은 플레이어에서 약 7)")]
    [SerializeField, Min(0f)] float spawnAhead = 8f;
    [Tooltip("날아오는 속도 (화면 기준 units/sec)")]
    [SerializeField, Min(1f)] float flySpeed = 12f;
    [Tooltip("플레이어 높이를 따라가는 속도 (units/sec)")]
    [SerializeField, Min(0f)] float followSpeed = 8f;

    const float ExitDistance = 10f;   // 플레이어 뒤로 이만큼 지나가면 멈춘다 (화면 밖)

    Vector3 origin;
    PlayerController player;
    bool flying;
    bool done;
    float offset;   // 플레이어 기준 가로 거리

    void Awake()
    {
        origin = transform.localPosition;
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetState;
    }

    void ResetState()
    {
        flying = false;
        done = false;
        transform.localPosition = origin;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (done || run == null || run.Current != RunManager.State.Running || player == null || trap == null) return;

        var target = player.transform.position;
        if (!flying)
        {
            if (!trap.TimedOutAttack || !player.DoubleJumped || player.Grounded) return;
            flying = true;
            offset = spawnAhead;
            transform.position = new Vector3(target.x + offset, target.y, transform.position.z);
            return;
        }

        offset -= flySpeed * Time.deltaTime;
        float y = Mathf.MoveTowards(transform.position.y, target.y, followSpeed * Time.deltaTime);
        transform.position = new Vector3(target.x + offset, y, transform.position.z);
        if (offset < -ExitDistance) done = true;
    }
}
