using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("References")]
    public GroundGeneratorFixed groundGenerator;
    
    [Header("Grid Settings")]
    public float gridCellSize = 2f;
    public int gridSafetyMargin = 1; // Celdas de separación mínima
    
    // Diccionarios para rastrear posiciones ocupadas
    private Dictionary<Vector2Int, GameObject> occupiedPositions = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<GameObject, Vector2Int> objectPositions = new Dictionary<GameObject, Vector2Int>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (groundGenerator == null)
        {
            groundGenerator = FindObjectOfType<GroundGeneratorFixed>();
        }
        
        // Suscribirse a eventos de generación si el groundGenerator los tuviera
        // Por ahora, verificaremos manualmente
    }
    
    // ========== SISTEMA DE GRID ==========
    
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int gridX = Mathf.RoundToInt(worldPos.x / gridCellSize);
        int gridZ = Mathf.RoundToInt(worldPos.z / gridCellSize);
        
        return new Vector2Int(gridX, gridZ);
    }
    
    public Vector3 GridToWorldPosition(Vector2Int gridPos, float yPosition)
    {
        float worldX = gridPos.x * gridCellSize;
        float worldZ = gridPos.y * gridCellSize;
        
        return new Vector3(worldX, yPosition, worldZ);
    }
    
    // ========== REGISTRO DE OBJETOS ==========
    
    public bool RegisterObject(GameObject obj, Vector3 worldPosition, string objectType)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        
        // Verificar si la posición está ocupada
        if (IsPositionOccupied(gridPos))
        {
            Debug.LogWarning($"{objectType} cannot be placed at {gridPos}. Position occupied.");
            return false;
        }
        
        // Verificar posiciones adyacentes
        if (!CheckSafetyZone(gridPos))
        {
            Debug.LogWarning($"{objectType} too close to another object at {gridPos}");
            return false;
        }
        
        // Registrar objeto
        occupiedPositions[gridPos] = obj;
        objectPositions[obj] = gridPos;
        
        Debug.Log($"{objectType} registered at grid position {gridPos}");
        return true;
    }
    
    public bool TryRegisterObject(GameObject obj, Vector3 worldPosition, string objectType, int maxAttempts = 10)
    {
        Vector2Int originalGridPos = WorldToGridPosition(worldPosition);
        
        // Intentar registrar en la posición original
        if (RegisterObject(obj, worldPosition, objectType))
        {
            return true;
        }
        
        // Buscar posición alternativa cercana
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // Buscar en un radio cada vez mayor
            int searchRadius = Mathf.Min(attempt, 3);
            
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int z = -searchRadius; z <= searchRadius; z++)
                {
                    if (x == 0 && z == 0) continue;
                    
                    Vector2Int testPos = new Vector2Int(
                        originalGridPos.x + x,
                        originalGridPos.y + z
                    );
                    
                    Vector3 testWorldPos = GridToWorldPosition(testPos, worldPosition.y);
                    
                    if (RegisterObject(obj, testWorldPos, objectType))
                    {
                        // Mover objeto a la nueva posición
                        obj.transform.position = testWorldPos;
                        Debug.Log($"{objectType} moved to alternative position {testPos}");
                        return true;
                    }
                }
            }
        }
        
        Debug.LogError($"{objectType} could not find valid position after {maxAttempts} attempts");
        return false;
    }
    
    public void UnregisterObject(GameObject obj)
    {
        if (objectPositions.ContainsKey(obj))
        {
            Vector2Int gridPos = objectPositions[obj];
            occupiedPositions.Remove(gridPos);
            objectPositions.Remove(obj);
            
            Debug.Log($"Object unregistered from grid position {gridPos}");
        }
    }
    
    // ========== VERIFICACIONES ==========
    
    bool IsPositionOccupied(Vector2Int gridPos)
    {
        return occupiedPositions.ContainsKey(gridPos);
    }
    
    bool CheckSafetyZone(Vector2Int gridPos)
    {
        // Verificar celdas adyacentes según el margen de seguridad
        for (int x = -gridSafetyMargin; x <= gridSafetyMargin; x++)
        {
            for (int z = -gridSafetyMargin; z <= gridSafetyMargin; z++)
            {
                if (x == 0 && z == 0) continue;
                
                Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + z);
                
                if (occupiedPositions.ContainsKey(checkPos))
                {
                    GameObject existingObj = occupiedPositions[checkPos];
                    
                    // Reglas específicas de superposición
                    if (existingObj.CompareTag("Obstacle"))
                    {
                        // Nunca poner nada cerca de un obstáculo
                        return false;
                    }
                    else if (existingObj.CompareTag("Coin"))
                    {
                        // Monedas pueden estar cerca de otras monedas, pero no demasiado
                        if (Mathf.Abs(x) <= 1 && Mathf.Abs(z) <= 1)
                        {
                            return false;
                        }
                    }
                }
            }
        }
        
        return true;
    }
    
    // ========== MÉTODOS PARA EL GROUND GENERATOR ==========
    
    // Llama a este método ANTES de generar objetos en un segmento
    public void PrepareSegmentGeneration(float segmentStartZ, float segmentEndZ)
    {
        // Limpiar objetos antiguos de este segmento (opcional)
        // Podrías implementar limpieza por distancia si quieres
    }
    
    // Método para que el Ground Generator consulte posiciones válidas
    public Vector3? GetValidPositionForObject(float minZ, float maxZ, float xPosition, float yPosition, string objectType)
    {
        int gridCellsZ = Mathf.FloorToInt((maxZ - minZ) / gridCellSize);
        
        // Probar múltiples posiciones en Z
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float randomZ = Random.Range(minZ, maxZ);
            Vector3 testPos = new Vector3(xPosition, yPosition, randomZ);
            Vector2Int gridPos = WorldToGridPosition(testPos);
            
            if (!IsPositionOccupied(gridPos) && CheckSafetyZone(gridPos))
            {
                return testPos;
            }
        }
        
        return null;
    }
    
    // ========== DEBUG Y VISUALIZACIÓN ==========
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Dibujar grid ocupado
        Gizmos.color = Color.red;
        foreach (var kvp in occupiedPositions)
        {
            Vector2Int gridPos = kvp.Key;
            Vector3 worldPos = GridToWorldPosition(gridPos, 0.5f);
            
            Gizmos.DrawWireCube(worldPos, new Vector3(gridCellSize, 1, gridCellSize) * 0.8f);
            
            // Etiqueta con tipo de objeto
            #if UNITY_EDITOR
            GameObject obj = kvp.Value;
            string label = obj.CompareTag("Obstacle") ? "O" : "C";
            UnityEditor.Handles.Label(worldPos + Vector3.up, label);
            #endif
        }
        
        // Dibujar grid completo
        Gizmos.color = Color.gray;
        if (groundGenerator != null)
        {
            float playerZ = groundGenerator.player.position.z;
            
            // Dibujar 20 celdas adelante y atrás
            for (int z = -10; z <= 10; z++)
            {
                for (int x = -5; x <= 5; x++)
                {
                    Vector3 cellCenter = new Vector3(
                        x * gridCellSize,
                        0,
                        playerZ + (z * gridCellSize)
                    );
                    
                    Gizmos.DrawWireCube(cellCenter, new Vector3(gridCellSize, 0.1f, gridCellSize));
                }
            }
        }
    }
    
    // ========== LIMPIEZA ==========
    
    public void ClearAllRegistrations()
    {
        occupiedPositions.Clear();
        objectPositions.Clear();
        Debug.Log("All object registrations cleared");
    }
    
    // Limpiar objetos lejanos para optimizar
    public void CleanupDistantObjects(float playerZ, float cleanupDistance)
    {
        List<GameObject> toRemove = new List<GameObject>();
        
        foreach (var kvp in objectPositions)
        {
            GameObject obj = kvp.Key;
            if (obj == null) continue;
            
            float distance = Mathf.Abs(obj.transform.position.z - playerZ);
            if (distance > cleanupDistance)
            {
                toRemove.Add(obj);
            }
        }
        
        foreach (GameObject obj in toRemove)
        {
            UnregisterObject(obj);
        }
        
        if (toRemove.Count > 0)
        {
            Debug.Log($"Cleaned up {toRemove.Count} distant objects");
        }
    }
}