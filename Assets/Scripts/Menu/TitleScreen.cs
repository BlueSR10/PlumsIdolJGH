using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 시작 화면. 게임 시작을 누르면:
//  - 튜토리얼을 아직 안 했고 튜토리얼 씬(Stage_Tutorial)이 있으면 바로 튜토리얼로 들어간다.
//  - 그 외에는 스테이지 목록 화면으로 간다.
// 설정(볼륨)도 여기서 열 수 있다. 씬에는 이 컴포넌트만 두면 UI는 코드가 만든다.
public class TitleScreen : MonoBehaviour
{
    [Tooltip("메뉴 글꼴을 따로 지정할 때만 (비우면 Assets/Resources/MenuStyle.asset)")]
    [SerializeField] Font font;

    MenuUI.Controls controls;
    MenuPage main, settings, current;
    readonly List<GameObject> titleTexts = new List<GameObject>();
    bool loading;

    void Awake()
    {
        SoundManager.PlayBgm(null, 0f);   // 스테이지에서 돌아왔을 때 이어지는 배경음악을 끈다

        controls = new MenuUI.Controls();
        MenuUI.SetFont(font);
        var root = MenuUI.CreateRoot(transform);

        titleTexts.Add(MenuUI.Label(root, "Title", "런쿠키", 200, MenuUI.Gold, new Vector2(0f, 0.55f), new Vector2(1f, 0.85f)).gameObject);
        titleTexts.Add(MenuUI.Label(root, "Subtitle", "PLUM JAM", 56, MenuUI.Muted, new Vector2(0f, 0.47f), new Vector2(1f, 0.56f)).gameObject);
        titleTexts.Add(MenuUI.Label(root, "Hint", "Enter / Space / Click", 36, MenuUI.Muted, new Vector2(0f, 0.03f), new Vector2(1f, 0.09f)).gameObject);

        main = new MenuPage(root, null, new List<MenuPage.Item>
        {
            new MenuPage.Item { label = "게임 시작", onConfirm = StartGame },
            new MenuPage.Item { label = "설정", onConfirm = () => Show(settings) },
        }, new Vector2(0.35f, 0.11f), new Vector2(0.65f, 0.41f), panel: false, fontSize: 52);
        settings = MenuPage.Settings(root, () => Show(main), new Vector2(0.30f, 0.20f), new Vector2(0.70f, 0.80f));
        Show(main);
    }

    void OnDestroy() => controls?.Dispose();

    void Show(MenuPage page)
    {
        main.SetActive(page == main);
        settings.SetActive(page == settings);
        current = page;
        foreach (var t in titleTexts) t.SetActive(page == main);   // 설정 중에는 제목을 가린다
    }

    void Update()
    {
        if (loading) return;
#if UNITY_EDITOR
        // 에디터 전용 단축키: F9 = 엔딩 바로 보기 (빌드에는 들어가지 않는다)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f9Key.wasPressedThisFrame) { loading = true; StageLoader.Load(StageCatalog.EndingScene); return; }
#endif
        if (current == settings && controls.Back) { Show(main); return; }
        current.Tick(controls);
    }

    void StartGame()
    {
        loading = true;
        if (!Progress.TutorialDone && StageLoader.Exists(StageCatalog.TutorialScene)) StageLoader.Load(StageCatalog.TutorialScene);
        else StageLoader.Load(StageCatalog.StageSelectScene);
    }
}
