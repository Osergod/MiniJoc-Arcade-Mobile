using System.Collections.Generic;
using UnityEngine;

public class AdvancedGroundGenerator : MonoBehaviour
{
    [System.Serializable]
    public class GroundPool
    {
        public GameObject prefab;
        public int poolSize;
        [HideInInspector] public Queue<GameObject> availableObjects = new Queue<GameObject>();
        [HideInInspector] public List<GameObject> activeObjects = new List<GameObject>();
    }
    
    [Header("Player Reference")]
    public Transform player;
    
    [Header("Ground Generation")]
    public GroundPool groundPool;
    public float groundSegmentLength = 10f;
    public int segmentsAhead = 10;
    public int segmentsBehind = 2;
    
    [Header("Obstacle & Collectible Pools")]
    public GroundPool obstaclePool;
    public GroundPool coinPool;
    public GroundPool powerUpPool;
    
    [Header("Generation Settings")]
    public AnimationCurve obstacleDensityCurve;
    public float minObstacleDistance = 2f;
    public float laneWidth = 2f;
    public int numberOfLanes = 3;
    
    private float nextGroundZ = 0f;
    private List<float> segmentStartZ = new List<float>();
    
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogError("No player found with tag 'Player'");
        }
        
        // Inicializar pools solo si tienen prefab
        if (groundPool.prefab != null)
            InitializePool(groundPool);
        else
            Debug.LogError("Ground Pool prefab is not assigned!");
            
        if (obstaclePool.prefab != null)
            InitializePool(obstaclePool);
            
        if (coinPool.prefab != null)
            InitializePool(coinPool);
            
        // PowerUp pool es opcional
        if (powerUpPool.prefab != null && powerUpPool.poolSize > 0)
            InitializePool(powerUpPool);
        
        // Generar suelo inicial
        for (int i = 0; i < segmentsAhead; i++)
        {
            GenerateNextSegment();
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Generar nuevos segmentos cuando el jugador se acerca
        if (player.position.z > nextGroundZ - (segmentsAhead * groundSegmentLength))
        {
            GenerateNextSegment();
        }
        
        // Reciclar segmentos atrás
        RecycleOldSegments();
    }
    
    void InitializePool(GroundPool pool)
    {
        if (pool.prefab == null)
        {
            Debug.LogWarning($"Cannot initialize pool with null prefab");
            return;
        }
        
        for (int i = 0; i < pool.poolSize; i++)
        {
            GameObject obj = Instantiate(pool.prefab, Vector3.zero, Quaternion.identity, transform);
            obj.SetActive(false);
            pool.availableObjects.Enqueue(obj);
        }
    }
    
    GameObject GetFromPool(GroundPool pool)
    {
        if (pool.prefab == null)
        {
            Debug.LogWarning($"Trying to get object from pool with null prefab!");
            return null;
        }
        
        if (pool.availableObjects.Count == 0)
        {
            // Expandir pool dinámicamente
            GameObject newObj = Instantiate(pool.prefab, Vector3.zero, Quaternion.identity, transform);
            pool.availableObjects.Enqueue(newObj);
        }
        
        GameObject obj = pool.availableObjects.Dequeue();
        pool.activeObjects.Add(obj);
        return obj;
    }
    
    void ReturnToPool(GroundPool pool, GameObject obj)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        obj.transform.parent = transform;
        pool.activeObjects.Remove(obj);
        pool.availableObjects.Enqueue(obj);
    }
    
    void GenerateNextSegment()
    {
        // Obtener segmento de suelo del pool
        if (groundPool.prefab == null) return;
        
        GameObject groundSegment = GetFromPool(groundPool);
        if (groundSegment == null) return;
        
        groundSegment.transform.position = new Vector3(0, 0, nextGroundZ);
        groundSegment.SetActive(true);
        
        // Generar contenido del segmento
        PopulateSegment(groundSegment.transform, nextGroundZ);
        
        segmentStartZ.Add(nextGroundZ);
        nextGroundZ += groundSegmentLength;
    }
    
    void PopulateSegment(Transform segment, float segmentZ)
    {
        float segmentStart = segmentZ - groundSegmentLength / 2;
        float segmentEnd = segmentZ + groundSegmentLength / 2;
        
        // Calcular densidad de obstáculos basada en distancia
        float gameProgress = segmentZ / 1000f;
        float obstacleDensity = obstacleDensityCurve.Evaluate(gameProgress);
        
        // Generar obstáculos
        GenerateObstacles(segment, segmentStart, segmentEnd, obstacleDensity);
        
        // Generar monedas
        GenerateCoins(segment, segmentStart, segmentEnd);
        
        // Generar power-ups SOLO si el pool está configurado
        if (powerUpPool.prefab != null && powerUpPool.poolSize > 0)
        {
            if (Random.value < 0.1f)
            {
                GeneratePowerUp(segment, segmentStart, segmentEnd);
            }
        }
    }
    
    void GenerateObstacles(Transform parent, float startZ, float endZ, float density)
    {
        if (obstaclePool.prefab == null) return;
        
        int maxObstacles = Mathf.FloorToInt((endZ - startZ) / minObstacleDistance * density);
        int obstacleCount = Random.Range(1, Mathf.Max(2, maxObstacles + 1));
        
        List<float> usedPositions = new List<float>();
        
        for (int i = 0; i < obstacleCount; i++)
        {
            // Encontrar posición no ocupada
            float obstacleZ;
            int attempts = 0;
            do
            {
                obstacleZ = Random.Range(startZ + 1f, endZ - 1f);
                attempts++;
            } while (IsPositionTooClose(obstacleZ, usedPositions, minObstacleDistance) && attempts < 10);
            
            if (attempts >= 10) continue;
            
            usedPositions.Add(obstacleZ);
            
            // Seleccionar carril
            int laneIndex = Random.Range(0, numberOfLanes);
            float xPosition = (laneIndex - (numberOfLanes - 1) / 2f) * laneWidth;
            
            // Obtener obstáculo del pool
            GameObject obstacle = GetFromPool(obstaclePool);
            if (obstacle == null) continue;
            
            obstacle.transform.position = new Vector3(xPosition, 0.5f, obstacleZ);
            obstacle.transform.parent = parent;
            obstacle.SetActive(true);
            
            // Rotación aleatoria
            obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }
    }
    
    void GenerateCoins(Transform parent, float startZ, float endZ)
    {
        if (coinPool.prefab == null) return;
        
        int coinCount = Random.Range(5, 12);
        float coinsPerRow = 3f;

        for (int i = 0; i < coinCount; i++)
        {
            float coinZ = Random.Range(startZ + 1f, endZ - 1f);
            int pattern = Random.Range(0, 3);

            switch (pattern)
            {
                case 0: // Fila recta
                    for (int laneIndex = 0; laneIndex < numberOfLanes; laneIndex++)
                    {
                        float laneXPos = (laneIndex - (numberOfLanes - 1) / 2f) * laneWidth;
                        SpawnCoin(parent, laneXPos, coinZ);
                    }
                    break;

                case 1: // Patrón zigzag
                    int startLane = Random.Range(0, numberOfLanes);
                    for (int j = 0; j < coinsPerRow; j++)
                    {
                        float zigzagXPos = ((startLane + j) % numberOfLanes - (numberOfLanes - 1) / 2f) * laneWidth;
                        SpawnCoin(parent, zigzagXPos, coinZ + j * 0.5f);
                    }
                    break;

                case 2: // Monedas individuales
                    int singleLane = Random.Range(0, numberOfLanes);
                    float singleXPos = (singleLane - (numberOfLanes - 1) / 2f) * laneWidth;
                    SpawnCoin(parent, singleXPos, coinZ);
                    break;
            }
        }
    }
    
    void SpawnCoin(Transform parent, float x, float z)
    {
        GameObject coin = GetFromPool(coinPool);
        if (coin == null) return;
        
        coin.transform.position = new Vector3(x, 1f, z);
        coin.transform.parent = parent;
        coin.SetActive(true);
        
        // Animación rotación
        coin.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }
    
    void GeneratePowerUp(Transform parent, float startZ, float endZ)
    {
        if (powerUpPool.prefab == null || powerUpPool.poolSize <= 0)
            return;
        
        float powerUpZ = Random.Range(startZ + 2f, endZ - 2f);
        int laneIndex = Random.Range(0, numberOfLanes);
        float xPosition = (laneIndex - (numberOfLanes - 1) / 2f) * laneWidth;
        
        GameObject powerUp = GetFromPool(powerUpPool);
        if (powerUp == null) return;
        
        powerUp.transform.position = new Vector3(xPosition, 1f, powerUpZ);
        powerUp.transform.parent = parent;
        powerUp.SetActive(true);
    }
    
    bool IsPositionTooClose(float pos, List<float> usedPositions, float minDistance)
    {
        foreach (float usedPos in usedPositions)
        {
            if (Mathf.Abs(pos - usedPos) < minDistance)
                return true;
        }
        return false;
    }
    
    void RecycleOldSegments()
    {
        if (player == null) return;
        
        float recycleZ = player.position.z - (segmentsBehind * groundSegmentLength);
        
        // Crear copia de la lista para evitar modificar mientras iteramos
        List<float> segmentsToRemove = new List<float>();
        
        foreach (float segmentZ in segmentStartZ)
        {
            if (segmentZ < recycleZ)
            {
                segmentsToRemove.Add(segmentZ);
            }
        }
        
        // Eliminar segmentos
        foreach (float segmentZ in segmentsToRemove)
        {
            RecycleSegmentAtZ(segmentZ);
            segmentStartZ.Remove(segmentZ);
        }
    }
    
    void RecycleSegmentAtZ(float segmentZ)
    {
        // Buscar el segmento
        foreach (GameObject segment in groundPool.activeObjects.ToArray())
        {
            if (segment != null && Mathf.Abs(segment.transform.position.z - segmentZ) < 0.1f)
            {
                // Reciclar hijos
                RecycleChildren(segment.transform);
                
                // Retornar segmento al pool
                ReturnToPool(groundPool, segment);
                break;
            }
        }
    }
    
    void RecycleChildren(Transform parent)
    {
        if (parent == null) return;
        
        // Usar lista temporal
        List<Transform> children = new List<Transform>();
        foreach (Transform child in parent)
        {
            children.Add(child);
        }
        
        foreach (Transform child in children)
        {
            if (child == null) continue;
            
            if (child.CompareTag("Obstacle"))
            {
                ReturnToPool(obstaclePool, child.gameObject);
            }
            else if (child.CompareTag("Coin"))
            {
                ReturnToPool(coinPool, child.gameObject);
            }
            else if (child.CompareTag("PowerUp") && powerUpPool.prefab != null)
            {
                ReturnToPool(powerUpPool, child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    // Método para limpiar todo
    public void ResetGenerator()
    {
        // Retornar todo al pool
        foreach (GameObject segment in groundPool.activeObjects.ToArray())
        {
            RecycleChildren(segment.transform);
            ReturnToPool(groundPool, segment);
        }
        
        segmentStartZ.Clear();
        nextGroundZ = 0f;
        
        // Regenerar suelo inicial
        for (int i = 0; i < segmentsAhead; i++)
        {
            GenerateNextSegment();
        }
    }
    
    // Debug
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(0, 0, nextGroundZ), 2f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(-10, 0, player.position.z - (segmentsBehind * groundSegmentLength)),
            new Vector3(10, 0, player.position.z - (segmentsBehind * groundSegmentLength))
        );
    }
}