using UnityEditor;
using UnityEngine;

// StageSettings Inspector: 값 아래에 맵 검증 결과(골, 시작 바닥, 예상 시간)를 보여 준다.
[CustomEditor(typeof(StageSettings))]
public class StageSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DrawBackgroundPicker();
        DrawNextStagePicker();

        var s = (StageSettings)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("맵 검증", EditorStyles.boldLabel);

        var goals = s.GetComponentsInChildren<Goal>();
        if (goals.Length == 0)
        {
            EditorGUILayout.HelpBox("Goal이 없습니다. 팔레트의 Goal을 맵 끝에 놓으세요.", MessageType.Error);
        }
        else
        {
            if (goals.Length > 1)
                EditorGUILayout.HelpBox($"Goal이 {goals.Length}개입니다. 하나만 두세요.", MessageType.Warning);

            float goalX = s.transform.InverseTransformPoint(goals[0].transform.position).x;
            float distance = goalX - StageSettings.PlayerStartX;
            float seconds = s.EstimatedSeconds(distance);
            var type = Mathf.Abs(seconds - StageSettings.TargetSeconds) <= 10f ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(
                $"시작~Goal 거리 {distance:0.#}유닛, 감속 없이 약 {seconds:0.#}초 (목표 {StageSettings.TargetSeconds:0}초, 감속하면 더 걸림)",
                type);
        }

        if (!HasGroundAt(s, StageSettings.PlayerStartX))
            EditorGUILayout.HelpBox($"시작 위치(x={StageSettings.PlayerStartX})에 바닥이 없습니다. 플레이어가 떨어집니다.", MessageType.Error);
    }

    // Assets/Resources/Backgrounds/ 의 프리팹 목록에서 배경을 고른다. 새 배경 프리팹을 넣으면 자동으로 목록에 나온다.
    void DrawBackgroundPicker()
    {
        const string folder = "Assets/Resources/Backgrounds";
        var names = new System.Collections.Generic.List<string> { "(없음)" };
        if (AssetDatabase.IsValidFolder(folder))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { folder }))
                names.Add(System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
        }

        serializedObject.Update();
        var prop = serializedObject.FindProperty("background");
        int index = Mathf.Max(0, names.IndexOf(prop.stringValue));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("배경", EditorStyles.boldLabel);
        int picked = EditorGUILayout.Popup("배경 선택", index, names.ToArray());
        if (picked != index || (picked == 0 && !string.IsNullOrEmpty(prop.stringValue)))
        {
            prop.stringValue = picked == 0 ? "" : names[picked];
            serializedObject.ApplyModifiedProperties();
        }

        if (!string.IsNullOrEmpty(prop.stringValue) && !names.Contains(prop.stringValue))
            EditorGUILayout.HelpBox($"'{prop.stringValue}' 배경을 찾을 수 없습니다. 다시 고르세요.", MessageType.Warning);

        if (picked > 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{names[picked]}.prefab");
            var preview = prefab != null ? AssetPreview.GetAssetPreview(prefab) : null;
            if (preview != null)
            {
                var rect = GUILayoutUtility.GetRect(1f, 120f, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
            }
            else if (AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
            {
                Repaint();
            }
        }
        EditorGUILayout.HelpBox("배경은 Play할 때 나타납니다 (Prefab 모드에서는 보이지 않음).", MessageType.None);
    }

    // Build Settings에 등록된 씬 중에서 클리어 후 넘어갈 스테이지를 고른다. 없음 = 같은 스테이지를 다시 시작.
    void DrawNextStagePicker()
    {
        var names = new System.Collections.Generic.List<string> { "(없음 - 같은 스테이지 반복)" };
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) names.Add(System.IO.Path.GetFileNameWithoutExtension(scene.path));
        }

        serializedObject.Update();
        var prop = serializedObject.FindProperty("nextStage");
        int index = Mathf.Max(0, names.IndexOf(prop.stringValue));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("스테이지 전환", EditorStyles.boldLabel);
        int picked = EditorGUILayout.Popup("다음 스테이지", index, names.ToArray());
        if (picked != index || (picked == 0 && !string.IsNullOrEmpty(prop.stringValue)))
        {
            prop.stringValue = picked == 0 ? "" : names[picked];
            serializedObject.ApplyModifiedProperties();
        }

        if (!string.IsNullOrEmpty(prop.stringValue) && !names.Contains(prop.stringValue))
            EditorGUILayout.HelpBox($"'{prop.stringValue}' 씬이 Build Settings에 없습니다. 프로그래머에게 말하세요.", MessageType.Warning);
    }

    static bool HasGroundAt(StageSettings s, float localX)
    {
        foreach (var col in s.GetComponentsInChildren<BoxCollider2D>())
        {
            if (col.isTrigger) continue;
            float center = s.transform.InverseTransformPoint(col.transform.TransformPoint(col.offset)).x;
            float half = col.size.x * Mathf.Abs(col.transform.lossyScale.x) * 0.5f;
            if (localX >= center - half && localX <= center + half) return true;
        }
        return false;
    }
}
