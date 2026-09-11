using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInventory))]
public class InventoryBarUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField, Min(0f)] private float bottomMargin = 48f;
    [SerializeField, Min(32f)] private float slotSize = 72f;
    [SerializeField, Min(0f)] private float slotSpacing = 8f;
    [SerializeField] private Color barColor = new Color(0.04f, 0.05f, 0.08f, 0.9f);
    [SerializeField] private Color slotColor = new Color(0.14f, 0.16f, 0.21f, 0.95f);
    [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0f);

    private PlayerInventory inventory;
    private Image[] slotImages;
    private Canvas canvas;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        CreateCanvas();
        CreateBar();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= Refresh;
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("InventoryCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.transform.SetParent(null, false);

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void CreateBar()
    {
        GameObject barObject = new GameObject("InventoryBar");
        barObject.transform.SetParent(canvas.transform, false);

        RectTransform barRect = barObject.AddComponent<RectTransform>();
        float desiredWidth = PlayerInventory.SlotCount * slotSize
            + (PlayerInventory.SlotCount - 1) * slotSpacing
            + 24f;
        float maximumWidth = Mathf.Max(160f, Screen.width - 32f);
        float barWidth = Mathf.Min(desiredWidth, maximumWidth);
        float effectiveSlotSize = Mathf.Min(slotSize,
            (barWidth - 24f - (PlayerInventory.SlotCount - 1) * slotSpacing)
            / PlayerInventory.SlotCount);
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(barWidth, effectiveSlotSize + 24f);
        barRect.anchoredPosition = new Vector2(0f, Mathf.Max(bottomMargin, 16f));

        Image barImage = barObject.AddComponent<Image>();
        barImage.color = barColor;

        HorizontalLayoutGroup layout = barObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        slotImages = new Image[PlayerInventory.SlotCount];

        for (int index = 0; index < PlayerInventory.SlotCount; index++)
        {
            GameObject slotObject = new GameObject($"Slot_{index + 1}");
            slotObject.transform.SetParent(barObject.transform, false);

            RectTransform slotRect = slotObject.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(effectiveSlotSize, effectiveSlotSize);

            Image slotBackground = slotObject.AddComponent<Image>();
            slotBackground.color = slotColor;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slotObject.transform, false);

            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8f, 8f);
            iconRect.offsetMax = new Vector2(-8f, -8f);

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.color = emptyIconColor;
            slotImages[index] = iconImage;
        }
    }

    private void Refresh()
    {
        if (slotImages == null || inventory == null)
        {
            return;
        }

        for (int index = 0; index < slotImages.Length; index++)
        {
            Sprite icon = inventory.GetIcon(index);
            slotImages[index].sprite = icon;
            slotImages[index].color = icon == null ? emptyIconColor : Color.white;
        }
    }
}