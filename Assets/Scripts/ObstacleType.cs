using UnityEngine;

public class ObstacleType : MonoBehaviour
{
    public enum Type
    {
        Wide,    // Ancho - salta
        Long,    // Largo - esquiva
        High     // Alto - deslízate
    }
    
    [Header("Obstacle Settings")]
    public Type obstacleType = Type.Wide;
    
    [Header("Visual Settings")]
    public Color wideColor = Color.red;
    public Color longColor = Color.blue;
    public Color highColor = Color.yellow;
    
    void Start()
    {
        ConfigureObstacle();
    }
    
    void ConfigureObstacle()
    {
        // Aplicar color según tipo
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            switch (obstacleType)
            {
                case Type.Wide:
                    rend.material.color = wideColor;
                    gameObject.name = "WideObstacle";
                    break;
                    
                case Type.Long:
                    rend.material.color = longColor;
                    gameObject.name = "LongObstacle";
                    break;
                    
                case Type.High:
                    rend.material.color = highColor;
                    gameObject.name = "HighObstacle";
                    
                    // Ajustar posición Y para High obstacles
                    transform.position = new Vector3(
                        transform.position.x,
                        2.0f,  // Más alto para pasar por debajo
                        transform.position.z
                    );
                    break;
            }
        }
        
        // Ajustar collider según tipo
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            switch (obstacleType)
            {
                case Type.Wide:
                    collider.size = new Vector3(3f, 1f, 1f);
                    break;
                    
                case Type.Long:
                    collider.size = new Vector3(1f, 1f, 3f);
                    break;
                    
                case Type.High:
                    collider.size = new Vector3(3f, 3f, 1f);
                    collider.center = new Vector3(0, 1.5f, 0);
                    break;
            }
        }
    }
    
    // Método para debug/visualización
    void OnDrawGizmosSelected()
    {
        Color gizmoColor = Color.white;
        
        switch (obstacleType)
        {
            case Type.Wide: gizmoColor = Color.red; break;
            case Type.Long: gizmoColor = Color.blue; break;
            case Type.High: gizmoColor = Color.yellow; break;
        }
        
        Gizmos.color = gizmoColor;
        
        // Dibujar wireframe del obstáculo
        Bounds bounds = GetComponent<Renderer>()?.bounds ?? GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        
        // Etiqueta con el tipo
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2,
            $"Type: {obstacleType}",
            new GUIStyle() { normal = { textColor = gizmoColor }, fontStyle = FontStyle.Bold }
        );
        #endif
    }
    
    // Información sobre el obstáculo
    public string GetObstacleInfo()
    {
        switch (obstacleType)
        {
            case Type.Wide:
                return "Jump over me! (Press Space/Swipe Up)";
            case Type.Long:
                return "Dodge me! (Move Left/Right)";
            case Type.High:
                return "Slide under me! (Swipe Down)";
            default:
                return "Unknown obstacle";
        }
    }
}