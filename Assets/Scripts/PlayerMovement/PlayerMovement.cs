using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // --- Components & Input ---
    private Rigidbody2D rb;
    private PlayerControls playerControls;
    private InputAction moveAction;

    // --- Movement Variables ---
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    private float moveInput;
    private bool isFacingRight = true;

    // --- Jumping Variables ---
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float jumpCutMultiplier = 0.5f; // NEW: For variable jump height
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    // --- Buffers & Timers (NEW) ---
    [Header("Timers & Buffers")]
    [SerializeField] private float coyoteTime = 0.1f; // How long to still be able to jump after leaving ground
    private float coyoteTimeCounter;

    [SerializeField] private float jumpBufferTime = 0.15f; // How long to remember a jump press before landing
    private float jumpBufferTimeCounter;

    // --- Dashing Variables ---
    [Header("Dashing")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;
    private float dashTimer;
    private float dashCooldownTimer;
    private float originalGravity;

    // --- Setup ---
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        moveAction = playerControls.Player.Move;
        moveAction.Enable();

        playerControls.Player.Jump.Enable();
        playerControls.Player.Jump.performed += OnJumpPerformed; // MODIFIED: Was OnJump
        playerControls.Player.Jump.canceled += OnJumpCanceled; // NEW: For variable jump

        playerControls.Player.Dash.Enable();
        playerControls.Player.Dash.performed += OnDash;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        playerControls.Player.Jump.performed -= OnJumpPerformed;
        playerControls.Player.Jump.canceled -= OnJumpCanceled; // NEW
        playerControls.Player.Jump.Disable();
        playerControls.Player.Dash.performed -= OnDash;
        playerControls.Player.Dash.Disable();
    }

    // --- Frame-by-Frame Logic ---
    private void Update()
    {
        // --- Exit update if dashing ---
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                StopDash();
            }
            return;
        }

        // --- Ground Check ---
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // --- Coyote Time Logic (NEW) ---
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // --- Jump Buffer Logic (NEW) ---
        jumpBufferTimeCounter -= Time.deltaTime;

        // --- Read Move Input ---
        moveInput = moveAction.ReadValue<Vector2>().x;
        
        // --- Handle Jump Logic (NEW) ---
        // We check for a buffered jump AND if we're in coyote time
        if (jumpBufferTimeCounter > 0f && coyoteTimeCounter > 0f)
        {
            // Perform the jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            
            // Reset counters to prevent multiple jumps
            jumpBufferTimeCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // --- Handle Dash Cooldown ---
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0)
            {
                canDash = true;
            }
        }

        // --- Handle Flipping ---
        FlipCheck();
    }

    // --- Physics-based Logic ---
    private void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    // --- Action Event Methods ---

    // MODIFIED: This function now only sets the buffer
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpBufferTimeCounter = jumpBufferTime;
    }

    // NEW: This function handles variable jump height
    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        // If we are moving upwards and release the jump button, cut the velocity
        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (canDash)
        {
            StartDash();
        }
    }

    // --- Dash Methods ---
    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTimer = dashTime;
        dashCooldownTimer = dashCooldown;
        rb.gravityScale = 0f;
        
        float dashDirection = isFacingRight ? 1 : -1;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);
    }

    private void StopDash()
    {
        isDashing = false;
        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    // --- Helper Methods ---
    private void FlipCheck()
    {
        if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}