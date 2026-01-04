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
        randomOffset = Random.Range(0f, 2f * Mathf.PI);
        ConfigureCollider();
        isCollected = false;
    }

    void ConfigureCollider()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        if (capsule == null)
        {
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

        // Rotación en Y
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
        ConfigureCollider();
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coinRadius);
    }
    #endif
}
