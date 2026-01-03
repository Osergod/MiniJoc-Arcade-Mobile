using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Visual Settings")]
    public float rotationSpeed = 100f;
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;
    
    [Header("Collider Settings")]
    public float coinRadius = 0.5f;
    public float coinThickness = 0.1f;
    
    private Vector3 startPosition;
    private float randomOffset;
    private bool isCollected = false;
    
    void Start()
    {
        SetupCoin();
    }
    
    void OnEnable()
    {
        // Reset cuando se reactive (útil para pooling)
        ResetCoin();
    }
    
    void SetupCoin()
    {
        // Guardar posición inicial para animación flotante
        startPosition = transform.position;
        randomOffset = Random.Range(0f, 2f * Mathf.PI);
        
        // Configurar apariencia
        ConfigureAppearance();
        
        // Configurar collider
        ConfigureCollider();
        
        isCollected = false;
    }
    
    void ConfigureAppearance()
    {
        // Resetear escala
        transform.localScale = Vector3.one;
        
        // Rotar cilindro para que quede horizontal (como moneda)
        // Los cilindros en Unity se crean verticales por defecto
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        // Ajustar escala del mesh si es un cilindro
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            // Escalar solo el mesh (no todo el GameObject)
            Transform meshTransform = meshFilter.transform;
            meshTransform.localScale = new Vector3(coinRadius, coinThickness, coinRadius);
        }
    }
    
    void ConfigureCollider()
    {
        // Intentar usar CapsuleCollider (ideal para cilindros)
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        
        if (capsule == null)
        {
            // Si no hay, crear uno
            capsule = gameObject.AddComponent<CapsuleCollider>();
        }
        
        // Configurar como trigger
        capsule.isTrigger = true;
        
        // Ajustar tamaño
        capsule.radius = coinRadius;
        capsule.height = coinThickness;
        capsule.direction = 1; // Eje Y
        capsule.center = Vector3.zero;
    }
    
    void Update()
    {
        if (isCollected) return;
        
        // Rotación continua
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        // Animación de flotación
        FloatAnimation();
    }
    
    void FloatAnimation()
    {
        float floatY = Mathf.Sin((Time.time + randomOffset) * floatSpeed) * floatHeight;
        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + floatY,
            startPosition.z
        );
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            CollectCoin();
        }
    }
    
    void CollectCoin()
    {
        isCollected = true;
        
        // Efectos visuales/sonoros (opcional)
        PlayCollectionEffects();
        
        // Desactivar moneda
        gameObject.SetActive(false);
        
        // Notificar al GameManager
        NotifyCoinCollection();
    }
    
    void PlayCollectionEffects()
    {
        // Aquí puedes añadir:
        // - Sonido de recolección
        // - Partículas
        // - Animación
        
        // Ejemplo básico de partículas:
        // GameObject particles = Instantiate(collectionParticles, transform.position, Quaternion.identity);
        // Destroy(particles, 2f);
    }
    
    void NotifyCoinCollection()
    {
        // Buscar CoinManager en la escena
        CoinManager coinManager = FindObjectOfType<CoinManager>();
        if (coinManager != null)
        {
            coinManager.AddCoin(1);
        }
        else
        {
            // Alternativa: usar singleton o evento
            Debug.Log("Moneda recolectada! (CoinManager no encontrado)");
        }
    }
    
    // Método público para resetear la moneda (para pooling)
    public void ResetCoin()
    {
        isCollected = false;
        ConfigureAppearance();
        ConfigureCollider();
    }
    
    // Método para cambiar valores desde código si es necesario
    public void SetCoinValues(float newRadius, float newThickness)
    {
        coinRadius = newRadius;
        coinThickness = newThickness;
        ConfigureAppearance();
        ConfigureCollider();
    }
    
    // Visualización en el editor
    void OnDrawGizmos()
    {
        // Solo mostrar en selección
        if (!UnityEditor.Selection.Contains(gameObject)) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coinRadius);
        
        // Mostrar dirección de rotación
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.7f);
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.up * 0.7f
        );
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar área del collider cuando está seleccionado
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            // Si no hay collider, mostrar área basada en radio
            Gizmos.DrawSphere(transform.position, coinRadius);
        }
    }
}