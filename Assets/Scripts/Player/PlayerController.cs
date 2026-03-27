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

    private Rigidbody rb;
    private bool isGrounded;
    private float moveInput;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
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
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        }

        if (jumpRequested && isGrounded)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
        jumpRequested = false;

        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        localVelocity.x = moveInput * moveSpeed;
        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }
}
