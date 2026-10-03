using UnityEngine;

// 은호 전용: 가까이 오면 낮은 불꽃이 키 큰 불꽃으로 변한다.
[DisallowMultipleComponent]
public sealed class EhFireToLong : MonoBehaviour
{
    [SerializeField] GameObject fire;
    [SerializeField] GameObject fireLong;
    [Tooltip("초기 Fire 중심과 플레이어의 가로 거리 (유닛)")]
    [SerializeField, Min(0f)] float triggerDistance = 2f;

    Transform player;
    SpriteRenderer fireRenderer;
    Collider2D fireCollider;
    bool transformed;

    void Awake()
    {
        if (fire != null)
        {
            fireRenderer = fire.GetComponent<SpriteRenderer>();
            fireCollider = fire.GetComponent<Collider2D>();
        }
        StageReset.Requested += ResetState;
        ResetState();
    }

    void Start()
    {
        var controller = FindFirstObjectByType<PlayerController>();
        if (controller != null) player = controller.transform;
    }

    void OnDestroy() => StageReset.Requested -= ResetState;

    void ResetState()
    {
        transformed = false;
        if (fireLong != null) fireLong.SetActive(false);
        if (fireRenderer != null) fireRenderer.enabled = true;
        if (fireCollider != null) fireCollider.enabled = true;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running || transformed || player == null) return;
        if (fire == null || fireLong == null || fireCollider == null || fireRenderer == null) return;
        // 거대화로 이미 부서진 Fire는 변신하지 않는다.
        if (!fireCollider.enabled || !fireRenderer.enabled) return;
        if (Mathf.Abs(fire.transform.position.x - player.position.x) > Mathf.Max(0f, triggerDistance)) return;

        transformed = true;
        fireRenderer.enabled = false;
        fireCollider.enabled = false;
        fireLong.SetActive(true);
    }

    void OnValidate() => triggerDistance = Mathf.Max(0f, triggerDistance);
}
