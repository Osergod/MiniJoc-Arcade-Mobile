using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float laneSwitchSpeed = 15f;
    public float laneWidth = 2.5f;
    
    [Header("Input Settings")]
    public float swipeThreshold = 50f; // Mínimo desplazamiento para cambiar carril
    public float holdTimeThreshold = 0.15f; // Tiempo mínimo para considerar "hold"
    
    [Header("Lanes")]
    public int currentLane = 1; // 0: izquierda, 1: centro, 2: derecha
    public int numberOfLanes = 3;
    
    // Input states
    private Vector2 touchStartPos;
    private Vector2 lastTouchPos;
    private bool isTouching = false;
    private bool swipeProcessed = false; // Evitar múltiples cambios con un swipe
    private float touchStartTime;
    private float targetX;
    
    // Components
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        CalculateTargetX();
    }
    
    void Update()
    {
        MoveForward();
        HandleTouchInput();
        SmoothLaneSwitch();
    }
    
    void MoveForward()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
    
    void HandleTouchInput()
    {
        // 1. DETECTAR INICIO DE TOQUE
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            touchStartPos = Touchscreen.current.primaryTouch.position.ReadValue();
            lastTouchPos = touchStartPos;
            touchStartTime = Time.time;
            isTouching = true;
            swipeProcessed = false; // Resetear para nuevo swipe
        }
        
        // 2. MIENTRAS EL DEDO ESTÁ PRESIONADO
        if (isTouching && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 currentTouchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            
            // Calcular desplazamiento desde el inicio
            Vector2 totalDelta = currentTouchPos - touchStartPos;
            
            // Calcular desplazamiento desde la última posición
            Vector2 frameDelta = currentTouchPos - lastTouchPos;
            
            // 3. DETECTAR SWIPE HORIZONTAL SIN SOLTAR
            if (!swipeProcessed && Mathf.Abs(totalDelta.x) > swipeThreshold)
            {
                // Determinar dirección
                if (totalDelta.x > 0) // Swipe derecha
                {
                    MoveRight();
                }
                else // Swipe izquierda
                {
                    MoveLeft();
                }
                
                swipeProcessed = true; // Marcar como procesado
            }
            
            // 4. MOVIMIENTO CONTINUO OPCIONAL (si quieres feedback visual)
            // Puedes mostrar un indicador visual sin cambiar carril realmente
            ShowTouchFeedback(frameDelta.x);
            
            lastTouchPos = currentTouchPos;
        }
        
        // 3. DETECTAR FIN DE TOQUE
        if (isTouching && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            Vector2 touchEndPos = Touchscreen.current.primaryTouch.position.ReadValue();
            float touchDuration = Time.time - touchStartTime;
            
            // SWIPE TRADICIONAL (si no se procesó durante el hold)
            if (!swipeProcessed)
            {
                Vector2 swipeDelta = touchEndPos - touchStartPos;
                
                // Si el desplazamiento es muy pequeño, es un tap (salto)
                if (swipeDelta.magnitude < swipeThreshold)
                {
                    Jump();
                }
                else // Es un swipe
                {
                    ProcessSwipe(swipeDelta);
                }
            }
            
            // Resetear estados
            isTouching = false;
            swipeProcessed = false;
            HideTouchFeedback();
        }
        
        // Input de teclado para testing (opcional)
        #if UNITY_EDITOR
        HandleKeyboardInput();
        #endif
    }
    
    void ProcessSwipe(Vector2 swipeDelta)
    {
        bool isHorizontalSwipe = Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y);
        
        if (isHorizontalSwipe)
        {
            if (swipeDelta.x > 0)
            {
                MoveRight();
            }
            else
            {
                MoveLeft();
            }
        }
        else
        {
            if (swipeDelta.y > 0)
            {
                Jump();
            }
            else
            {
                Slide();
            }
        }
    }
    
    void MoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
            CalculateTargetX();
            // AudioManager.Instance.PlaySound("Swipe");
        }
    }
    
    void MoveRight()
    {
        if (currentLane < numberOfLanes - 1)
        {
            currentLane++;
            CalculateTargetX();
            // AudioManager.Instance.PlaySound("Swipe");
        }
    }
    
    void CalculateTargetX()
    {
        targetX = (currentLane - 1) * laneWidth; // Para 3 carriles: -2.5, 0, 2.5
    }
    
    void SmoothLaneSwitch()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(targetX, currentPos.y, currentPos.z);
        
        transform.position = Vector3.Lerp(currentPos, targetPos, laneSwitchSpeed * Time.deltaTime);
    }
    
    void ShowTouchFeedback(float horizontalDelta)
    {
        // Opcional: Mover ligeramente al jugador o mostrar efecto visual
        // sin cambiar realmente de carril
        // Ejemplo: transform.position += Vector3.right * horizontalDelta * 0.001f;
    }
    
    void HideTouchFeedback()
    {
        // Resetear cualquier efecto visual temporal
    }
    
    void Jump()
    {
        // Tu lógica de salto
        // if (isGrounded) { ... }
    }
    
    void Slide()
    {
        // Tu lógica de deslizamiento
    }
    
    #if UNITY_EDITOR
    void HandleKeyboardInput()
    {
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            MoveLeft();
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            MoveRight();
        }
        else if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Jump();
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            Slide();
        }
    }
    #endif
    
    void OnDrawGizmos()
    {
        // Visualizar carriles en el editor
        Gizmos.color = Color.blue;
        for (int i = 0; i < numberOfLanes; i++)
        {
            float laneX = (i - 1) * laneWidth;
            Gizmos.DrawLine(
                new Vector3(laneX, 0.5f, transform.position.z - 5),
                new Vector3(laneX, 0.5f, transform.position.z + 5)
            );
        }
    }
}