using UnityEngine;

// 스테이지에 놓는 아이템 (GDD 7장): 거대화, 소형화, 가속. 플레이어가 닿으면 효과가 적용되고 사라진다.
// 스테이지가 재시작되면 다시 나타난다. 트리거 콜라이더와 함께 쓴다.
public class ItemPickup : MonoBehaviour
{
    public enum Kind { Giant, Small, Boost }

    // 아이템을 먹은 순간 발생한다 (소리·이펙트용).
    public static event System.Action<Kind> Picked;

    [SerializeField] Kind kind;
    [SerializeField] float duration = 5f;       // 효과 지속 시간 (임시)
    [SerializeField] float boostAmount = 4f;    // 가속 아이템 전용: 스크롤 속도에 더해지는 값 (units/sec, 임시)

    Collider2D[] colliders;
    Renderer[] renderers;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<Renderer>();
        StageReset.Requested += Restore;
    }

    void OnDestroy()
    {
        StageReset.Requested -= Restore;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        switch (kind)
        {
            case Kind.Giant:
                player.GetComponent<PlayerForm>().Apply(FormType.Giant, duration);
                break;
            case Kind.Small:
                player.GetComponent<PlayerForm>().Apply(FormType.Small, duration);
                break;
            case Kind.Boost:
                FindFirstObjectByType<SpeedController>().Boost(boostAmount, duration);
                break;
        }

        SetVisible(false);
        Picked?.Invoke(kind);
    }

    void Restore() => SetVisible(true);

    void SetVisible(bool visible)
    {
        foreach (var c in colliders) c.enabled = visible;
        foreach (var r in renderers) r.enabled = visible;
    }
}
