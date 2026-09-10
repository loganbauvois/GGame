using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerController : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField, Min(0f)] private float walkSpeed = 4f;
	[SerializeField, Min(0f)] private float sprintSpeed = 7f;
	[SerializeField, Min(0f)] private float acceleration = 18f;
	[SerializeField, Min(0f)] private float deceleration = 24f;
	[SerializeField, Range(0f, 1f)] private float analogDeadzone = 0.1f;
	[SerializeField] private Transform cameraTransform;

	[Header("Jump and gravity")]
	[SerializeField, Min(0f)] private float jumpHeight = 1.4f;
	[SerializeField] private float gravity = -25f;
	[SerializeField, Min(0f)] private float groundedStickForce = 2f;

	private CharacterController characterController;
	private PlayerInputReader inputReader;
	private Vector3 planarVelocity;
	private float verticalVelocity;

	private void Awake()
	{
		characterController = GetComponent<CharacterController>();
		inputReader = GetComponent<PlayerInputReader>();

		if (cameraTransform == null && Camera.main != null)
		{
			cameraTransform = Camera.main.transform;
		}
	}

	private void Update()
	{
		Vector2 input = Vector2.ClampMagnitude(inputReader.Move, 1f);
		Vector3 desiredDirection = GetCameraRelativeDirection(input);
		bool sprinting = inputReader.SprintHeld && input.sqrMagnitude > 0.01f;
		float targetSpeed = sprinting ? sprintSpeed : walkSpeed;
		Vector3 targetVelocity = desiredDirection * (targetSpeed * input.magnitude);

		float speedChange = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude
			? acceleration
			: deceleration;
		planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, speedChange * Time.deltaTime);

		RotateTowardsMovement(desiredDirection);
		UpdateVerticalVelocity();

		Vector3 movement = planarVelocity + Vector3.up * verticalVelocity;
		characterController.Move(movement * Time.deltaTime);
	}

	private Vector3 GetCameraRelativeDirection(Vector2 input)
	{
		if (input.sqrMagnitude <= analogDeadzone * analogDeadzone)
		{
			return Vector3.zero;
		}

		Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
		Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
		forward.y = 0f;
		right.y = 0f;
		forward.Normalize();
		right.Normalize();

		return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
	}

	private void RotateTowardsMovement(Vector3 direction)
	{
		if (direction.sqrMagnitude <= 0.001f)
		{
			return;
		}

		transform.rotation = Quaternion.RotateTowards(
			transform.rotation,
			Quaternion.LookRotation(direction),
			720f * Time.deltaTime);
	}

	private void UpdateVerticalVelocity()
	{
		if (characterController.isGrounded)
		{
			if (verticalVelocity < 0f)
			{
				verticalVelocity = -groundedStickForce;
			}

			if (inputReader.JumpPressedThisFrame)
			{
				verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
			}
		}
		else
		{
			verticalVelocity += gravity * Time.deltaTime;
		}
	}
}
