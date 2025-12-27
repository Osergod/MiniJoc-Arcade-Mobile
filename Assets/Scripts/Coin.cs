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
        ResetCoin();
    }
    
    void SetupCoin()
    {
        startPosition = transform.position;
        randomOffset = Random.Range(0f, 2f * Mathf.PI); // CORREGIDO: 2 argumentos
        ConfigureAppearance();
        ConfigureCollider();
        isCollected = false;
    }
    
    void ConfigureAppearance()
    {
        // Resetear todo
        transform.localScale = Vector3.one;
        
        // Para cilindros: rotar 90° en X para que quede horizontal
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        // Escalar para tamaño de moneda
        transform.localScale = new Vector3(coinRadius * 2f, coinThickness, coinRadius * 2f);
    }
    
    void ConfigureCollider()
    {
        // Buscar o crear CapsuleCollider
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        
        if (capsule == null)
        {
            capsule = gameObject.AddComponent<CapsuleCollider>();
        }
        
        // Configurar como trigger
        capsule.isTrigger = true;
        capsule.radius = 0.5f;
        capsule.height = 0.1f;
        capsule.direction = 1; // Eje Y
        capsule.center = Vector3.zero;
    }
    
    void Update()
    {
        if (isCollected) return;
        
        // Rotación en Y solamente
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
        gameObject.SetActive(false);
        NotifyCoinCollection();
    }
    
    void NotifyCoinCollection()
    {
        CoinManager coinManager = FindObjectOfType<CoinManager>();
        if (coinManager != null)
        {
            coinManager.AddCoin(1);
        }
    }
    
    public void ResetCoin()
    {
        isCollected = false;
        ConfigureAppearance();
        ConfigureCollider();
    }
    
    // Gizmos solo en Editor
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coinRadius);
    }
    #endif
}