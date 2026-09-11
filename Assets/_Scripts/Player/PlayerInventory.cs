using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public const int SlotCount = 4;

    private Sprite[] itemIcons = new Sprite[SlotCount];

    public event Action InventoryChanged;
    public Sprite GetIcon(int slotIndex) => itemIcons[slotIndex];

    public bool TryAddItem(Sprite icon)
    {
        if (icon == null)
        {
            return false;
        }

        for (int index = 0; index < itemIcons.Length; index++)
        {
            if (itemIcons[index] != null)
            {
                continue;
            }

            itemIcons[index] = icon;
            InventoryChanged?.Invoke();
            return true;
        }

        return false;
    }
}