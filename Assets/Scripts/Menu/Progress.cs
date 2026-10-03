using UnityEngine;

// 스테이지 목록(본 스테이지 6개: 집 밖 2 / 집 2 / 오븐 2)과 씬 이름. 튜토리얼은 6개에 포함되지 않는다.
// 팀원이 만든 실제 씬 이름이 정해지면 Scenes만 고치면 된다. 씬이 아직 없으면 목록에서 "COMING SOON"으로 보이고 들어가지 않는다.
public static class StageCatalog
{
    public const string TitleScene = "Title";
    public const string StageSelectScene = "StageSelect";
    public const string TutorialScene = "Stage_Tutorial";

    public const int StagesPerWorld = 2;
    public static readonly string[] Worlds = { "OUTSIDE", "HOUSE", "OVEN" };

    // 클리어해야 하는 순서(앞에서부터 차례로 열린다)
    public static readonly string[] Scenes =
    {
        "Stage_1_1", "Stage_1_2",
        "Stage_2_1", "Stage_2_2",
        "Stage_3_1", "Stage_3_2",
    };

    public static int Count => Scenes.Length;

    public static int IndexOf(string sceneName) => System.Array.IndexOf(Scenes, sceneName);

    public static string Label(int index) => $"{index / StagesPerWorld + 1}-{index % StagesPerWorld + 1}";
}

// 진행 저장(PlayerPrefs). 앞 스테이지를 순서대로 깨야 다음 스테이지가 열린다.
public static class Progress
{
    const string ClearedKey = "plum.clearedStages";
    const string TutorialKey = "plum.tutorialDone";

    // 클리어한 스테이지 수(= 순서상 첫 번째 못 깬 스테이지의 번호)
    public static int ClearedCount => Mathf.Clamp(PlayerPrefs.GetInt(ClearedKey, 0), 0, StageCatalog.Count);

    public static bool TutorialDone => PlayerPrefs.GetInt(TutorialKey, 0) != 0;

    public static bool IsCleared(int index) => index < ClearedCount;

    public static bool IsUnlocked(int index) => index <= ClearedCount;

    // RunManager가 클리어할 때 부른다. 목록에 없는 씬(테스트 맵 등)은 무시한다.
    public static void MarkCleared(string sceneName)
    {
        if (sceneName == StageCatalog.TutorialScene)
        {
            PlayerPrefs.SetInt(TutorialKey, 1);
            PlayerPrefs.Save();
            return;
        }

        int index = StageCatalog.IndexOf(sceneName);
        if (index < 0 || index < ClearedCount) return;
        PlayerPrefs.SetInt(ClearedKey, index + 1);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(ClearedKey);
        PlayerPrefs.DeleteKey(TutorialKey);
        PlayerPrefs.Save();
    }

    public static void UnlockAll(bool tutorialDone = true)
    {
        PlayerPrefs.SetInt(ClearedKey, StageCatalog.Count);
        PlayerPrefs.SetInt(TutorialKey, tutorialDone ? 1 : 0);
        PlayerPrefs.Save();
    }
}
