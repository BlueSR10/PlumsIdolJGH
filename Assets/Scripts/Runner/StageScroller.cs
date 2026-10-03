using UnityEngine;

// 스테이지 루트를 왼쪽으로 이동시킨다. 플레이어는 화면에 고정되고 맵이 흐른다.
[RequireComponent(typeof(Rigidbody2D))]
public class StageScroller : MonoBehaviour
{
    [SerializeField] SpeedController speed;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + Vector2.left * (speed.Speed * Time.fixedDeltaTime));
    }
}
