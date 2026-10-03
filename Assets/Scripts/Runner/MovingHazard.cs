using UnityEngine;

// 위아래로 왕복하는 장애물. 스테이지의 자식으로 두면 맵과 함께 흐른다.
// 시도마다 같은 타이밍이 되도록 RunManager의 스테이지 시간(체크포인트 부활 시 복원)을 기준으로 움직인다.
public class MovingHazard : MonoBehaviour
{
    [SerializeField] float amplitude = 1.1f;   // 위아래 이동 폭 (유닛)
    [SerializeField] float period = 3f;        // 왕복 주기 (초)
    [SerializeField, Range(0f, 1f)] float phase;

    Vector3 origin;

    void Awake()
    {
        origin = transform.localPosition;
    }

    void Update()
    {
        var run = RunManager.Instance;
        float t = run != null ? run.StageTime : Time.timeSinceLevelLoad;
        float s = Mathf.Sin((t / period + phase) * Mathf.PI * 2f);
        transform.localPosition = origin + Vector3.up * (s * amplitude);
    }
}
