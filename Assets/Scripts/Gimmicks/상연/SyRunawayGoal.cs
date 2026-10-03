using UnityEngine;

// 닿기 직전에 한 번 뒤로 물러나는 Goal. Goal 조각에 붙인다.
// 플레이어와의 가로 거리가 triggerDistance 이하가 되면 fleeDistance만큼 오른쪽으로 옮겨 가고, 그 뒤로는 가만히 있는다.
// 물러난 자리 뒤에도 바닥이 있어야 한다. 재시작하면 처음 자리로 돌아간다.
public class SyRunawayGoal : MonoBehaviour
{
    [Tooltip("플레이어 앞끝과 Goal 앞끝의 가로 거리가 이 값 이하가 되면 물러난다")]
    [SerializeField, Min(0f)] float triggerDistance = 3f;
    [Tooltip("물러나는 거리")]
    [SerializeField, Min(0f)] float fleeDistance = 6f;
    [Tooltip("물러나는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0.01f)] float duration = 0.35f;

    Vector3 origin;
    Collider2D box;
    Collider2D playerBox;
    float timer;
    bool triggered;

    void Awake()
    {
        origin = transform.localPosition;
        box = GetComponent<Collider2D>();
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) playerBox = player.GetComponent<Collider2D>();
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetState;
    }

    void ResetState()
    {
        triggered = false;
        timer = 0f;
        transform.localPosition = origin;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running || playerBox == null) return;

        if (!triggered)
        {
            if (box.bounds.min.x - playerBox.bounds.max.x > triggerDistance) return;
            triggered = true;
        }

        if (timer >= duration) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        t = 1f - (1f - t) * (1f - t);   // 빠르게 출발해서 부드럽게 멈춘다
        transform.localPosition = origin + Vector3.right * (fleeDistance * t);
    }
}
