using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // ========== ENUM DE ESTADOS ==========
    public enum PlayerState
    {
        Grounded,
        Jumping,
        Falling,
        Sliding
    }
    
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
    public float groundCheckRadius = 0.5f;
    
    [Header("Slide Settings")]
    public float slideDuration = 1f;
    public float slideHeight = 0.5f;
    public float originalHeight;
    
    [Header("Input Settings")]
    public float swipeThreshold = 50f;
    
    [Header("Auto Jump Settings")]
    public bool enableAutoJump = true;
    public float autoJumpIntervalMin = 3f;
    public float autoJumpIntervalMax = 8f;
    public float nextAutoJumpTime = 0f;
    
    // ========== VARIABLES PÚBLICAS ==========
    [HideInInspector] public float verticalVelocity = 0f;
    [HideInInspector] public bool isGrounded = true;
    [HideInInspector] public int currentLane = 1;
    [HideInInspector] public float targetX;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Animator animator;
    [HideInInspector] public CapsuleCollider playerCollider;
    [HideInInspector] public PlayerState currentState = PlayerState.Grounded;
    
    // ========== PROPERTY PARA LA CÁMARA ==========
    public bool IsSliding { get; private set; }
    
    // ========== ESTADOS ==========
    private GroundedState groundedState = new GroundedState();
    private JumpingState jumpingState = new JumpingState();
    private FallingState fallingState = new FallingState();
    private SlidingState slidingState = new SlidingState();
    
    // Input
    private Vector2 touchStartPos;
    private bool isTouching = false;
    private bool swipeProcessed = false;
    
    // Variables para deslizamiento
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private Coroutine slideCoroutine;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();
        
        if (playerCollider != null)
        {
            originalHeight = playerCollider.height;
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
        }
        
        InitializeRigidbody();
        CalculateTargetX();
        
        if (enableAutoJump)
        {
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
        
        // Iniciar en estado Grounded
        ChangeState(PlayerState.Grounded);
        IsSliding = false;
    }
    
    void InitializeRigidbody()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;
        }
    }
    
    void Update()
    {
        // Ejecutar estado actual
        ExecuteCurrentState();
        
        // Manejar input
        HandleMobileInput();
        HandleKeyboardInput();
        
        // Movimiento horizontal (siempre activo)
        MoveForward();
        SmoothLaneSwitch();
        
        // Actualizar animaciones
        UpdateAnimations();
        
        // Verificar auto salto si está en el suelo
        if (currentState == PlayerState.Grounded)
        {
            CheckAutoJump();
        }
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
    
    void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Grounded:
                groundedState.UpdateState(this);
                break;
            case PlayerState.Jumping:
                jumpingState.UpdateState(this);
                break;
            case PlayerState.Falling:
                fallingState.UpdateState(this);
                break;
            case PlayerState.Sliding:
                slidingState.UpdateState(this);
                break;
        }
    }
    
    public void ChangeState(PlayerState newState)
    {
        // Salir del estado actual
        ExitCurrentState();
        
        // Actualizar estado
        currentState = newState;
        
        // Entrar al nuevo estado
        EnterNewState(newState);
    }
    
    void ExitCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Grounded:
                groundedState.ExitState(this);
                break;
            case PlayerState.Jumping:
                jumpingState.ExitState(this);
                break;
            case PlayerState.Falling:
                fallingState.ExitState(this);
                break;
            case PlayerState.Sliding:
                slidingState.ExitState(this);
                break;
        }
    }
    
    void EnterNewState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Grounded:
                groundedState.EnterState(this);
                IsSliding = false;
                break;
            case PlayerState.Jumping:
                jumpingState.EnterState(this);
                IsSliding = false;
                break;
            case PlayerState.Falling:
                fallingState.EnterState(this);
                IsSliding = false;
                break;
            case PlayerState.Sliding:
                slidingState.EnterState(this);
                IsSliding = true;
                break;
        }
    }
    
    // ========== MOVIMIENTO ==========
    
    public void MoveForward()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
    
    public void SmoothLaneSwitch()
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
        if (currentLane < 2)
        {
            currentLane++;
            CalculateTargetX();
        }
    }
    
    void CalculateTargetX()
    {
        targetX = (currentLane - 1) * laneWidth;
    }
    
    // ========== FÍSICA ==========
    
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
    
    void ApplyVerticalMovement()
    {
        ApplyVerticalVelocity();
    }
    
    public void LockYPosition()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
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
    
    void HandleMobileInput()
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
                if (deltaX > 0) MoveRight();
                else MoveLeft();
                
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
                Jump();
            }
            else if (!swipeProcessed && Mathf.Abs(delta.y) > swipeThreshold)
            {
                if (delta.y > 0) Jump();
                else Slide();
            }
            
            isTouching = false;
            swipeProcessed = false;
        }
    }
    
    void HandleKeyboardInput()
    {
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            MoveLeft();
        }
        
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            MoveRight();
        }
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            Jump();
        }
        
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            Slide();
        }
    }
    
    // ========== ACCIONES ==========
    
    public void Jump()
    {
        if (currentState == PlayerState.Grounded)
        {
            ChangeState(PlayerState.Jumping);
        }
    }
    
    public void Slide()
    {
        Debug.Log("Tecla ABAJO presionada - Slide() llamado");
    
        if (currentState == PlayerState.Grounded)
        {
            Debug.Log("Cambiando a estado Sliding");
            ChangeState(PlayerState.Sliding);
        
            // AÑADE ESTO para que realmente se deslice:
            IsSliding = true;
        
            // Reducir collider inmediatamente
            if (playerCollider != null)
            {
                playerCollider.height = slideHeight;
                playerCollider.center = new Vector3(0f, slideHeight / 2f, 0f);
            }
        
            // Iniciar corrutina para restaurar
            StartCoroutine(EndSlideAfterDuration());
        }
        else
        {
            Debug.Log($"No puede deslizar. Estado actual: {currentState}");
        }
    }

IEnumerator EndSlideAfterDuration()
{
    yield return new WaitForSeconds(slideDuration);
    
    // Restaurar collider
    if (playerCollider != null)
    {
        playerCollider.height = originalHeight;
        playerCollider.center = Vector3.zero;
    }
    
    IsSliding = false;
    
    // Volver a estado Grounded si está en el suelo
    if (isGrounded)
    {
        ChangeState(PlayerState.Grounded);
    }
}
    
    IEnumerator PerformSlide()
    {
        // Configurar collider para deslizamiento
        if (playerCollider != null)
        {
            playerCollider.height = slideHeight;
            playerCollider.center = new Vector3(0f, slideHeight / 2f, 0f);
        }
        
        // Esperar duración del deslizamiento
        yield return new WaitForSeconds(slideDuration);
        
        // Restaurar collider
        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }
        
        // Verificar estado después del deslizamiento
        if (isGrounded)
        {
            ChangeState(PlayerState.Grounded);
        }
        else
        {
            ChangeState(PlayerState.Falling);
        }
        
        slideCoroutine = null;
    }
    
    void CheckAutoJump()
    {
        if (enableAutoJump && Time.time >= nextAutoJumpTime)
        {
            if (currentState == PlayerState.Grounded)
            {
                Jump();
            }
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
    }
    
    // ========== COLISIONES ==========
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
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
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            CollectCoin(other.gameObject);
        }
    }
    
    void CollectCoin(GameObject coin)
    {
        coin.SetActive(false);
        Debug.Log("Moneda recolectada!");
    }
    
    // ========== UTILIDADES ==========
    
    public bool CheckGroundContact()
    {
        if (groundCheck == null) return true;
        bool grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = grounded;
        return grounded;
    }
    
    public void UpdateAnimations()
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
    // Esto solo en Editor
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
    #endif
}