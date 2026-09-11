using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject overlayPrefab;

    private GameObject overlayInstance;

    private void Start()
    {
        // Crée l'overlay au début de la partie
        overlayInstance = Instantiate(overlayPrefab, transform);
    }
}