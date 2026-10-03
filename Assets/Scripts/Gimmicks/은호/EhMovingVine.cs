using UnityEngine;

// 은호 전용 Vine. 플레이어 접근 시 한 번 상승하고 재시작 때 복귀한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Hazard), typeof(BoxCollider2D))]
public sealed class EhMovingVine : MonoBehaviour
{
    [Tooltip("플레이어와의 가로 거리가 이 값 이하이면 상승 시작 (유닛)")]
    [SerializeField, Min(0f)] float triggerDistance = 2f;
    [Tooltip("배치 위치에서 위로 올라갈 높이 (유닛)")]
    [SerializeField, Min(0f)] float riseHeight = 2.5f;
    [Tooltip("상승 완료까지 걸리는 시간 (초)")]
    [SerializeField, Min(0.1f)] float riseDuration = 0.75f;

    Vector3 origin;
    Transform player;
    bool initialized;
    bool triggered;
    float triggeredAt;

    void Awake()
    {
        origin = transform.localPosition;
        initialized = true;
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        var controller = FindFirstObjectByType<PlayerController>();
        if (controller != null) player = controller.transform;
    }

    void OnDestroy() => StageReset.Requested -= ResetState;

    void ResetState()
    {
        triggered = false;
        triggeredAt = 0f;
        transform.localPosition = origin;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running) return;

        if (!triggered)
        {
            if (player == null) return;
            float distance = Mathf.Abs(transform.position.x - player.position.x);
            if (distance > Mathf.Max(0f, triggerDistance)) return;
            triggered = true;
            triggeredAt = run.StageTime;
        }

        float progress = Mathf.Clamp01((run.StageTime - triggeredAt) / Mathf.Max(0.1f, riseDuration));
        progress = Mathf.SmoothStep(0f, 1f, progress);
        transform.localPosition = origin + Vector3.up * (Mathf.Max(0f, riseHeight) * progress);
    }

    void OnValidate()
    {
        triggerDistance = Mathf.Max(0f, triggerDistance);
        riseHeight = Mathf.Max(0f, riseHeight);
        riseDuration = Mathf.Max(0.1f, riseDuration);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying && initialized ? origin : transform.localPosition;
        Vector3 end = start + Vector3.up * Mathf.Max(0f, riseHeight);
        if (transform.parent != null)
        {
            start = transform.parent.TransformPoint(start);
            end = transform.parent.TransformPoint(end);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.12f);
        Gizmos.color = Color.cyan;
        Vector3 approach = start + Vector3.left * Mathf.Max(0f, triggerDistance);
        Gizmos.DrawLine(approach + Vector3.down, approach + Vector3.up);
    }
}
