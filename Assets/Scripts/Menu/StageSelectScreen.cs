using UnityEngine;
using UnityEngine.UI;

// 스테이지 목록 화면. 6개 스테이지를 세계(집 밖 / 집 / 오븐)별 열 3개 x 2칸으로 보여 준다.
// 방향키(WASD, 게임패드 십자키) 또는 마우스로 고르고 Enter/Space/클릭으로 들어간다. Esc는 시작 화면으로 돌아간다.
// 앞 스테이지를 순서대로 깨야 다음 칸이 열리고, 못 깬 칸은 LOCKED로 보인다. 씬이 아직 없는 칸은 COMING SOON이다.
public class StageSelectScreen : MonoBehaviour
{
    class Slot
    {
        public RectTransform rect;
        public Image image;
        public Text number;
        public Image lockIcon;   // 잠긴 칸은 번호 대신 이 아이콘을 보인다
        public Text status;
    }

    const int Rows = StageCatalog.StagesPerWorld;

    [Tooltip("잠긴 스테이지에 번호 대신 보일 아이콘 (Assets/Art/UI)")]
    [SerializeField] Sprite lockIcon;

    MenuUI.Controls controls;
    Slot[] slots;
    bool[] ready;   // 씬이 있는가 (에디터의 FindAssets가 느려서 한 번만 확인)
    Text message;
    float messageTimer;
    int selected;
    bool loading;

    void Awake()
    {
        SoundManager.PlayBgm(null, 0f);

        controls = new MenuUI.Controls();
        var root = MenuUI.CreateRoot(transform);

        MenuUI.Label(root, "Header", "SELECT STAGE", 110, MenuUI.Gold, new Vector2(0f, 0.86f), new Vector2(1f, 0.98f));

        int columns = StageCatalog.Worlds.Length;
        slots = new Slot[StageCatalog.Count];
        ready = new bool[StageCatalog.Count];
        for (int i = 0; i < ready.Length; i++) ready[i] = StageLoader.Exists(StageCatalog.Scenes[i]);
        for (int c = 0; c < columns; c++)
        {
            float x0 = 0.07f + c * 0.30f, x1 = x0 + 0.26f;
            MenuUI.Label(root, "World" + c, StageCatalog.Worlds[c], 52, MenuUI.Muted, new Vector2(x0, 0.76f), new Vector2(x1, 0.84f));

            for (int r = 0; r < Rows; r++)
            {
                int index = c * Rows + r;
                float y1 = 0.72f - r * 0.29f, y0 = y1 - 0.25f;
                var rect = MenuUI.Box(root, "Stage" + StageCatalog.Label(index), new Vector2(x0, y0), new Vector2(x1, y1), MenuUI.Card);
                slots[index] = new Slot
                {
                    rect = rect,
                    image = rect.GetComponent<Image>(),
                    number = MenuUI.Label(rect, "Number", StageCatalog.Label(index), 110, Color.white, new Vector2(0f, 0.30f), Vector2.one),
                    lockIcon = MenuUI.Icon(rect, "Lock", lockIcon, new Vector2(0.3f, 0.34f), new Vector2(0.7f, 0.94f)),
                    status = MenuUI.Label(rect, "Status", "", 40, Color.white, Vector2.zero, new Vector2(1f, 0.32f)),
                };
            }
        }

        message = MenuUI.Label(root, "Message", "", 40, new Color(1f, 0.6f, 0.6f), new Vector2(0f, 0.095f), new Vector2(1f, 0.15f));
        MenuUI.Label(root, "Hint", "Arrows / Mouse: select     Enter / Click: play     Esc: back", 34, MenuUI.Muted,
            new Vector2(0f, 0.02f), new Vector2(1f, 0.085f));

        // 처음 선택은 지금 도전할 스테이지(첫 번째 못 깬 칸, 전부 깼으면 마지막)
        selected = Mathf.Min(Progress.ClearedCount, StageCatalog.Count - 1);
        Refresh();
    }

    void OnDestroy() => controls?.Dispose();

    void Update()
    {
        if (loading) return;

        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) message.text = "";
        }

        if (controls.Back) { Leave(StageCatalog.TitleScene); return; }

        int col = selected / Rows, row = selected % Rows;
        if (controls.Left) col = (col - 1 + StageCatalog.Worlds.Length) % StageCatalog.Worlds.Length;
        if (controls.Right) col = (col + 1) % StageCatalog.Worlds.Length;
        if (controls.Up || controls.Down) row = (row + 1) % Rows;   // 칸이 위아래 2개뿐이라 어느 쪽이든 반대 칸으로
        selected = col * Rows + row;

        // 마우스는 움직일 때만 선택을 바꾼다 (가만히 둔 마우스가 키보드 선택을 덮어쓰지 않게)
        bool clickedSlot = false;
        if (MenuUI.MouseMoved || MenuUI.MouseClicked)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!MenuUI.MouseOver(slots[i].rect)) continue;
                selected = i;
                clickedSlot = MenuUI.MouseClicked;
                break;
            }
        }

        if (controls.Confirm || clickedSlot) Enter(selected);
        Refresh();
    }

    void Enter(int index)
    {
        if (!Progress.IsUnlocked(index))
        {
            Say("Clear the previous stage first.");
            return;
        }
        if (!ready[index])
        {
            Say("This stage is not ready yet.");
            return;
        }
        Leave(StageCatalog.Scenes[index]);
    }

    void Leave(string scene)
    {
        loading = true;
        StageLoader.Load(scene);
    }

    void Say(string text)
    {
        message.text = text;
        messageTimer = 2f;
    }

    void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            bool unlocked = Progress.IsUnlocked(i);
            bool cleared = Progress.IsCleared(i);
            bool isSelected = i == selected;

            s.image.color = !unlocked ? MenuUI.CardLocked : isSelected ? MenuUI.CardSelected : MenuUI.Card;
            // 잠금 아이콘이 없으면(미지정) 흐린 번호로 대신한다
            bool showIcon = !unlocked && s.lockIcon.sprite != null;
            s.lockIcon.enabled = showIcon;
            s.number.enabled = !showIcon;
            s.number.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            if (!unlocked) { s.status.text = "LOCKED"; s.status.color = new Color(1f, 1f, 1f, 0.45f); }
            else if (cleared) { s.status.text = "CLEAR"; s.status.color = new Color(0.55f, 1f, 0.6f); }
            else if (!ready[i]) { s.status.text = "COMING SOON"; s.status.color = MenuUI.Muted; }
            else { s.status.text = "PLAY"; s.status.color = MenuUI.Gold; }

            s.rect.localScale = Vector3.one * (isSelected ? 1.06f : 1f);
        }
    }
}
