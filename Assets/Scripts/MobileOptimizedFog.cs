using UnityEngine;

public class MobileOptimizedFog : MonoBehaviour
{
    [Header("Fog Settings - MOBILE OPTIMIZED")]
    [SerializeField] private Color fogColor = new Color(0.5f, 0.6f, 0.7f, 1f);
    [SerializeField] private float fogStart = 15f;
    [SerializeField] private float fogEnd = 40f;
    
    [Header("Performance Optimizations")]
    [SerializeField] private bool useLinearFog = true; // Mejor para móvil
    [SerializeField] private bool disableOnLowEnd = true;
    [SerializeField] private int targetFPS = 30; // Para móvil
    
    [Header("Dynamic Adjustments")]
    [SerializeField] private bool adjustWithDistance = true;
    [SerializeField] private float minFogEnd = 25f;
    [SerializeField] private float maxFogEnd = 60f;
    
    void Start()
    {
        // Optimizar para móvil
        OptimizeForMobile();
        
        // Aplicar niebla
        ApplyFog();
        
        // Ajustar FPS para móvil (ahorra batería)
        Application.targetFrameRate = targetFPS;
    }
    
    void OptimizeForMobile()
    {
        // Detectar dispositivo bajo
        if (disableOnLowEnd && SystemInfo.systemMemorySize < 2000) // Menos de 2GB RAM
        {
            Debug.Log("Low-end device detected. Disabling fog for performance.");
            RenderSettings.fog = false;
            enabled = false; // Desactivar script
            return;
        }
        
        // Forzar Linear fog (más barato que Exponential)
        if (useLinearFog)
        {
            RenderSettings.fogMode = FogMode.Linear;
        }
    }
    
    void ApplyFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        
        if (useLinearFog)
        {
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }
        else
        {
            // Exponential es más pesado, usar densidad baja
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.015f; // MUY baja para móvil
        }
    }
    
    void Update()
    {
        if (!adjustWithDistance || !RenderSettings.fog) return;
        
        // Ajustar niebla basado en distancia del jugador (opcional)
        // Esto simula que la niebla se adapta a tu velocidad
        float playerZ = 0f;
        
        // Obtén la posición Z del jugador de tu PlayerController
        // Ejemplo: playerZ = PlayerController.instance.transform.position.z;
        
        // Niebla más densa cuanto más avanzas
        float progress = Mathf.Clamp01(playerZ / 1000f);
        float currentFogEnd = Mathf.Lerp(minFogEnd, maxFogEnd, progress);
        
        RenderSettings.fogEndDistance = currentFogEnd;
    }
    
    // Método para activar/desactivar dinámicamente
    public void SetFogEnabled(bool enabled)
    {
        RenderSettings.fog = enabled;
        
        if (!enabled)
        {
            // Guardar batería desactivando updates
            this.enabled = false;
        }
    }
    
    // Cambiar color de niebla (para efectos)
    public void ChangeFogColor(Color newColor, float duration = 1f)
    {
        StartCoroutine(TransitionFogColor(newColor, duration));
    }
    
    private System.Collections.IEnumerator TransitionFogColor(Color targetColor, float duration)
    {
        Color startColor = RenderSettings.fogColor;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            RenderSettings.fogColor = Color.Lerp(startColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        RenderSettings.fogColor = targetColor;
    }
}