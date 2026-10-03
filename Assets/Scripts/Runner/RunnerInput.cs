using UnityEngine;
using UnityEngine.InputSystem;

// 러너 입력. 키 바인딩은 여기서만 정의한다.
public class RunnerInput : MonoBehaviour
{
    InputAction jump;
    InputAction slide;
    InputAction brake;

    InputAction ability;

    public bool JumpPressed => jump.WasPressedThisFrame();
    public bool JumpHeld => jump.IsPressed();
    public bool SlideHeld => slide.IsPressed();
    public bool BrakeHeld => brake.IsPressed();
    public bool AbilityPressed => ability.WasPressedThisFrame();

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
