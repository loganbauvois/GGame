using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    public Vector2 Move => moveAction.ReadValue<Vector2>();
    public Vector2 Look => lookAction.ReadValue<Vector2>();
    public bool SprintHeld => sprintAction.IsPressed();
    public bool JumpPressedThisFrame => jumpAction.WasPressedThisFrame();

    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("PlayerInputReader: assignez InputSystem_Actions dans l'inspecteur.", this);
            enabled = false;
            return;
        }

        playerMap = inputActions.FindActionMap("Player", true);
        moveAction = playerMap.FindAction("Move", true);
        lookAction = playerMap.FindAction("Look", true);
        sprintAction = playerMap.FindAction("Sprint", true);
        jumpAction = playerMap.FindAction("Jump", true);
    }

    private void OnEnable()
    {
        playerMap?.Enable();
    }

    private void OnDisable()
    {
        playerMap?.Disable();
    }
}