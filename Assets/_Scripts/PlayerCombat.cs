using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private GameObject impactPrefab;
    [SerializeField, Min(0f)] private float impactLifetime = 2f;
    [SerializeField] private Vector3 impactOffset;

    [Header("Melee attack")]
    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField, Min(0.01f)] private float attackRange = 1.2f;
    [SerializeField, Min(0.01f)] private float attackRadius = 0.55f;
    [SerializeField, Min(0f)] private float activeDelay = 0.12f;
    [SerializeField, Min(0.01f)] private float attackDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float attackCooldown = 0.55f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private bool logCombatEvents;

    public bool IsAttacking { get; private set; }

    private readonly HashSet<IDamageable> hitTargets = new();
    private PlayerInputReader inputReader;
    private float attackTimer;
    private float cooldownTimer;
    private bool attackHasHit;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();

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

        if (cooldownTimer <= 0f && inputReader.AttackPressedThisFrame)
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
        attackHasHit = false;
        hitTargets.Clear();

        if (animator != null && !string.IsNullOrWhiteSpace(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }
    }

    private void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

        if (!attackHasHit && attackTimer >= activeDelay)
        {
            PerformAttackHit();
            attackHasHit = true;
        }

        if (attackTimer >= attackDuration)
        {
            IsAttacking = false;
        }
    }

    private void PerformAttackHit()
    {
        Vector3 center = attackOrigin.position + attackOrigin.forward * attackRange;
        Collider[] colliders = Physics.OverlapSphere(
            center,
            attackRadius,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        foreach (Collider hitCollider in colliders)
        {
            if (hitCollider.transform.IsChildOf(transform) || hitCollider.transform == transform)
            {
                continue;
            }

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();

            if (damageable == null || !damageable.IsAlive || !hitTargets.Add(damageable))
            {
                if (logCombatEvents && damageable == null)
                {
                    Debug.LogWarning(
                        $"PlayerCombat: {hitCollider.name} a ete detecte, mais ne possede pas de Health.",
                        hitCollider);
                }

                continue;
            }

            damageable.TakeDamage(damage, gameObject);
            SpawnImpact(hitCollider);

            if (logCombatEvents)
            {
                Debug.Log($"PlayerCombat: {hitCollider.name} a recu {damage} degats.", hitCollider);
            }
        }
    }

    private void SpawnImpact(Collider hitCollider)
    {
        if (impactPrefab == null)
        {
            return;
        }

        Vector3 impactPosition = hitCollider.ClosestPoint(attackOrigin.position) + impactOffset;
        GameObject impact = Instantiate(impactPrefab, impactPosition, Quaternion.LookRotation(attackOrigin.forward));

        if (impactLifetime > 0f)
        {
            Destroy(impact, impactLifetime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position + origin.forward * attackRange, attackRadius);
    }
}