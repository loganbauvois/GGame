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
    private float travelledDistance;
    private bool initialized;
    private RaycastHit[] hitBuffer;

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
        Vector3 impactOffset)
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
        hitBuffer = new RaycastHit[16];
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
        int hitCount = Physics.SphereCastNonAlloc(
            start,
            radius,
            direction,
            hitBuffer,
            movement.magnitude,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = hitBuffer[index];

            if (hit.collider == null)
            {
                continue;
            }

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