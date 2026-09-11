using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum StatType
    {
        MoveSpeed,
        Damage
    }

    [Header("Pickup")]
    [SerializeField] private Sprite icon;
    [SerializeField] private StatType statType;
    [SerializeField, Min(0f)] private float bonus = 0.1f;
    [SerializeField, Min(0f)] private float rotationSpeed = 90f;
    [SerializeField, Min(0f)] private float lifetime;

    private void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();

        if (playerStats == null)
        {
            return;
        }

        PlayerInventory inventory = playerStats.GetComponent<PlayerInventory>();

        if (inventory == null || !inventory.TryAddItem(icon))
        {
            return;
        }

        switch (statType)
        {
            case StatType.MoveSpeed:
                playerStats.AddMoveSpeedMultiplier(bonus);
                break;
            case StatType.Damage:
                playerStats.AddDamageMultiplier(bonus);
                break;
        }

        Destroy(gameObject);
    }
}