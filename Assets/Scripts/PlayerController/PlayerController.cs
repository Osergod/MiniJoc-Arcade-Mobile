using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // ========== REFERENCIAS ==========
    [Header("References")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    
    // ========== CONFIGURACIONES ==========
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float laneSwitchSpeed = 15f;
    public float laneWidth = 2.5f;
    
    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float gravity = -30f;
    public float groundCheckRadius = 0.5f; // Aumentado para mejor detección
    
    [Header("Slide Settings")]
    public float slideDuration = 1f;
    public float slideHeight = 0.5f;
    
    [Header("Input Settings")]
    public float swipeThreshold = 50f;
    
    [Header("Auto Jump Settings")]
    public bool enableAutoJump = true;
    public float autoJumpIntervalMin = 3f;
    public float autoJumpIntervalMax = 8f;
    
    // ========== VARIABLES PÚBLICAS ==========
    [HideInInspector] public float verticalVelocity = 0f;
    [HideInInspector] public bool isGrounded = true;
    [HideInInspector] public int currentLane = 1;
    [HideInInspector] public float targetX;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Animator animator;
    [HideInInspector] public CapsuleCollider playerCollider;
    [HideInInspector] public float originalHeight;
    [HideInInspector] public float nextAutoJumpTime = 0f;
    
    // ========== COMPONENTES ==========
    private PlayerStateMachine stateMachine;
    private Vector2 touchStartPos;
    private bool isTouching = false;
    private bool swipeProcessed = false;
    
    // ========== INICIALIZACIÓN ==========
    void Start()
    {
        // Obtener componentes
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();
        stateMachine = GetComponent<PlayerStateMachine>();
        
        // IMPORTANTE: Configurar Rigidbody ANTES que nada
        SetupRigidbody();
        
        // Inicializar collider
        if (playerCollider != null)
        {
            originalHeight = playerCollider.height;
            
            // Asegurar que el collider esté configurado
            playerCollider.center = new Vector3(0, 1f, 0); // Ajusta según tu modelo
            playerCollider.radius = 0.5f; // Radio adecuado
            playerCollider.height = 2f; // Altura adecuada
            
            Debug.Log("CapsuleCollider configurado: height=" + playerCollider.height);
        }
        
        // Calcular posición inicial
        CalculateLanePosition();
        
        // Programar primer salto automático
        if (enableAutoJump)
        {
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
        
        Debug.Log("PlayerController inicializado correctamente");
    }
    
    void SetupRigidbody()
    {
        if (rb != null)
        {
            // CONFIGURACIONES CRÍTICAS:
            rb.constraints = RigidbodyConstraints.FreezePositionY | 
                            RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;
            
            // AGREGAR ESTAS CONFIGURACIONES:
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Configurar colisiones
            rb.isKinematic = false;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            
            Debug.Log("Rigidbody configurado correctamente");
        }
    }
    
    // ========== UPDATE ==========
    void Update()
    {
        // La máquina de estados maneja el Update en su propio script
        
        // Manejar input
        ProcessMobileInput();
        ProcessKeyboardInput();
        
        // Movimiento horizontal (siempre activo)
        MovePlayerForward();
        SmoothLaneTransition();
        
        // Verificar salto automático
        CheckForAutoJump();
        
        // Actualizar animaciones
        UpdateAnimationStates();
    }
    
    // ========== MOVIMIENTO ==========
    
    public void MovePlayerForward()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
    
    public void SmoothLaneTransition()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(targetX, currentPos.y, currentPos.z);
        transform.position = Vector3.Lerp(currentPos, targetPos, laneSwitchSpeed * Time.deltaTime);
    }
    
    public void RequestMoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
            CalculateLanePosition();
        }
    }
    
    public void RequestMoveRight()
    {
        if (currentLane < 2)
        {
            currentLane++;
            CalculateLanePosition();
        }
    }
    
    public void CalculateLanePosition()
    {
        targetX = (currentLane - 1) * laneWidth;
    }
    
    // ========== FÍSICA Y GRAVEDAD ==========
    
    public void ApplyGravityForce()
    {
        verticalVelocity += gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -20f);
    }
    
    public void ApplyVerticalVelocity()
    {
        Vector3 movement = new Vector3(0, verticalVelocity * Time.fixedDeltaTime, 0);
        transform.Translate(movement, Space.World);
    }
    
    public void LockYPosition()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY | 
                            RigidbodyConstraints.FreezeRotation;
            Vector3 pos = transform.position;
            pos.y = Mathf.Round(pos.y * 100f) / 100f;
            transform.position = pos;
        }
    }
    
    public void UnlockYPosition()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
    
    // ========== INPUT ==========
    
    void ProcessMobileInput()
    {
        if (Touchscreen.current == null) return;
        
        if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            touchStartPos = Touchscreen.current.primaryTouch.position.ReadValue();
            isTouching = true;
            swipeProcessed = false;
        }
        
        if (isTouching && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            float deltaX = currentPos.x - touchStartPos.x;
            
            if (!swipeProcessed && Mathf.Abs(deltaX) > swipeThreshold)
            {
                if (deltaX > 0) RequestMoveRight();
                else RequestMoveLeft();
                
                swipeProcessed = true;
                touchStartPos = currentPos;
            }
        }
        
        if (isTouching && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            Vector2 endPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector2 delta = endPos - touchStartPos;
            
            if (!swipeProcessed && delta.magnitude < swipeThreshold)
            {
                RequestJump();
            }
            else if (!swipeProcessed && Mathf.Abs(delta.y) > swipeThreshold)
            {
                if (delta.y > 0) RequestJump();
                else RequestSlide();
            }
            
            isTouching = false;
            swipeProcessed = false;
        }
    }
    
    void ProcessKeyboardInput()
    {
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            RequestMoveLeft();
        }
        
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            RequestMoveRight();
        }
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RequestJump();
        }
        
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            RequestSlide();
        }
    }
    
    // ========== ACCIONES ==========
    
    void RequestJump()
    {
        if (stateMachine != null)
        {
            stateMachine.RequestJumpAction();
        }
    }
    
    void RequestSlide()
    {
        if (stateMachine != null)
        {
            stateMachine.RequestSlideAction();
        }
    }
    
    void CheckForAutoJump()
    {
        if (enableAutoJump && Time.time >= nextAutoJumpTime)
        {
            if (stateMachine != null && stateMachine.IsPlayerGrounded())
            {
                RequestJump();
            }
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
    }
    
    public void FinishSlide()
    {
        if (stateMachine != null && stateMachine.IsPlayerSliding())
        {
            stateMachine.RequestLanding();
        }
    }
    
    public bool CheckIfSliding()
    {
        return stateMachine != null && stateMachine.IsPlayerSliding();
    }
    
    // ========== COLISIONES ==========
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            ProcessObstacleCollision(collision.gameObject);
        }
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            if (stateMachine != null)
            {
                stateMachine.RequestLanding();
            }
        }
    }
    
    void ProcessObstacleCollision(GameObject obstacle)
    {
        // COMENTADO: Desactivar muerte por obstáculos
        // Solo mostrar debug, sin game over
        
        Debug.Log("Colisión con obstáculo ignorada (modo sin muerte)");
        
        // Opcional: puedes agregar efectos visuales o sonido sin game over
        // Ejemplo: obstacle.SetActive(false); // Hacer desaparecer el obstáculo
        // Ejemplo: GetComponent<AudioSource>().PlayOneShot(hitSound);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            CollectCoinItem(other.gameObject);
        }
    }
    
    void CollectCoinItem(GameObject coin)
    {
        coin.SetActive(false);
        // ScoreManager.Instance.AddScore(10);
        // AudioManager.Instance.PlaySound("CoinCollect");
    }
    
    // ========== UTILIDADES ==========
    
    public bool CheckGroundContact()
    {
        if (groundCheck == null) 
        {
            Debug.LogWarning("groundCheck no asignado!");
            return false;
        }
        
        // Usar esfera más grande y debug
        bool grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        
        // DEBUG: Mostrar info
        #if UNITY_EDITOR
        Debug.DrawRay(groundCheck.position, Vector3.down * groundCheckRadius, 
                     grounded ? Color.green : Color.red, 0.1f);
        #endif
        
        return grounded;
    }
    
    public void UpdateAnimationStates()
    {
        if (animator != null && stateMachine != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", stateMachine.IsPlayerJumping());
            animator.SetBool("IsFalling", stateMachine.IsPlayerFalling());
            animator.SetBool("IsSliding", stateMachine.IsPlayerSliding());
            animator.SetFloat("VerticalVelocity", verticalVelocity);
        }
    }
    
    void TriggerGameOver()
    {
        // COMENTADO: Desactivar game over
        Debug.Log("Game Over desactivado - Continuando juego");
    }
    
    // ========== DEBUG ==========
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}