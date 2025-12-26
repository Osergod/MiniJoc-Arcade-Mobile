using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // ========== ENUM DE ESTADOS ==========
    public enum PlayerState
    {
        Grounded,   // En suelo, Y congelado
        Jumping,    // Saltando, Y libre
        Falling,    // Cayendo, Y libre
        Sliding     // Deslizando, Y congelado
    }
    
    [Header("References")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float laneSwitchSpeed = 15f;
    public float laneWidth = 2.5f;
    
    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float gravity = -30f;
    public float groundCheckRadius = 0.3f;
    
    [Header("Input Settings")]
    public float swipeThreshold = 50f;
    
    [Header("Auto Jump Settings")]
    public bool enableAutoJump = true;
    public float autoJumpIntervalMin = 3f;
    public float autoJumpIntervalMax = 8f;
    private float nextAutoJumpTime = 0f;
    
    // Estado actual
    private PlayerState currentState = PlayerState.Grounded;
    
    // Variables de estado
    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    private int currentLane = 1;
    private float targetX;
    
    // Input
    private Vector2 touchStartPos;
    private bool isTouching = false;
    private bool swipeProcessed = false;
    
    // Components
    private Rigidbody rb;
    private Animator animator;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        InitializeRigidbody();
        CalculateTargetX();
        
        // Programar primer salto automático
        if (enableAutoJump)
        {
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
    }
    
    void InitializeRigidbody()
    {
        if (rb != null)
        {
            // Congelar posición Y al inicio
            rb.constraints = RigidbodyConstraints.FreezePositionY | 
                            RigidbodyConstraints.FreezeRotation;
            
            // Desactivar gravedad del Rigidbody (la controlamos nosotros)
            rb.useGravity = false;
        }
    }
    
    void Update()
    {
        // Actualizar estado
        UpdateState();
        
        // Ejecutar lógica del estado actual
        ExecuteState();
        
        // Manejar input (MÓVIL + TECLADO)
        HandleMobileInput();
        HandleKeyboardInput();
        
        // Movimiento horizontal
        MoveForward();
        SmoothLaneSwitch();
        
        // Salto automático
        CheckAutoJump();
        
        // Actualizar animaciones
        UpdateAnimations();
    }
    
    void FixedUpdate()
    {
        // Aplicar movimiento vertical en FixedUpdate
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            ApplyVerticalMovement();
        }
    }
    
    // ========== MÁQUINA DE ESTADOS ==========
    
    void UpdateState()
    {
        // Verificar contacto con el suelo
        bool wasGrounded = isGrounded;
        isGrounded = CheckGround();
        
        // Transiciones de estado
        switch (currentState)
        {
            case PlayerState.Grounded:
                if (!isGrounded)
                {
                    TransitionToState(PlayerState.Falling);
                }
                break;
                
            case PlayerState.Jumping:
                if (verticalVelocity < 0)
                {
                    TransitionToState(PlayerState.Falling);
                }
                break;
                
            case PlayerState.Falling:
                if (isGrounded)
                {
                    TransitionToState(PlayerState.Grounded);
                }
                break;
                
            case PlayerState.Sliding:
                if (!IsSliding())
                {
                    TransitionToState(PlayerState.Grounded);
                }
                break;
        }
        
        // Detectar aterrizaje
        if (!wasGrounded && isGrounded && currentState != PlayerState.Grounded)
        {
            OnLand();
        }
    }
    
    void ExecuteState()
    {
        switch (currentState)
        {
            case PlayerState.Grounded:
                // Y congelado, aplicar gravedad cero
                verticalVelocity = 0f;
                FreezeYPosition();
                break;
                
            case PlayerState.Jumping:
                // Y libre, aplicar gravedad
                UnfreezeYPosition();
                ApplyGravity();
                break;
                
            case PlayerState.Falling:
                // Y libre, aplicar gravedad
                UnfreezeYPosition();
                ApplyGravity();
                break;
                
            case PlayerState.Sliding:
                // Y congelado durante el deslizamiento
                FreezeYPosition();
                break;
        }
    }
    
    void TransitionToState(PlayerState newState)
    {
        // Salir del estado anterior
        ExitState(currentState);
        
        // Entrar al nuevo estado
        EnterState(newState);
        
        // Cambiar estado
        currentState = newState;
    }
    
    void EnterState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Jumping:
                verticalVelocity = jumpForce;
                if (animator != null) animator.SetTrigger("Jump");
                break;
                
            case PlayerState.Grounded:
                verticalVelocity = 0f;
                FreezeYPosition();
                if (animator != null) animator.SetTrigger("Land");
                break;
                
            case PlayerState.Sliding:
                if (animator != null) animator.SetTrigger("Slide");
                break;
        }
    }
    
    void ExitState(PlayerState state)
    {
        // Limpiar estados si es necesario
    }
    
    // ========== MOVIMIENTO HORIZONTAL ==========
    
    void MoveForward()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
    
    void SmoothLaneSwitch()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(targetX, currentPos.y, currentPos.z);
        
        transform.position = Vector3.Lerp(currentPos, targetPos, laneSwitchSpeed * Time.deltaTime);
    }
    
    public void MoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
            CalculateTargetX();
        }
    }
    
    public void MoveRight()
    {
        if (currentLane < 2) // 0, 1, 2 = 3 carriles
        {
            currentLane++;
            CalculateTargetX();
        }
    }
    
    void CalculateTargetX()
    {
        targetX = (currentLane - 1) * laneWidth; // Para 3 carriles: -2.5, 0, 2.5
    }
    
    // ========== MOVIMIENTO VERTICAL ==========
    
    void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -20f);
    }
    
    void ApplyVerticalMovement()
    {
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            Vector3 movement = new Vector3(0, verticalVelocity * Time.fixedDeltaTime, 0);
            transform.Translate(movement, Space.World);
        }
    }
    
    void FreezeYPosition()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY | 
                            RigidbodyConstraints.FreezeRotation;
            
            // Mantener posición exacta en Y
            Vector3 pos = transform.position;
            pos.y = Mathf.Round(pos.y * 100f) / 100f;
            transform.position = pos;
        }
    }
    
    void UnfreezeYPosition()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
    
    // ========== INPUT MÓVIL ==========
    
    void HandleMobileInput()
    {
        if (Touchscreen.current == null) return;
        
        // INICIO DE TOQUE
        if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            touchStartPos = Touchscreen.current.primaryTouch.position.ReadValue();
            isTouching = true;
            swipeProcessed = false;
        }
        
        // TOQUE MANTENIDO
        if (isTouching && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            float deltaX = currentPos.x - touchStartPos.x;
            
            // SWIPE HORIZONTAL SIN SOLTAR
            if (!swipeProcessed && Mathf.Abs(deltaX) > swipeThreshold)
            {
                if (deltaX > 0) MoveRight();
                else MoveLeft();
                
                swipeProcessed = true;
                touchStartPos = currentPos;
            }
        }
        
        // FIN DE TOQUE
        if (isTouching && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            Vector2 endPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector2 delta = endPos - touchStartPos;
            
            // TAP (salto) si no hubo swipe
            if (!swipeProcessed && delta.magnitude < swipeThreshold)
            {
                Jump();
            }
            // SWIPE VERTICAL
            else if (!swipeProcessed && Mathf.Abs(delta.y) > swipeThreshold)
            {
                if (delta.y > 0) Jump();
                else Slide();
            }
            
            isTouching = false;
            swipeProcessed = false;
        }
    }
    
    // ========== INPUT TECLADO ==========
    
    void HandleKeyboardInput()
    {
        // MOVIMIENTO LATERAL CON FLECHAS
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            MoveLeft();
        }
        
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            MoveRight();
        }
        
        // SALTO CON ESPACIO
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Jump();
        }
        
        // DESLIZAMIENTO CON FLECHA ABAJO
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            Slide();
        }
    }
    
    // ========== SALTO ==========
    
    void Jump()
    {
        // Solo puede saltar desde el suelo
        if (currentState == PlayerState.Grounded)
        {
            TransitionToState(PlayerState.Jumping);
        }
    }
    
    void CheckAutoJump()
    {
        if (enableAutoJump && Time.time >= nextAutoJumpTime)
        {
            if (currentState == PlayerState.Grounded)
            {
                Jump();
            }
            
            // Programar próximo salto automático
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
    }
    
    void OnLand()
    {
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            TransitionToState(PlayerState.Grounded);
        }
    }
    
    // ========== DESLIZAMIENTO ==========
    
    void Slide()
    {
        if (currentState == PlayerState.Grounded)
        {
            TransitionToState(PlayerState.Sliding);
            // El slide dura 1 segundo
            Invoke("EndSlide", 1f);
        }
    }
    
    void EndSlide()
    {
        if (currentState == PlayerState.Sliding)
        {
            TransitionToState(PlayerState.Grounded);
        }
    }
    
    bool IsSliding()
    {
        // El slide está activo mientras no se haya llamado EndSlide
        return currentState == PlayerState.Sliding;
    }
    
    // ========== UTILIDADES ==========
    
    bool CheckGround()
    {
        if (groundCheck == null) return true;
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", currentState == PlayerState.Jumping);
            animator.SetBool("IsFalling", currentState == PlayerState.Falling);
            animator.SetBool("IsSliding", currentState == PlayerState.Sliding);
            animator.SetFloat("VerticalVelocity", verticalVelocity);
        }
    }
    
    // ========== DEBUG ==========
    
    void OnDrawGizmosSelected()
    {
        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}