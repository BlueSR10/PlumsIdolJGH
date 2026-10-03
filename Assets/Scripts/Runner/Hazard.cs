using UnityEngine;

// 닿으면 실패하는 장애물. 트리거 콜라이더와 함께 쓴다.
// destructible이면 거대화 중에 닿았을 때 부서지고(사망하지 않음), 스테이지가 재시작되면 복구된다.
// 파괴 불가 장애물(Block)과 벽(Gate)은 destructible을 끄고 쓴다.
public class Hazard : MonoBehaviour
{
    [SerializeField] bool destructible;

    Collider2D[] colliders;
    Renderer[] renderers;
    bool broken;

    public bool Destructible => destructible;

    // 부서질 때 발생한다 (부서지기 직전의 충돌 범위). 이펙트·소리가 구독한다.
    public static event System.Action<Bounds> Broken;

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

    public void Break()
    {
        if (broken) return;
        var bounds = colliders[0].bounds;
        foreach (var c in colliders) bounds.Encapsulate(c.bounds);
        Broken?.Invoke(bounds);
        SetBroken(true);
    }

    void Restore()
    {
        if (broken) SetBroken(false);
    }

    void SetBroken(bool value)
    {
        broken = value;
        foreach (var c in colliders) c.enabled = !value;
        foreach (var r in renderers) r.enabled = !value;
    }
}
