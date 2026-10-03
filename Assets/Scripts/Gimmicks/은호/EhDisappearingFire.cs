using UnityEngine;

// 은호 전용: 플레이어가 접근하면 Fire의 그림과 충돌을 끈다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Hazard), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class EhDisappearingFire : MonoBehaviour
{
    [Tooltip("Fire 중심과 플레이어의 가로 거리. 이 값 이하에서 사라짐")]
    [SerializeField, Min(0f)] float triggerDistance = 3f;

    Transform player;
    SpriteRenderer fireRenderer;
    BoxCollider2D fireCollider;
    bool disappeared;

    void Awake()
    {
        fireRenderer = GetComponent<SpriteRenderer>();
        fireCollider = GetComponent<BoxCollider2D>();
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
        disappeared = false;
        fireRenderer.enabled = true;
        fireCollider.enabled = true;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running || disappeared || player == null) return;
        if (Mathf.Abs(transform.position.x - player.position.x) > Mathf.Max(0f, triggerDistance)) return;
        disappeared = true;
        // 접근 소멸은 파괴가 아니므로 Hazard.Break의 파괴 이펙트는 발생시키지 않는다.
        fireRenderer.enabled = false;
        fireCollider.enabled = false;
    }

    void OnValidate() => triggerDistance = Mathf.Max(0f, triggerDistance);
}
