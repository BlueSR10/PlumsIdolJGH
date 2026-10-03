using UnityEngine;
using UnityEngine.UI;

// 시작 화면. Start Game을 누르면:
//  - 튜토리얼을 아직 안 했고 튜토리얼 씬(Stage_Tutorial)이 있으면 바로 튜토리얼로 들어간다.
//  - 그 외에는 스테이지 목록 화면으로 간다 (튜토리얼 씬이 아직 없는 지금은 항상 목록으로 간다).
// 씬에는 이 컴포넌트만 두면 UI는 코드가 만든다.
public class TitleScreen : MonoBehaviour
{
    [Tooltip("메뉴 글꼴 (Assets/Fonts)")]
    [SerializeField] Font font;

    MenuUI.Controls controls;
    RectTransform button;
    Image buttonImage;
    Text buttonLabel;
    bool hover;
    bool loading;

    void Awake()
    {
        SoundManager.PlayBgm(null, 0f);   // 스테이지에서 돌아왔을 때 이어지는 배경음악을 끈다

        controls = new MenuUI.Controls();
        MenuUI.SetFont(font);
        var root = MenuUI.CreateRoot(transform);

        MenuUI.Label(root, "Title", "런쿠키", 200, MenuUI.Gold, new Vector2(0f, 0.55f), new Vector2(1f, 0.85f));
        MenuUI.Label(root, "Subtitle", "PLUM JAM", 56, MenuUI.Muted, new Vector2(0f, 0.47f), new Vector2(1f, 0.56f));

        button = MenuUI.Box(root, "StartButton", new Vector2(0.36f, 0.20f), new Vector2(0.64f, 0.32f), MenuUI.CardSelected);
        buttonImage = button.GetComponent<Image>();
        buttonLabel = MenuUI.Label(button, "Label", "게임 시작", 64, Color.white, Vector2.zero, Vector2.one);

        MenuUI.Label(root, "Hint", "Enter / Space / Click", 36, MenuUI.Muted, new Vector2(0f, 0.06f), new Vector2(1f, 0.13f));
    }

    void OnDestroy() => controls?.Dispose();

    void Update()
    {
        if (loading) return;

        hover = MenuUI.MouseOver(button);
        // 시작 버튼은 항상 선택된 상태라, 선택 표시는 마우스를 올리면 더 강조되는 정도로만 둔다
        float pulse = 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 4f);
        button.localScale = Vector3.one * (hover ? 1.08f : pulse);
        buttonImage.color = hover ? Color.Lerp(MenuUI.CardSelected, Color.white, 0.25f) : MenuUI.CardSelected;

        if (controls.Confirm || (hover && MenuUI.MouseClicked)) StartGame();
    }

    void StartGame()
    {
        loading = true;
        if (!Progress.TutorialDone && StageLoader.Exists(StageCatalog.TutorialScene)) StageLoader.Load(StageCatalog.TutorialScene);
        else StageLoader.Load(StageCatalog.StageSelectScene);
    }
}
