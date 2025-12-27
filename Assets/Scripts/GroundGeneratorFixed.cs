using UnityEngine;
using System.Collections.Generic;

public class GroundGeneratorFixed : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject groundPrefab;
    public GameObject coinPrefab;
    public GameObject obstacleBasePrefab;
    
    [Header("Settings")]
    public Transform player;
    public float groundLength = 10f;
    public int segmentsAhead = 8;
    public float[] lanePositions = { -2.5f, 0f, 2.5f };
    public int cellsPerSegment = 8;
    public float laneWidth = 2.5f;
    
    [Header("Probabilities")]
    [Range(0f, 1f)] public float coinProbability = 0.3f;
    [Range(0f, 1f)] public float obstacleProbability = 0.25f;
    public float noObstacleStartTime = 5f;
    
    [Header("Obstacle Dimensions")]
    public Vector3 wideObstacleScale = new Vector3(7.5f, 1f, 2f);
    public Vector3 longObstacleScale = new Vector3(2f, 1f, 4f);
    public Vector3 highObstacleScale = new Vector3(7.5f, 3f, 2f);
    
    [Header("Obstacle Placement")]
    public float highObstacleHeight = 2f;
    
    private float nextZ = 0f;
    private Queue<GameObject> activeSegments = new Queue<GameObject>();
    private float gameStartTime;
    private Dictionary<int, bool[,]> occupiedCellsMap = new Dictionary<int, bool[,]>();
    private int currentSegmentIndex = 0;
    
    void Start()
    {
        gameStartTime = Time.time;
        
        for (int i = 0; i < segmentsAhead; i++)
        {
            CreateSegment();
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        float playerDistance = nextZ - (segmentsAhead * groundLength * 0.7f);
        
        if (player.position.z > playerDistance)
        {
            CreateSegment();
            
            if (activeSegments.Count > segmentsAhead)
            {
                DestroyOldSegment();
            }
        }
    }
    
    void CreateSegment()
    {
        GameObject segment = Instantiate(groundPrefab);
        segment.transform.position = new Vector3(0f, 0f, nextZ);
        activeSegments.Enqueue(segment);
        
        occupiedCellsMap[currentSegmentIndex] = new bool[cellsPerSegment, lanePositions.Length];
        
        GenerateObjectsForSegment(segment.transform, currentSegmentIndex);
        
        nextZ += groundLength;
        currentSegmentIndex++;
    }
    
    void DestroyOldSegment()
    {
        GameObject oldSegment = activeSegments.Dequeue();
        
        int oldestSegmentIndex = currentSegmentIndex - activeSegments.Count - 1;
        if (occupiedCellsMap.ContainsKey(oldestSegmentIndex))
        {
            occupiedCellsMap.Remove(oldestSegmentIndex);
        }
        
        Destroy(oldSegment);
    }
    
    void GenerateObjectsForSegment(Transform segment, int segmentIndex)
    {
        float cellLength = groundLength / cellsPerSegment;
        float segmentStartZ = nextZ - groundLength;
        
        // Primero intentar colocar obstáculos
        for (int cellIndex = 0; cellIndex < cellsPerSegment; cellIndex++)
        {
            float cellZ = segmentStartZ + (cellIndex * cellLength) + (cellLength / 2f);
            
            // Decidir si poner obstáculo en esta celda (probabilidad más alta)
            if (Random.value < obstacleProbability && CanSpawnObstacles())
            {
                TryPlaceObstacleInCell(segment, segmentIndex, cellIndex, cellZ);
            }
        }
        
        // Luego colocar monedas en celdas libres
        for (int cellIndex = 0; cellIndex < cellsPerSegment; cellIndex++)
        {
            float cellZ = segmentStartZ + (cellIndex * cellLength) + (cellLength / 2f);
            
            for (int laneIndex = 0; laneIndex < lanePositions.Length; laneIndex++)
            {
                if (!occupiedCellsMap[segmentIndex][cellIndex, laneIndex] && 
                    Random.value < coinProbability)
                {
                    PlaceCoin(segment, lanePositions[laneIndex], cellZ);
                }
            }
        }
    }
    
    void TryPlaceObstacleInCell(Transform segment, int segmentIndex, int cellIndex, float cellZ)
    {
        // Seleccionar tipo de obstáculo
        ObstacleType.Type obstacleType = SelectObstacleType();
        
        // DEBUG: Ver qué tipo se seleccionó
        Debug.Log($"Intentando colocar obstáculo tipo: {obstacleType} en celda {cellIndex}");
        
        // Para Wide y High, siempre usar carril central y ocupar 3 carriles
        if (obstacleType == ObstacleType.Type.Wide || obstacleType == ObstacleType.Type.High)
        {
            // Verificar si hay espacio para obstáculo de 3 carriles
            if (HasSpaceForWideObstacle(segmentIndex, cellIndex))
            {
                PlaceObstacle(segment, lanePositions[1], cellZ, 1, segmentIndex, cellIndex, obstacleType);
            }
            else
            {
                // Si no hay espacio, intentar con Long en su lugar
                TryPlaceLongObstacle(segment, segmentIndex, cellIndex, cellZ);
            }
        }
        else // Long obstacle
        {
            // Intentar en cada carril hasta encontrar uno libre
            for (int laneIndex = 0; laneIndex < lanePositions.Length; laneIndex++)
            {
                if (CanPlaceLongObstacle(segmentIndex, cellIndex, laneIndex))
                {
                    PlaceObstacle(segment, lanePositions[laneIndex], cellZ, laneIndex, segmentIndex, cellIndex, obstacleType);
                    break;
                }
            }
        }
    }
    
    bool HasSpaceForWideObstacle(int segmentIndex, int cellIndex)
    {
        // Verificar que los 3 carriles estén libres en esta celda
        for (int laneIndex = 0; laneIndex < 3; laneIndex++)
        {
            if (occupiedCellsMap[segmentIndex][cellIndex, laneIndex])
                return false;
        }
        
        // También verificar celdas adyacentes por el largo del obstáculo
        int cellsNeeded = Mathf.CeilToInt(wideObstacleScale.z / (groundLength / cellsPerSegment));
        
        for (int c = 0; c < cellsNeeded; c++)
        {
            int checkCell = cellIndex + c;
            if (checkCell >= cellsPerSegment) return false;
            
            for (int laneIndex = 0; laneIndex < 3; laneIndex++)
            {
                if (occupiedCellsMap[segmentIndex][checkCell, laneIndex])
                    return false;
            }
        }
        
        return true;
    }
    
    bool CanPlaceLongObstacle(int segmentIndex, int cellIndex, int laneIndex)
    {
        // Verificar que este carril esté libre
        if (occupiedCellsMap[segmentIndex][cellIndex, laneIndex])
            return false;
        
        // Verificar celdas adyacentes por el largo
        int cellsNeeded = Mathf.CeilToInt(longObstacleScale.z / (groundLength / cellsPerSegment));
        
        for (int c = 0; c < cellsNeeded; c++)
        {
            int checkCell = cellIndex + c;
            if (checkCell >= cellsPerSegment) return false;
            
            if (occupiedCellsMap[segmentIndex][checkCell, laneIndex])
                return false;
        }
        
        return true;
    }
    
    void TryPlaceLongObstacle(Transform segment, int segmentIndex, int cellIndex, float cellZ)
    {
        // Intentar en cada carril
        List<int> availableLanes = new List<int> { 0, 1, 2 };
        
        // Mezclar los carriles para variedad
        ShuffleList(availableLanes);
        
        foreach (int laneIndex in availableLanes)
        {
            if (CanPlaceLongObstacle(segmentIndex, cellIndex, laneIndex))
            {
                PlaceObstacle(segment, lanePositions[laneIndex], cellZ, laneIndex, segmentIndex, cellIndex, ObstacleType.Type.Long);
                return;
            }
        }
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
    
    bool CanSpawnObstacles()
    {
        return Time.time - gameStartTime >= noObstacleStartTime;
    }
    
    void PlaceCoin(Transform segment, float xPos, float zPos)
    {
        if (coinPrefab == null) return;
        
        GameObject coin = Instantiate(coinPrefab);
        coin.transform.position = new Vector3(xPos, 1f, zPos);
        coin.transform.SetParent(segment);
    }
    
    void PlaceObstacle(Transform segment, float xPos, float zPos, int laneIndex, 
                       int segmentIndex, int cellIndex, ObstacleType.Type obstacleType)
    {
        if (obstacleBasePrefab == null) return;
        
        GameObject obstacle = Instantiate(obstacleBasePrefab);
        
        Vector3 scale = Vector3.one;
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        
        switch (obstacleType)
        {
            case ObstacleType.Type.Wide:
                scale = wideObstacleScale;
                position = new Vector3(lanePositions[1], wideObstacleScale.y / 2f, zPos);
                Debug.Log($"Colocando Wide en posición: {position}");
                break;
                
            case ObstacleType.Type.Long:
                scale = longObstacleScale;
                position = new Vector3(xPos, longObstacleScale.y / 2f, zPos);
                rotation = Quaternion.Euler(0f, 90f, 0f);
                Debug.Log($"Colocando Long en posición: {position}, carril: {laneIndex}");
                break;
                
            case ObstacleType.Type.High:
                scale = highObstacleScale;
                position = new Vector3(lanePositions[1], highObstacleHeight, zPos);
                Debug.Log($"Colocando High en posición: {position}");
                break;
        }
        
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.transform.rotation = rotation;
        obstacle.transform.SetParent(segment);
        
        ObstacleType obstacleScript = obstacle.GetComponent<ObstacleType>();
        if (obstacleScript == null)
            obstacleScript = obstacle.AddComponent<ObstacleType>();
            
        obstacleScript.obstacleType = obstacleType;
        
        MarkCellsAsOccupied(obstacleType, segmentIndex, cellIndex, laneIndex);
        
        // DEBUG
        Debug.Log($"Obstáculo {obstacleType} colocado exitosamente!");
    }
    
    ObstacleType.Type SelectObstacleType()
    {
        float random = Random.value;
        
        // Aumentar probabilidad de Wide y High para debugging
        if (random < 0.33f) return ObstacleType.Type.Wide;    // 33%
        if (random < 0.66f) return ObstacleType.Type.High;    // 33%
        return ObstacleType.Type.Long;                        // 34%
    }
    
    void MarkCellsAsOccupied(ObstacleType.Type type, int segmentIndex, int cellIndex, int laneIndex)
    {
        int lanesOccupied = 1;
        int cellsOccupied = 1;
        
        switch (type)
        {
            case ObstacleType.Type.Wide:
                lanesOccupied = 3;
                cellsOccupied = Mathf.Max(1, Mathf.CeilToInt(wideObstacleScale.z / (groundLength / cellsPerSegment)));
                laneIndex = 0; // Empezar desde carril 0
                break;
                
            case ObstacleType.Type.Long:
                cellsOccupied = Mathf.Max(1, Mathf.CeilToInt(longObstacleScale.z / (groundLength / cellsPerSegment)));
                break;
                
            case ObstacleType.Type.High:
                lanesOccupied = 3;
                cellsOccupied = Mathf.Max(1, Mathf.CeilToInt(highObstacleScale.z / (groundLength / cellsPerSegment)));
                laneIndex = 0;
                break;
        }
        
        for (int l = 0; l < lanesOccupied; l++)
        {
            for (int c = 0; c < cellsOccupied; c++)
            {
                int markLane = laneIndex + l;
                int markCell = cellIndex + c;
                
                if (markLane < lanePositions.Length && markCell < cellsPerSegment)
                {
                    occupiedCellsMap[segmentIndex][markCell, markLane] = true;
                    Debug.Log($"Marcando celda [{markCell}, {markLane}] como ocupada");
                }
            }
        }
    }
    
    // Método para debugging
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Dibujar celdas ocupadas
        Gizmos.color = Color.red;
        foreach (var segmentPair in occupiedCellsMap)
        {
            int segmentIndex = segmentPair.Key;
            bool[,] cells = segmentPair.Value;
            
            float segmentZ = segmentIndex * groundLength;
            
            for (int cell = 0; cell < cellsPerSegment; cell++)
            {
                for (int lane = 0; lane < lanePositions.Length; lane++)
                {
                    if (cells[cell, lane])
                    {
                        float cellZ = segmentZ + (cell * groundLength / cellsPerSegment) + (groundLength / cellsPerSegment / 2f);
                        Vector3 center = new Vector3(lanePositions[lane], 0.5f, cellZ);
                        Gizmos.DrawWireCube(center, new Vector3(2f, 1f, groundLength / cellsPerSegment));
                    }
                }
            }
        }
    }
}