using UnityEngine;

public enum FormType { Normal, Giant, Small }

// 변신 아이템 효과 (GDD 7.1): 일정 시간 거대화 또는 소형화.
// 거대화 중에는 파괴 가능 장애물에 닿아도 사망하지 않고 부순다 (PlayerController가 Giant를 본다).
[RequireComponent(typeof(PlayerController))]
public class PlayerForm : MonoBehaviour
{
    [SerializeField] float giantScale = 2f;
    [SerializeField] float smallScale = 0.4f;   // 슬라이드(높이 50%)보다 낮아야 소형화 전용 통로를 만들 수 있다

    PlayerController player;
    float remaining;
    GUIStyle style;

    public FormType Current { get; private set; }
    public bool Giant => Current == FormType.Giant;

    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Current == FormType.Normal) return;
        var run = RunManager.Instance;
        if (run != null && run.Current != RunManager.State.Running) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f) ResetState();
    }

    // 이미 변신 중이면 새 변신으로 바꾸고 시간을 다시 센다
    public void Apply(FormType type, float duration)
    {
        Current = type;
        remaining = duration;
        player.SetSizeScale(type == FormType.Giant ? giantScale : type == FormType.Small ? smallScale : 1f);
    }

    // 변신 종료. 재시작 시에도 호출된다.
    public void ResetState()
    {
        Current = FormType.Normal;
        remaining = 0f;
        player.SetSizeScale(1f);
    }

    // 튜닝용 임시 표시
    void OnGUI()
    {
        if (Current == FormType.Normal) return;
        style ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        style.normal.textColor = Current == FormType.Giant ? new Color(0.4f, 1f, 0.5f) : new Color(0.4f, 0.8f, 1f);
        GUI.Label(new Rect(16, 92, 400, 36), $"{Current.ToString().ToUpper()} {remaining:F1}s", style);
    }
}
