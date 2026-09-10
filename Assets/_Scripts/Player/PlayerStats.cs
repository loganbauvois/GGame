using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Base stats")]
    [SerializeField, Min(0f)] private float moveSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;

    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float DamageMultiplier => damageMultiplier;

    public void AddMoveSpeedMultiplier(float amount)
    {
        moveSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier + amount);
    }

    public void AddDamageMultiplier(float amount)
    {
        damageMultiplier = Mathf.Max(0f, damageMultiplier + amount);
    }
}