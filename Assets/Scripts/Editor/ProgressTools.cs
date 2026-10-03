using UnityEditor;
using UnityEngine;

// 진행 저장(PlayerPrefs) 확인용 메뉴. 스테이지 목록의 잠금·클리어 표시를 시험할 때 쓴다.
public static class ProgressTools
{
    [MenuItem("Plum/Progress/Reset (처음부터)")]
    static void Reset()
    {
        Progress.Reset();
        Debug.Log("진행 저장을 지웠습니다 (1-1만 열림, 튜토리얼 미완료).");
    }

    [MenuItem("Plum/Progress/Unlock All (전부 열기)")]
    static void UnlockAll()
    {
        Progress.UnlockAll();
        Debug.Log("모든 스테이지를 클리어한 상태로 저장했습니다.");
    }

    [MenuItem("Plum/Progress/Clear Next (다음 스테이지 클리어 처리)")]
    static void ClearNext()
    {
        int next = Progress.ClearedCount;
        if (next >= StageCatalog.Count) { Debug.Log("이미 전부 클리어한 상태입니다."); return; }
        Progress.MarkCleared(StageCatalog.Scenes[next]);
        Debug.Log($"{StageCatalog.Label(next)} 클리어 처리 (클리어 {Progress.ClearedCount}/{StageCatalog.Count}).");
    }
}
