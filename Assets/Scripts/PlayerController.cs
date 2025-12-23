using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float jumpForce = 10f;
    public float moveSpeed = 5f;
    public float laneSwitchSpeed = 10f;
    public float swipeThreshold = 50f; // Mínimo desplazamiento para considerar un swipe
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    [Header("Lane Positions")]
    public float[] lanePositions = { -2f, 0f, 2f }; // Posiciones de carriles
    public int currentLane = 1; // Carril central por defecto (0: izquierda, 1: centro, 2: derecha)
    
    [Header("Debug")]
    public TMPro.TextMeshProUGUI inputDebugText;
    
    // Componentes
    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;
    private bool isJumping = false;
    
    // Input
    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;
    private bool isTouching = false;
    
    void Start()
    {
        // Obtener componentes
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        // Asegurarse que el Rigidbody está configurado para endless runner
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = true;
        }
        
        // Posicionar en carril inicial
        transform.position = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
    }
    
    void Update()
    {
        // Verificar si está en el suelo
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Movimiento automático hacia adelante
        MoveForward();
        
        // Manejo de input táctil
        HandleTouchInput();
        
        // Actualizar texto debug si existe
        if (inputDebugText != null)
        {
            inputDebugText.text = $"Lane: {currentLane}\nGrounded: {isGrounded}\nJumping: {isJumping}";
        }
        
        // Animaciones
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", isJumping);
            animator.SetFloat("Speed", moveSpeed);
        }
    }
    
    void FixedUpdate()
    {
        // Suavizar cambio de carril
        Vector3 targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, laneSwitchSpeed * Time.fixedDeltaTime);
    }
    
    private void MoveForward()
    {
        // Movimiento constante hacia adelante
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
    
    private void HandleTouchInput()
    {
        // Detectar toque inicial
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            touchStartPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            isTouching = true;
        }
        
        // Detectar fin de toque
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame && isTouching)
        {
            touchEndPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            ProcessSwipe();
            isTouching = false;
        }
        
        // Input alternativo con teclado para testing en editor
        #if UNITY_EDITOR
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Jump();
            }
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                MoveLeft();
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                MoveRight();
            }
        }
        #endif
    }
    
    private void ProcessSwipe()
    {
        Vector2 swipeDelta = touchEndPosition - touchStartPosition;
        
        // Si el desplazamiento es muy pequeño, es un toque (salto)
        if (swipeDelta.magnitude < swipeThreshold)
        {
            Jump();
            return;
        }
        
        // Determinar dirección del swipe
        bool isHorizontalSwipe = Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y);
        
        if (isHorizontalSwipe)
        {
            // Swipe horizontal - cambiar carril
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
            // Swipe vertical - agacharse o deslizarse
            if (swipeDelta.y > 0 && isGrounded)
            {
                // Swipe arriba - salto
                Jump();
            }
            else if (swipeDelta.y < 0 && isGrounded)
            {
                // Swipe abajo - deslizarse
                Slide();
            }
        }
    }
    
    public void Jump()
    {
        if (isGrounded && !isJumping)
        {
            if (rb != null)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            else
            {
                // Alternativa si no hay Rigidbody
                transform.position += Vector3.up * 2f;
            }
            
            isJumping = true;
            
            // Sonido de salto (si tienes sistema de audio)
            // AudioManager.Instance.PlaySound("Jump");
        }
    }
    
    public void MoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
            // Sonido de cambio de carril
            // AudioManager.Instance.PlaySound("Swipe");
        }
    }
    
    public void MoveRight()
    {
        if (currentLane < lanePositions.Length - 1)
        {
            currentLane++;
            // Sonido de cambio de carril
            // AudioManager.Instance.PlaySound("Swipe");
        }
    }
    
    public void Slide()
    {
        if (isGrounded && animator != null)
        {
            animator.SetTrigger("Slide");
            // Invocar para terminar el deslizamiento después de un tiempo
            Invoke("EndSlide", 1f);
            // Sonido de deslizamiento
            // AudioManager.Instance.PlaySound("Slide");
        }
    }
    
    private void EndSlide()
    {
        // Lógica para terminar el deslizamiento
        if (animator != null)
        {
            animator.ResetTrigger("Slide");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Detectar cuando toca el suelo
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
        }
        
        // Detectar colisión con obstáculos
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            GameOver();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Detectar recolección de monedas/items
        if (other.CompareTag("Coin"))
        {
            CollectCoin(other.gameObject);
        }
    }
    
    private void CollectCoin(GameObject coin)
    {
        // Destruir la moneda
        Destroy(coin);
        
        // Aumentar puntuación
        // ScoreManager.Instance.AddScore(10);
        
        // Sonido de moneda
        // AudioManager.Instance.PlaySound("Coin");
    }
    
    private void GameOver()
    {
        // Lógica de game over
        moveSpeed = 0f;
        
        // Activar animación de muerte
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // Mostrar pantalla de game over
        // GameManager.Instance.GameOver();
        
        // Sonido de game over
        // AudioManager.Instance.PlaySound("GameOver");
    }
    
    // Método para aumentar dificultad progresiva
    public void IncreaseSpeed(float increment)
    {
        moveSpeed += increment;
    }
    
    // Método para resetear jugador
    public void ResetPlayer()
    {
        currentLane = 1;
        transform.position = new Vector3(lanePositions[currentLane], 1f, 0f);
        isJumping = false;
        moveSpeed = 5f;
        
        if (animator != null)
        {
            animator.Rebind();
        }
    }
    
    // Dibujar gizmos para debugging
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Dibujar posiciones de carriles
        Gizmos.color = Color.blue;
        foreach (float lanePos in lanePositions)
        {
            Vector3 pos = new Vector3(lanePos, transform.position.y, transform.position.z);
            Gizmos.DrawWireCube(pos, new Vector3(1f, 2f, 0.5f));
        }
    }
}