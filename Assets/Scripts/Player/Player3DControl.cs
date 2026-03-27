using UnityEngine;
using UnityEngine.InputSystem;

public class Player3DControl : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private Rigidbody rb;
    private bool isGrounded;
    private float moveInput;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        isGrounded = Physics.Raycast(transform.position, -transform.up, 1.2f, walkableLayer);

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
