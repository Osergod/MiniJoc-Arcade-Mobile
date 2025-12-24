using System.Collections.Generic;
using UnityEngine;

public class AdvancedGroundGenerator : MonoBehaviour
{
    [System.Serializable]
    public class GroundPool
    {
        public GameObject prefab;
        public int poolSize;
        public Queue<GameObject> availableObjects = new Queue<GameObject>();
        public List<GameObject> activeObjects = new List<GameObject>();
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
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        
        // Inicializar pools
        InitializePool(groundPool);
        InitializePool(obstaclePool);
        InitializePool(coinPool);
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
        for (int i = 0; i < pool.poolSize; i++)
        {
            GameObject obj = Instantiate(pool.prefab, Vector3.zero, Quaternion.identity, transform);
            obj.SetActive(false);
            pool.availableObjects.Enqueue(obj);
        }
    }
    
    GameObject GetFromPool(GroundPool pool)
    {
        if (pool.availableObjects.Count == 0)
        {
            // Crear nuevo objeto si el pool está vacío
            GameObject newObj = Instantiate(pool.prefab, Vector3.zero, Quaternion.identity, transform);
            pool.availableObjects.Enqueue(newObj);
        }
        
        GameObject obj = pool.availableObjects.Dequeue();
        pool.activeObjects.Add(obj);
        return obj;
    }
    
    void ReturnToPool(GroundPool pool, GameObject obj)
    {
        obj.SetActive(false);
        pool.activeObjects.Remove(obj);
        pool.availableObjects.Enqueue(obj);
    }
    
    void GenerateNextSegment()
    {
        // Obtener segmento de suelo del pool
        GameObject groundSegment = GetFromPool(groundPool);
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
        float gameProgress = segmentZ / 1000f; // Ajusta según tu juego
        float obstacleDensity = obstacleDensityCurve.Evaluate(gameProgress);
        
        // Generar obstáculos
        GenerateObstacles(segment, segmentStart, segmentEnd, obstacleDensity);
        
        // Generar monedas
        GenerateCoins(segment, segmentStart, segmentEnd);
        
        // Generar power-ups (menos frecuentes)
        if (Random.value < 0.1f) // 10% de chance por segmento
        {
            GeneratePowerUp(segment, segmentStart, segmentEnd);
        }
    }
    
    void GenerateObstacles(Transform parent, float startZ, float endZ, float density)
    {
        int maxObstacles = Mathf.FloorToInt((endZ - startZ) / minObstacleDistance * density);
        int obstacleCount = Random.Range(1, maxObstacles + 1);
        
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
            int lane = Random.Range(0, numberOfLanes);
            float xPos = (lane - (numberOfLanes - 1) / 2f) * laneWidth;
            
            // Obtener obstáculo del pool
            GameObject obstacle = GetFromPool(obstaclePool);
            obstacle.transform.position = new Vector3(xPos, 0.5f, obstacleZ);
            obstacle.transform.parent = parent;
            obstacle.SetActive(true);
            
            // Rotación aleatoria
            obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }
    }
    
    void GenerateCoins(Transform parent, float startZ, float endZ)
    {
        int coinCount = Random.Range(5, 12);
        float coinsPerRow = 3f; // Coins en fila
        
        for (int i = 0; i < coinCount; i++)
        {
            float coinZ = Random.Range(startZ + 1f, endZ - 1f);
            int pattern = Random.Range(0, 3);
            
            switch (pattern)
            {
                case 0: // Fila recta
                    for (int lane = 0; lane < numberOfLanes; lane++)
                    {
                        float xPos = (lane - (numberOfLanes - 1) / 2f) * laneWidth;
                        SpawnCoin(parent, xPos, coinZ);
                    }
                    break;
                    
                case 1: // Patrón zigzag
                    int startLane = Random.Range(0, numberOfLanes);
                    for (int j = 0; j < coinsPerRow; j++)
                    {
                        float xPos = ((startLane + j) % numberOfLanes - (numberOfLanes - 1) / 2f) * laneWidth;
                        SpawnCoin(parent, xPos, coinZ + j * 0.5f);
                    }
                    break;
                    
                case 2: // Monedas individuales
                    int lane = Random.Range(0, numberOfLanes);
                    float xPos = (lane - (numberOfLanes - 1) / 2f) * laneWidth;
                    SpawnCoin(parent, xPos, coinZ);
                    break;
            }
        }
    }
    
    void SpawnCoin(Transform parent, float x, float z)
    {
        GameObject coin = GetFromPool(coinPool);
        coin.transform.position = new Vector3(x, 1f, z);
        coin.transform.parent = parent;
        coin.SetActive(true);
        
        // Animación rotación
        coin.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }
    
    void GeneratePowerUp(Transform parent, float startZ, float endZ)
    {
        float powerUpZ = Random.Range(startZ + 2f, endZ - 2f);
        int lane = Random.Range(0, numberOfLanes);
        float xPos = (lane - (numberOfLanes - 1) / 2f) * laneWidth;
        
        GameObject powerUp = GetFromPool(powerUpPool);
        powerUp.transform.position = new Vector3(xPos, 1f, powerUpZ);
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
        float recycleZ = player.position.z - (segmentsBehind * groundSegmentLength);
        
        for (int i = segmentStartZ.Count - 1; i >= 0; i--)
        {
            if (segmentStartZ[i] < recycleZ)
            {
                // Reciclar todos los objetos en este segmento
                RecycleSegmentAtZ(segmentStartZ[i]);
                segmentStartZ.RemoveAt(i);
            }
        }
    }
    
    void RecycleSegmentAtZ(float segmentZ)
    {
        // Buscar y reciclar todos los objetos en este segmento Z
        // Esto requiere que los hijos tengan referencias o tags específicos
        // Implementación simplificada:
        foreach (GameObject segment in groundPool.activeObjects.ToArray())
        {
            if (Mathf.Abs(segment.transform.position.z - segmentZ) < 0.1f)
            {
                // Reciclar hijos (obstáculos, monedas, etc.)
                RecycleChildren(segment.transform);
                ReturnToPool(groundPool, segment);
            }
        }
    }
    
    void RecycleChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag("Obstacle"))
                ReturnToPool(obstaclePool, child.gameObject);
            else if (child.CompareTag("Coin"))
                ReturnToPool(coinPool, child.gameObject);
            else if (child.CompareTag("PowerUp"))
                ReturnToPool(powerUpPool, child.gameObject);
        }
    }
    
    // Método para limpiar todo
    public void ResetGenerator()
    {
        foreach (GameObject segment in groundPool.activeObjects.ToArray())
        {
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
}