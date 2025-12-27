using UnityEngine;

public class SlideCameraController : MonoBehaviour
{
    [Header("Camera References")]
    public Transform player;
    public Camera mainCamera;
    
    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    public float followSpeed = 10f;
    
    [Header("Slide Tilt Settings")]
    public float maxTiltAngle = 15f; // Cuánto se inclina la cámara
    public float tiltSpeed = 5f;     // Velocidad de inclinación
    public float slideHeightOffset = -0.5f; // La cámara baja un poco al deslizarse
    
    [Header("Look At Settings")]
    public float lookAheadDistance = 3f; // Mira un poco adelante del jugador
    public float lookAtHeight = 1f;      // Altura a la que mira
    
    private PlayerController playerController;
    private float currentTilt = 0f;
    private Vector3 originalOffset;
    private Vector3 slideOffset;
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player != null)
            playerController = player.GetComponent<PlayerController>();
        
        originalOffset = offset;
        slideOffset = new Vector3(offset.x, offset.y + slideHeightOffset, offset.z);
    }
    
    void LateUpdate()
    {
        if (player == null) return;
        
        // 1. Seguir al jugador
        FollowPlayer();
        
        // 2. Inclinarse si está deslizándose
        HandleSlideTilt();
        
        // 3. Mirar hacia adelante del jugador
        LookAtPlayer();
    }
    
    void FollowPlayer()
    {
        Vector3 targetPosition = player.position;
        Vector3 currentOffset = offset;
        
        // Si está deslizándose, usar offset más bajo
        if (playerController != null && playerController.IsSliding)
        {
            currentOffset = slideOffset;
        }
        
        // Calcular posición objetivo
        Vector3 desiredPosition = targetPosition + currentOffset;
        
        // Suavizar movimiento
        transform.position = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            followSpeed * Time.deltaTime
        );
    }
    
    void HandleSlideTilt()
    {
        float targetTilt = 0f;
        
        // Determinar ángulo de inclinación
        if (playerController != null && playerController.IsSliding)
        {
            targetTilt = maxTiltAngle; // Inclinar hacia adelante
        }
        
        // Suavizar inclinación
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.deltaTime);
        
        // Aplicar rotación a la cámara
        Vector3 cameraRotation = mainCamera.transform.localEulerAngles;
        cameraRotation.x = currentTilt;
        mainCamera.transform.localEulerAngles = cameraRotation;
    }
    
    void LookAtPlayer()
    {
        if (player == null) return;
        
        // Punto adelante del jugador para mirar
        Vector3 lookAtPoint = player.position + 
                             (player.forward * lookAheadDistance) + 
                             (Vector3.up * lookAtHeight);
        
        // Suavizar rotación para mirar
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            followSpeed * Time.deltaTime
        );
    }
    
    // Método para ajustes rápidos
    public void SetTiltAngle(float angle)
    {
        maxTiltAngle = angle;
    }
    
    public void SetTiltSpeed(float speed)
    {
        tiltSpeed = speed;
    }
}