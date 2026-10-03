using UnityEngine;

// 스테이지별 속도 설정. 맵 프리팹 루트에 하나 두고, 맵 디자이너는 여기 값만 조정한다.
// 씬이 시작되면 StageRig의 SpeedController/Chaser에 적용된다.
public class StageSettings : MonoBehaviour
{
    public const float PlayerStartX = -2f;   // StageRig의 Player 시작 X (맵 로컬 좌표와 같다)

    [Header("플레이어 속도")]
    public float startSpeed = 4f;            // units/sec
    public float acceleration = 0.1f;        // 기준 속도 증가량 (units/sec²)

    [Header("추격자")]
    public float chaserSpeed = 4.5f;         // units/sec
    public float chaserStartGap = 8f;        // 시작 시 플레이어와의 거리

    // 목표 플레이타임 (GDD: 스테이지당 30초 정도)
    public const float TargetSeconds = 30f;

    // 감속 없이 거리 d를 달리는 데 걸리는 시간
    public float EstimatedSeconds(float distance)
    {
        if (acceleration <= 0f) return distance / startSpeed;
        return (-startSpeed + Mathf.Sqrt(startSpeed * startSpeed + 2f * acceleration * distance)) / acceleration;
    }

    void Awake()
    {
        var speed = FindFirstObjectByType<SpeedController>();
        if (speed != null) speed.Configure(startSpeed, acceleration);

        var chaser = FindFirstObjectByType<Chaser>();
        if (chaser != null) chaser.Configure(chaserSpeed, chaserStartGap);
    }

    // Scene 뷰 보조선: 시작 위치(초록)와 시작 화면 범위(회색, 640x360 기준 가로 10 x 세로 5.625)
    void OnDrawGizmos()
    {
        var start = transform.TransformPoint(new Vector3(PlayerStartX, 0f, 0f));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start + Vector3.down * 4f, start + Vector3.up * 6f);

        var center = transform.TransformPoint(new Vector3(0f, 0.703125f, 0f));
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireCube(center, new Vector3(10f, 5.625f, 0f));
    }
}
