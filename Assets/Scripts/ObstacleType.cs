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
    Renderer r;
    
    void Start()
    {
        switch (obstacleType)
        {
            case Type.Wide:
                gameObject.name = "WideObstacle";
                r = GetComponent<Renderer>();
                r.material.mainTextureScale = new Vector2(1f, 1/6f);
                break;
            case Type.Long:
                gameObject.name = "LongObstacle";
                r = GetComponent<Renderer>();
                r.material.mainTextureScale = new Vector2(1/2f, 1/10f);
                break;
            case Type.High:
                gameObject.name = "HighObstacle";
                r = GetComponent<Renderer>();
                r.material.mainTextureScale = new Vector2(1f, 1/6f);
                break;
        }
        
        // Configurar collider
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider>();
    }
    
    void OnDrawGizmosSelected()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
        }
    }
}