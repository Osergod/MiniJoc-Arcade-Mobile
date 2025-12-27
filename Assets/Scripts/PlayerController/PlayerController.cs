using UnityEngine;
using UnityEngine.InputSystem;

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
    public float groundCheckRadius = 0.5f; // Aumentado
    
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
    
    // ========== ESTADOS ==========
    private PlayerState currentState = PlayerState.Grounded;
    private GroundedState groundedState = new GroundedState();
    private JumpingState jumpingState = new JumpingState();
    private FallingState fallingState = new FallingState();
    private SlidingState slidingState = new SlidingState();
    
    // Input
    private Vector2 touchStartPos;
    private bool isTouching = false;
    private bool swipeProcessed = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();
        
        if (playerCollider != null)
        {
            originalHeight = playerCollider.height;
        }
        
        InitializeRigidbody();
        CalculateTargetX();
        
        if (enableAutoJump)
        {
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
        
        // Iniciar en estado Grounded
        ChangeState(PlayerState.Grounded);
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
        ExitCurrentState();
        EnterNewState(newState);
        currentState = newState;
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
                break;
            case PlayerState.Jumping:
                jumpingState.EnterState(this);
                break;
            case PlayerState.Falling:
                fallingState.EnterState(this);
                break;
            case PlayerState.Sliding:
                slidingState.EnterState(this);
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
        // Este método lo llaman los estados en FixedUpdate
        Vector3 movement = new Vector3(0, verticalVelocity * Time.fixedDeltaTime, 0);
        transform.Translate(movement, Space.World);
    }
    
    void ApplyVerticalMovement()
    {
        // Para uso interno
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
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Jump();
        }
        
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
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
        if (currentState == PlayerState.Grounded)
        {
            ChangeState(PlayerState.Sliding);
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
            nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
        }
    }
    
    // ========== COLISIONES ==========
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // Sin muerte
            Debug.Log("Colisión con obstáculo (sin efecto)");
        }
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
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
    }
    
    // ========== UTILIDADES ==========
    
    public bool CheckGroundContact()
    {
        if (groundCheck == null) return true;
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
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
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}