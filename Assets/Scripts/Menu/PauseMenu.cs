using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 일시정지 메뉴. Esc(게임패드 Start)로 열고 닫는다. RunManager가 붙인다(씬에 따로 둘 필요 없음).
// 달리는 중에만 열린다(튜토리얼 대화·시작 연출·사망·클리어 중에는 안 열림). 열면 시간(timeScale)과 효과음이 멈추고 배경음악은 이어진다.
// 항목: 계속하기 / 다시 시작 / 설정(볼륨) / 시작 화면으로. 스테이지 선택 화면은 시작 화면의 게임 시작으로만 간다.
public class PauseMenu : MonoBehaviour
{
    RunManager run;
    InputAction toggle;
    MenuUI.Controls controls;
    GameObject canvasRoot;
    MenuPage main, settings, current;
    bool open;
    float prevTimeScale = 1f;

    void Awake()
    {
        run = GetComponent<RunManager>();
        toggle = new InputAction("Pause", InputActionType.Button);
        toggle.AddBinding("<Keyboard>/escape");
        toggle.AddBinding("<Gamepad>/start");
        toggle.Enable();
        controls = new MenuUI.Controls();

        var canvas = MenuUI.CreateCanvas(transform, 200);
        canvasRoot = canvas.gameObject;
        MenuUI.Box(canvas.transform, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.6f));

        main = new MenuPage(canvas.transform, "일시정지", new List<MenuPage.Item>
        {
            new MenuPage.Item { label = "계속하기", onConfirm = Close },
            new MenuPage.Item { label = "다시 시작", onConfirm = Restart },
            new MenuPage.Item { label = "설정", onConfirm = () => Show(settings) },
            new MenuPage.Item { label = "시작 화면으로", onConfirm = () => Leave(StageCatalog.TitleScene) },
        }, new Vector2(0.30f, 0.14f), new Vector2(0.70f, 0.86f));
        settings = MenuPage.Settings(canvas.transform, () => Show(main), new Vector2(0.30f, 0.22f), new Vector2(0.70f, 0.78f));
        canvasRoot.SetActive(false);
    }

    void OnDestroy()
    {
        toggle?.Disable();
        controls?.Dispose();
        if (open) { Time.timeScale = prevTimeScale; AudioListener.pause = false; }
    }

    void Update()
    {
        if (!open)
        {
            if (toggle.WasPressedThisFrame() && run.Current == RunManager.State.Running && !run.Holding && !run.Transitioning) Open();
            return;
        }

        if (controls.Back)
        {
            if (current == settings) Show(main);
            else Close();
            return;
        }
        current.Tick(controls);
    }

    void Show(MenuPage page)
    {
        main.SetActive(page == main);
        settings.SetActive(page == settings);
        current = page;
    }

    void Open()
    {
        open = true;
        run.Holding = true;   // 입력 잠금·스테이지 시간·추격자 정지
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        canvasRoot.SetActive(true);
        Show(main);
    }

    void Close()
    {
        open = false;
        canvasRoot.SetActive(false);
        Time.timeScale = prevTimeScale;
        AudioListener.pause = false;
        StartCoroutine(ReleaseNextFrame());
    }

    // 메뉴를 누른 Space가 같은 프레임에 점프로 읽히지 않도록 조작 잠금은 한 프레임 뒤에 푼다
    IEnumerator ReleaseNextFrame()
    {
        yield return null;
        run.Holding = false;
    }

    void Restart()
    {
        Close();
        run.RestartStage();
    }

    void Leave(string scene)
    {
        open = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        StageLoader.Load(scene);
    }
}
