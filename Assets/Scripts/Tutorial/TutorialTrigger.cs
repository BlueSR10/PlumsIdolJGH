using UnityEngine;

// 튜토리얼 대화 지점. 맵(스테이지의 자식)에 두면 마녀가 이 위치를 지날 때 대화창이 뜬다 (TutorialDirector가 맵에 있어야 한다).
// 한 번 본 대화는 사망 후 재시작해도 다시 뜨지 않는다. 위치(X)만 정하면 되고 높이는 상관없다.
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("한 줄로 짧게. 말투는 ~있어요 / ~어요")]
    [TextArea(2, 4)] public string message = "여기에 설명을 적어 주세요.";

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
        var p = transform.position;
        Gizmos.DrawLine(new Vector3(p.x, p.y - 4f, 0f), new Vector3(p.x, p.y + 4f, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.Label(p + Vector3.up * 4.2f, "대화: " + message);
#endif
    }
}
