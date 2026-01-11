using UnityEngine;
using TMPro;

public class PlayerDistanceTracker : MonoBehaviour
{
    public TextMeshProUGUI distanceText; // Arrastra aquí el Text del Canvas

    private Vector3 lastPosition;
    private float distanceTravelled = 0f;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculamos la distancia solo en XZ (ignora Y)
        Vector3 currentPosition = transform.position;
        Vector3 lastPositionXZ = new Vector3(lastPosition.x, 0f, lastPosition.z);
        Vector3 currentPositionXZ = new Vector3(currentPosition.x, 0f, currentPosition.z);

        float distanceThisFrame = Vector3.Distance(currentPositionXZ, lastPositionXZ);
        distanceTravelled += distanceThisFrame;

        lastPosition = currentPosition;

        // Actualizamos el texto en pantalla
        if (distanceText != null)
        {
            distanceText.text = "Distancia: " + distanceTravelled.ToString("F2") + " m";
        }
    }

    public float GetDistanceTravelled()
    {
        return distanceTravelled;
    }
}
