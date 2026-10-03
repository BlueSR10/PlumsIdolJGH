using UnityEngine;

// 이번 시도에서 한 번이라도 감속하면 장애물이 길어진다. 폭을 바꿀 수 있는 조각(ArtPiece의 Repeat Width)에 붙인다.
// 뒤쪽 끝은 그대로 두고 앞쪽(플레이어가 오는 쪽)으로 extraWidth만큼 늘어나서, 감속 없이 겨우 넘던 구간을 못 넘게 된다.
// 늘어날 부분이 화면에 보이는 거리까지 와서 한 감속은 무시한다 (눈앞에서 갑자기 생기지 않게).
// 공중에서 누른 감속 키는 속도가 줄지 않으므로 세지 않는다. 재시작하면 원래 길이로 돌아간다.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class SyBrakePenalty : MonoBehaviour
{
    [Tooltip("감속했을 때 앞쪽으로 늘어나는 길이 (유닛). 그림 한 장 폭의 배수로 쓴다")]
    [SerializeField, Min(0f)] float extraWidth = 10f;

    const float ScreenMargin = 1f;   // 화면 오른쪽 끝에서 이만큼 더 떨어져 있어야 늘린다

    SpriteRenderer sr;
    BoxCollider2D box;
    SpeedController speed;
    Camera cam;
    Vector3 origin;
    Vector2 spriteSize;
    Vector2 boxSize;
    bool extended;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        StageReset.Requested += ResetState;
    }

    void Start()
    {
        // ArtPiece가 Awake에서 맞춘 크기를 기준으로 삼는다
        speed = FindFirstObjectByType<SpeedController>();
        cam = Camera.main;
        origin = transform.localPosition;
        spriteSize = sr.size;
        boxSize = box.size;
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetState;
    }

    void ResetState()
    {
        if (!extended) return;
        extended = false;
        sr.size = spriteSize;
        box.size = boxSize;
        transform.localPosition = origin;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (extended || speed == null || run == null || run.Current != RunManager.State.Running) return;
        if (!speed.Braking) return;

        // 늘어난 뒤의 앞쪽 끝이 화면 안(또는 바로 옆)이면 늘리지 않는다
        float newFront = box.bounds.min.x - extraWidth;
        float screenRight = cam != null ? cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f)).x : float.NegativeInfinity;
        if (newFront < screenRight + ScreenMargin) return;

        extended = true;
        sr.size = spriteSize + Vector2.right * extraWidth;
        box.size = boxSize + Vector2.right * extraWidth;
        transform.localPosition = origin + Vector3.left * (extraWidth * 0.5f);
    }
}
