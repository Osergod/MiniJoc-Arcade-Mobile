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
    
    [Header("Lane System")]
    public float[] lanePositions = { -2.5f, 0f, 2.5f };
    public int lanesPerSegment = 4;
    
    [Header("Generation Rules")]
    [Range(0f, 1f)] public float obstacleChance = 0.25f;
    [Range(0f, 1f)] public float coinChance = 0.4f;
    public int maxObstaclesPerSegment = 3;
    public int maxCoinsPerSegment = 8;
    
    [Header("Height Settings")]
    public float groundCoinHeight = 1f;
    public float obstacleCoinHeight = 2f; // Altura cuando está encima de obstáculo
    
    // Track de objetos por celda
    private Dictionary<Vector2Int, GameObject> cellObstacles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> cellCoins = new Dictionary<Vector2Int, GameObject>();
    
    // Pools
    private ObjectPool groundPool = new ObjectPool();
    private ObjectPool obstaclePool = new ObjectPool();
    private ObjectPool coinPool = new ObjectPool();
    
    // State
    private float nextZ = 0f;
    private Queue<GameObject> activeSegments = new Queue<GameObject>();
    private int currentSegmentIndex = 0;
    
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
        
        PlaceObjectsInSegment(ground.transform, currentSegmentIndex);
        
        nextZ += groundLength;
        currentSegmentIndex++;
    }
    
    void PlaceObjectsInSegment(Transform segment, int segmentIndex)
    {
        float segmentStartZ = nextZ - groundLength;
        float segmentEndZ = nextZ;
        float laneLength = groundLength / lanesPerSegment;
        
        // PRIMERO: Colocar obstáculos
        PlaceObstaclesInSegment(segment, segmentIndex, segmentStartZ, laneLength);
        
        // LUEGO: Colocar monedas (pueden estar encima de obstáculos)
        PlaceCoinsInSegment(segment, segmentIndex, segmentStartZ, laneLength);
    }
    
    void PlaceObstaclesInSegment(Transform segment, int segmentIndex, float segmentStartZ, float laneLength)
    {
        if (obstaclePrefab == null) return;
        
        int obstaclesPlaced = 0;
        List<Vector2Int> availableCells = GetAllSegmentCells(segmentIndex);
        
        // Mezclar celdas disponibles
        ShuffleList(availableCells);
        
        foreach (Vector2Int cell in availableCells)
        {
            if (obstaclesPlaced >= maxObstaclesPerSegment) break;
            
            // Aplicar probabilidad de obstáculo
            if (Random.value <= obstacleChance)
            {
                // Crear obstáculo en esta celda
                if (CreateObstacleAtCell(segment, cell, segmentStartZ, laneLength))
                {
                    obstaclesPlaced++;
                }
            }
        }
    }
    
    void PlaceCoinsInSegment(Transform segment, int segmentIndex, float segmentStartZ, float laneLength)
    {
        if (coinPrefab == null) return;
        
        int coinsPlaced = 0;
        List<Vector2Int> allCells = GetAllSegmentCells(segmentIndex);
        
        // Mezclar celdas
        ShuffleList(allCells);
        
        foreach (Vector2Int cell in allCells)
        {
            if (coinsPlaced >= maxCoinsPerSegment) break;
            
            // Aplicar probabilidad de moneda
            if (Random.value <= coinChance)
            {
                // Crear moneda en esta celda (puede estar encima de obstáculo)
                if (CreateCoinAtCell(segment, cell, segmentStartZ, laneLength))
                {
                    coinsPlaced++;
                }
            }
        }
    }
    
    bool CreateObstacleAtCell(Transform segment, Vector2Int cell, float segmentStartZ, float laneLength)
    {
        // Verificar que no haya ya un obstáculo en esta celda
        if (cellObstacles.ContainsKey(cell))
        {
            return false;
        }
        
        // Calcular posición mundial
        Vector3 worldPos = CellToWorldPosition(cell, segmentStartZ, laneLength, 0.5f);
        
        // Crear obstáculo
        GameObject obstacle = GetFromPool(obstaclePool);
        if (obstacle == null) return false;
        
        obstacle.transform.position = worldPos;
        obstacle.transform.parent = segment;
        obstacle.SetActive(true);
        
        // Registrar obstáculo en esta celda
        cellObstacles[cell] = obstacle;
        
        return true;
    }
    
    bool CreateCoinAtCell(Transform segment, Vector2Int cell, float segmentStartZ, float laneLength)
    {
        // Verificar si ya hay una moneda en esta celda
        if (cellCoins.ContainsKey(cell))
        {
            return false;
        }
        
        // Calcular altura: si hay obstáculo, moneda más alta
        float height = groundCoinHeight;
        bool hasObstacle = cellObstacles.ContainsKey(cell);
        
        if (hasObstacle)
        {
            height = obstacleCoinHeight;
        }
        
        // Calcular posición mundial
        Vector3 worldPos = CellToWorldPosition(cell, segmentStartZ, laneLength, height);
        
        // Crear moneda
        GameObject coin = GetFromPool(coinPool);
        if (coin == null) return false;
        
        coin.transform.position = worldPos;
        coin.transform.parent = segment;
        coin.SetActive(true);
        
        // Registrar moneda en esta celda
        cellCoins[cell] = coin;
        
        return true;
    }
    
    Vector3 CellToWorldPosition(Vector2Int cell, float segmentStartZ, float laneLength, float yHeight)
    {
        int laneIndex = cell.x;
        int laneZIndex = cell.y % lanesPerSegment;
        int segmentIndex = cell.y / lanesPerSegment;
        
        // Calcular posición Z basada en el índice de segmento actual
        float segmentOffset = (currentSegmentIndex - 1 - segmentIndex) * groundLength;
        float zPos = nextZ - groundLength - segmentOffset + (laneZIndex * laneLength) + (laneLength / 2);
        
        // Posición X según el carril
        float xPos = lanePositions[laneIndex];
        
        return new Vector3(xPos, yHeight, zPos);
    }
    
    List<Vector2Int> GetAllSegmentCells(int segmentIndex)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        
        for (int lane = 0; lane < lanePositions.Length; lane++)
        {
            for (int zLane = 0; zLane < lanesPerSegment; zLane++)
            {
                cells.Add(new Vector2Int(lane, segmentIndex * lanesPerSegment + zLane));
            }
        }
        
        return cells;
    }
    
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    void RecycleSegment(GameObject segment)
    {
        // Buscar todas las celdas que pertenecen a este segmento
        int segmentIndex = -1;
        
        // Determinar el índice del segmento basado en su posición Z
        for (int i = 0; i < currentSegmentIndex; i++)
        {
            float expectedZ = nextZ - groundLength * (currentSegmentIndex - i);
            if (Mathf.Abs(segment.transform.position.z - expectedZ) < 0.1f)
            {
                segmentIndex = i;
                break;
            }
        }
        
        if (segmentIndex >= 0)
        {
            // Remover obstáculos y monedas de este segmento de los diccionarios
            List<Vector2Int> cellsToRemove = new List<Vector2Int>();
            
            foreach (var kvp in cellObstacles)
            {
                if (kvp.Key.y / lanesPerSegment == segmentIndex)
                {
                    cellsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var cell in cellsToRemove)
            {
                if (cellObstacles.ContainsKey(cell))
                {
                    GameObject obstacle = cellObstacles[cell];
                    ReturnToPool(obstaclePool, obstacle);
                    cellObstacles.Remove(cell);
                }
                
                if (cellCoins.ContainsKey(cell))
                {
                    GameObject coin = cellCoins[cell];
                    ReturnToPool(coinPool, coin);
                    cellCoins.Remove(cell);
                }
            }
        }
        
        // Limpiar cualquier hijo restante
        foreach (Transform child in segment.transform)
        {
            if (child.CompareTag("Obstacle"))
                ReturnToPool(obstaclePool, child.gameObject);
            else if (child.CompareTag("Coin"))
                ReturnToPool(coinPool, child.gameObject);
            else
                Destroy(child.gameObject);
        }
        
        // Devolver segmento al pool
        ReturnToPool(groundPool, segment);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Dibujar carriles
        Gizmos.color = Color.blue;
        for (int i = 0; i < lanePositions.Length; i++)
        {
            float xPos = lanePositions[i];
            Gizmos.DrawLine(
                new Vector3(xPos, 0, player.position.z - 20),
                new Vector3(xPos, 0, player.position.z + 20)
            );
        }
        
        // Dibujar celdas con contenido
        foreach (var kvp in cellObstacles)
        {
            Vector2Int cell = kvp.Key;
            GameObject obstacle = kvp.Value;
            
            if (obstacle != null)
            {
                // Celda con obstáculo: rojo
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(
                    obstacle.transform.position,
                    new Vector3(0.8f, 1f, 0.8f)
                );
                
                // Verificar si también tiene moneda encima
                if (cellCoins.ContainsKey(cell))
                {
                    Gizmos.color = Color.yellow;
                    Vector3 coinPos = obstacle.transform.position + Vector3.up * (obstacleCoinHeight - 0.5f);
                    Gizmos.DrawWireSphere(coinPos, 0.3f);
                    Gizmos.DrawLine(obstacle.transform.position, coinPos);
                }
            }
        }
        
        // Dibujar monedas solas (sin obstáculo debajo)
        Gizmos.color = Color.green;
        foreach (var kvp in cellCoins)
        {
            Vector2Int cell = kvp.Key;
            GameObject coin = kvp.Value;
            
            // Solo dibujar si NO tiene obstáculo debajo
            if (!cellObstacles.ContainsKey(cell) && coin != null)
            {
                Gizmos.DrawWireSphere(coin.transform.position, 0.3f);
            }
        }
    }
}