using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 엔딩 연출을 6스테이지를 깨지 않고 바로 보기 위한 메뉴.
public static class EndingTools
{
    [MenuItem("Plum/Ending/Play Ending (엔딩 바로 보기)")]
    static void PlayEnding()
    {
        // 이미 플레이 중이면 엔딩 씬만 불러온다
        if (EditorApplication.isPlaying)
        {
            StageLoader.Load(StageCatalog.EndingScene);
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var path = $"Assets/Scenes/{StageCatalog.EndingScene}.unity";
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("엔딩 씬을 찾을 수 없습니다: " + path);
            return;
        }
        EditorSceneManager.OpenScene(path);
        EditorApplication.isPlaying = true;
    }
}
