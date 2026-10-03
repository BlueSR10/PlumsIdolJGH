using UnityEngine;

// 스테이지 목표 지점. 닿으면 클리어. 트리거 콜라이더와 함께 쓴다.
public class Goal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        RunManager.Instance.Clear();
    }
}
