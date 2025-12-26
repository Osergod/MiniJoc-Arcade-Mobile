using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float laneSwitchSpeed = 15f;
    public float laneWidth = 2.5f;
    
    [Header("Jump Settings")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    [Header("Input Settings")]
    public float swipeThreshold = 50f;
    public float holdTimeThreshold = 0.15f;
    
    [Header("Lanes")]
    public int currentLane = 1;
    public int numberOfLanes = 3;
    
    // Input states
    private Vector2 touchStartPos;
    private Vector2 lastTouchPos;
    private bool isTouching = false;
    private bool swipeProcessed = false;
    private float touchStartTime;
    private float targetX;
    
    // Jump states
    private float verticalVelocity;
    private bool isGrounded = true;
    private bool isJumping = false;
    
    // Components
    private Rigidbody rb;
    private Animator animator;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        CalculateTargetX();
        
        // Configurar Rigidbody para salto
        if (rb != null)
        {
            rb.useGravity = false; // Vamos a controlar la gravedad manualmente
        }
    }
    
    void Update()
    {
        // Verificar si está en el suelo
        CheckGround();
        
        // Movimiento adelante
        MoveForward();
        
        // Manejo de input táctil
        HandleTouchInput();
        
        // Aplicar gravedad
        ApplyGravity();
        
        // Suavizar cambio de carril
        SmoothLaneSwitch();
        
        // Actualizar animaciones
        UpdateAnimations();
    }
    
    void FixedUpdate()
    {
        // Aplicar movimiento vertical en FixedUpdate para mejor física
        ApplyVerticalMovement();
    }
    
    void MoveForward()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
    
    void CheckGround()
    {
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
            
            // Si acaba de tocar el suelo
            if (!wasGrounded && isGrounded)
            {
                OnLand();
            }
        }
        else
        {
            // Fallback si no hay groundCheck
            isGrounded = true;
        }
    }
    
    void ApplyGravity()
    {
        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
            
            // Limitar velocidad de caída
            verticalVelocity = Mathf.Max(verticalVelocity, -20f);
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = 0;
            isJumping = false;
        }
    }
    
    void ApplyVerticalMovement()
    {
        if (!isGrounded || isJumping)
        {
            Vector3 movement = new Vector3(0, verticalVelocity * Time.fixedDeltaTime, 0);
            transform.Translate(movement, Space.World);
        }
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
            swipeProcessed = false;
        }
        
        // 2. MIENTRAS EL DEDO ESTÁ PRESIONADO
        if (isTouching && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 currentTouchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector2 totalDelta = currentTouchPos - touchStartPos;
            Vector2 frameDelta = currentTouchPos - lastTouchPos;
            
            // DETECTAR SWIPE HORIZONTAL SIN SOLTAR
            if (!swipeProcessed && Mathf.Abs(totalDelta.x) > swipeThreshold)
            {
                if (totalDelta.x > 0) // Swipe derecha
                {
                    MoveRight();
                }
                else // Swipe izquierda
                {
                    MoveLeft();
                }
                swipeProcessed = true;
            }
            
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
        targetX = (currentLane - 1) * laneWidth;
    }
    
    void SmoothLaneSwitch()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(targetX, currentPos.y, currentPos.z);
        
        transform.position = Vector3.Lerp(currentPos, targetPos, laneSwitchSpeed * Time.deltaTime);
    }
    
    public void Jump()
    {
        if (isGrounded && !isJumping)
        {
            isGrounded = false;
            isJumping = true;
            verticalVelocity = jumpForce;
            
            // Animación
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
            
            // Sonido
            // AudioManager.Instance.PlaySound("Jump");
            
            Debug.Log("Jump!");
        }
    }
    
    void OnLand()
    {
        isJumping = false;
        
        // Efecto de aterrizaje
        if (animator != null)
        {
            animator.SetTrigger("Land");
        }
        
        // Sonido
        // AudioManager.Instance.PlaySound("Land");
    }
    
    void Slide()
    {
        // Tu lógica de deslizamiento
        if (isGrounded && animator != null)
        {
            animator.SetTrigger("Slide");
            // AudioManager.Instance.PlaySound("Slide");
        }
    }
    
    void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", isJumping);
            animator.SetFloat("VerticalVelocity", verticalVelocity);
        }
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
    
    void OnDrawGizmosSelected()
    {
        // Visualizar ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Visualizar carriles
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
    
    // Para detectar colisiones con el suelo
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            isJumping = false;
            verticalVelocity = 0;
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}