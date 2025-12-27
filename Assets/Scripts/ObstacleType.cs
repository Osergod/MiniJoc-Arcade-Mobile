using UnityEngine;

public class ObstacleType : MonoBehaviour
{
    public enum Type
    {
        Wide,
        Long,
        High
    }
    
    [Header("Obstacle Settings")]
    public Type obstacleType = Type.Wide;
    
    void Start()
    {
        // Solo configurar nombre
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
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
    }
}