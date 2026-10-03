using UnityEngine;

// 지나가면 사망 시 이 지점에서 다시 시작한다. 트리거 콜라이더와 함께 쓴다.
public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        var run = RunManager.Instance;
        run.ReachCheckpoint(run.ToLocalX(transform.position.x));
    }
}
