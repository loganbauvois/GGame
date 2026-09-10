using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
public class EnemyWanderer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 360f;
    [SerializeField, Min(0f)] private float gravity = -25f;
    [SerializeField, Min(0f)] private float groundedStickForce = 2f;

    [Header("Wander behaviour")]
    [SerializeField, Min(0f)] private float wanderRadius = 6f;
    [SerializeField, Min(0f)] private float minimumMoveDuration = 1.5f;
    [SerializeField, Min(0f)] private float maximumMoveDuration = 4f;
    [SerializeField, Min(0f)] private float minimumPauseDuration = 0.75f;
    [SerializeField, Min(0f)] private float maximumPauseDuration = 2.5f;
    [SerializeField, Range(0f, 1f)] private float movementChance = 0.8f;
    [SerializeField] private bool chooseNewDirectionOnCollision = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleState = "Breathing Idle";
    [SerializeField] private string walkState = "Walking";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string groundedParameter = "Grounded";
    [SerializeField, Min(0f)] private float animationDampTime = 0.1f;

    private CharacterController characterController;
    private Health health;
    private Vector3 spawnPosition;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float stateTimer;
    private bool moving;
    private int speedHash;
    private int groundedHash;
    private int idleStateHash;
    private int walkStateHash;
    private int currentAnimationStateHash;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        animator ??= GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
        speedHash = Animator.StringToHash(speedParameter);
        groundedHash = Animator.StringToHash(groundedParameter);
        idleStateHash = Animator.StringToHash(idleState);
        walkStateHash = Animator.StringToHash(walkState);

        ValidateAnimator();
        BeginNextWanderState();
    }

    private void Update()
    {
        if (!health.IsAlive)
        {
            UpdateAnimator(0f);
            return;
        }

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            BeginNextWanderState();
        }

        Vector3 horizontalVelocity = moving ? moveDirection * moveSpeed : Vector3.zero;
        Vector3 movement = horizontalVelocity + Vector3.up * GetVerticalVelocity();
        CollisionFlags collisionFlags = characterController.Move(movement * Time.deltaTime);

        if (chooseNewDirectionOnCollision
            && moving
            && (collisionFlags & CollisionFlags.Sides) != 0)
        {
            BeginNextWanderState();
        }

        if (moving && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        UpdateAnimator(moving ? moveSpeed : 0f);
    }

    private float GetVerticalVelocity()
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -groundedStickForce;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        return verticalVelocity;
    }

    private void BeginNextWanderState()
    {
        moving = Random.value <= movementChance;

        if (!moving)
        {
            moveDirection = Vector3.zero;
            stateTimer = Random.Range(minimumPauseDuration, maximumPauseDuration);
            return;
        }

        Vector2 randomPoint = Random.insideUnitCircle * wanderRadius;
        Vector3 target = spawnPosition + new Vector3(randomPoint.x, 0f, randomPoint.y);
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            direction = Random.insideUnitSphere;
            direction.y = 0f;
        }

        moveDirection = direction.normalized;
        stateTimer = Random.Range(minimumMoveDuration, maximumMoveDuration);
    }

    private void UpdateAnimator(float horizontalSpeed)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(speedHash, horizontalSpeed, animationDampTime, Time.deltaTime);
        animator.SetBool(groundedHash, characterController.isGrounded);

        int desiredStateHash = horizontalSpeed > 0.01f ? walkStateHash : idleStateHash;

        if (desiredStateHash != currentAnimationStateHash)
        {
            currentAnimationStateHash = desiredStateHash;
            animator.CrossFadeInFixedTime(desiredStateHash, animationDampTime, 0, 0f);
        }
    }

    private void ValidateAnimator()
    {
        if (animator == null)
        {
            Debug.LogError("EnemyWanderer: aucun Animator n'a ete trouve sur le modele enfant.", this);
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("EnemyWanderer: assignez PlayerAnimatorController.controller au champ Controller de l'Animator ennemi.", animator);
            return;
        }

        if (!animator.HasState(0, idleStateHash) || !animator.HasState(0, walkStateHash))
        {
            Debug.LogError(
                $"EnemyWanderer: les etats '{idleState}' et/ou '{walkState}' sont absents du Controller '{animator.runtimeAnimatorController.name}'.",
                animator);
            return;
        }

        animator.applyRootMotion = false;
        currentAnimationStateHash = idleStateHash;
        animator.Play(idleStateHash, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}