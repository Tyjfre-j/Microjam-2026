using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;
    private bool jumpRequested;
    private bool isInputEnabled = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }

    private void OnEnable()
    {
        if (moveAction != null) { moveAction.action.Enable(); }
        if (jumpAction != null) { jumpAction.action.Enable(); }
    }

    private void OnDisable()
    {
        if (moveAction != null) { moveAction.action.Disable(); }
        if (jumpAction != null) { jumpAction.action.Disable(); }
    }

    private void Update()
    {
        if (!isInputEnabled) { return; }

        if (moveAction != null)
        {
            Vector2 move = moveAction.action.ReadValue<Vector2>();
            moveInput = move.x;
        }

        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (!isInputEnabled) { return; }

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        if (jumpRequested && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        jumpRequested = false;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        rb.linearVelocity = velocity;
    }

    /// <summary>Enable or disable player input.</summary>
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
        if (!isInputEnabled)
        {
            moveInput = 0f;
            jumpRequested = false;
        }
    }
}
