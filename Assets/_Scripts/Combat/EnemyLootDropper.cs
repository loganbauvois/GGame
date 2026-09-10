using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyLootDropper : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField, Min(0f)] private float scatterRadius = 1f;
    [SerializeField, Min(0.1f)] private float raycastHeight = 3f;
    [SerializeField, Min(0.1f)] private float raycastDistance = 10f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Serializable]
    private class LootDrop
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.25f;
        [SerializeField, Min(0f)] private float spawnHeight = 0.5f;

        public void TrySpawn(Vector3 position, float scatterRadius, float raycastHeight, float raycastDistance, LayerMask groundMask)
        {
            if (prefab == null || UnityEngine.Random.value > dropChance)
            {
                return;
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle * scatterRadius;
            Vector3 candidatePosition = position + new Vector3(offset.x, raycastHeight, offset.y);
            Vector3 spawnPosition = position + new Vector3(offset.x, spawnHeight, offset.y);

            if (Physics.Raycast(
                    candidatePosition,
                    Vector3.down,
                    out RaycastHit groundHit,
                    raycastDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                spawnPosition = groundHit.point + Vector3.up * spawnHeight;
            }

            GameObject loot = Instantiate(prefab, spawnPosition, prefab.transform.rotation);

            if (loot.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }
        }
    }

    [SerializeField] private LootDrop[] drops;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Died += SpawnLoot;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Died -= SpawnLoot;
        }
    }

    private void SpawnLoot(GameObject source)
    {
        if (drops == null)
        {
            return;
        }

        foreach (LootDrop drop in drops)
        {
            drop.TrySpawn(transform.position, scatterRadius, raycastHeight, raycastDistance, groundMask);
        }
    }
}