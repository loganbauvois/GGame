using System;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCombat playerCombat;

    [Header("Animator parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string groundedParameter = "Grounded";
    [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string locomotionState = "Locomotion";
    [SerializeField] private string jumpState = "Jump";
    [SerializeField] private string fallState = "Falling";
    [SerializeField] private string attackState = "Fireball";
    [SerializeField, Range(0f, 1f)] private float nonLoopingAnimationEnd = 0.95f;
    [SerializeField, Min(0f)] private float locomotionTransitionDuration = 0.12f;
    [SerializeField, Min(0f)] private float jumpTransitionDuration = 0.08f;
    [SerializeField, Min(0f)] private float attackTransitionDuration = 0.04f;
    [Tooltip("Pourcentage de progression du clip Fireball. 0.75 = 75 %, 0.98 = 98 %.")]
    [SerializeField, Range(0f, 1f)] private float projectileLaunchNormalizedTime = 0.98f;
    [SerializeField, Min(0f)] private float longFallDuration = 0.35f;
    [SerializeField, Min(0f)] private float voluntaryJumpFallDelay = 2f;
    [SerializeField, Min(0f)] private float speedDampTime = 0.1f;

    private int speedHash;
    private int groundedHash;
    private int verticalSpeedHash;
    private int jumpTriggerHash;
    private int attackHash;
    private int locomotionStateHash;
    private int jumpStateHash;
    private int fallStateHash;
    private int attackStateHash;
    private bool attackAnimationLocked;

    public event Action AttackAnimationFinished;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    private void Awake()
    {
        animator ??= GetComponentInChildren<Animator>();
        playerController ??= GetComponent<PlayerController>();
        playerCombat ??= GetComponent<PlayerCombat>();

        if (attackState == "Boxing")
        {
            attackState = "Fireball";
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        speedHash = Animator.StringToHash(speedParameter);
        groundedHash = Animator.StringToHash(groundedParameter);
        verticalSpeedHash = Animator.StringToHash(verticalSpeedParameter);
        jumpTriggerHash = Animator.StringToHash(jumpTrigger);
        attackHash = Animator.StringToHash(attackTrigger);
        locomotionStateHash = Animator.StringToHash(locomotionState);
        jumpStateHash = Animator.StringToHash(jumpState);
        fallStateHash = Animator.StringToHash(fallState);
        attackStateHash = Animator.StringToHash(attackState);

        ValidateState(locomotionState, locomotionStateHash);
        ValidateState(jumpState, jumpStateHash);
        ValidateState(fallState, fallStateHash);
        ValidateState(attackState, attackStateHash);
    }

    private void OnEnable()
    {
        if (playerCombat != null)
        {
            playerCombat.AttackStarted += OnAttackStarted;
        }

        if (playerController != null)
        {
            playerController.JumpStarted += OnJumpStarted;
        }
    }

    private void OnDisable()
    {
        if (playerCombat != null)
        {
            playerCombat.AttackStarted -= OnAttackStarted;
        }

        if (playerController != null)
        {
            playerController.JumpStarted -= OnJumpStarted;
        }
    }

    private void Update()
    {
        if (animator == null || playerController == null)
        {
            return;
        }

        animator.SetFloat(speedHash, playerController.HorizontalSpeed, speedDampTime, Time.deltaTime);
        animator.SetBool(groundedHash, playerController.IsGrounded);
        animator.SetFloat(verticalSpeedHash, playerController.VerticalSpeed);

        if (attackAnimationLocked)
        {
            LaunchProjectileAtAnimationEnd();

            if (HasFinishedState(attackStateHash))
            {
                attackAnimationLocked = false;
                AttackAnimationFinished?.Invoke();
            }
            else
            {
                return;
            }
        }

        if (playerCombat != null && playerCombat.IsAttacking)
        {
            return;
        }

        int desiredStateHash = GetLocomotionStateHash();
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (!animator.IsInTransition(0) && currentState.shortNameHash != desiredStateHash)
        {
            if (desiredStateHash == jumpStateHash || desiredStateHash == fallStateHash)
            {
                animator.CrossFadeInFixedTime(desiredStateHash, jumpTransitionDuration, 0, 0f);
                return;
            }

            animator.CrossFadeInFixedTime(desiredStateHash, locomotionTransitionDuration);
        }
    }

    private bool HasFinishedState(int stateHash)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float endThreshold = stateHash == attackStateHash
            ? Mathf.Max(nonLoopingAnimationEnd, projectileLaunchNormalizedTime)
            : nonLoopingAnimationEnd;

        return !animator.IsInTransition(0)
            && state.shortNameHash == stateHash
            && state.normalizedTime >= endThreshold;
    }

    private void LaunchProjectileAtAnimationEnd()
    {
        if (playerCombat == null || playerCombat.ProjectileLaunched || animator.IsInTransition(0))
        {
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.shortNameHash == attackStateHash
            && state.normalizedTime >= projectileLaunchNormalizedTime)
        {
            playerCombat.LaunchProjectile();
        }
    }

    private int GetLocomotionStateHash()
    {
        bool fallAfterVoluntaryJump = playerController.IsJumping
            && playerController.AirborneDuration >= voluntaryJumpFallDelay;
        bool fallAfterUnplannedDrop = !playerController.IsJumping
            && playerController.AirborneDuration >= longFallDuration;

        if (!playerController.IsGrounded
            && (fallAfterVoluntaryJump || fallAfterUnplannedDrop)
            && playerController.VerticalSpeed < 0f)
        {
            return fallStateHash;
        }

        if (playerController.IsJumping)
        {
            return jumpStateHash;
        }

        return locomotionStateHash;
    }

    private void ValidateState(string stateName, int stateHash)
    {
        if (animator == null)
        {
            Debug.LogError("PlayerAnimator: aucun Animator n'est assigne ou trouve sur le modele.", this);
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("PlayerAnimator: l'Animator n'a aucun Runtime Animator Controller assigne.", animator);
            return;
        }

        if (!animator.HasState(0, stateHash))
        {
            Debug.LogError(
                $"PlayerAnimator: l'etat '{stateName}' est introuvable dans '{animator.runtimeAnimatorController.name}'. Assignez PlayerAnimatorController.controller a l'Animator du modele.",
                animator);
        }
    }

    private void OnAttackStarted()
    {
        if (animator == null)
        {
            return;
        }

        if (!animator.HasState(0, attackStateHash))
        {
            Debug.LogError($"PlayerAnimator: l'etat d'attaque '{attackState}' est introuvable dans la couche Base Layer.", animator);
            return;
        }

        attackAnimationLocked = true;
        animator.Play(attackStateHash, 0, 0f);

        if (!string.IsNullOrWhiteSpace(attackTrigger) && animator.parameters.Length > 0)
        {
            animator.SetTrigger(attackHash);
        }
    }

    private void OnJumpStarted()
    {
        if (animator == null || attackAnimationLocked)
        {
            return;
        }

        animator.CrossFadeInFixedTime(jumpStateHash, jumpTransitionDuration, 0, 0f);
        animator.SetTrigger(jumpTriggerHash);
    }
}