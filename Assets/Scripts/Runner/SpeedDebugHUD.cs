using UnityEngine;

// 튜닝용 속도 표시. 최종 UI가 아니다.
public class SpeedDebugHUD : MonoBehaviour
{
    [SerializeField] SpeedController speed;

    GUIStyle style;

    void OnGUI()
    {
        if (RunManager.Instance != null && RunManager.Instance.HideHud) return;

        style ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;
        string boost = speed.BoostBonus > 0f ? $" (+{speed.BoostBonus:F0} BOOST)" : "";
        GUI.Label(new Rect(16, 12, 700, 40), $"Speed {speed.Speed:F2}{boost}   t {Time.timeSinceLevelLoad:F1}s", style);
    }
}
