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
    
    [Header("Slide Settings")]
    public float slideDuration = 1f;
    public float slideHeight = 0.5f;
    private float originalHeight;
    private CapsuleCollider playerCollider;
    
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
        playerCollider = GetComponent<CapsuleCollider>();
        
        if (playerCollider != null)
        {
            originalHeight = playerCollider.height;
        }
        
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
            rb.constraints = RigidbodyConstraints.FreezePositionY | 
                            RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;
        }
    }
    
    void Update()
    {
        // Actualizar estado
        UpdateState();
        
        // Ejecutar lógica del estado actual
        ExecuteState();
        
        // Manejar input
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
        bool wasGrounded = isGrounded;
        isGrounded = CheckGround();
        
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
                verticalVelocity = 0f;
                FreezeYPosition();
                break;
                
            case PlayerState.Jumping:
                UnfreezeYPosition();
                ApplyGravity();
                break;
                
            case PlayerState.Falling:
                UnfreezeYPosition();
                ApplyGravity();
                break;
                
            case PlayerState.Sliding:
                FreezeYPosition();
                break;
        }
    }
    
    void TransitionToState(PlayerState newState)
    {
        ExitState(currentState);
        EnterState(newState);
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
                if (playerCollider != null)
                {
                    playerCollider.height = slideHeight;
                }
                Invoke("EndSlide", slideDuration);
                break;
        }
    }
    
    void ExitState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Sliding:
                if (playerCollider != null)
                {
                    playerCollider.height = originalHeight;
                }
                break;
        }
    }
    
    // ========== MOVIMIENTO ==========
    
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
    
    // ========== SALTO ==========
    
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
    
    void Jump()
    {
        if (currentState == PlayerState.Grounded)
        {
            TransitionToState(PlayerState.Jumping);
        }
    }
    
    void Slide()
    {
        if (currentState == PlayerState.Grounded)
        {
            TransitionToState(PlayerState.Sliding);
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
    
    void OnLand()
    {
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            TransitionToState(PlayerState.Grounded);
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
        return currentState == PlayerState.Sliding;
    }
    
    // ========== COLISIONES CON OBSTÁCULOS ==========
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            HandleObstacleCollision(collision.gameObject);
        }
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            if (currentState == PlayerState.Falling)
            {
                TransitionToState(PlayerState.Grounded);
            }
        }
    }
    
    void HandleObstacleCollision(GameObject obstacle)
    {
        ObstacleType obstacleScript = obstacle.GetComponent<ObstacleType>();
        
        if (obstacleScript != null)
        {
            switch (obstacleScript.obstacleType)
            {
                case ObstacleType.Type.Wide:
                    // Ancho: necesita saltar
                    if (currentState != PlayerState.Jumping)
                    {
                        GameOver();
                    }
                    break;
                    
                case ObstacleType.Type.Long:
                    // Largo: colisión normal
                    GameOver();
                    break;
                    
                case ObstacleType.Type.High:
                    // Alto: necesita deslizarse
                    if (currentState != PlayerState.Sliding)
                    {
                        GameOver();
                    }
                    break;
            }
        }
        else
        {
            // Obstáculo sin tipo
            GameOver();
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
        // ScoreManager.Instance.AddScore(10);
        // AudioManager.Instance.PlaySound("CoinCollect");
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
    
    void GameOver()
    {
        // Tu lógica de Game Over
        Debug.Log("Game Over!");
        moveSpeed = 0f;
        // GameManager.Instance.EndGame();
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