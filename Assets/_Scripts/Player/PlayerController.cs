using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(InventoryBarUI))]
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
	private PlayerCombat playerCombat;
	private PlayerAnimator playerAnimator;
	private PlayerStats playerStats;
	private Vector3 planarVelocity;
	private float verticalVelocity;
	private bool attackMovementLocked;
	private bool attackControlLocked;
	private float airborneDuration;

	public float HorizontalSpeed => planarVelocity.magnitude;
	public bool IsGrounded => characterController != null && characterController.isGrounded;
	public float VerticalSpeed => verticalVelocity;
	public bool IsJumping { get; private set; }
	public float AirborneDuration => airborneDuration;
	public event Action JumpStarted;

	private void Awake()
	{
		characterController = GetComponent<CharacterController>();
		inputReader = GetComponent<PlayerInputReader>();
		playerCombat = GetComponent<PlayerCombat>();
		playerAnimator = GetComponent<PlayerAnimator>();
		playerStats = GetComponent<PlayerStats>();
		ValidateCharacterController();

		if (cameraTransform == null && Camera.main != null)
		{
			cameraTransform = Camera.main.transform;
		}
	}

	private void OnEnable()
	{
		if (playerCombat != null)
		{
			playerCombat.AttackStarted += LockMovementForAttack;
		}

		if (playerAnimator != null)
		{
			playerAnimator.AttackAnimationFinished += UnlockMovementAfterAttack;
		}
	}

	private void OnDisable()
	{
		if (playerCombat != null)
		{
			playerCombat.AttackStarted -= LockMovementForAttack;
		}

		if (playerAnimator != null)
		{
			playerAnimator.AttackAnimationFinished -= UnlockMovementAfterAttack;
		}
	}

	private void ValidateCharacterController()
	{
		if (characterController.height <= characterController.radius * 2f)
		{
			Debug.LogWarning(
				"PlayerController: la hauteur du CharacterController doit etre superieure a deux fois son rayon.",
				this);
		}

		if (characterController.center.y - characterController.height * 0.5f < 0f)
		{
			Debug.LogWarning(
				"PlayerController: le bas du CharacterController est sous le pivot du Player. Verifiez Center et Height.",
				this);
		}
	}

	private void Update()
	{
		bool attackIsActive = attackMovementLocked
			|| attackControlLocked
			|| (playerCombat != null && playerCombat.IsAttacking);
		Vector2 input = Vector2.ClampMagnitude(inputReader.Move, 1f);
		Vector3 desiredDirection = attackIsActive
			? Vector3.zero
			: GetCameraRelativeDirection(input);
		bool sprinting = inputReader.SprintHeld && input.sqrMagnitude > 0.01f;
		float targetSpeed = (sprinting ? sprintSpeed : walkSpeed) * playerStats.MoveSpeedMultiplier;
		Vector3 targetVelocity = attackIsActive
			? Vector3.zero
			: desiredDirection * (targetSpeed * input.magnitude);

		float speedChange = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude
			? acceleration
			: deceleration;
		planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, speedChange * Time.deltaTime);

		if (!attackIsActive)
		{
			RotateTowardsMovement(desiredDirection);
		}
		UpdateVerticalVelocity();

		Vector3 movement = planarVelocity + Vector3.up * verticalVelocity;
		characterController.Move(movement * Time.deltaTime);

	}

	private void LockMovementForAttack()
	{
		attackMovementLocked = true;
		attackControlLocked = true;
		planarVelocity = Vector3.zero;
		RotateTowardsCamera();
	}

	private void UnlockMovementAfterAttack()
	{
		attackMovementLocked = false;
		attackControlLocked = false;
		planarVelocity = Vector3.zero;
	}

	private void RotateTowardsCamera()
	{
		if (cameraTransform == null)
		{
			return;
		}

		Vector3 cameraForward = cameraTransform.forward;
		cameraForward.y = 0f;

		if (cameraForward.sqrMagnitude > 0.001f)
		{
			transform.rotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
		}
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
			airborneDuration = 0f;

			if (verticalVelocity < 0f)
			{
				verticalVelocity = -groundedStickForce;
				IsJumping = false;
			}

			if (!attackControlLocked && inputReader.JumpPressedThisFrame)
			{
				IsJumping = true;
				airborneDuration = 0f;
				verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
				JumpStarted?.Invoke();
			}
		}
		else
		{
			airborneDuration += Time.deltaTime;
			verticalVelocity += gravity * Time.deltaTime;
		}
	}

	private void OnDrawGizmosSelected()
	{
		CharacterController controller = GetComponent<CharacterController>();

		if (controller == null)
		{
			return;
		}

		Gizmos.color = Color.cyan;
		Vector3 center = transform.TransformPoint(controller.center);
		float cylinderHeight = Mathf.Max(0f, controller.height - controller.radius * 2f);
		Gizmos.DrawWireSphere(center + Vector3.up * cylinderHeight * 0.5f, controller.radius);
		Gizmos.DrawWireSphere(center - Vector3.up * cylinderHeight * 0.5f, controller.radius);
	}
}
