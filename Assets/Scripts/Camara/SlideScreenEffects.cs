// Script adicional para efectos de pantalla durante el deslizamiento
using UnityEngine;
using UnityEngine.UI;

public class SlideScreenEffects : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Image screenOverlay;
    
    [Header("Effects")]
    public Color slideColor = new Color(0.5f, 0.8f, 1f, 0.1f);
    public float effectFadeSpeed = 5f;
    
    private Color originalColor;
    private Color transparentColor;
    
    void Start()
    {
        if (screenOverlay == null)
        {
            // Crear overlay automáticamente
            CreateOverlay();
        }
        
        originalColor = screenOverlay.color;
        transparentColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }
    
    void Update()
    {
        if (player == null || screenOverlay == null) return;
        
        // Verificar si está deslizándose
        bool isSliding = (player.currentState == PlayerController.PlayerState.Sliding);
        
        // Aplicar efecto
        Color targetColor = isSliding ? slideColor : transparentColor;
        screenOverlay.color = Color.Lerp(screenOverlay.color, targetColor, effectFadeSpeed * Time.deltaTime);
    }
    
    void CreateOverlay()
    {
        GameObject canvasObj = new GameObject("SlideCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(canvasObj.transform);
        screenOverlay = overlayObj.AddComponent<Image>();
        screenOverlay.color = new Color(0f, 0f, 0f, 0f);
        screenOverlay.rectTransform.anchorMin = Vector2.zero;
        screenOverlay.rectTransform.anchorMax = Vector2.one;
        screenOverlay.rectTransform.offsetMin = Vector2.zero;
        screenOverlay.rectTransform.offsetMax = Vector2.zero;
    }
}