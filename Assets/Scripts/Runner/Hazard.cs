using UnityEngine;

// 닿으면 실패하는 장애물. 트리거 콜라이더와 함께 쓴다.
// destructible이면 거대화 중에 닿았을 때 부서지고(사망하지 않음), 스테이지가 재시작되면 복구된다.
// 그림을 가로로 반복한 조각(ArtPiece의 Repeat Width)은 통째로 사라지지 않고 닿은 칸만 한 장씩 부서진다.
// 파괴 불가 장애물(Block)과 벽(Gate)은 destructible을 끄고 쓴다.
public class Hazard : MonoBehaviour
{
    [SerializeField] bool destructible;

    Collider2D[] colliders;
    Renderer[] renderers;
    bool broken;

    // 한 장씩 부서지도록 나눈 칸들. 처음 부서질 때 만들고 재시작하면 없앤다
    GameObject[] tiles;
    Collider2D[] tileColliders;

    public bool Destructible => destructible;

    // 부서질 때 발생한다 (부서지기 직전의 충돌 범위). 이펙트·소리가 구독한다.
    public static event System.Action<Bounds> Broken;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<Renderer>();
        StageReset.Requested += Restore;
    }

    void OnDestroy()
    {
        StageReset.Requested -= Restore;
    }

    // by: 부순 쪽(플레이어)의 충돌 범위. 여러 칸으로 된 조각은 이 범위에 닿은 칸만 부순다
    public void Break(Bounds by)
    {
        if (broken) return;
        if (tiles == null) Split();
        if (tiles != null)
        {
            BreakTiles(by);
            return;
        }

        var bounds = colliders[0].bounds;
        foreach (var c in colliders) bounds.Encapsulate(c.bounds);
        Broken?.Invoke(bounds);
        SetBroken(true);
    }

    void Restore()
    {
        if (tiles != null)
        {
            foreach (var tile in tiles) Destroy(tile);
            tiles = null;
            tileColliders = null;
            SetBroken(false);
        }
        else if (broken) SetBroken(false);
    }

    void SetBroken(bool value)
    {
        broken = value;
        foreach (var c in colliders) c.enabled = !value;
        foreach (var r in renderers) r.enabled = !value;
    }

    // 그림을 가로로 반복한 조각이면 그림 한 장마다 따로 부술 수 있는 칸(그림 + 충돌 상자)으로 바꾼다.
    // 지금 크기 그대로 나누므로, 플레이 중에 길이가 바뀐 조각(SyBrakePenalty)도 그 길이로 나뉜다.
    void Split()
    {
        var sr = GetComponent<SpriteRenderer>();
        var box = GetComponent<BoxCollider2D>();
        if (sr == null || box == null || sr.sprite == null || sr.drawMode != SpriteDrawMode.Tiled) return;

        var sprite = sr.sprite;
        float tileWidth = sprite.bounds.size.x;
        int count = Mathf.RoundToInt(sr.size.x / tileWidth);
        if (count < 2) return;

        float left = sr.localBounds.min.x;
        float boxLeft = box.offset.x - box.size.x * 0.5f;
        float boxRight = box.offset.x + box.size.x * 0.5f;

        tiles = new GameObject[count];
        tileColliders = new Collider2D[count];
        for (int i = 0; i < count; i++)
        {
            float tileLeft = left + i * tileWidth;
            float x = tileLeft + tileWidth * 0.5f - sprite.bounds.center.x;

            var tile = new GameObject("Tile");
            tile.layer = gameObject.layer;
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = new Vector3(x, 0f, 0f);

            var tileSprite = tile.AddComponent<SpriteRenderer>();
            tileSprite.sprite = sprite;
            tileSprite.color = sr.color;
            tileSprite.sharedMaterial = sr.sharedMaterial;
            tileSprite.sortingLayerID = sr.sortingLayerID;
            tileSprite.sortingOrder = sr.sortingOrder;
            tileSprite.flipX = sr.flipX;
            tileSprite.flipY = sr.flipY;

            // 원래 충돌 상자에서 이 칸에 해당하는 부분만 잘라 쓴다 (양 끝 칸은 원래처럼 조금 좁다)
            float hitLeft = Mathf.Max(tileLeft, boxLeft);
            float hitRight = Mathf.Min(tileLeft + tileWidth, boxRight);
            if (hitRight > hitLeft)
            {
                var tileBox = tile.AddComponent<BoxCollider2D>();
                tileBox.isTrigger = box.isTrigger;
                tileBox.size = new Vector2(hitRight - hitLeft, box.size.y);
                tileBox.offset = new Vector2((hitLeft + hitRight) * 0.5f - x, box.offset.y);
                tileColliders[i] = tileBox;
            }
            tiles[i] = tile;
        }

        // 원래 그림과 충돌 상자는 끈다 (broken은 아니다: 남은 칸에 닿으면 계속 부서지거나 사망한다)
        foreach (var c in colliders) c.enabled = false;
        foreach (var r in renderers) r.enabled = false;
    }

    void BreakTiles(Bounds by)
    {
        by.Expand(new Vector3(0f, 0f, 100f));   // 깊이(z)는 보지 않는다

        int nearest = -1;
        float nearestDistance = float.PositiveInfinity;
        bool any = false;
        for (int i = 0; i < tiles.Length; i++)
        {
            var c = tileColliders[i];
            if (c == null || !c.enabled) continue;

            if (c.bounds.Intersects(by))
            {
                BreakTile(i);
                any = true;
                continue;
            }
            float distance = Mathf.Abs(c.bounds.center.x - by.center.x);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        // 닿았다는 신호는 왔는데 겹치는 칸을 못 찾았으면 가장 가까운 칸을 부순다
        if (!any && nearest >= 0) BreakTile(nearest);
    }

    void BreakTile(int i)
    {
        Broken?.Invoke(tileColliders[i].bounds);
        tileColliders[i].enabled = false;
        tiles[i].GetComponent<SpriteRenderer>().enabled = false;
    }
}
