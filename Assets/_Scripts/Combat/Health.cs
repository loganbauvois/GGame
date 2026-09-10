using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath;

    [Header("Damage feedback")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField, Min(0f)] private float damageFlashDuration = 0.12f;
    [SerializeField] private Renderer[] damageRenderers;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsAlive => CurrentHealth > 0f;
    public event Action<float, GameObject> Damaged;
    public event Action<GameObject> Died;

    private MaterialPropertyBlock propertyBlock;
    private MaterialPropertyBlock[] originalPropertyBlocks;
    private Coroutine damageFlashRoutine;
    private int baseColorPropertyId;
    private int colorPropertyId;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        propertyBlock = new MaterialPropertyBlock();
        baseColorPropertyId = Shader.PropertyToID("_BaseColor");
        colorPropertyId = Shader.PropertyToID("_Color");

        if (damageRenderers == null || damageRenderers.Length == 0)
        {
            damageRenderers = GetComponentsInChildren<Renderer>();
        }

        originalPropertyBlocks = new MaterialPropertyBlock[damageRenderers.Length];

        for (int index = 0; index < damageRenderers.Length; index++)
        {
            if (damageRenderers[index] == null)
            {
                continue;
            }

            MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
            damageRenderers[index].GetPropertyBlock(originalBlock);
            originalPropertyBlocks[index] = originalBlock;
        }
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Damaged?.Invoke(amount, source);
        TriggerDamageFlash();

        if (CurrentHealth > 0f)
        {
            return;
        }

        Died?.Invoke(source);

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void Restore(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    private void TriggerDamageFlash()
    {
        if (!flashOnDamage || damageFlashDuration <= 0f || damageRenderers.Length == 0)
        {
            return;
        }

        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
        }

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        ApplyFlashColor(damageFlashColor);
        yield return new WaitForSeconds(damageFlashDuration);
        ClearFlashColor();
        damageFlashRoutine = null;
    }

    private void ApplyFlashColor(Color color)
    {
        foreach (Renderer damageRenderer in damageRenderers)
        {
            if (damageRenderer == null)
            {
                continue;
            }

            damageRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(baseColorPropertyId, color);
            propertyBlock.SetColor(colorPropertyId, color);
            damageRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ClearFlashColor()
    {
        for (int index = 0; index < damageRenderers.Length; index++)
        {
            Renderer damageRenderer = damageRenderers[index];

            if (damageRenderer == null)
            {
                continue;
            }

            damageRenderer.SetPropertyBlock(originalPropertyBlocks[index]);
        }
    }
}