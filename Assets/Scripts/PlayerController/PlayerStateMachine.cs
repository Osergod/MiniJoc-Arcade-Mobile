using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    // ========== ENUM DE ESTADOS ==========
    public enum PlayerState
    {
        Grounded,
        Jumping,
        Falling,
        Sliding
    }

    // ========== REFERENCIA AL CONTROLADOR ==========
    private PlayerController playerController;
    private PlayerState currentState = PlayerState.Grounded;

    // ========== REFERENCIAS A ESTADOS (para usar en scripts separados) ==========
    [System.NonSerialized] public GroundedState groundedState;
    [System.NonSerialized] public JumpingState jumpingState;
    [System.NonSerialized] public FallingState fallingState;
    [System.NonSerialized] public SlidingState slidingState;

    // ========== VARIABLES PÚBLICAS PARA ESTADOS ==========
    [HideInInspector] public float verticalVelocity;
    [HideInInspector] public bool isGrounded;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Animator animator;
    [HideInInspector] public CapsuleCollider playerCollider;

    // ========== CONFIGURACIONES PÚBLICAS ==========
    [HideInInspector] public float jumpForce;
    [HideInInspector] public float gravity;
    [HideInInspector] public float slideDuration;
    [HideInInspector] public float slideHeight;
    [HideInInspector] public float originalHeight;
    [HideInInspector] public bool enableAutoJump;
    [HideInInspector] public float autoJumpIntervalMin;
    [HideInInspector] public float autoJumpIntervalMax;
    [HideInInspector] public float nextAutoJumpTime;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController no encontrado en el GameObject");
            return;
        }

        // Inicializar referencias desde PlayerController
        rb = playerController.rb;
        animator = playerController.animator;
        playerCollider = playerController.playerCollider;
        
        // Inicializar configuraciones desde PlayerController
        jumpForce = playerController.jumpForce;
        gravity = playerController.gravity;
        slideDuration = playerController.slideDuration;
        slideHeight = playerController.slideHeight;
        originalHeight = playerController.originalHeight;
        enableAutoJump = playerController.enableAutoJump;
        autoJumpIntervalMin = playerController.autoJumpIntervalMin;
        autoJumpIntervalMax = playerController.autoJumpIntervalMax;
        nextAutoJumpTime = playerController.nextAutoJumpTime;

        // Inicializar estados
        groundedState = new GroundedState();
        jumpingState = new JumpingState();
        fallingState = new FallingState();
        slidingState = new SlidingState();

        // Iniciar en estado Grounded
        ChangeState(groundedState);
        
        Debug.Log("PlayerStateMachine inicializado");
    }

    void Update()
    {
        if (playerController == null) return;

        // Actualizar estado actual
        if (currentState == PlayerState.Grounded)
        {
            groundedState?.UpdateState(this);
        }
        else if (currentState == PlayerState.Jumping)
        {
            jumpingState?.UpdateState(this);
        }
        else if (currentState == PlayerState.Falling)
        {
            fallingState?.UpdateState(this);
        }
        else if (currentState == PlayerState.Sliding)
        {
            slidingState?.UpdateState(this);
        }
        
        // Actualizar animaciones
        playerController.UpdateAnimationStates();
    }

    void FixedUpdate()
    {
        if (playerController == null) return;

        // Aplicar movimiento vertical en FixedUpdate para estados específicos
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            playerController.ApplyVerticalVelocity();
        }
    }

    // ========== MÉTODOS DE TRANSICIÓN ==========

    public void ChangeState(IPlayerState newState)
    {
        ExitCurrentState();
        
        if (newState is GroundedState)
        {
            currentState = PlayerState.Grounded;
            groundedState?.EnterState(this);
        }
        else if (newState is JumpingState)
        {
            currentState = PlayerState.Jumping;
            jumpingState?.EnterState(this);
        }
        else if (newState is FallingState)
        {
            currentState = PlayerState.Falling;
            fallingState?.EnterState(this);
        }
        else if (newState is SlidingState)
        {
            currentState = PlayerState.Sliding;
            slidingState?.EnterState(this);
        }
    }

    void ExitCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Grounded:
                groundedState?.ExitState(this);
                break;
            case PlayerState.Jumping:
                jumpingState?.ExitState(this);
                break;
            case PlayerState.Falling:
                fallingState?.ExitState(this);
                break;
            case PlayerState.Sliding:
                slidingState?.ExitState(this);
                break;
        }
    }

    // ========== MÉTODOS PÚBLICOS PARA EL PLAYERCONTROLLER ==========

    public void RequestJumpAction()
    {
        if (currentState == PlayerState.Grounded)
        {
            ChangeState(jumpingState);
        }
    }

    public void RequestSlideAction()
    {
        if (currentState == PlayerState.Grounded)
        {
            ChangeState(slidingState);
        }
    }

    public void RequestLanding()
    {
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            ChangeState(groundedState);
        }
    }

    // ========== MÉTODOS DE UTILIDAD PARA ESTADOS ==========

    public bool CheckGround()
    {
        return playerController?.CheckGroundContact() ?? false;
    }

    public void MoveForward()
    {
        playerController?.MovePlayerForward();
    }

    public void SmoothLaneSwitch()
    {
        playerController?.SmoothLaneTransition();
    }

    public void ApplyGravity()
    {
        playerController?.ApplyGravityForce();
    }

    public void ApplyVerticalMovement()
    {
        playerController?.ApplyVerticalVelocity();
    }

    public void FreezeYPosition()
    {
        playerController?.LockYPosition();
    }

    public void UnfreezeYPosition()
    {
        playerController?.UnlockYPosition();
    }

    public void Jump()
    {
        RequestJumpAction();
    }

    // ========== MÉTODOS DE CONSULTA ==========

    public PlayerState GetCurrentState()
    {
        return currentState;
    }

    public bool IsPlayerGrounded()
    {
        return currentState == PlayerState.Grounded;
    }

    public bool IsPlayerJumping()
    {
        return currentState == PlayerState.Jumping;
    }

    public bool IsPlayerFalling()
    {
        return currentState == PlayerState.Falling;
    }

    public bool IsPlayerSliding()
    {
        return currentState == PlayerState.Sliding;
    }
}