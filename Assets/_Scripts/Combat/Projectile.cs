using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float maxDistance;
    private float radius;
    private float damage;
    private LayerMask targetLayers;
    private GameObject source;
    private GameObject impactPrefab;
    private float impactLifetime;
    private Vector3 impactOffset;
    private bool logProjectileEvents;
    private float travelledDistance;
    private bool initialized;

    public void Initialize(
        Vector3 direction,
        float speed,
        float maxDistance,
        float radius,
        float damage,
        LayerMask targetLayers,
        GameObject source,
        GameObject impactPrefab,
        float impactLifetime,
        Vector3 impactOffset,
        bool logProjectileEvents)
    {
        this.direction = direction.normalized;
        this.speed = speed;
        this.maxDistance = maxDistance;
        this.radius = radius;
        this.damage = damage;
        this.targetLayers = targetLayers;
        this.source = source;
        this.impactPrefab = impactPrefab;
        this.impactLifetime = impactLifetime;
        this.impactOffset = impactOffset;
        this.logProjectileEvents = logProjectileEvents;
        initialized = true;

        transform.rotation = Quaternion.LookRotation(this.direction);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        float distanceThisFrame = speed * Time.deltaTime;
        Vector3 start = transform.position;
        Vector3 movement = direction * distanceThisFrame;

        if (TryFindTarget(start, movement, out RaycastHit hit, out IDamageable damageable))
        {
            transform.position = hit.point;
            damageable.TakeDamage(damage, source);
            SpawnImpact(hit.point);
            Destroy(gameObject);
            return;
        }

        transform.position += movement;
        travelledDistance += distanceThisFrame;

        if (travelledDistance >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private bool TryFindTarget(
        Vector3 start,
        Vector3 movement,
        out RaycastHit closestHit,
        out IDamageable closestDamageable)
    {
        closestHit = default;
        closestDamageable = null;
        RaycastHit[] hits = Physics.SphereCastAll(
            start,
            radius,
            direction,
            movement.magnitude,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(source.transform)
                || hit.collider.transform == source.transform)
            {
                continue;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable == null || !damageable.IsAlive || hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            closestHit = hit;
            closestDamageable = damageable;
        }

        return closestDamageable != null;
    }

    private void SpawnImpact(Vector3 position)
    {
        if (impactPrefab == null)
        {
            return;
        }

        GameObject impact = Instantiate(
            impactPrefab,
            position + impactOffset,
            Quaternion.LookRotation(direction));

        if (impactLifetime > 0f)
        {
            Destroy(impact, impactLifetime);
        }
    }
}