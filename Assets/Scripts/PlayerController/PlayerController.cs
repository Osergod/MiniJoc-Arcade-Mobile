using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine. SceneManagement;

public class PlayerController :   MonoBehaviour
{
    // === Enums ===
    public enum PlayerState { Grounded, Jumping, Sliding }

    // === Referencias (Inspector) ===
    [Header("References")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Scene Management")]
    public string gameOverSceneName = "GameOver"; // Nombre de la escena a cargar

    // === Configuración general ===
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
    [HideInInspector] public float originalHeight;

    [Header("Auto Jump Settings")]
    public bool enableAutoJump = true;
    public float autoJumpIntervalMin = 3f;
    public float autoJumpIntervalMax = 8f;
    public float nextAutoJumpTime = 0f;

    [Header("Input Settings")]
    public float swipeThreshold = 50f;

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    // === Variables públicas ===
    [HideInInspector] public float verticalVelocity = 0f;
    [HideInInspector] public bool isGrounded = false;
    [HideInInspector] public int currentLane = 1;
    [HideInInspector] public float targetX;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Animator animator;
    [HideInInspector] public CapsuleCollider playerCollider;

    // === Estados internos ===
    private PlayerState currentState = PlayerState.Grounded;
    private GroundedState groundedState = new GroundedState();
    private JumpingState jumpingState = new JumpingState();
    private SlidingState slidingState = new SlidingState();

    // === Input ===
    private Vector2 touchStartPos;
    private bool isTouching = false;
    private bool swipeProcessed = false;
    
    // === Control de tiempo ===
    private float slideEndTime = 0f;

    #region Unity Methods

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();

        if (playerCollider != null)
            originalHeight = playerCollider.height;

        InitializeRigidbody();
        CalculateTargetX();

        if (enableAutoJump)
            ScheduleNextAutoJump();

        ChangeState(PlayerState.Grounded);
    }

    void Update()
    {
        // Verificar contacto con el suelo
        bool groundContactDetected = CheckGroundContact();
        
        // Depuración del estado actual
        if (enableDebugLogs)
            Debug.Log($"Estado: {currentState} | En suelo (Raycast): {groundContactDetected} | Velocidad Y: {verticalVelocity: F2}");

        // Manejar input
        HandleMobileInput();
        HandleKeyboardInput();

        // Actualizar animaciones
        UpdateAnimations();

        // Ejecutar estado actual
        ExecuteCurrentState();
    }

    void FixedUpdate()
    {
        // Verificar colisiones con el suelo
        isGrounded = CheckGroundContact();

        // Aplicar fuerza de gravedad
        ApplyGravityForce();

        // Movimientos según el estado del jugador
        if (currentState == PlayerState.  Jumping || currentState == PlayerState.Sliding)
            ApplyVerticalVelocity();

        // Movimiento constante hacia adelante y cambio de carril
        MoveForward();
        SmoothLaneSwitch();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            if (enableDebugLogs)
                Debug.Log("OnCollisionEnter: Personaje en el suelo");
        }

        // Detectar colisión con obstáculos
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            OnObstacleHit(collision.gameObject);
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
            if (enableDebugLogs)
                Debug.Log("OnCollisionExit: Personaje fuera del suelo");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
            CollectCoin(other.gameObject);

        // También detectar obstáculos con triggers
        if (other.CompareTag("Obstacle"))
        {
            OnObstacleHit(other.gameObject);
        }
    }

    #endregion

    #region State Machine Control

    void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case PlayerState. Grounded:
                groundedState.UpdateState(this);
                CheckAutoJump();
                break;
            case PlayerState.Jumping:
                jumpingState.UpdateState(this);
                break;
            case PlayerState.Sliding:
                slidingState.UpdateState(this);
                break;
        }
    }

    public void ChangeState(PlayerState newState)
    {
        if (currentState == newState) return;

        ExitCurrentState();
        EnterNewState(newState);
        currentState = newState;

        if (enableDebugLogs)
            Debug.Log($"Cambio de estado a: {newState}");
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
            case PlayerState.Sliding:
                slidingState.EnterState(this);
                break;
        }
    }

    #endregion

    #region Movement

    public void MoveForward()
    {
        Vector3 forwardMovement = transform.forward * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb. position + forwardMovement);
    }

    public void SmoothLaneSwitch()
    {
        Vector3 targetPos = new Vector3(targetX, rb.position.y, rb.position.z);
        Vector3 smoothedPos = Vector3.Lerp(rb.position, targetPos, laneSwitchSpeed * Time.fixedDeltaTime);
        rb.MovePosition(smoothedPos);
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

    public void ApplyVerticalVelocity()
    {
        Vector3 verticalMovement = Vector3.up * verticalVelocity * Time.fixedDeltaTime;
        rb.  MovePosition(rb.position + verticalMovement);
    }

    #endregion

    #region Physics & Detection

    public void ApplyGravityForce()
    {
        if (!  isGrounded)
        {
            verticalVelocity += gravity * Time.fixedDeltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -20f);
        }
        else
        {
            if (verticalVelocity < 0)
                verticalVelocity = 0f;
        }
    }

    public bool CheckGroundContact()
    {
        if (groundCheck == null) return false;
        
        // Usar Raycast en lugar de CheckSphere para mejor precisión
        bool raycastHit = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckRadius, groundLayer);
        bool sphereHit = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        
        return raycastHit || sphereHit;
    }

    #endregion

    #region Lock & Unlock Position

    public void LockYPosition()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.  FreezeRotation;
        }
    }

    public void UnlockYPosition()
    {
        if (rb != null)
        {
            rb.  constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    #endregion

    #region Actions

    public void Jump()
    {
        if (currentState == PlayerState.Grounded && isGrounded)
        {
            verticalVelocity = jumpForce;
            UnlockYPosition();
            ChangeState(PlayerState.Jumping);
            
            if (enableDebugLogs)
                Debug.Log("SALTO INICIADO");
        }
    }

    public void Slide()
    {
        if (currentState == PlayerState.Grounded && isGrounded)
        {
            LockYPosition();
            slideEndTime = Time.time + slideDuration;
            ChangeState(PlayerState.Sliding);
            
            if (enableDebugLogs)
                Debug.Log("DESLIZAMIENTO INICIADO");
        }
    }

    #endregion

    #region Auto Jump

    public void ScheduleNextAutoJump()
    {
        nextAutoJumpTime = Time.time + Random.Range(autoJumpIntervalMin, autoJumpIntervalMax);
    }

    void CheckAutoJump()
    {
        if (enableAutoJump && Time.time >= nextAutoJumpTime && currentState == PlayerState.Grounded && isGrounded)
        {
            Jump();
            ScheduleNextAutoJump();
        }
    }

    #endregion

    #region Obstacle Handling

    /// <summary>
    /// Se llama cuando el jugador colisiona con un obstáculo. 
    /// Carga la escena configurada en el Inspector.
    /// </summary>
    private void OnObstacleHit(GameObject obstacle)
    {
        if (enableDebugLogs)
            Debug.Log($"💥 ¡COLISIÓN CON OBSTÁCULO:  {obstacle.name}!");

        // Pausar el juego brevemente (opcional)
        Time.timeScale = 0.1f;

        // Esperar un pequeño momento y luego cargar la escena
        StartCoroutine(LoadGameOverScene());
    }

    /// <summary>
    /// Corrutina para cargar la escena de Game Over con un pequeño retraso.
    /// </summary>
    private System.Collections.IEnumerator LoadGameOverScene()
    {
        yield return new WaitForSecondsRealtime(0.5f); // Espera 0.5 segundos (en tiempo real)
        
        Time.timeScale = 1f; // Restaurar el tiempo normal

        if (string.IsNullOrEmpty(gameOverSceneName))
        {
            Debug.LogError("ERROR: No se ha asignado el nombre de la escena 'GameOver'");
        }

        Debug.Log($"Cargando escena:  {gameOverSceneName}");
        SceneManager.LoadScene(gameOverSceneName);
    }

    #endregion

    #region Input Handling

    void HandleMobileInput()
    {
        if (Touchscreen.current == null) return;

        if (Touchscreen. current.primaryTouch.press.  wasPressedThisFrame)
        {
            touchStartPos = Touchscreen.current.primaryTouch.position. ReadValue();
            isTouching = true;
            swipeProcessed = false;
        }

        if (isTouching && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 currentPos = Touchscreen.current. primaryTouch.position. ReadValue();
            float deltaX = currentPos.x - touchStartPos.x;

            if (! swipeProcessed && Mathf.Abs(deltaX) > swipeThreshold)
            {
                if (deltaX > 0) MoveRight();
                else MoveLeft();

                swipeProcessed = true;
                touchStartPos = currentPos;
            }
        }

        if (isTouching && Touchscreen. current. primaryTouch.press.wasReleasedThisFrame)
        {
            Vector2 endPos = Touchscreen.current. primaryTouch.position.ReadValue();
            Vector2 delta = endPos - touchStartPos;

            if (! swipeProcessed && delta.magnitude < swipeThreshold)
            {
                Jump();
            }
            else if (! swipeProcessed && Mathf.Abs(delta.  y) > swipeThreshold)
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
        if (Keyboard.current.  leftArrowKey.wasPressedThisFrame) MoveLeft();
        if (Keyboard. current. rightArrowKey.wasPressedThisFrame) MoveRight();
        if (Keyboard. current.spaceKey.wasPressedThisFrame) Jump();
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) Slide();
    }

    #endregion

    #region Animations

    void UpdateAnimations()
    {
        if (animator == null) return;
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", currentState == PlayerState.Jumping);
        animator.SetBool("IsSliding", currentState == PlayerState.Sliding);
        animator.SetFloat("VerticalVelocity", verticalVelocity);
    }

    #endregion

    #region Utilities

    void CollectCoin(GameObject coin)
    {
        coin.SetActive(false);
        Debug.Log("Moneda recolectada!");
    }

    void InitializeRigidbody()
    {
        if (rb != null)
        {
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;
        }
    }

    #endregion

    #region Gizmos

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos. DrawWireSphere(groundCheck.position, groundCheckRadius);
            Gizmos.DrawRay(groundCheck.position, Vector3.down * groundCheckRadius);
        }
    }
    #endif

    #endregion
}