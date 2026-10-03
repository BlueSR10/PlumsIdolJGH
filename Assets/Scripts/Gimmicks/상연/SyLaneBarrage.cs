using UnityEngine;

// 화면 위 끝(천장)부터 floorY까지를 위에서부터 여러 줄로 나누고, 한 줄만 빨갛게 깜빡여 알려 준 뒤 나머지 줄 전부에 장애물(액체)을 쏟아 보내는 함정.
// 빈 오브젝트에 붙이고 맵에 놓는다. 플레이어가 이 오브젝트의 X를 지나면 시작한다 (빗자루 구간 중간에 둔다).
// 자식으로 빨간 띠(warning, SpriteRenderer)와 날아올 장애물(liquids, Hazard가 붙은 조각)을 둔다. 장애물은 안전한 줄을 뺀 줄 수만큼 필요하다.
// markedLaneIsDanger를 켜면 반대로 빨간 줄에만 장애물이 날아온다 (이때 장애물은 하나만 있으면 된다).
// 띠와 장애물은 화면 기준으로 움직이므로 놓은 위치는 상관없다. 재시작하면 처음 상태로 돌아간다.
public class SyLaneBarrage : MonoBehaviour
{
    enum Phase { Waiting, Warning, Flying, Done }

    [Tooltip("천장부터 Floor Y까지를 세로로 나누는 줄 수")]
    [SerializeField, Min(2)] int lanes = 4;
    [Tooltip("나누는 범위의 아래 끝 높이. 바닥 장애물 그림의 바로 위에 맞춘다 (크리스탈 윗면은 약 -0.13)")]
    [SerializeField] float floorY = -0.13f;
    [Tooltip("빨간 띠로 표시하는 줄 (위에서부터 1번). 기본은 이 줄만 안전하다")]
    [SerializeField, Min(1)] int safeLane = 2;
    [Tooltip("켜면 반대로 빨간 줄에만 장애물이 날아오고 나머지 줄이 안전하다")]
    [SerializeField] bool markedLaneIsDanger;
    [Tooltip("빨간 띠가 깜빡이는 시간 (초). 이 시간이 지나면 장애물이 날아온다")]
    [SerializeField, Min(0f)] float warnTime = 1.2f;
    [Tooltip("깜빡이는 간격 (초)")]
    [SerializeField, Min(0.02f)] float blinkInterval = 0.15f;
    [Tooltip("장애물이 한 줄의 높이를 채우는 비율. 1이면 줄을 꽉 채운다")]
    [SerializeField, Range(0.3f, 1f)] float fill = 0.55f;
    [Tooltip("장애물 한 줄기의 가로 길이 (유닛)")]
    [SerializeField, Min(0.5f)] float length = 3f;
    [Tooltip("날아오는 속도 (화면 기준 units/sec)")]
    [SerializeField, Min(1f)] float flySpeed = 14f;
    [SerializeField] SpriteRenderer warning;
    [SerializeField] Transform[] liquids;

    Camera cam;
    Transform player;
    Phase phase;
    float timer;
    float offset;       // 화면 중심 기준 장애물의 가로 위치
    float halfWidth;    // 화면 가로 절반
    float top;          // 화면 위 끝
    float laneHeight;

    void Awake()
    {
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        cam = Camera.main;
        var p = FindFirstObjectByType<PlayerController>();
        if (p != null) player = p.transform;

        ResetState();
    }

    // 화면 크기에 맞춰 줄 높이를 정하고 장애물을 그 높이와 길이로 늘린다.
    // Pixel Perfect Camera가 화면 크기를 조정한 뒤의 값을 쓰려고 시작할 때가 아니라 함정이 켜질 때 잰다.
    void Measure()
    {
        halfWidth = cam.orthographicSize * cam.aspect;
        top = cam.transform.position.y + cam.orthographicSize;
        laneHeight = Mathf.Max(0.1f, top - floorY) / lanes;

        foreach (var liquid in liquids)
        {
            var box = liquid.GetComponent<BoxCollider2D>();
            if (box == null) continue;
            liquid.localScale = new Vector3(length / box.size.x, laneHeight * fill / box.size.y, 1f);
        }
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetState;
    }

    void ResetState()
    {
        phase = Phase.Waiting;
        timer = 0f;
        if (warning != null) warning.enabled = false;
        foreach (var liquid in liquids) SetVisible(liquid, false);
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running || player == null || cam == null) return;

        float centerX = cam.transform.position.x;
        switch (phase)
        {
            case Phase.Waiting:
                if (player.position.x < transform.position.x) return;
                phase = Phase.Warning;
                timer = 0f;
                Measure();
                break;

            case Phase.Warning:
                timer += Time.deltaTime;
                PlaceWarning(centerX, Mathf.FloorToInt(timer / blinkInterval) % 2 == 0);
                if (timer < warnTime) return;

                phase = Phase.Flying;
                offset = halfWidth + length * 0.5f + 1f;
                int count = markedLaneIsDanger ? 1 : lanes - 1;
                for (int i = 0; i < liquids.Length && i < count; i++) SetVisible(liquids[i], true);
                PlaceLiquids(centerX);
                break;

            case Phase.Flying:
                PlaceWarning(centerX, true);
                offset -= flySpeed * Time.deltaTime;
                PlaceLiquids(centerX);
                if (offset > -(halfWidth + length)) return;

                phase = Phase.Done;
                if (warning != null) warning.enabled = false;
                foreach (var liquid in liquids) SetVisible(liquid, false);
                break;
        }
    }

    // 위에서부터 lane번째 줄의 가운데 높이
    float LaneCenter(int lane) => top - (lane - 0.5f) * laneHeight;

    void PlaceWarning(float centerX, bool visible)
    {
        if (warning == null) return;
        warning.enabled = visible;
        warning.transform.position = new Vector3(centerX, LaneCenter(safeLane), warning.transform.position.z);
        // 띠 그림 한 장을 화면 가로 × 한 줄 높이로 늘린다
        var size = warning.sprite != null ? (Vector2)warning.sprite.bounds.size : Vector2.one;
        warning.transform.localScale = new Vector3(halfWidth * 2f / size.x, laneHeight / size.y, 1f);
    }

    void PlaceLiquids(float centerX)
    {
        int index = 0;
        for (int lane = 1; lane <= lanes && index < liquids.Length; lane++)
        {
            if ((lane == safeLane) != markedLaneIsDanger) continue;
            var liquid = liquids[index++];
            liquid.position = new Vector3(centerX + offset, LaneCenter(lane), liquid.position.z);
        }
    }

    static void SetVisible(Transform target, bool visible)
    {
        foreach (var c in target.GetComponentsInChildren<Collider2D>()) c.enabled = visible;
        foreach (var r in target.GetComponentsInChildren<Renderer>()) r.enabled = visible;
    }
}
