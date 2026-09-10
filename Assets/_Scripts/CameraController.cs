using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField, Min(0.1f)] private float distance = 4f;
    [SerializeField, Min(0f)] private float height = 1.6f;
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
    [SerializeField, Min(0f)] private float gamepadSensitivity = 140f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private LayerMask obstructionMask = ~0;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.1f;

    private InputAction lookAction;
    private Vector3 positionVelocity;
    private float yaw;
    private float pitch = 15f;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError("CameraController: assignez la cible du joueur dans l'inspecteur.", this);
            enabled = false;
            return;
        }

        yaw = target.eulerAngles.y;
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogError("CameraController: assignez InputSystem_Actions dans l'inspecteur.", this);
            enabled = false;
            return;
        }

        lookAction = inputActions.FindActionMap("Player", true).FindAction("Look", true);
        lookAction.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        lookAction?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        UpdateOrbit();

        Vector3 focusPoint = target.position + Vector3.up * height;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = focusPoint - orbitRotation * Vector3.forward * distance;
        desiredPosition = ResolveObstruction(focusPoint, desiredPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime);
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private void UpdateOrbit()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.001f;
        float sensitivity = usingGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        yaw += look.x * sensitivity;
        pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
    }

    private Vector3 ResolveObstruction(Vector3 focusPoint, Vector3 desiredPosition)
    {
        Vector3 offset = desiredPosition - focusPoint;
        float desiredDistance = offset.magnitude;

        if (desiredDistance <= 0.001f || !Physics.SphereCast(
                focusPoint,
                collisionRadius,
                offset.normalized,
                out RaycastHit hit,
                desiredDistance,
                obstructionMask,
                QueryTriggerInteraction.Ignore))
        {
            return desiredPosition;
        }

        return focusPoint + offset.normalized * Mathf.Max(0.05f, hit.distance - collisionPadding);
    }
}
