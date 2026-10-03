using UnityEngine;

// 플레이어와 같은 방향(오른쪽)으로 플레이어보다 느리게 날아가는 장애물. Hazard와 함께 쓴다.
// 맵에 놓아 두었다가 플레이어와의 가로 거리가 triggerDistance 이하가 되면 날기 시작한다. 높이는 놓은 위치 그대로다.
// 화면에서는 천천히 다가오는 것처럼 보여서 뒤따라가야 할 것 같지만, 슬라이드 높이에 두면 밑으로 추월할 수 있다.
// 플레이어 뒤로 화면 밖까지 밀려나면 멈춘다. 재시작하면 처음 자리로 돌아간다.
public class SySlowBird : MonoBehaviour
{
    [Tooltip("스테이지에 대해 오른쪽으로 날아가는 속도 (units/sec). 플레이어 속도보다 느려야 따라잡힌다")]
    [SerializeField, Min(0f)] float flySpeed = 3.5f;
    [Tooltip("플레이어와의 가로 거리가 이 값 이하가 되면 날기 시작한다. 화면 오른쪽 끝은 플레이어에서 약 7")]
    [SerializeField, Min(0f)] float triggerDistance = 7f;

    const float ExitDistance = 10f;   // 플레이어 뒤로 이만큼 밀려나면 멈춘다 (화면 밖)

    Vector3 origin;
    Transform player;
    bool flying;
    bool done;

    void Awake()
    {
        origin = transform.localPosition;
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        var p = FindFirstObjectByType<PlayerController>();
        if (p != null) player = p.transform;
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
        if (done || run == null || run.Current != RunManager.State.Running || player == null) return;

        float ahead = transform.position.x - player.position.x;
        if (!flying)
        {
            if (ahead > triggerDistance) return;
            flying = true;
        }

        transform.localPosition += Vector3.right * (flySpeed * Time.deltaTime);
        if (ahead < -ExitDistance) done = true;
    }
}
