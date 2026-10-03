using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 아래에 뜨는 튜토리얼 대화창 (uGUI를 코드로 만든다). 글자가 한 글자씩 나오고, Space(Enter, 게임패드 A)를 누르면
// 한 번에 전부 보이고, 한 번 더 누르면 닫힌다. 시간이 멈춘 상태(timeScale 0)에서도 돌도록 unscaled 시간을 쓴다.
public class DialogBox : MonoBehaviour
{
    public float charsPerSecond = 28f;

    GameObject root;
    Text body;
    Text hint;
    MenuUI.Controls controls;
    string full = "";
    bool pressedInDialog;

    void Awake()
    {
        controls = new MenuUI.Controls();
        var canvas = MenuUI.CreateCanvas(transform, 100);   // 게임 HUD 위에

        root = new GameObject("Dialog", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // 금색 테두리 + 어두운 바탕. 마녀가 서 있는 화면 아래쪽을 가리지 않게 위쪽에 둔다
        var frame = MenuUI.Box(root.transform, "Frame", new Vector2(0.10f, 0.72f), new Vector2(0.90f, 0.94f), MenuUI.Gold);
        var panel = MenuUI.Box(frame, "Panel", Vector2.zero, Vector2.one, new Color(0.10f, 0.05f, 0.16f, 0.94f));
        panel.offsetMin = new Vector2(6f, 6f);
        panel.offsetMax = new Vector2(-6f, -6f);

        body = MenuUI.Label(panel, "Text", "", 50, Color.white, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.92f), TextAnchor.MiddleLeft);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        hint = MenuUI.Label(panel, "Hint", "Space", 30, MenuUI.Muted, new Vector2(0.80f, 0f), new Vector2(0.98f, 0.22f), TextAnchor.MiddleRight);
        hint.enabled = false;

        root.SetActive(false);
    }

    void OnDestroy() => controls?.Dispose();

    // 대화를 보여 주고 닫힐 때까지 기다린다 (코루틴). 호출한 쪽이 시간 정지·입력 잠금을 맡는다.
    public IEnumerator Run(string text)
    {
        full = text;
        body.text = "";
        hint.enabled = false;
        root.SetActive(true);
        yield return null;   // 이 대화를 연 입력이 바로 넘기기로 읽히지 않게 한 프레임 쉰다

        float shown = 0f;
        int count = 0;
        while (count < full.Length)
        {
            if (controls.Confirm) { count = full.Length; break; }   // 한 번에 전부 표시
            shown += charsPerSecond * Time.unscaledDeltaTime;
            count = Mathf.Min(full.Length, Mathf.FloorToInt(shown));
            SetShown(count);
            yield return null;
        }
        SetShown(full.Length);

        yield return null;   // 전부 보이게 한 같은 프레임의 입력이 닫기로 읽히지 않게
        hint.enabled = true;
        while (!controls.Confirm)
        {
            hint.color = new Color(MenuUI.Muted.r, MenuUI.Muted.g, MenuUI.Muted.b, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
            yield return null;
        }

        root.SetActive(false);
    }

    // 아직 안 나온 글자는 투명하게 두어 줄바꿈 위치가 글자가 나오는 도중에 바뀌지 않게 한다
    void SetShown(int count)
    {
        body.text = count >= full.Length
            ? full
            : full.Substring(0, count) + "<color=#00000000>" + full.Substring(count) + "</color>";
    }
}
