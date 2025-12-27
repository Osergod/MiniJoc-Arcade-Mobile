using UnityEngine;

public class ObstacleType : MonoBehaviour
{
    public enum Type
    {
        Wide,
        Long,
        High
    }
    
    public Type obstacleType = Type.Wide;
    
    void Start()
    {
        // Solo para identificar el tipo, no necesita color
        switch (obstacleType)
        {
            case Type.Wide:
                gameObject.name = "WideObstacle";
                break;
            case Type.Long:
                gameObject.name = "LongObstacle";
                break;
            case Type.High:
                gameObject.name = "HighObstacle";
                break;
        }
        
        // Configurar collider
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider>();
        
        // High obstacle es trigger para poder deslizarse
        collider.isTrigger = (obstacleType == Type.High);
    }
    
    void OnDrawGizmosSelected()
    {
        // Gizmo en color granate para todos
        Gizmos.color = new Color(0.5f, 0f, 0f, 0.3f);
        
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
        }
    }
}