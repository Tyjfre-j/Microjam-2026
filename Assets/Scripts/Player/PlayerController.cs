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
    [Tooltip("Surfaces tilted more than this from the player's current up are not used to re-align it.")]
    public float maxAlignAngle = 50f;
    public Animator Animator; // Optional reference to animate child objects (like legs)

    [Header("Void Respawn")]
    [Tooltip("Distance from the cube centre beyond which the player counts as fallen into the void.")]
    public float voidDistance = 3f;
    [Tooltip("How far above the top edge of the cube the player reappears before dropping back in.")]
    public float skyHeight = 0.8f;
    [Tooltip("Half size of the cube (pieces at +-0.5, faces at +-1).")]
    public float cubeHalfSize = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Transform cubeCenter;
    private Vector3 lastGroundedPosition;
    private bool hasGrounded;
    public event System.Action OnFellIntoVoid;

    private Rigidbody rb;
    private bool isGrounded;
    private int jumpsRemaining;
    private Vector3 currentUp = Vector3.up;
    private bool isFrozen;
    private static readonly int IsJumpingHash = Animator.StringToHash("isjumping");
    private static readonly int IsWalkingHash = Animator.StringToHash("iswalking");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerController: Rigidbody not found on this GameObject!");
            return;
        }
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Set player to Ignore Raycast layer so he doesn't hit himself
        gameObject.layer = 2;
    }

    private void Update()
    {
        if (isFrozen || rb == null) { return; }
        CheckVoid();
        if (Keyboard.current == null) { return; }

        // 1. JUMP INPUT (Allows exactly 2 jumps)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded || jumpsRemaining > 0)
            {
                ExecuteJump();
            }
        }
    }

    // ── Void respawn ──────────────────────────────────────────────
    private void CheckVoid()
    {
        if (cubeCenter == null)
        {
            var holder = GameObject.Find("CubeHolder");
            if (holder == null) return;
            cubeCenter = holder.transform;
        }
        if ((transform.position - cubeCenter.position).sqrMagnitude > voidDistance * voidDistance)
        {
            RespawnFromSky();
        }
    }

    /// <summary>
    /// Drops the player back in from "the sky": same column it last stood in, but above the
    /// top edge of the current face, on the same face plane. Gravity pulls it back down.
    /// </summary>
    public void RespawnFromSky()
    {
        Vector3 center = cubeCenter != null ? cubeCenter.position : Vector3.zero;
        Vector3 up = currentUp.sqrMagnitude > 0.5f ? currentUp.normalized : Vector3.up;
        Vector3 anchor = hasGrounded ? lastGroundedPosition : spawnPosition;

        // Keep the anchor's position across the face (and its depth on the face plane),
        // replace only the height along "up" with a spot above the cube's top edge.
        Vector3 rel = anchor - center;
        float height = Vector3.Dot(rel, up);
        Vector3 across = rel - up * height;
        Vector3 skyPos = center + across + up * (cubeHalfSize + skyHeight);

        Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, up).sqrMagnitude > 1e-4f
            ? Vector3.ProjectOnPlane(transform.forward, up) : transform.forward, up);

        transform.SetPositionAndRotation(skyPos, rot);
        if (rb != null)
        {
            rb.position = skyPos;
            rb.rotation = rot;
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        }
        currentUp = up;
        jumpsRemaining = 1;
        SetAnimatorBool(IsJumpingHash, true);
        OnFellIntoVoid?.Invoke();
    }

    private void ExecuteJump()
    {
        // Kill existing vertical velocity for a snappy double jump
        SetAnimatorBool(IsJumpingHash, true);
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
        if (rb == null) return;

        if (isFrozen || rb.isKinematic)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            return;
        }

        RaycastHit hit;

        // 2. GRAVITY & ALIGNMENT (No Parenting)
        Vector3 rayStart = transform.position;
        bool rayHit = Physics.Raycast(rayStart, -transform.up, out hit, 5f, walkableLayers);

        // Ignore steep/sideways surfaces (e.g. the side of a slice that just turned):
        // adopting them would tip the thin player onto its edge so it looks flattened.
        if (rayHit && Vector3.Angle(hit.normal, currentUp) > maxAlignAngle)
        {
            rayHit = false;
        }

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

        if (isGrounded)
        {
            lastGroundedPosition = transform.position;
            hasGrounded = true;
            jumpsRemaining = 1;
            SetAnimatorBool(IsJumpingHash, false);
        }

        // 4. MOVEMENT (A/D Only)
        float xInput = 0f;
        bool isMoving = false;
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.aKey.isPressed || kb.leftArrowKey.isPressed))
        {
            xInput = 1f;
            isMoving = true;
        }
        if (kb != null && (kb.dKey.isPressed || kb.rightArrowKey.isPressed))
        {
            xInput = -1f;
            isMoving = true;
        }
                SetAnimatorBool(IsWalkingHash, isMoving);
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
        if (enabled && rb != null && rb.isKinematic && !IsCubeTurning()) { rb.isKinematic = false; rb.WakeUp(); }
        if (isFrozen && rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        }
    }

    private RotationAnimator cubeAnimator;

    private void OnEnable()
    {
        // Re-enabled after the startup shuffle: never come back stuck as kinematic.
        if (rb != null && rb.isKinematic && !IsCubeTurning()) { rb.isKinematic = false; rb.WakeUp(); }
    }

    private bool IsCubeTurning()
    {
        if (cubeAnimator == null) cubeAnimator = FindAnyObjectByType<RotationAnimator>();
        return cubeAnimator != null && cubeAnimator.isAnimating;
    }

    /// <summary>Freeze the player instantly (used during cube rotation).</summary>
    public void Freeze()
    {
        isFrozen = true;
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            // Kinematic while the slice turns so the rotating cube colliders can't shove
            // the player around (that push read as lag and clipped the player into the face).
            rb.isKinematic = true;
        }
    }

    /// <summary>Unfreeze the player after rotation.</summary>
    public void Unfreeze()
    {
        isFrozen = false;
        if (rb != null && rb.isKinematic)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.WakeUp();
        }
    }

    /// <summary>Placeholder for hit reactions.</summary>
    public void TakeHit() { }

    /// <summary>Placeholder for death reactions.</summary>
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    /// <summary>Instantly move the player (used by face teleport). Also becomes the new respawn point.</summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
        Die();
    }

    /// <summary>Respawn the player at its starting point.</summary>
    public void Die()
    {
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        currentUp = spawnRotation * Vector3.up;
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
        }
        SetAnimatorBool(IsJumpingHash, false);
    }

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
            Die(); 
        }
    }

    private void SetAnimatorBool(int param, bool value)
    {
        if (Animator != null)
        {
            Animator.SetBool(param, value);
        }
    }

}
