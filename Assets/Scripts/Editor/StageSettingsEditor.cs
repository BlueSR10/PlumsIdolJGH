using UnityEditor;
using UnityEngine;

// StageSettings Inspector: 값 아래에 맵 검증 결과(골, 시작 바닥, 예상 시간)를 보여 준다.
[CustomEditor(typeof(StageSettings))]
public class StageSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

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
