using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField, Min(0f)] private float impactLifetime = 2f;
    [SerializeField] private Vector3 impactOffset;

    [Header("Fireball attack")]
    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField, Min(0.01f)] private float attackDuration = 1f;
    [SerializeField, Min(0.01f)] private float attackCooldown = 0.55f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.01f)] private float projectileMaxDistance = 20f;
    [SerializeField, Min(0.01f)] private float projectileRadius = 0.15f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private bool logCombatEvents;

    public bool IsAttacking { get; private set; }
    public bool ProjectileLaunched { get; private set; }
    public event Action AttackStarted;

    private PlayerInputReader inputReader;
    private PlayerController playerController;
    private PlayerStats playerStats;
    private float attackTimer;
    private float cooldownTimer;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerController = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();

        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }

        if (targetLayers.value == 0)
        {
            Debug.LogWarning("PlayerCombat: Target Layers est vide, aucune cible ne pourra etre detectee.", this);
            targetLayers = ~0;
        }
    }

    private void Update()
    {
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        if (IsAttacking)
        {
            UpdateAttack();
            return;
        }

        if (cooldownTimer <= 0f
            && inputReader.AttackPressedThisFrame
            && (playerController == null || playerController.IsGrounded))
        {
            if (logCombatEvents)
            {
                Debug.Log("PlayerCombat: attaque declenchee.", this);
            }

            StartAttack();
        }
    }

    private void StartAttack()
    {
        IsAttacking = true;
        attackTimer = 0f;
        cooldownTimer = attackCooldown;
        ProjectileLaunched = false;

        AttackStarted?.Invoke();
    }

    private void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackDuration)
        {
            IsAttacking = false;
        }
    }

    public void LaunchProjectile()
    {
        if (ProjectileLaunched)
        {
            return;
        }

        ProjectileLaunched = true;

        if (projectilePrefab == null)
        {
            Debug.LogWarning("PlayerCombat: Projectile Prefab n'est pas assigne.", this);
            return;
        }

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            attackOrigin.position,
            Quaternion.LookRotation(attackOrigin.forward));
        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<Projectile>();
        }

        projectile.Initialize(
            attackOrigin.forward,
            projectileSpeed,
            projectileMaxDistance,
            projectileRadius,
            damage * playerStats.DamageMultiplier,
            targetLayers,
            gameObject,
            impactPrefab,
            impactLifetime,
            impactOffset);

        if (logCombatEvents)
        {
            Debug.Log("PlayerCombat: Fireball lancee.", this);
        }
    }

}