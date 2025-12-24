using UnityEngine;

public class Coin : MonoBehaviour
{
    // NADA en Start() ni Update() que modifique escala
    // Solo OnTriggerEnter si necesitas
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false);
        }
    }
}