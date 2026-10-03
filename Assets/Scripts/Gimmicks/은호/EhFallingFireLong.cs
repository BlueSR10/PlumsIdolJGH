using UnityEngine;

// 은호 전용: FireLong의 바닥을 축으로 왼쪽으로 한 번 넘어진다.
[DisallowMultipleComponent]
public sealed class EhFallingFireLong : MonoBehaviour
{
    [Tooltip("플레이어와 회전축의 가로 거리. 이 값 이하에서 넘어지기 시작")]
    [SerializeField, Min(0f)] float triggerDistance = 2f;
    [Tooltip("왼쪽으로 90도 넘어지는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0.1f)] float fallDuration = 0.4f;

    Transform player;
    Quaternion initialRotation;
    bool triggered;
    float triggeredAt;

    void Awake()
    {
        initialRotation = transform.localRotation;
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
        transform.localRotation = initialRotation;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running) return;
        if (!triggered)
        {
            if (player == null || Mathf.Abs(transform.position.x - player.position.x) > Mathf.Max(0f, triggerDistance)) return;
            triggered = true;
            triggeredAt = run.StageTime;
        }
        float progress = Mathf.Clamp01((run.StageTime - triggeredAt) / Mathf.Max(0.1f, fallDuration));
        // 2D의 +Z 회전은 위쪽 끝을 왼쪽으로 눕힌다.
        float angle = 90f * Mathf.SmoothStep(0f, 1f, progress);
        transform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, angle);
    }

    void OnValidate()
    {
        triggerDistance = Mathf.Max(0f, triggerDistance);
        fallDuration = Mathf.Max(0.1f, fallDuration);
    }
}
