using System.Collections.Generic;
using UnityEngine;

public class GroundGeneratorFixed : MonoBehaviour
{
    [System.Serializable]
    public class ObjectPool
    {
        public GameObject prefab;
        public int poolSize;
        [HideInInspector] public Queue<GameObject> available = new Queue<GameObject>();
    }
    
    [Header("References")]
    public Transform player;
    
    [Header("Ground")]
    public GameObject groundPrefab;
    public float groundLength = 10f;
    public int segmentsAhead = 8;
    
    [Header("Objects")]
    public GameObject obstaclePrefab;
    public GameObject coinPrefab;
    
    [Header("Spacing")]
    public float minObstacleDistance = 5f;
    public float minCoinDistance = 2f;
    
    [Header("Lanes")]
    public float[] lanePositions = { -2.5f, 0f, 2.5f };
    
    [Header("Generation")]
    public int maxObstaclesPerSegment = 2;
    public int maxCoinsPerSegment = 6;
    
    // Pools
    private ObjectPool groundPool = new ObjectPool();
    private ObjectPool obstaclePool = new ObjectPool();
    private ObjectPool coinPool = new ObjectPool();
    
    // State
    private float nextZ = 0f;
    private Queue<GameObject> activeSegments = new Queue<GameObject>();
    
    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
        
        InitializePool(groundPool, groundPrefab, 12);
        InitializePool(obstaclePool, obstaclePrefab, 20);
        InitializePool(coinPool, coinPrefab, 40);
        
        for (int i = 0; i < segmentsAhead; i++)
        {
            GenerateSegment();
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        if (player.position.z > nextZ - (segmentsAhead * groundLength * 0.7f))
        {
            GenerateSegment();
            
            if (activeSegments.Count > segmentsAhead)
            {
                RecycleSegment(activeSegments.Dequeue());
            }
        }
    }
    
    void InitializePool(ObjectPool pool, GameObject prefab, int size)
    {
        pool.prefab = prefab;
        pool.poolSize = size;
        
        if (prefab == null) return;
        
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            obj.SetActive(false);
            pool.available.Enqueue(obj);
        }
    }
    
    GameObject GetFromPool(ObjectPool pool)
    {
        if (pool.prefab == null) return null;
        
        if (pool.available.Count == 0)
        {
            GameObject newObj = Instantiate(pool.prefab, Vector3.zero, Quaternion.identity, transform);
            return newObj;
        }
        
        return pool.available.Dequeue();
    }
    
    void ReturnToPool(ObjectPool pool, GameObject obj)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        obj.transform.parent = transform;
        pool.available.Enqueue(obj);
    }
    
    void GenerateSegment()
    {
        GameObject ground = GetFromPool(groundPool);
        if (ground == null) return;
        
        ground.transform.position = new Vector3(0, 0, nextZ);
        ground.SetActive(true);
        activeSegments.Enqueue(ground);
        
        PlaceObjects(ground.transform);
        
        nextZ += groundLength;
    }
    
    void PlaceObjects(Transform segment)
    {
        float segmentStart = nextZ - groundLength;
        float segmentEnd = nextZ;
        
        List<Vector3> placedObstacles = new List<Vector3>();
        List<Vector3> placedCoins = new List<Vector3>();
        
        // Obstáculos
        for (int i = 0; i < maxObstaclesPerSegment; i++)
        {
            Vector3? obstaclePos = FindPosition(segmentStart, segmentEnd, placedObstacles, placedCoins, 
                                              minObstacleDistance, true);
            
            if (obstaclePos.HasValue)
            {
                GameObject obstacle = GetFromPool(obstaclePool);
                if (obstacle != null)
                {
                    // SOLO POSICIÓN, SIN ESCALADO NI ROTACIÓN
                    obstacle.transform.position = obstaclePos.Value;
                    obstacle.transform.parent = segment;
                    obstacle.SetActive(true);
                    placedObstacles.Add(obstaclePos.Value);
                }
            }
        }
        
        // Monedas
        for (int i = 0; i < maxCoinsPerSegment; i++)
        {
            Vector3? coinPos = FindPosition(segmentStart, segmentEnd, placedObstacles, placedCoins, 
                                          minCoinDistance, false);
            
            if (coinPos.HasValue)
            {
                GameObject coin = GetFromPool(coinPool);
                if (coin != null)
                {
                    // SOLO POSICIÓN, SIN ESCALADO NI ROTACIÓN
                    coin.transform.position = coinPos.Value;
                    coin.transform.parent = segment;
                    coin.SetActive(true);
                    placedCoins.Add(coinPos.Value);
                }
            }
        }
    }
    
    Vector3? FindPosition(float startZ, float endZ, List<Vector3> obstacles, List<Vector3> coins, 
                         float minDistance, bool isObstacle)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float z = Random.Range(startZ + 1f, endZ - 1f);
            float x = lanePositions[Random.Range(0, lanePositions.Length)];
            float y = isObstacle ? 0.5f : 1f;
            
            Vector3 pos = new Vector3(x, y, z);
            
            bool valid = true;
            
            // Check obstacles
            foreach (Vector3 obstacle in obstacles)
            {
                if (Vector3.Distance(pos, obstacle) < minDistance)
                {
                    valid = false;
                    break;
                }
            }
            
            if (!valid) continue;
            
            // Check coins (solo para obstáculos)
            if (isObstacle)
            {
                foreach (Vector3 coin in coins)
                {
                    if (Vector3.Distance(pos, coin) < minDistance)
                    {
                        valid = false;
                        break;
                    }
                }
            }
            
            if (valid) return pos;
        }
        
        return null;
    }
    
    void RecycleSegment(GameObject segment)
    {
        foreach (Transform child in segment.transform)
        {
            if (child.CompareTag("Obstacle"))
                ReturnToPool(obstaclePool, child.gameObject);
            else if (child.CompareTag("Coin"))
                ReturnToPool(coinPool, child.gameObject);
            else
                Destroy(child.gameObject);
        }
        
        ReturnToPool(groundPool, segment);
    }
}