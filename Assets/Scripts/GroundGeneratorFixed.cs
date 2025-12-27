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
    public float minDistanceBetweenObstacles = 0.5f;
    
    [Header("Visual Settings")]
    [Range(0f, 1f)] public float obstacleTransparency = 0.85f;
    public Color obstacleColor = new Color(0.5f, 0f, 0f, 1f); // COLOR GRANATE para todos
    
    private float nextZ = 0f;
    private Queue<GameObject> activeSegments = new Queue<GameObject>();
    private float gameStartTime;
    private Dictionary<int, bool[,]> occupiedCellsMap = new Dictionary<int, bool[,]>();
    private int currentSegmentIndex = 0;
    private float lastObstacleTime = -10f;
    private List<float> obstaclePositions = new List<float>();
    
    void Start()
    {
        gameStartTime = Time.time;
        lastObstacleTime = -minDistanceBetweenObstacles;
        
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
        
        CleanObstaclePositions();
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
    
    void CleanObstaclePositions()
    {
        if (player != null)
        {
            for (int i = obstaclePositions.Count - 1; i >= 0; i--)
            {
                if (obstaclePositions[i] < player.position.z - 20f)
                {
                    obstaclePositions.RemoveAt(i);
                }
            }
        }
    }
    
    void GenerateObjectsForSegment(Transform segment, int segmentIndex)
    {
        float cellLength = groundLength / cellsPerSegment;
        float segmentStartZ = nextZ - groundLength;
        
        // Primero monedas
        for (int cellIndex = 0; cellIndex < cellsPerSegment; cellIndex++)
        {
            float cellZ = segmentStartZ + (cellIndex * cellLength) + (cellLength / 2f);
            
            for (int laneIndex = 0; laneIndex < lanePositions.Length; laneIndex++)
            {
                if (Random.value < coinProbability)
                {
                    PlaceCoin(segment, lanePositions[laneIndex], cellZ);
                }
            }
        }
        
        // Luego obstáculos
        for (int cellIndex = 0; cellIndex < cellsPerSegment; cellIndex++)
        {
            float cellZ = segmentStartZ + (cellIndex * cellLength) + (cellLength / 2f);
            
            if (CanPlaceObstacleAtPosition(cellZ) && 
                Random.value < obstacleProbability && 
                CanSpawnObstacles())
            {
                TryPlaceObstacleInCell(segment, segmentIndex, cellIndex, cellZ);
            }
        }
    }
    
    bool CanPlaceObstacleAtPosition(float zPosition)
    {
        float timeSinceLastObstacle = Time.time - lastObstacleTime;
        if (timeSinceLastObstacle < minDistanceBetweenObstacles)
        {
            return false;
        }
        
        float minDistanceInUnits = minDistanceBetweenObstacles * 10f;
        foreach (float obstacleZ in obstaclePositions)
        {
            if (Mathf.Abs(zPosition - obstacleZ) < minDistanceInUnits)
            {
                return false;
            }
        }
        
        return true;
    }
    
    void TryPlaceObstacleInCell(Transform segment, int segmentIndex, int cellIndex, float cellZ)
    {
        ObstacleType.Type obstacleType = SelectObstacleType();
        
        if (obstacleType == ObstacleType.Type.Wide || obstacleType == ObstacleType.Type.High)
        {
            if (HasSpaceForWideObstacle(segmentIndex, cellIndex))
            {
                PlaceObstacle(segment, lanePositions[1], cellZ, 1, segmentIndex, cellIndex, obstacleType);
                lastObstacleTime = Time.time;
                obstaclePositions.Add(cellZ);
            }
        }
        else // Long obstacle
        {
            List<int> availableLanes = new List<int> { 0, 1, 2 };
            ShuffleList(availableLanes);
            
            foreach (int laneIndex in availableLanes)
            {
                if (CanPlaceLongObstacle(segmentIndex, cellIndex, laneIndex))
                {
                    PlaceObstacle(segment, lanePositions[laneIndex], cellZ, laneIndex, segmentIndex, cellIndex, obstacleType);
                    lastObstacleTime = Time.time;
                    obstaclePositions.Add(cellZ);
                    break;
                }
            }
        }
    }
    
    bool HasSpaceForWideObstacle(int segmentIndex, int cellIndex)
    {
        for (int laneIndex = 0; laneIndex < 3; laneIndex++)
        {
            if (occupiedCellsMap[segmentIndex][cellIndex, laneIndex])
                return false;
        }
        
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
        if (occupiedCellsMap[segmentIndex][cellIndex, laneIndex])
            return false;
        
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
                break;
                
            case ObstacleType.Type.Long:
                scale = longObstacleScale;
                position = new Vector3(xPos, longObstacleScale.y / 2f, zPos);
                rotation = Quaternion.Euler(0f, 90f, 0f);
                break;
                
            case ObstacleType.Type.High:
                scale = highObstacleScale;
                position = new Vector3(lanePositions[1], highObstacleHeight, zPos);
                break;
        }
        
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.transform.rotation = rotation;
        obstacle.transform.SetParent(segment);
        
        // Aplicar color granate con transparencia
        ApplyMarbleColor(obstacle);
        
        // Configurar script ObstacleType
        ObstacleType obstacleScript = obstacle.GetComponent<ObstacleType>();
        if (obstacleScript == null)
            obstacleScript = obstacle.AddComponent<ObstacleType>();
            
        obstacleScript.obstacleType = obstacleType;
        
        MarkCellsAsOccupied(obstacleType, segmentIndex, cellIndex, laneIndex);
    }
    
    void ApplyMarbleColor(GameObject obstacle)
    {
        Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer renderer in renderers)
        {
            Material material = new Material(renderer.material);
            
            // Color granate con transparencia
            Color marbleColor = obstacleColor;
            marbleColor.a = obstacleTransparency;
            material.color = marbleColor;
            
            // Configurar para transparencia
            if (material.shader.name.Contains("Standard"))
            {
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            
            renderer.material = material;
        }
    }
    
    ObstacleType.Type SelectObstacleType()
    {
        float random = Random.value;
        
        if (random < 0.33f) return ObstacleType.Type.Wide;
        if (random < 0.66f) return ObstacleType.Type.High;
        return ObstacleType.Type.Long;
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
                laneIndex = 0;
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
                }
            }
        }
    }
}