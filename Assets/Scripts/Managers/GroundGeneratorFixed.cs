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
    
    [System.Serializable]
    public class ObstacleConfig
    {
        public GameObject prefab;
        public ObstacleType.Type obstacleType;
        [Range(0f, 1f)] public float spawnChance = 0.33f;
        public int minPerSegment = 0;
        public int maxPerSegment = 2;
        public bool coinsBelow = false; // Para High obstacles: monedas ABAJO
    }
    
    [Header("References")]
    public Transform player;
    
    [Header("Ground")]
    public GameObject groundPrefab;
    public float groundLength = 10f;
    public int segmentsAhead = 8;
    
    [Header("Objects")]
    public GameObject coinPrefab;
    
    [Header("Multiple Obstacles")]
    public ObstacleConfig[] obstacleConfigs;
    
    [Header("Lane System")]
    public float[] lanePositions = { -2.5f, 0f, 2.5f };
    public int lanesPerSegment = 4;
    
    [Header("Generation Rules")]
    [Range(0f, 1f)] public float totalObstacleChance = 0.25f;
    [Range(0f, 1f)] public float coinChance = 0.4f;
    public int maxTotalObstaclesPerSegment = 3;
    public int maxCoinsPerSegment = 8;
    
    [Header("Height Settings")]
    public float groundCoinHeight = 1f;
    public float obstacleCoinHeight = 2.5f;
    public float belowObstacleCoinHeight = 0.3f; // Monedas ABAJO de High obstacles
    
    // Track de objetos por celda
    private Dictionary<Vector2Int, GameObject> cellObstacles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> cellCoins = new Dictionary<Vector2Int, GameObject>();
    
    // Pools
    private Dictionary<ObstacleType.Type, ObjectPool> obstaclePools = new Dictionary<ObstacleType.Type, ObjectPool>();
    private ObjectPool groundPool = new ObjectPool();
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
        InitializePool(coinPool, coinPrefab, 40);
        
        foreach (var config in obstacleConfigs)
        {
            if (config.prefab != null)
            {
                ObjectPool pool = new ObjectPool();
                InitializePool(pool, config.prefab, 10);
                obstaclePools[config.obstacleType] = pool;
            }
        }
        
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
    
    GameObject GetObstacleByType(ObstacleType.Type type)
    {
        if (obstaclePools.ContainsKey(type))
        {
            return GetFromPool(obstaclePools[type]);
        }
        return null;
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
        float laneLength = groundLength / lanesPerSegment;
        
        PlaceMultipleObstaclesInSegment(segment, segmentIndex, segmentStartZ, laneLength);
        PlaceCoinsInSegment(segment, segmentIndex, segmentStartZ, laneLength);
    }
    
    void PlaceMultipleObstaclesInSegment(Transform segment, int segmentIndex, float segmentStartZ, float laneLength)
    {
        if (obstacleConfigs.Length == 0) return;
        
        List<Vector2Int> availableCells = GetAllSegmentCells(segmentIndex);
        ShuffleList(availableCells);
        
        int totalObstaclesPlaced = 0;
        Dictionary<ObstacleType.Type, int> obstaclesPlacedByType = new Dictionary<ObstacleType.Type, int>();
        
        foreach (var config in obstacleConfigs)
        {
            obstaclesPlacedByType[config.obstacleType] = 0;
        }
        
        foreach (Vector2Int cell in availableCells)
        {
            if (totalObstaclesPlaced >= maxTotalObstaclesPerSegment) break;
            
            if (Random.value <= totalObstacleChance)
            {
                ObstacleType.Type selectedType = SelectObstacleType();
                var config = GetConfigByType(selectedType);
                
                if (config != null && obstaclesPlacedByType[selectedType] >= config.maxPerSegment)
                {
                    continue;
                }
                
                bool canPlace = true;
                
                if (selectedType == ObstacleType.Type.Long)
                {
                    canPlace = CanPlaceLongObstacle(cell, segmentIndex);
                }
                else if (selectedType == ObstacleType.Type.Wide)
                {
                    canPlace = CanPlaceWideObstacle(cell);
                }
                else if (selectedType == ObstacleType.Type.High)
                {
                    canPlace = CanPlaceHighObstacle(cell, segmentIndex);
                }
                
                if (canPlace && CreateObstacleAtCell(segment, cell, segmentStartZ, laneLength, selectedType))
                {
                    totalObstaclesPlaced++;
                    obstaclesPlacedByType[selectedType]++;
                    
                    if (selectedType == ObstacleType.Type.Long)
                    {
                        MarkLongObstacleCells(cell, segmentIndex);
                    }
                    else if (selectedType == ObstacleType.Type.Wide)
                    {
                        MarkWideObstacleCells(cell);
                    }
                    else if (selectedType == ObstacleType.Type.High)
                    {
                        MarkHighObstacleCells(cell);
                    }
                }
            }
        }
    }
    
    ObstacleType.Type SelectObstacleType()
    {
        float totalWeight = 0f;
        foreach (var config in obstacleConfigs)
        {
            totalWeight += config.spawnChance;
        }
        
        float randomPoint = Random.value * totalWeight;
        float currentWeight = 0f;
        
        foreach (var config in obstacleConfigs)
        {
            currentWeight += config.spawnChance;
            if (randomPoint <= currentWeight)
            {
                return config.obstacleType;
            }
        }
        
        return obstacleConfigs[0].obstacleType;
    }
    
    ObstacleConfig GetConfigByType(ObstacleType.Type type)
    {
        foreach (var config in obstacleConfigs)
        {
            if (config.obstacleType == type)
            {
                return config;
            }
        }
        return null;
    }
    
    bool CanPlaceLongObstacle(Vector2Int startCell, int segmentIndex)
    {
        int startZIndex = startCell.y % lanesPerSegment;
        
        if (startZIndex > lanesPerSegment - 3)
        {
            return false;
        }
        
        for (int zOffset = 0; zOffset < 3; zOffset++)
        {
            Vector2Int checkCell = new Vector2Int(
                startCell.x,
                segmentIndex * lanesPerSegment + (startZIndex + zOffset)
            );
            
            if (cellObstacles.ContainsKey(checkCell) && cellObstacles[checkCell] != null)
            {
                return false;
            }
        }
        
        return true;
    }
    
    bool CanPlaceWideObstacle(Vector2Int startCell)
    {
        int laneIndex = startCell.x;
        
        if (laneIndex > lanePositions.Length - 3)
        {
            return false;
        }
        
        for (int xOffset = 0; xOffset < 3; xOffset++)
        {
            Vector2Int checkCell = new Vector2Int(
                laneIndex + xOffset,
                startCell.y
            );
            
            if (cellObstacles.ContainsKey(checkCell) && cellObstacles[checkCell] != null)
            {
                return false;
            }
        }
        
        return true;
    }
    
    bool CanPlaceHighObstacle(Vector2Int cell, int segmentIndex)
    {
        // High obstacle ocupa 3x3 celdas
        int laneIndex = cell.x;
        int startZIndex = cell.y % lanesPerSegment;
        
        if (laneIndex > lanePositions.Length - 3 || startZIndex > lanesPerSegment - 3)
        {
            return false;
        }
        
        for (int xOffset = 0; xOffset < 3; xOffset++)
        {
            for (int zOffset = 0; zOffset < 3; zOffset++)
            {
                Vector2Int checkCell = new Vector2Int(
                    laneIndex + xOffset,
                    segmentIndex * lanesPerSegment + (startZIndex + zOffset)
                );
                
                if (cellObstacles.ContainsKey(checkCell) && cellObstacles[checkCell] != null)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    void MarkLongObstacleCells(Vector2Int startCell, int segmentIndex)
    {
        int startZIndex = startCell.y % lanesPerSegment;
        
        for (int zOffset = 1; zOffset < 3; zOffset++)
        {
            Vector2Int cell = new Vector2Int(
                startCell.x,
                segmentIndex * lanesPerSegment + (startZIndex + zOffset)
            );
            
            cellObstacles[cell] = null;
        }
    }
    
    void MarkWideObstacleCells(Vector2Int startCell)
    {
        for (int xOffset = 1; xOffset < 3; xOffset++)
        {
            Vector2Int cell = new Vector2Int(
                startCell.x + xOffset,
                startCell.y
            );
            
            cellObstacles[cell] = null;
        }
    }
    
    void MarkHighObstacleCells(Vector2Int startCell)
    {
        // Marcar todas las 9 celdas del obstáculo alto
        for (int xOffset = 1; xOffset < 3; xOffset++)
        {
            for (int zOffset = 0; zOffset < 3; zOffset++)
            {
                Vector2Int cell = new Vector2Int(
                    startCell.x + xOffset,
                    startCell.y + zOffset
                );
                
                if (xOffset != 0 || zOffset != 0) // No marcar la celda central otra vez
                {
                    cellObstacles[cell] = null;
                }
            }
        }
    }
    
    bool CreateObstacleAtCell(Transform segment, Vector2Int cell, float segmentStartZ, float laneLength, ObstacleType.Type type)
    {
        if (cellObstacles.ContainsKey(cell) && cellObstacles[cell] != null)
        {
            return false;
        }
        
        Vector3 worldPos = CellToWorldPosition(cell, segmentStartZ, laneLength, 0.5f);
        
        // Ajustar posición para High obstacle (más alto)
        if (type == ObstacleType.Type.High)
        {
            worldPos.y = 2.0f; // Más alto para pasar por debajo
        }
        
        GameObject obstacle = GetObstacleByType(type);
        if (obstacle == null) return false;
        
        ObstacleType obstacleScript = obstacle.GetComponent<ObstacleType>();
        if (obstacleScript != null)
        {
            obstacleScript.obstacleType = type;
        }
        
        obstacle.transform.position = worldPos;
        obstacle.transform.parent = segment;
        obstacle.SetActive(true);
        
        cellObstacles[cell] = obstacle;
        
        return true;
    }
    
    void PlaceCoinsInSegment(Transform segment, int segmentIndex, float segmentStartZ, float laneLength)
    {
        if (coinPrefab == null) return;
        
        int coinsPlaced = 0;
        List<Vector2Int> allCells = GetAllSegmentCells(segmentIndex);
        ShuffleList(allCells);
        
        foreach (Vector2Int cell in allCells)
        {
            if (coinsPlaced >= maxCoinsPerSegment) break;
            
            if (Random.value <= coinChance)
            {
                if (CreateCoinAtCell(segment, cell, segmentStartZ, laneLength))
                {
                    coinsPlaced++;
                }
            }
        }
    }
    
    bool CreateCoinAtCell(Transform segment, Vector2Int cell, float segmentStartZ, float laneLength)
    {
        if (cellCoins.ContainsKey(cell))
        {
            return false;
        }
        
        // Verificar si hay obstáculo en esta celda
        bool hasObstacle = cellObstacles.ContainsKey(cell) && cellObstacles[cell] != null;
        
        float height = groundCoinHeight;
        
        if (hasObstacle)
        {
            GameObject obstacle = cellObstacles[cell];
            ObstacleType obstacleScript = obstacle.GetComponent<ObstacleType>();
            
            if (obstacleScript != null)
            {
                var config = GetConfigByType(obstacleScript.obstacleType);
                
                if (config != null && config.coinsBelow && obstacleScript.obstacleType == ObstacleType.Type.High)
                {
                    // Para High obstacles: moneda ABAJO (a ras de suelo)
                    height = belowObstacleCoinHeight;
                }
                else
                {
                    // Para otros obstáculos: moneda ENCIMA
                    height = obstacleCoinHeight;
                }
            }
            else
            {
                // Obstáculo sin script: moneda encima por defecto
                height = obstacleCoinHeight;
            }
        }
        
        Vector3 worldPos = CellToWorldPosition(cell, segmentStartZ, laneLength, height);
        
        GameObject coin = GetFromPool(coinPool);
        if (coin == null) return false;
        
        coin.transform.position = worldPos;
        coin.transform.parent = segment;
        coin.SetActive(true);
        
        cellCoins[cell] = coin;
        
        return true;
    }
    
    Vector3 CellToWorldPosition(Vector2Int cell, float segmentStartZ, float laneLength, float yHeight)
    {
        int laneIndex = cell.x;
        int laneZIndex = cell.y % lanesPerSegment;
        int segmentIndex = cell.y / lanesPerSegment;
        
        float segmentOffset = (currentSegmentIndex - 1 - segmentIndex) * groundLength;
        float zPos = nextZ - groundLength - segmentOffset + (laneZIndex * laneLength) + (laneLength / 2);
        
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
        // Buscar y remover objetos de este segmento
        List<Vector2Int> cellsToRemove = new List<Vector2Int>();
        
        foreach (var kvp in cellObstacles)
        {
            if (kvp.Value != null && kvp.Value.transform.parent == segment)
            {
                ReturnObstacleToPool(kvp.Value);
                cellsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var cell in cellsToRemove)
        {
            cellObstacles.Remove(cell);
        }
        
        cellsToRemove.Clear();
        
        foreach (var kvp in cellCoins)
        {
            if (kvp.Value != null && kvp.Value.transform.parent == segment)
            {
                ReturnToPool(coinPool, kvp.Value);
                cellsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var cell in cellsToRemove)
        {
            cellCoins.Remove(cell);
        }
        
        // Limpiar null markers
        cellsToRemove.Clear();
        foreach (var kvp in cellObstacles)
        {
            if (kvp.Value == null)
            {
                cellsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var cell in cellsToRemove)
        {
            cellObstacles.Remove(cell);
        }
        
        ReturnToPool(groundPool, segment);
    }
    
    void ReturnObstacleToPool(GameObject obstacle)
    {
        if (obstacle == null) return;
        
        ObstacleType obstacleScript = obstacle.GetComponent<ObstacleType>();
        if (obstacleScript != null && obstaclePools.ContainsKey(obstacleScript.obstacleType))
        {
            ReturnToPool(obstaclePools[obstacleScript.obstacleType], obstacle);
        }
        else
        {
            Destroy(obstacle);
        }
    }
}