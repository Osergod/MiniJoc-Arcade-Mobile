using UnityEngine;

public class ObstacleType : MonoBehaviour
{
    public enum Type
    {
        Wide,   // Ancho (3 carriles)
        Long,   // Largo (1 carril, largo en Z)
        High    // Alto (3 carriles, elevado)
    }
    
    [Header("Obstacle Settings")]
    public Type obstacleType = Type.Wide;
    
    [Header("Collider Settings")]
    public bool isTrigger = false; // Para obstáculos que son triggers
    
    void Start()
    {
        ConfigureObstacle();
    }
    
    void ConfigureObstacle()
    {
        // Configurar nombre
        switch (obstacleType)
        {
            case Type.Wide:
                gameObject.name = "WideObstacle";
                isTrigger = false; // Obstáculo sólido
                break;
            case Type.Long:
                gameObject.name = "LongObstacle";
                isTrigger = false; // Obstáculo sólido
                break;
            case Type.High:
                gameObject.name = "HighObstacle";
                isTrigger = true; // Trigger para poder deslizarse por debajo
                break;
        }
        
        // Configurar collider
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider>();
        
        collider.isTrigger = isTrigger;
        
        // Para High obstacle, podemos añadir un collider secundario sólido arriba
        if (obstacleType == Type.High)
        {
            AddTopCollider();
        }
    }
    
    void AddTopCollider()
    {
        // Crear un GameObject hijo para el collider sólido de arriba
        GameObject topColliderObj = new GameObject("TopCollider");
        topColliderObj.transform.SetParent(transform);
        topColliderObj.transform.localPosition = Vector3.zero;
        
        BoxCollider topCollider = topColliderObj.AddComponent<BoxCollider>();
        topCollider.size = new Vector3(1f, 0.5f, 1f); // Collider delgado en la parte superior
        topCollider.center = new Vector3(0f, 0.5f, 0f); // En la parte de arriba
        topCollider.isTrigger = false; // Sólido
    }
    
    void OnDrawGizmosSelected()
    {
        Color gizmoColor = Color.red;
        
        switch (obstacleType)
        {
            case Type.Wide:
                gizmoColor = Color.red;
                break;
            case Type.Long:
                gizmoColor = Color.blue;
                break;
            case Type.High:
                gizmoColor = Color.green;
                break;
        }
        
        Gizmos.color = gizmoColor;
        
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            
            // Para High obstacle, mostrar área de deslizamiento
            if (obstacleType == Type.High)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Vector3 slideArea = collider.bounds.center;
                slideArea.y = 1f; // Altura para deslizarse
                Gizmos.DrawCube(slideArea, new Vector3(collider.bounds.size.x, 1f, collider.bounds.size.z));
            }
        }
    }
}