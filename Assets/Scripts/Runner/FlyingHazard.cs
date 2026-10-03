using UnityEngine;

// 공중에서 날아오는 장애물 (GDD 4.2, 예: 새). 맵에 놓아 두었다가 플레이어와의 가로 거리가
// triggerDistance 이하가 되면 왼쪽으로 날기 시작한다. 거리 기준이라 플레이어 속도에 따라 도착 시점이 달라지고,
// 높이는 놓은 위치 그대로다 (비행 중인 마녀와 같은 높이에 두면 마주친다). 재시작하면 처음 자리로 돌아간다.
// Hazard와 함께 쓴다 (destructible이면 거대화로 부술 수 있다).
public class FlyingHazard : MonoBehaviour
{
    [Tooltip("스테이지에 대해 왼쪽으로 날아오는 속도 (units/sec). 화면 스크롤 속도는 따로 더해진다")]
    [SerializeField, Min(0f)] float flySpeed = 3f;
    [Tooltip("플레이어와의 가로 거리가 이 값 이하가 되면 날기 시작한다. 화면 오른쪽 끝은 플레이어에서 약 7")]
    [SerializeField, Min(0f)] float triggerDistance = 10f;

    Vector3 origin;
    Transform player;
    ProjectileSound sound;
    bool flying;

    void Awake()
    {
        origin = transform.localPosition;
        // 날아오는 소리(Wind + 도플러). 프리팹에 따로 붙이지 않아도 새 변형들이 모두 같은 소리를 쓴다
        sound = GetComponent<ProjectileSound>();
        if (sound == null) sound = gameObject.AddComponent<ProjectileSound>();
        sound.DetectMovement = false;   // 날기 시작하는 시점을 아니까 Begin()/End()로 직접 제어
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
        transform.localPosition = origin;
        sound.End();
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running || player == null) return;

        if (!flying)
        {
            if (transform.position.x - player.position.x > triggerDistance) return;
            flying = true;
            sound.Begin();
        }

        transform.localPosition += Vector3.left * (flySpeed * Time.deltaTime);
    }

    // Scene 뷰: 선택했을 때 날아갈 경로(노랑)를 보여 준다
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var start = transform.position;
        Gizmos.DrawLine(start, start + Vector3.left * 14f);
        Gizmos.DrawWireSphere(start + Vector3.left * 14f, 0.15f);
    }
}
