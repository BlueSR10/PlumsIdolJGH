using UnityEngine;

// 은호 전용: 왼쪽 끝을 고정한 채 불꽃과 충돌 범위를 오른쪽으로 확장한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Hazard), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class EhExtendingFire : MonoBehaviour
{
    [Tooltip("플레이어와 불꽃 왼쪽 끝의 가로 거리가 이 값 이하이면 확장 시작")]
    [SerializeField, Min(0f)] float triggerDistance = 2f;
    [Tooltip("오른쪽으로 추가할 길이 (유닛). 초기 폭은 Art Piece > Width로 설정")]
    [SerializeField, Min(0f)] float extraWidth = 3f;
    [Tooltip("확장 완료까지 걸리는 시간 (초)")]
    [SerializeField, Min(0.1f)] float extendDuration = 0.5f;

    [SerializeField] SpriteRenderer leftFlame;
    [SerializeField] SpriteRenderer rightFlame;

    SpriteRenderer sprite;
    BoxCollider2D hitbox;
    Transform player;
    Vector3 origin;
    Vector2 initialSize;
    Vector2 initialHitSize;
    Vector2 initialHitOffset;
    float pivotRatio;
    bool initialized;
    bool triggered;
    float triggeredAt;

    void Awake() => StageReset.Requested += ResetState;

    void Start()
    {
        // ArtPiece.Awake에서 초기 폭과 충돌 범위를 설정한 뒤 기록한다.
        sprite = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<BoxCollider2D>();
        origin = transform.localPosition;
        initialSize = sprite.size;
        // 두 자식 그림으로 표시하고 루트 그림은 폭 정보만 유지한다.
        sprite.drawMode = SpriteDrawMode.Sliced;
        sprite.forceRenderingOff = true;
        initialHitSize = hitbox.size;
        initialHitOffset = hitbox.offset;
        pivotRatio = sprite.sprite != null ? sprite.sprite.pivot.x / sprite.sprite.rect.width : 0.5f;
        var controller = FindFirstObjectByType<PlayerController>();
        if (controller != null) player = controller.transform;
        initialized = true;
        ApplyVisuals(initialSize.x);
    }

    void OnDestroy() => StageReset.Requested -= ResetState;

    void ResetState()
    {
        triggered = false;
        triggeredAt = 0f;
        if (!initialized) return;
        transform.localPosition = origin;
        sprite.size = initialSize;
        hitbox.size = initialHitSize;
        hitbox.offset = initialHitOffset;
        ApplyVisuals(initialSize.x);
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (!initialized || run == null || run.Current != RunManager.State.Running) return;
        if (!triggered)
        {
            if (player == null) return;
            Vector3 left = origin - Vector3.right * (initialSize.x * pivotRatio);
            if (transform.parent != null) left = transform.parent.TransformPoint(left);
            if (Mathf.Abs(left.x - player.position.x) > Mathf.Max(0f, triggerDistance)) return;
            triggered = true;
            triggeredAt = run.StageTime;
        }
        float progress = Mathf.Clamp01((run.StageTime - triggeredAt) / Mathf.Max(0.1f, extendDuration));
        float added = Mathf.Max(0f, extraWidth) * Mathf.SmoothStep(0f, 1f, progress);
        sprite.size = new Vector2(initialSize.x + added, initialSize.y);
        ApplyVisuals(sprite.size.x);
        transform.localPosition = origin + Vector3.right * (added * pivotRatio);
        hitbox.size = new Vector2(initialHitSize.x + added, initialHitSize.y);
        // 중앙이 아닌 피벗에서도 충돌 범위의 왼쪽 끝을 고정한다.
        hitbox.offset = initialHitOffset + Vector2.right * (added * (0.5f - pivotRatio));
    }

    void ApplyVisuals(float totalWidth)
    {
        float halfWidth = totalWidth * 0.5f;
        float left = -totalWidth * pivotRatio;
        SetFlame(leftFlame, left + halfWidth * pivotRatio, halfWidth);
        SetFlame(rightFlame, left + halfWidth + halfWidth * pivotRatio, halfWidth);
    }

    void SetFlame(SpriteRenderer flame, float x, float width)
    {
        if (flame == null) return;
        flame.drawMode = SpriteDrawMode.Sliced;
        flame.size = new Vector2(width, initialSize.y);
        flame.transform.localPosition = new Vector3(x, 0f, 0f);
    }

    void OnValidate()
    {
        triggerDistance = Mathf.Max(0f, triggerDistance);
        extraWidth = Mathf.Max(0f, extraWidth);
        extendDuration = Mathf.Max(0.1f, extendDuration);
    }
}
