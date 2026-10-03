using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 시작 화면·스테이지 선택 화면이 같이 쓰는 uGUI 도우미. 화면은 씬에 컴포넌트 하나만 두고 코드가 UI를 만든다
// (아트가 오면 Image의 sprite만 바꾸면 된다). EventSystem 없이 마우스는 Input System으로 직접 판정한다.
public static class MenuUI
{
    public static readonly Color Background = new Color(0.14f, 0.07f, 0.20f);
    public static readonly Color Card = new Color(0.30f, 0.17f, 0.40f);
    public static readonly Color CardSelected = new Color(0.62f, 0.36f, 0.80f);
    public static readonly Color CardLocked = new Color(0.17f, 0.13f, 0.21f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.35f);
    public static readonly Color Muted = new Color(0.72f, 0.66f, 0.80f);

    static Font font;
    static Font DefaultFont => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    // 배경색 카메라와 오버레이 Canvas(기준 1920x1080)를 만든다. 씬에 카메라가 없어도 화면이 나온다.
    public static RectTransform CreateRoot(Transform owner)
    {
        if (Camera.main == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();
        }

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(owner, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var bg = Box(canvasGo.transform, "Background", Vector2.zero, Vector2.one, Background);
        return bg.parent as RectTransform;
    }

    // 앵커(화면 비율)로 영역을 잡은 색 상자
    public static RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    public static Text Label(Transform parent, string name, string text, int size, Color color, Vector2 min, Vector2 max,
        TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = DefaultFont;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    // 비율을 유지해 영역 안에 그리는 아이콘
    public static Image Icon(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max)
    {
        var rt = Box(parent, name, min, max, Color.white);
        var img = rt.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    // 마우스가 이 영역 위에 있는가 (오버레이 Canvas라 카메라는 null)
    public static bool MouseOver(RectTransform rt)
    {
        var mouse = Mouse.current;
        return mouse != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mouse.position.ReadValue(), null);
    }

    public static bool MouseMoved => Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

    public static bool MouseClicked => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

    // 방향·확인·뒤로 입력. 키 바인딩은 여기서만 정의한다.
    public class Controls
    {
        readonly InputAction left, right, up, down, confirm, back;

        public bool Left => left.WasPressedThisFrame();
        public bool Right => right.WasPressedThisFrame();
        public bool Up => up.WasPressedThisFrame();
        public bool Down => down.WasPressedThisFrame();
        public bool Confirm => confirm.WasPressedThisFrame();
        public bool Back => back.WasPressedThisFrame();

        public Controls()
        {
            left = Make("Left", "<Keyboard>/leftArrow", "<Keyboard>/a", "<Gamepad>/dpad/left", "<Gamepad>/leftStick/left");
            right = Make("Right", "<Keyboard>/rightArrow", "<Keyboard>/d", "<Gamepad>/dpad/right", "<Gamepad>/leftStick/right");
            up = Make("Up", "<Keyboard>/upArrow", "<Keyboard>/w", "<Gamepad>/dpad/up", "<Gamepad>/leftStick/up");
            down = Make("Down", "<Keyboard>/downArrow", "<Keyboard>/s", "<Gamepad>/dpad/down", "<Gamepad>/leftStick/down");
            confirm = Make("Confirm", "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            back = Make("Back", "<Keyboard>/escape", "<Gamepad>/buttonEast");
        }

        static InputAction Make(string name, params string[] bindings)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) a.AddBinding(b);
            a.Enable();
            return a;
        }

        public void Dispose()
        {
            foreach (var a in new[] { left, right, up, down, confirm, back }) { a.Disable(); a.Dispose(); }
        }
    }
}
