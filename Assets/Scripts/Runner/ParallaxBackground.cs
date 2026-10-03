using UnityEngine;

// 층마다 다른 속도로 흐르는 배경. 스테이지 스크롤 속도를 따라가며 화면(640x360 기준, 세로 5.625유닛)을 가득 채운다.
// 배경 프리팹은 Assets/Resources/Backgrounds/ 에 두고, StageSettings에서 이름으로 고른다.
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public SpriteRenderer renderer;
        [Range(0f, 1f)] public float scroll;   // 스테이지 스크롤 속도 대비 비율 (0 = 화면에 고정, 1 = 맵과 같은 속도)
    }

    const float ViewHeight = 5.625f;      // 기준 해상도 640x360, PPU 64
    const float ViewCenterY = 0.703125f;  // 카메라 y

    [SerializeField] Layer[] layers;

    SpeedController speed;
    Transform[][] tiles;
    float[] widths;
    float[] offsets;

    void Awake()
    {
        transform.position = new Vector3(0f, ViewCenterY, 0f);
        speed = FindFirstObjectByType<SpeedController>();

        tiles = new Transform[layers.Length][];
        widths = new float[layers.Length];
        offsets = new float[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            var sr = layers[i].renderer;
            float scale = ViewHeight / sr.sprite.bounds.size.y;
            widths[i] = sr.sprite.bounds.size.x * scale;

            // 같은 층을 3장 이어 붙여 좌우로 끊김 없이 반복한다
            tiles[i] = new Transform[3];
            tiles[i][1] = sr.transform;
            tiles[i][0] = Instantiate(sr.gameObject, sr.transform.parent).transform;
            tiles[i][2] = Instantiate(sr.gameObject, sr.transform.parent).transform;
            foreach (var t in tiles[i]) t.localScale = new Vector3(scale, scale, 1f);
            Place(i);
        }

        StageReset.Requested += ResetOffsets;
    }

    void OnDestroy()
    {
        StageReset.Requested -= ResetOffsets;
    }

    void Update()
    {
        if (speed == null) return;
        for (int i = 0; i < layers.Length; i++)
        {
            offsets[i] = Mathf.Repeat(offsets[i] + speed.ScrollSpeed * layers[i].scroll * Time.deltaTime, widths[i]);
            Place(i);
        }
    }

    void ResetOffsets()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            offsets[i] = 0f;
            Place(i);
        }
    }

    // offset은 0 ~ width. 타일 3장의 가운데를 -offset에 두고 양옆에 이어 붙인다.
    void Place(int i)
    {
        float x = -offsets[i];
        for (int k = 0; k < 3; k++)
        {
            var p = tiles[i][k].localPosition;
            tiles[i][k].localPosition = new Vector3(x + (k - 1) * widths[i], 0f, p.z);
        }
    }
}
