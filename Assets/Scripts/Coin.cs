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
        // Reset cuando se reactive (�til para pooling)
        ResetCoin();
    }

    void SetupCoin()
    {
        // Guardar posici�n inicial para animaci�n flotante
        startPosition = transform.position;
        randomOffset = Random.Range(0f, 2f * Mathf.PI);
        ConfigureCollider();
        
        isCollected = false;
    }

    void ConfigureCollider()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        if (capsule == null)
        {
            // Si no hay, crear uno
            capsule = gameObject.AddComponent<CapsuleCollider>();
        }

        capsule.isTrigger = true;
        capsule.radius = 0.5f;
        capsule.height = 0.1f;
        capsule.direction = 1; // eje Y
        capsule.center = Vector3.zero;
    }

    void Update()
    {
        if (isCollected) return;

        // Rotaci�n en Y
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

        // Animaci�n de flotaci�n
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

        AudioManager am = FindObjectOfType<AudioManager>();
        am.SoundCoin();
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
    public void ResetCoin()
    {
        isCollected = false;
        ConfigureCollider();
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        coinRadius = newRadius;
        coinThickness = newThickness;
        ConfigureAppearance();
        ConfigureCollider();
    }
    
    // Visualizaci�n en el editor
    void OnDrawGizmos()
    {
        // Solo mostrar en selecci�n
        if (!UnityEditor.Selection.Contains(gameObject)) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coinRadius);
        
        // Mostrar direcci�n de rotaci�n
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.7f);
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.up * 0.7f
        );
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar �rea del collider cuando est� seleccionado
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            // Si no hay collider, mostrar �rea basada en radio
            Gizmos.DrawSphere(transform.position, coinRadius);
        }
    }
    #endif
}
