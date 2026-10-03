using System.Collections;
using UnityEngine;

// 스테이지 전환 연출 (GDD 8.4): 마녀만 남기고 화면이 어두워진다.
// 카메라 앞에 검은 사각형을 두고, 어두워지는 동안 마녀 스프라이트만 그 위로 올린다.
public class ScreenFade : MonoBehaviour
{
    const int OverlayOrder = 100;

    SpriteRenderer overlay;
    SpriteRenderer witch;
    int witchOrder;

    public void Init(Camera cam, SpriteRenderer witchRenderer)
    {
        witch = witchRenderer;
        witchOrder = witch != null ? witch.sortingOrder : 0;

        var go = new GameObject("ScreenFade");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 1f);
        go.transform.localScale = new Vector3(100f, 100f, 1f);

        var tex = Texture2D.whiteTexture;
        overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        overlay.sortingOrder = OverlayOrder;
        SetAlpha(0f);
    }

    public void SetAlpha(float alpha)
    {
        overlay.color = new Color(0f, 0f, 0f, alpha);
        overlay.enabled = alpha > 0f;
    }

    // 어두워지는 동안 마녀만 어둠 위로 보이게 한다
    public void LiftWitch(bool lifted)
    {
        if (witch != null) witch.sortingOrder = lifted ? OverlayOrder + 1 : witchOrder;
    }

    public IEnumerator FadeTo(float target, float duration)
    {
        float from = overlay.color.a;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetAlpha(Mathf.Lerp(from, target, t / duration));
            yield return null;
        }
        SetAlpha(target);
    }
}
