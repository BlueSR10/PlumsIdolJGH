using UnityEngine;

// 마녀 능력: 일정 시간 빗자루를 타고 비행 (스테이지당 1회, GDD 5장).
// 비행 중에는 해제할 수 없고, 점프 키=위 / 슬라이드 키=아래로 높이를 조절한다.
// 비행 중에는 공중에서도 속도 조절이 가능하다 (SpeedController가 PlayerController.Flying을 본다).
[RequireComponent(typeof(Rigidbody2D), typeof(RunnerInput), typeof(PlayerController))]
public class WitchFlight : MonoBehaviour
{
    [SerializeField] float duration = 5f;          // 비행 시간 (변경될 수 있음)
    [SerializeField] float verticalSpeed = 4.5f;   // 위/아래 이동 속도 (units/sec)
    [SerializeField] float maxHeight = 3f;         // 시작 높이 기준 비행 상한 (화면 밖으로 나가지 않게)
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Color flightColor = new Color(0.8f, 0.5f, 1f);

    PlayerController player;
    RunnerInput input;
    Rigidbody2D rb;
    Color baseColor;
    float startY;
    float remaining;
    bool used;
    GUIStyle style;

    public bool Active { get; private set; }

    void Awake()
    {
        player = GetComponent<PlayerController>();
        input = GetComponent<RunnerInput>();
        rb = GetComponent<Rigidbody2D>();
        startY = transform.position.y;
        baseColor = visual.color;
    }

    void Update()
    {
        var run = RunManager.Instance;
        if (run != null && run.Current != RunManager.State.Running) return;

        if (!Active && !used && input.AbilityPressed) Begin();
        if (!Active) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f) End();
    }

    void FixedUpdate()
    {
        if (!Active) return;

        float vy = 0f;
        if (input.JumpHeld) vy += verticalSpeed;
        if (input.SlideHeld) vy -= verticalSpeed;
        if (vy > 0f && rb.position.y >= startY + maxHeight) vy = 0f;
        rb.linearVelocity = new Vector2(0f, vy);
    }

    // 재시작 시 호출. 비행을 끝내고 능력을 다시 사용할 수 있게 한다.
    public void ResetState()
    {
        if (Active) End();
        used = false;
        remaining = 0f;
    }

    void Begin()
    {
        Active = true;
        used = true;
        remaining = duration;
        visual.color = flightColor;
        player.EnterFlight();
    }

    void End()
    {
        Active = false;
        visual.color = baseColor;
        player.ExitFlight();
    }

    // 튜닝용 임시 표시
    void OnGUI()
    {
        style ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        style.normal.textColor = Active ? flightColor : (used ? Color.gray : Color.white);
        string text = Active ? $"FLYING {remaining:F1}s" : (used ? "FLIGHT USED" : "FLIGHT READY [E]");
        GUI.Label(new Rect(16, 52, 400, 36), text, style);
    }
}
