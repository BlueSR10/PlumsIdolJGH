using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// 스테이지 씬 목록과 빌드 등록.
// 에디터에서는 StageLoader가 Build Settings 없이 씬을 불러오므로 팀원이 씬을 등록할 필요가 없다 (Build Settings는 공용 파일이라
// 여러 명이 고치면 충돌이 난다). 대신 빌드할 때 Assets/Scenes의 Stage_* 씬을 전부 Build Settings에 넣는다.
// 빌드는 프로그래머가 하므로 이때 바뀌는 ProjectSettings/EditorBuildSettings.asset은 프로그래머가 커밋한다.
public class StageBuildSync : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    // Build Settings에 있는 씬 이름(활성)과 Assets/Scenes의 Stage_* 씬 이름을 합친 목록. 순서는 Build Settings 먼저, 나머지는 이름순
    public static List<string> StageSceneNames()
    {
        var names = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            if (scene.enabled && !names.Contains(name)) names.Add(name);
        }

        var extra = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Scene Stage_", new[] { "Assets/Scenes" }))
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
            if (name.StartsWith("Stage_") && !names.Contains(name) && !extra.Contains(name)) extra.Add(name);
        }
        extra.Sort(System.StringComparer.Ordinal);
        names.AddRange(extra);
        return names;
    }

    public void OnPreprocessBuild(BuildReport report) => RegisterAll();

    [MenuItem("Plum/Register Stage Scenes in Build Settings")]
    public static void RegisterAll()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var have = new HashSet<string>();
        foreach (var s in scenes) have.Add(s.path);

        int added = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Scene Stage_", new[] { "Assets/Scenes" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!System.IO.Path.GetFileNameWithoutExtension(path).StartsWith("Stage_") || have.Contains(path)) continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            added++;
        }

        // 시작 화면(첫 씬)과 스테이지 선택 화면은 맨 앞에 둔다
        int menu = 0;
        foreach (var name in new[] { StageCatalog.StageSelectScene, StageCatalog.TitleScene })
        {
            var path = $"Assets/Scenes/{name}.unity";
            if (!System.IO.File.Exists(path)) continue;
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            menu++;
        }

        if (added > 0 || menu > 0) EditorBuildSettings.scenes = scenes.ToArray();
        UnityEngine.Debug.Log($"Build Settings에 스테이지 씬 {added}개를 등록했습니다 (총 {scenes.Count}개, 시작 화면이 첫 씬).");
    }
}
