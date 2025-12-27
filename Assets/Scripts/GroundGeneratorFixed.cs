using UnityEngine;
using System.Collections.Generic;

public class GroundGeneratorFixed : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject groundPrefab;
    public GameObject coinPrefab;
    public GameObject obstacleBasePrefab; // SOLO UN PREFAB BASE (cubo 1x1x1)
    
    [Header("Settings")]
    public Transform player;
    public float groundLength = 10f;
    public int segmentsAhead = 8;
    public float[] lanePositions = { -2.5f, 0f, 2.5f };
    public int cellsPerSegment = 8;
    
    [Header("Probabilities")]
    [Range(0f, 1f)] public float coinProbability = 0.3f;
    [Range(0f, 1f)] public float obstacleProbability = 0.2f;
    public float noObstacleStartTime = 5f;
    
    [Header("Obstacle Dimensions")]
    public Vector3 wideObstacleScale = new Vector3(2f, 1f, 1f);   // Ancho
    public Vector3 longObstacleScale = new Vector3(1f, 1f, 2f);   // Largo
    public Vector3 highObstacleScale = new Vector3(1f, 2f, 1f);   // Alto
    
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
        
        // Inicializar mapa de celdas ocupadas para este segmento
        occupiedCellsMap[currentSegmentIndex] = new bool[cellsPerSegment, lanePositions.Length];
        
        GenerateObjectsForSegment(segment.transform, currentSegmentIndex);
        
        nextZ += groundLength;
        currentSegmentIndex++;
    }
    
    void DestroyOldSegment()
    {
        GameObject oldSegment = activeSegments.Dequeue();
        
        // Limpiar mapa de celdas ocupadas
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
        
        for (int cellIndex = 0; cellIndex < cellsPerSegment; cellIndex++)
        {
            float cellZ = segmentStartZ + (cellIndex * cellLength) + (cellLength / 2f);
            
            for (int laneIndex = 0; laneIndex < lanePositions.Length; laneIndex++)
            {
                // Saltar si la celda ya está ocupada
                if (occupiedCellsMap[segmentIndex][cellIndex, laneIndex])
                    continue;
                    
                float laneX = lanePositions[laneIndex];
                
                DecideAndPlaceObject(segment, laneX, cellZ, laneIndex, segmentIndex, cellIndex);
            }
        }
    }
    
    void DecideAndPlaceObject(Transform segment, float xPos, float zPos, int laneIndex, 
                              int segmentIndex, int cellIndex)
    {
        float randomValue = Random.value;
        float totalProbability = coinProbability + obstacleProbability;
        
        if (randomValue < coinProbability)
        {
            PlaceCoin(segment, xPos, zPos);
        }
        else if (randomValue < totalProbability && CanSpawnObstacles())
        {
            PlaceObstacle(segment, xPos, zPos, laneIndex, segmentIndex, cellIndex);
        }
        // Si no, dejar vacío
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
        coin.transform.localScale = Vector3.one; // Forzar escala
    }
    
    void PlaceObstacle(Transform segment, float xPos, float zPos, int laneIndex, 
                       int segmentIndex, int cellIndex)
    {
        if (obstacleBasePrefab == null) return;
        
        // Seleccionar tipo de obstáculo
        ObstacleType.Type obstacleType = SelectObstacleType(laneIndex);
        
        // Verificar si cabe
        if (!CanPlaceObstacle(obstacleType, segmentIndex, cellIndex, laneIndex))
            return;
        
        // Instanciar el obstáculo base
        GameObject obstacle = Instantiate(obstacleBasePrefab);
        
        // Configurar escala según tipo
        Vector3 scale = Vector3.one;
        Vector3 position = new Vector3(xPos, 0.5f, zPos);
        
        switch (obstacleType)
        {
            case ObstacleType.Type.Wide:
                scale = wideObstacleScale;
                // Ajustar posición para obstáculos anchos
                if (laneIndex == 0 || laneIndex == 1)
                    position.x = (lanePositions[laneIndex] + lanePositions[laneIndex + 1]) / 2f;
                break;
                
            case ObstacleType.Type.Long:
                scale = longObstacleScale;
                break;
                
            case ObstacleType.Type.High:
                scale = highObstacleScale;
                position.y = highObstacleScale.y / 2f; // Centrar verticalmente
                break;
        }
        
        // Aplicar transformaciones
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.transform.SetParent(segment);
        
        // Configurar el script ObstacleType
        ObstacleType obstacleScript = obstacle.GetComponent<ObstacleType>();
        if (obstacleScript == null)
            obstacleScript = obstacle.AddComponent<ObstacleType>();
            
        obstacleScript.obstacleType = obstacleType;
        
        // Marcar celdas como ocupadas
        MarkCellsAsOccupied(obstacleType, segmentIndex, cellIndex, laneIndex);
    }
    
    ObstacleType.Type SelectObstacleType(int laneIndex)
    {
        // Solo Wide y High pueden estar en carril central
        if (laneIndex == 1) // Carril central
        {
            float random = Random.value;
            if (random < 0.33f) return ObstacleType.Type.Wide;
            if (random < 0.66f) return ObstacleType.Type.High;
            return ObstacleType.Type.Long;
        }
        else // Carriles laterales
        {
            return ObstacleType.Type.Long; // Solo Long en laterales
        }
    }
    
    bool CanPlaceObstacle(ObstacleType.Type type, int segmentIndex, int cellIndex, int laneIndex)
    {
        // Determinar cuántas celdas ocupa
        int cellsWidth = 1;
        int cellsLength = 1;
        
        switch (type)
        {
            case ObstacleType.Type.Wide:
                cellsWidth = 2; // Ocupa 2 carriles de ancho
                // Verificar que no se salga de los límites
                if (laneIndex >= lanePositions.Length - 1)
                    return false;
                break;
                
            case ObstacleType.Type.Long:
                cellsLength = 2; // Ocupa 2 celdas de largo
                // Verificar que no se salga del segmento
                if (cellIndex >= cellsPerSegment - 1)
                    return false;
                break;
        }
        
        // Verificar que todas las celdas necesarias estén libres
        for (int w = 0; w < cellsWidth; w++)
        {
            for (int l = 0; l < cellsLength; l++)
            {
                int checkLane = laneIndex + w;
                int checkCell = cellIndex + l;
                
                // Verificar límites
                if (checkLane >= lanePositions.Length || checkCell >= cellsPerSegment)
                    return false;
                    
                // Verificar si está ocupado
                if (occupiedCellsMap[segmentIndex][checkCell, checkLane])
                    return false;
            }
        }
        
        return true;
    }
    
    void MarkCellsAsOccupied(ObstacleType.Type type, int segmentIndex, int cellIndex, int laneIndex)
    {
        // Determinar cuántas celdas ocupa
        int cellsWidth = 1;
        int cellsLength = 1;
        
        switch (type)
        {
            case ObstacleType.Type.Wide:
                cellsWidth = 2;
                break;
                
            case ObstacleType.Type.Long:
                cellsLength = 2;
                break;
        }
        
        // Marcar todas las celdas como ocupadas
        for (int w = 0; w < cellsWidth; w++)
        {
            for (int l = 0; l < cellsLength; l++)
            {
                int markLane = laneIndex + w;
                int markCell = cellIndex + l;
                
                if (markLane < lanePositions.Length && markCell < cellsPerSegment)
                {
                    occupiedCellsMap[segmentIndex][markCell, markLane] = true;
                }
            }
        }
    }
}