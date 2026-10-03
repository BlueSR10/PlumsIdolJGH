using UnityEngine;
using UnityEngine.SceneManagement;

// 이름으로 스테이지 씬을 불러온다.
// 에디터에서는 Build Settings에 등록돼 있지 않아도 Assets/Scenes에서 찾아 불러온다. 그래서 New Stage로 만들거나 이름을 바꾼 씬을
// 팀원이 Build Settings(공용 파일이라 충돌이 난다)에 직접 넣지 않아도 "다음 스테이지"로 이어 달릴 수 있다.
// 빌드할 때는 StageBuildSync가 Stage_* 씬을 모두 등록한다.
public static class StageLoader
{
    // 불러올 수 있는 씬인가 (에디터에서는 Assets에 있으면, 빌드에서는 Build Settings에 있으면)
    public static bool Exists(string sceneName)
    {
#if UNITY_EDITOR
        if (FindScenePath(sceneName) != null) return true;
#endif
        return Application.CanStreamedLevelBeLoaded(sceneName);
    }

    public static void Load(string sceneName)
    {
#if UNITY_EDITOR
        var path = FindScenePath(sceneName);
        if (path != null && SceneUtility.GetBuildIndexByScenePath(path) < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(sceneName);
    }

#if UNITY_EDITOR
    // Assets 안에서 이름이 정확히 같은 씬 파일의 경로. 없으면 null
    static string FindScenePath(string sceneName)
    {
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Scene " + sceneName))
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return path;
        }
        return null;
    }
#endif
}
