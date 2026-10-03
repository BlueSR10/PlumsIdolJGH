using UnityEngine;

// 사망하면 화면 가운데에 "Game Over"와 "Space to retry"를 흰 글씨로 보여 준다 (임시 IMGUI). RunManager가 붙인다.
public class GameOverHUD : MonoBehaviour
{
    GUIStyle titleStyle;
    GUIStyle hintStyle;

    void OnGUI()
    {
        var run = RunManager.Instance;
        if (run == null || !run.CanRetry) return;

        float h = Screen.height;
        titleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        hintStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        titleStyle.fontSize = Mathf.RoundToInt(h * 0.12f);
        hintStyle.fontSize = Mathf.RoundToInt(h * 0.045f);

        var title = new Rect(0f, h * 0.30f, Screen.width, h * 0.16f);
        var hint = new Rect(0f, h * 0.47f, Screen.width, h * 0.08f);
        DrawLabel(title, "Game Over", titleStyle);
        DrawLabel(hint, "Space to retry", hintStyle);
    }

    // 밝은 배경에서도 읽히도록 검은 그림자 위에 흰 글씨를 겹친다
    static void DrawLabel(Rect rect, string text, GUIStyle style)
    {
        float o = Mathf.Max(2f, style.fontSize * 0.04f);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + o, rect.y + o, rect.width, rect.height), text, style);
        style.normal.textColor = Color.white;
        GUI.Label(rect, text, style);
    }
}
