using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject overlayPrefab;

    private GameObject overlayInstance;

    private void Awake()
    {
        CreateOverlay();
    }

    private void CreateOverlay()
    {
        if (overlayPrefab == null)
        {
            Debug.LogError("UIManager : aucune Overlay Prefab assignée.", this);
            return;
        }

        // Le Canvas/Overlay sera créé comme enfant du UIManager
        overlayInstance = Instantiate(overlayPrefab, transform);
        overlayInstance.name = overlayPrefab.name;
    }
}