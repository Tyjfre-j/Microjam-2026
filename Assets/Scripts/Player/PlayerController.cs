using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 15f;
    public float gravityPower = 40f;

    [Header("Detection")]
    public LayerMask walkableLayers;
    public float groundCheckDist = 0.15f;
    public Animator Animator; // Optional reference to animate child objects (like legs)

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Rigidbody rb;
    private bool isGrounded;
    private int jumpsRemaining;
    private Vector3 currentUp = Vector3.up;
    private bool isFrozen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerController: Rigidbody not found on this GameObject!");
            return;
        }
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Set player to Ignore Raycast layer so he doesn't hit himself
        gameObject.layer = 2;
        Animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isFrozen) { return; }

        // 1. JUMP INPUT (Allows exactly 2 jumps)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded || jumpsRemaining > 0)
            {
                ExecuteJump();
            }
        }
    }

    private void ExecuteJump()
    { 
        // Kill existing vertical velocity for a snappy double jump
        Animator.SetTrigger("isjumping");
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.y = 0f;
        rb.linearVelocity = transform.TransformDirection(localVel);

        // Apply jump
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        if (!isGrounded) jumpsRemaining--;
        isGrounded = false;
    }

    private void FixedUpdate()
    {
        if (isFrozen)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        RaycastHit hit;

        // 2. GRAVITY & ALIGNMENT (No Parenting)
        Vector3 rayStart = transform.position;
        bool rayHit = Physics.Raycast(rayStart, -transform.up, out hit, 5f, walkableLayers);

        if (rayHit)
        {
            currentUp = hit.normal;
            rb.AddForce(-currentUp * gravityPower, ForceMode.Acceleration);

            // Align rotation to the face
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, currentUp) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 20f * Time.fixedDeltaTime);

            // NOTE: Parenting logic removed as requested.
            // The player will NOT become a child of the platform.
        }
        else
        {
            // Center gravity pull if in mid-air
            rb.AddForce(-currentUp * gravityPower, ForceMode.Acceleration);
        }

        // 3. GROUND CHECK & JUMP RESET
        // Small ray from bottom of cube
        bool isTouching = Physics.Raycast(transform.position, -transform.up, groundCheckDist + (transform.localScale.y / 2f), walkableLayers);

        // Cannot be grounded if moving upwards fast
        Vector3 localV = transform.InverseTransformDirection(rb.linearVelocity);
        isGrounded = isTouching && localV.y <= 0.1f;

        if (isGrounded) jumpsRemaining = 1;

        // 4. MOVEMENT (A/D Only)
        float xInput = 0f;
        bool isMoving = false;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) 
        {
            xInput = 1f;
            isMoving = true;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) 
        {
            xInput = -1f;
            isMoving = true;
        }
        
        Vector3 moveVel = transform.right * xInput * moveSpeed;
        Vector3 verticalVel = Vector3.Project(rb.linearVelocity, transform.up);

        // 5. APPLY VELOCITY & LOCK Z
        Vector3 combinedVel = moveVel + verticalVel;
        Vector3 localFinalVel = transform.InverseTransformDirection(combinedVel);
        localFinalVel.z = 0f; // Prevent drifting off the 2D path
        rb.linearVelocity = transform.TransformDirection(localFinalVel);
    }

    // STUBS FOR OTHER SCRIPTS (To stop GameManager/Camera errors)
    /// <summary>Enable or disable player input.</summary>
    public void SetInputEnabled(bool enabled)
    {
        isFrozen = !enabled;
        if (isFrozen)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>Freeze the player instantly (used during cube rotation).</summary>
    public void Freeze()
    {
        isFrozen = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>Unfreeze the player after rotation.</summary>
    public void Unfreeze()
    {
        isFrozen = false;
    }

    /// <summary>Placeholder for hit reactions.</summary>
    public void TakeHit() { }

    /// <summary>Placeholder for death reactions.</summary>
    public void Die() { }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
       }
         void OnCollisionEnter(Collision collision) 
       {
         if(collision.gameObject.CompareTag("Hazard")) {
        Die(); // This calls the respawn logic we already built
        }
    } 
   // This detects when you enter the "Trigger" zone of an enemy
    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing we hit is tagged Hazard
        if (other.CompareTag("Hazard"))
        {
            Debug.Log("Touched an Enemy! Dying...");
            Die(); 
        }
    }

    
}
