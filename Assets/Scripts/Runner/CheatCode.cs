using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 치트코드: 0 키(숫자 패드 0 포함)를 15초 이상 누르고 있으면 엔딩 연출로 넘어간다.
// 게임 시작 때 자동으로 만들어져 어느 씬에서든(일시정지 중에도) 동작한다. 엔딩 씬 안에서는 동작하지 않는다.
public class CheatCode : MonoBehaviour
{
    const float HoldSeconds = 15f;

    float held;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("CheatCode");
        DontDestroyOnLoad(go);
        go.AddComponent<CheatCode>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !(kb.digit0Key.isPressed || kb.numpad0Key.isPressed))
        {
            held = 0f;
            return;
        }

        held += Time.unscaledDeltaTime;   // 일시정지(timeScale 0) 중에도 센다
        if (held < HoldSeconds) return;

        held = 0f;
        if (SceneManager.GetActiveScene().name == StageCatalog.EndingScene) return;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        StageLoader.Load(StageCatalog.EndingScene);
    }
}
