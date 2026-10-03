using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Plum 메뉴: 새 스테이지 생성, 스냅 설정.
public static class StageTools
{
    public const string RigPath = "Assets/Prefabs/StageRig.prefab";
    public const string PalettePath = "Assets/Prefabs/Palette";
    public const string MapsPath = "Assets/Prefabs/Maps";
    public const string ScenesPath = "Assets/Scenes";

    // 바닥 윗면이 y=-1.11이 되는 기준 높이 (Stage_Test와 동일)
    const float GroundY = -1.61f;
    const float GroundLength = 20f;

    [MenuItem("Plum/New Stage")]
    static void NewStage() => ScriptableWizard.DisplayWizard<NewStageWizard>("New Stage", "만들기");

    // 이동 스냅을 0.5 증분으로 켠다. Ctrl 없이도 이동 기즈모가 0.5 단위로 움직이고, 현재 위치 기준 증분이라 y는 유지된다.
    [MenuItem("Plum/Snap 0.5 켜기")]
    public static void EnableSnap()
    {
        EditorSnapSettings.move = Vector3.one * 0.5f;
        EditorSnapSettings.gridSnapEnabled = false;
        EditorSnapSettings.snapEnabled = true;
    }

    public static void CreateStage(string stageName)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var mapPath = $"{MapsPath}/Map_{stageName}.prefab";
        var scenePath = $"{ScenesPath}/Stage_{stageName}.unity";
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PalettePath}/Ground.prefab");
        var goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PalettePath}/Goal.prefab");
        if (rigPrefab == null || groundPrefab == null || goalPrefab == null)
        {
            Debug.LogError("StageRig 또는 팔레트(Ground, Goal) 프리팹을 찾을 수 없습니다.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1) 맵 프리팹: 바닥 9개(길이 170)와 Goal. Goal 160은 감속 없이 약 30초 (StageSettings 기본값 기준)
        var mapRoot = new GameObject($"Map_{stageName}");
        mapRoot.AddComponent<StageSettings>();
        for (int i = 0; i < 9; i++)
        {
            var ground = (GameObject)PrefabUtility.InstantiatePrefab(groundPrefab, mapRoot.transform);
            ground.transform.localPosition = new Vector3(i * GroundLength, GroundY, 0f);
        }
        var goal = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab, mapRoot.transform);
        goal.transform.localPosition = new Vector3(160f, 1.39f, 0f);
        goal.transform.localScale = new Vector3(0.6f, 5f, 1f);

        Directory.CreateDirectory(MapsPath);
        var mapAsset = PrefabUtility.SaveAsPrefabAsset(mapRoot, mapPath);
        Object.DestroyImmediate(mapRoot);

        // 2) 씬: StageRig + 맵 (맵은 Rig의 Stage 아래)
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        var stage = rig.transform.Find("Stage");
        var map = (GameObject)PrefabUtility.InstantiatePrefab(mapAsset, stage);
        map.transform.localPosition = Vector3.zero;

        EditorSceneManager.SaveScene(scene, scenePath);
        EnableSnap();
        Selection.activeObject = mapAsset;
        EditorGUIUtility.PingObject(mapAsset);
        Debug.Log($"새 스테이지 생성: {scenePath}, {mapPath}. Hierarchy에서 Map_{stageName}을 더블클릭하면 맵을 편집할 수 있습니다.");
    }
}

public class NewStageWizard : ScriptableWizard
{
    public string stageName = "";

    void OnWizardUpdate()
    {
        helpString = "스테이지 이름을 입력하면 Stage_이름 씬과 Map_이름 프리팹이 만들어집니다.";
        isValid = false;

        if (string.IsNullOrWhiteSpace(stageName)) { errorString = "이름을 입력하세요."; return; }
        if (stageName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || stageName.Contains(" "))
        {
            errorString = "공백이나 파일 이름에 쓸 수 없는 문자가 있습니다.";
            return;
        }
        if (File.Exists($"{StageTools.ScenesPath}/Stage_{stageName}.unity") || File.Exists($"{StageTools.MapsPath}/Map_{stageName}.prefab"))
        {
            errorString = "같은 이름의 씬이나 맵이 이미 있습니다.";
            return;
        }

        errorString = "";
        isValid = true;
    }

    void OnWizardCreate() => StageTools.CreateStage(stageName);
}
