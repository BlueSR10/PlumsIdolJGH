using UnityEngine;

// 메뉴·대화창 글꼴 설정. Assets/Resources/MenuStyle.asset 하나가 모든 씬(시작·선택·일시정지·튜토리얼)에서 쓰인다.
// 글꼴을 바꾸려면 이 에셋의 Font만 교체한다.
[CreateAssetMenu(menuName = "Plum/Menu Style")]
public class MenuStyle : ScriptableObject
{
    public Font font;
}
