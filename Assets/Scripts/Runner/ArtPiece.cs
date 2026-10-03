using UnityEngine;

// 아트가 있는 맵 조각. 그림 크기에 충돌 상자를 맞추고(조금 작게), 폭을 바꿀 수 있는 조각은 그림을 가로로 반복한다.
// Scale로 늘리면 픽셀 아트가 찌그러지므로 맵 디자이너는 Scale 대신 Width를 바꾼다 (Scale은 1로 둔다).
// 충돌 상자 아래쪽은 그림 아래쪽과 같고, 좌우와 위쪽만 줄인다.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ArtPiece : MonoBehaviour
{
    [Tooltip("켜면 Width만큼 그림을 가로로 반복한다. 끄면 그림 크기 그대로")]
    [SerializeField] bool repeatWidth;
    [Tooltip("가로 길이 (유닛, 1유닛 = 64px). 그림 한 장 폭의 정수배로 반올림된다")]
    [SerializeField, Min(0.5f)] float width = 1f;
    [Tooltip("충돌 상자를 좌우에서 각각 이만큼 줄인다 (그림 가장자리가 비어 보이지 않게)")]
    [SerializeField, Min(0f)] float insetSide = 0.1f;
    [Tooltip("충돌 상자를 위에서 이만큼 줄인다")]
    [SerializeField, Min(0f)] float insetTop = 0.15f;
    [Tooltip("충돌 상자를 아래에서 이만큼 줄인다 (매달린 장애물처럼 그림 아래 끝이 비어 있을 때)")]
    [SerializeField, Min(0f)] float insetBottom;

    void Awake() => Apply();

    void OnValidate() => Apply();

    void Apply()
    {
        var sr = GetComponent<SpriteRenderer>();
        var box = GetComponent<BoxCollider2D>();
        if (sr.sprite == null) return;

        var natural = sr.sprite.bounds.size;
        Vector2 size;
        if (repeatWidth)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            // 그림 한 장 폭의 정수배로 맞춘다 (끝에 그림이 잘려 보이지 않게)
            float tiles = Mathf.Max(1f, Mathf.Round(width / natural.x));
            size = new Vector2(tiles * natural.x, natural.y);
            sr.size = size;
        }
        else
        {
            sr.drawMode = SpriteDrawMode.Simple;
            size = natural;
        }

        var hit = new Vector2(Mathf.Max(0.1f, size.x - insetSide * 2f), Mathf.Max(0.1f, size.y - insetTop - insetBottom));
        box.size = hit;
        box.offset = new Vector2(0f, (insetBottom - insetTop) * 0.5f);
    }
}
