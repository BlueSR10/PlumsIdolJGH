using UnityEngine;
using UnityEngine.InputSystem;

// 러너 입력. 키 바인딩은 여기서만 정의한다.
public class RunnerInput : MonoBehaviour
{
    InputAction jump;
    InputAction slide;
    InputAction brake;

    InputAction ability;

    // 연출·튜토리얼 대화 중(RunManager.Holding)에는 모든 입력을 막는다 (대화를 넘기는 Space가 점프로 새지 않게)
    static bool Locked => RunManager.Instance != null && RunManager.Instance.Holding;

    public bool JumpPressed => !Locked && jump.WasPressedThisFrame();
    public bool JumpHeld => !Locked && jump.IsPressed();
    public bool SlideHeld => !Locked && slide.IsPressed();
    public bool BrakeHeld => !Locked && brake.IsPressed();
    public bool AbilityPressed => !Locked && ability.WasPressedThisFrame();

    void Awake()
    {
        jump = new InputAction("Jump", InputActionType.Button);
        jump.AddBinding("<Keyboard>/space");
        jump.AddBinding("<Keyboard>/upArrow");
        jump.AddBinding("<Keyboard>/w");
        jump.AddBinding("<Gamepad>/buttonSouth");

        slide = new InputAction("Slide", InputActionType.Button);
        slide.AddBinding("<Keyboard>/downArrow");
        slide.AddBinding("<Keyboard>/s");
        slide.AddBinding("<Gamepad>/leftStick/down");

        brake = new InputAction("Brake", InputActionType.Button);
        brake.AddBinding("<Keyboard>/leftShift");
        brake.AddBinding("<Gamepad>/leftTrigger");

        ability = new InputAction("Ability", InputActionType.Button);
        ability.AddBinding("<Keyboard>/e");
        ability.AddBinding("<Gamepad>/buttonWest");
    }

    void OnEnable()
    {
        jump.Enable();
        slide.Enable();
        brake.Enable();
        ability.Enable();
    }

    void OnDisable()
    {
        jump.Disable();
        slide.Disable();
        brake.Disable();
        ability.Disable();
    }
}
