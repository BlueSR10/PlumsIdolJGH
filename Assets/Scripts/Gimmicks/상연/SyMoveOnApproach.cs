using UnityEngine;

// 플레이어가 다가오면 한 번 움직이는 장애물(또는 장애물 묶음). 조각 하나에 직접 붙이거나, 빈 오브젝트 아래에 조각 여러 개를 넣고 그 부모에 붙인다.
// 놓은 자리를 기준으로 fromOffset에서 기다리다가, 플레이어와의 가로 거리가 triggerDistance 이하가 되면 toOffset으로 옮겨 간다.
// 예) 위로 떠오르는 가시: from (0,0), to (0,1.6). 땅에서 솟는 가시: from (0,-2.2), to (0,0), hiddenBefore 켬.
// 재시작하면 fromOffset 자리로 돌아간다.
public class SyMoveOnApproach : MonoBehaviour
{
    [Tooltip("움직이기 전 위치 (놓은 자리 기준)")]
    [SerializeField] Vector2 fromOffset;
    [Tooltip("움직인 뒤 위치 (놓은 자리 기준)")]
    [SerializeField] Vector2 toOffset = new Vector2(0f, 1.6f);
    [Tooltip("플레이어 앞끝과 이 장애물 앞끝의 가로 거리가 이 값 이하가 되면 움직인다")]
    [SerializeField, Min(0f)] float triggerDistance = 2f;
    [Tooltip("움직이는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0.01f)] float duration = 0.2f;
    [Tooltip("켜면 움직이기 전에는 보이지 않고 닿지도 않는다 (땅속에 숨겨 둘 때)")]
    [SerializeField] bool hiddenBefore;

    Vector3 origin;
    Collider2D[] colliders;
    Renderer[] renderers;
    Collider2D playerBox;
    float frontOffset;   // 이 오브젝트 위치에서 충돌 상자 앞끝(왼쪽)까지의 가로 거리
    float timer;
    bool triggered;

    void Awake()
    {
        origin = transform.localPosition;
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<Renderer>();
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) playerBox = player.GetComponent<Collider2D>();

        // 조각들의 ArtPiece가 Awake에서 충돌 상자를 맞춘 뒤에 앞끝을 잰다
        float front = float.PositiveInfinity;
        foreach (var c in colliders) front = Mathf.Min(front, c.bounds.min.x);
        frontOffset = colliders.Length > 0 ? front - transform.position.x : 0f;

        ResetState();
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetState;
    }

    void ResetState()
    {
        triggered = false;
        timer = 0f;
        transform.localPosition = origin + (Vector3)fromOffset;
        if (hiddenBefore) SetVisible(false);
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running || playerBox == null) return;

        if (!triggered)
        {
            float distance = transform.position.x + frontOffset - playerBox.bounds.max.x;
            if (distance > triggerDistance) return;
            triggered = true;
            if (hiddenBefore) SetVisible(true);
        }

        if (timer >= duration) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        t = 1f - (1f - t) * (1f - t);   // 빠르게 출발해서 부드럽게 멈춘다
        transform.localPosition = origin + (Vector3)Vector2.Lerp(fromOffset, toOffset, t);
    }

    void SetVisible(bool visible)
    {
        foreach (var c in colliders) c.enabled = visible;
        foreach (var r in renderers) r.enabled = visible;
    }
}
