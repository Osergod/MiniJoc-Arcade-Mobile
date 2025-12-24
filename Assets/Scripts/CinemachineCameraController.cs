using UnityEngine;
using Cinemachine;

public class CinemachineCameraController : MonoBehaviour
{
    [Header("Cinemachine References")]
    public CinemachineVirtualCamera virtualCamera;
    public CinemachineConfiner confiner;
    
    [Header("Camera Settings")]
    public float normalFOV = 60f;
    public float boostFOV = 70f;
    public float fovTransitionSpeed = 5f;
    
    private CinemachineTransposer transposer;
    private float currentFOV;
    
    void Start()
    {
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }
        
        if (virtualCamera != null)
        {
            transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            currentFOV = virtualCamera.m_Lens.FieldOfView;
            
            // Configurar follow target
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                virtualCamera.Follow = player.transform;
                virtualCamera.LookAt = player.transform;
            }
        }
    }
    
    void Update()
    {
        // Ajustar FOV dinámicamente (ejemplo: cuando el jugador corre más rápido)
        AdjustFOV();
        
        // Otros ajustes de cámara pueden ir aquí
    }
    
    void AdjustFOV()
    {
        if (virtualCamera == null) return;
        
        // Ejemplo: aumentar FOV cuando el jugador está en modo boost
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            // Aquí puedes agregar lógica para detectar cuando el jugador está en modo boost
            float targetFOV = normalFOV; // Cambia esto según tu lógica de juego
            
            // Suavizar transición de FOV
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.deltaTime);
            virtualCamera.m_Lens.FieldOfView = currentFOV;
        }
    }
    
    // Método para cambiar entre diferentes configuraciones de cámara
    public void SwitchToCameraProfile(string profileName)
    {
        // Implementa diferentes perfiles de cámara según la situación del juego
        switch (profileName)
        {
            case "Normal":
                SetCameraSettings(60f, new Vector3(0, 5, -10));
                break;
            case "Boost":
                SetCameraSettings(70f, new Vector3(0, 4, -12));
                break;
            case "Jump":
                SetCameraSettings(65f, new Vector3(0, 6, -8));
                break;
        }
    }
    
    void SetCameraSettings(float fov, Vector3 offset)
    {
        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.FieldOfView = fov;
        }
        
        if (transposer != null)
        {
            transposer.m_FollowOffset = offset;
        }
    }
}