using UnityEngine;

public class MobileOptimizedFog : MonoBehaviour
{
    [Header("Fog Settings - HAZLA VISIBLE")]
    [SerializeField] private Color fogColor = new Color(0.35f, 0.45f, 0.55f, 1f);
    [SerializeField] private float fogStart = 3f;
    [SerializeField] private float fogEnd = 20f;
    
    [Header("Fog Density (para Exponential)")]
    [SerializeField] private float fogDensity = 0.035f;
    
    [Header("Fog Mode")]
    [SerializeField] private bool useLinearFog = false;
    [SerializeField] private bool useExponentialSquared = false;
    
    [Header("Performance Optimizations")]
    [SerializeField] private bool disableOnLowEnd = true;
    [SerializeField] private int targetFPS = 30;
    
    [Header("Dynamic Adjustments")]
    [SerializeField] private bool adjustWithDistance = true;
    [SerializeField] private float minFogEnd = 15f;
    [SerializeField] private float maxFogEnd = 30f;
    
    [Header("Fog Pulso")]
    [SerializeField] private bool useFogPulse = false;
    [SerializeField] private float pulseSpeed = 0.5f;
    [SerializeField] private float pulseAmount = 3f;
    
    [Header("Niebla en Cielo (Material)")]
    [SerializeField] private Material skyboxWithFog;
    private Material originalSkybox;
    
    void Start()
    {
        // Guardar skybox original
        originalSkybox = RenderSettings.skybox;
        
        // Optimizar para móvil
        OptimizeForMobile();
        
        // Aplicar niebla MÁS VISIBLE
        ApplyFog();
        
        // Aplicar skybox con niebla si se especificó
        if (skyboxWithFog != null)
        {
            RenderSettings.skybox = skyboxWithFog;
        }
        
        // Ajustar FPS
        Application.targetFrameRate = targetFPS;
    }
    
    void OptimizeForMobile()
    {
        if (disableOnLowEnd && SystemInfo.systemMemorySize < 2000)
        {
            Debug.Log("Low-end device. Fog DISABLED.");
            RenderSettings.fog = false;
            enabled = false;
            return;
        }
    }
    
    void ApplyFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        
        if (useLinearFog)
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }
        else if (useExponentialSquared)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity * 1.5f;
        }
        else
        {
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
        }
        
        Debug.Log($"Fog APPLIED: Mode={RenderSettings.fogMode}, Density={RenderSettings.fogDensity}, Start={fogStart}, End={fogEnd}");
    }
    
    void Update()
    {
        if (!RenderSettings.fog) return;
        
        if (useFogPulse)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            float pulsedFogEnd = fogEnd + pulse;
            RenderSettings.fogEndDistance = Mathf.Clamp(pulsedFogEnd, minFogEnd, maxFogEnd);
        }
        
        if (adjustWithDistance)
        {
            AdjustFogWithDistance();
        }
    }
    
    void AdjustFogWithDistance()
    {
        float playerZ = 0f;
        
        // DESCOMENTA Y AJUSTA:
        // GameObject player = GameObject.FindGameObjectWithTag("Player");
        // if (player != null) playerZ = player.transform.position.z;
        
        float progress = Mathf.Clamp01(playerZ / 1000f);
        float currentFogEnd = Mathf.Lerp(minFogEnd, maxFogEnd, progress);
        
        if (RenderSettings.fogMode == FogMode.Linear)
        {
            RenderSettings.fogEndDistance = currentFogEnd;
        }
        else
        {
            float currentDensity = Mathf.Lerp(fogDensity * 0.8f, fogDensity * 1.2f, progress);
            RenderSettings.fogDensity = currentDensity;
        }
    }
    
    public void MakeFogMoreVisible()
    {
        if (RenderSettings.fogMode == FogMode.Linear)
        {
            fogEnd = 15f;
            fogStart = 2f;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }
        else
        {
            fogDensity = 0.05f;
            RenderSettings.fogDensity = fogDensity;
        }
        
        Debug.Log("Fog MADE MORE VISIBLE");
    }
    
    public void SetFogVeryDense()
    {
        useLinearFog = false;
        useExponentialSquared = true;
        fogDensity = 0.06f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;
        fogColor = new Color(0.3f, 0.4f, 0.5f);
        RenderSettings.fogColor = fogColor;
        
        Debug.Log("Fog set to VERY DENSE");
    }
    
    public void ResetToNormalFog()
    {
        useLinearFog = false;
        useExponentialSquared = false;
        fogDensity = 0.035f;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensity;
        fogColor = new Color(0.35f, 0.45f, 0.55f);
        RenderSettings.fogColor = fogColor;
        
        Debug.Log("Fog reset to NORMAL");
    }
    
    public void SetFogEnabled(bool enabled)
    {
        RenderSettings.fog = enabled;
    }
    
    // Para restaurar skybox al salir
    void OnDestroy()
    {
        if (originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
        }
    }
}