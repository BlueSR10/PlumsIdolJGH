using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 세로로 늘어선 메뉴 한 장 (시작 화면, 일시정지, 설정이 같이 쓴다). 항목은 버튼 또는 0~1 슬라이더.
// 위/아래로 고르고 Enter/Space/A로 누른다. 슬라이더는 좌/우로 0.1씩 바꾸고, 마우스로 눌러 끌 수도 있다.
// 입력은 unscaled 기준이라 시간이 멈춘 일시정지 중에도 돈다.
public class MenuPage
{
    public class Item
    {
        public string label;
        public Action onConfirm;          // 버튼: 눌렀을 때
        public Func<float> get;           // 슬라이더: 현재 값(0~1). 있으면 슬라이더
        public Action<float> set;
        public Action onChanged;          // 슬라이더 값이 바뀐 뒤(미리듣기 등)

        internal bool IsSlider => get != null;
        internal RectTransform rect, track, fill;
        internal Image bg;
        internal Text text, percent;
    }

    readonly GameObject root;
    readonly List<Item> items;
    int selected;

    public MenuPage(Transform parent, string title, List<Item> items, Vector2 min, Vector2 max, bool panel = true, int fontSize = 54)
    {
        this.items = items;
        var rt = MenuUI.Box(parent, "Page", min, max, panel ? new Color(0.10f, 0.05f, 0.16f, 0.94f) : Color.clear);
        root = rt.gameObject;

        float top = 1f;
        if (!string.IsNullOrEmpty(title))
        {
            MenuUI.Label(rt, "Title", title, fontSize + 14, MenuUI.Gold, new Vector2(0f, 0.80f), new Vector2(1f, 0.97f));
            top = 0.78f;
        }

        float slot = (top - 0.04f) / items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            float y1 = top - i * slot - slot * 0.06f, y0 = top - (i + 1) * slot + slot * 0.06f;
            it.rect = MenuUI.Box(rt, "Item" + i, new Vector2(0.06f, y0), new Vector2(0.94f, y1), MenuUI.Card);
            it.bg = it.rect.GetComponent<Image>();

            if (it.IsSlider)
            {
                it.text = MenuUI.Label(it.rect, "Label", it.label, fontSize, Color.white, new Vector2(0.04f, 0f), new Vector2(0.45f, 1f), TextAnchor.MiddleLeft);
                it.track = MenuUI.Box(it.rect, "Track", new Vector2(0.46f, 0.40f), new Vector2(0.77f, 0.60f), new Color(0.05f, 0.03f, 0.09f, 1f));
                it.fill = MenuUI.Box(it.track, "Fill", Vector2.zero, Vector2.one, MenuUI.Gold);
                it.percent = MenuUI.Label(it.rect, "Percent", "", fontSize - 14, Color.white, new Vector2(0.79f, 0f), new Vector2(0.98f, 1f), TextAnchor.MiddleRight);
            }
            else
            {
                it.text = MenuUI.Label(it.rect, "Label", it.label, fontSize, Color.white, Vector2.zero, Vector2.one);
            }
        }
        Refresh();
    }

    public void SetActive(bool value)
    {
        root.SetActive(value);
        if (value) { selected = 0; Refresh(); }
    }

    // 한 프레임 처리: 키보드·마우스 입력을 받아 항목을 누르거나 값을 바꾼다
    public void Tick(MenuUI.Controls c)
    {
        int n = items.Count;
        if (c.Up) selected = (selected - 1 + n) % n;
        if (c.Down) selected = (selected + 1) % n;

        var it = items[selected];
        if (it.IsSlider)
        {
            if (c.Left) Change(it, it.get() - 0.1f);
            if (c.Right) Change(it, it.get() + 0.1f);
        }

        bool clickedItem = false;
        var mouse = Mouse.current;
        if (mouse != null && (MenuUI.MouseMoved || MenuUI.MouseClicked || mouse.leftButton.isPressed))
        {
            for (int i = 0; i < n; i++)
            {
                if (!MenuUI.MouseOver(items[i].rect)) continue;
                selected = i;
                it = items[i];
                if (it.IsSlider)
                {
                    if (mouse.leftButton.isPressed) DragSlider(it, mouse.position.ReadValue());
                }
                else clickedItem = MenuUI.MouseClicked;
                break;
            }
        }

        if ((c.Confirm && !it.IsSlider) || clickedItem) it.onConfirm?.Invoke();
        Refresh();
    }

    static void Change(Item it, float value)
    {
        float v = Mathf.Clamp01(Mathf.Round(value * 20f) / 20f);   // 0.05 단위
        if (Mathf.Approximately(v, it.get())) return;
        it.set(v);
        it.onChanged?.Invoke();
    }

    static void DragSlider(Item it, Vector2 screenPoint)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(it.track, screenPoint, null, out var local)) return;
        Change(it, Mathf.InverseLerp(it.track.rect.xMin, it.track.rect.xMax, local.x));
    }

    void Refresh()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            it.bg.color = i == selected ? MenuUI.CardSelected : MenuUI.Card;
            if (!it.IsSlider) continue;
            float v = it.get();
            it.fill.anchorMax = new Vector2(v, 1f);
            it.percent.text = Mathf.RoundToInt(v * 100f) + "%";
        }
    }

    // 배경음악·효과음 볼륨 설정 한 장 (일시정지와 시작 화면이 같이 쓴다). 저장은 SoundManager가 PlayerPrefs에 한다.
    public static MenuPage Settings(Transform parent, Action onBack, Vector2 min, Vector2 max)
    {
        var list = new List<Item>
        {
            new Item { label = "배경음악", get = () => SoundManager.UserBgm, set = v => SoundManager.UserBgm = v },
            new Item { label = "효과음", get = () => SoundManager.UserSfx, set = v => SoundManager.UserSfx = v, onChanged = () => SoundManager.PreviewSfx(Sound.Item) },
            new Item { label = "뒤로", onConfirm = onBack },
        };
        return new MenuPage(parent, "설정", list, min, max);
    }
}
