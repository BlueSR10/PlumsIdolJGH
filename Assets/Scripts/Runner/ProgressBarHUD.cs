using UnityEngine;

// 화면 상단 막대바에 마녀·추격자·골 위치를 표시한다 (GDD 6.2). 임시 IMGUI 구현.
public class ProgressBarHUD : MonoBehaviour
{
    [SerializeField] Chaser chaser;

    GUIStyle label;

    void OnGUI()
    {
        var run = RunManager.Instance;
        if (run == null || run.HideHud) return;

        float w = Screen.width * 0.6f;
        float x0 = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.05f;
        float h = Mathf.Max(8f, Screen.height * 0.014f);
        float len = Mathf.Max(0.01f, run.GoalLocalX - run.StartLocalX);

        Fill(new Rect(x0, y, w, h), new Color(0f, 0f, 0f, 0.55f));
        Fill(new Rect(x0 + w - 3f, y - 8f, 6f, h + 16f), new Color(1f, 0.85f, 0.2f));

        float c = Mathf.Clamp01((chaser.LocalX - run.StartLocalX) / len);
        float p = Mathf.Clamp01((run.PlayerLocalX - run.StartLocalX) / len);
        float m = h + 10f;
        Fill(new Rect(x0 + w * c - m * 0.5f, y + h * 0.5f - m * 0.5f, m, m), new Color(0.95f, 0.25f, 0.25f));
        Fill(new Rect(x0 + w * p - m * 0.5f, y + h * 0.5f - m * 0.5f, m, m), new Color(0.35f, 0.65f, 1f));

        // 사망 문구는 GameOverHUD가 보여 준다
        if (run.Current != RunManager.State.Cleared) return;
        label ??= new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        label.normal.textColor = Color.white;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "CLEAR!", label);
    }

    static void Fill(Rect r, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
