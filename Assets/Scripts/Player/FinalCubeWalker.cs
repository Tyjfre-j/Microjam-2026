using UnityEngine;
using UnityEngine.InputSystem;

public class FinalCubeWalker : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 15f;
    public float gravityPower = 40f;

    [Header("Detection")]
    public LayerMask walkableLayers;
    // Distance tr�s courte pour �viter de sauter � l'infini
    public float groundCheckDist = 0.15f;

    private Rigidbody rb;
    private bool isGrounded;
    private int jumpsRemaining; // Pour le double saut
    private Vector3 currentUp = Vector3.up;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // S�curit� : met le joueur sur la couche "Ignore Raycast" pour qu'il ne se touche pas lui-m�me
        gameObject.layer = 2;
    }

    void Update()
    {
        // 1. Capture du saut (Exactement 2 sauts autoris�s)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded || jumpsRemaining > 0)
            {
                ExecuteJump();
            }
        }
    }

    void ExecuteJump()
    {
        // Reset la vitesse verticale locale pour que le 2�me saut soit puissant
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.y = 0;
        rb.linearVelocity = transform.TransformDirection(localVel);

        // Appliquer la force de saut
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        // Logique de compteur
        if (!isGrounded) jumpsRemaining--;
        isGrounded = false;
    }

    void FixedUpdate()
    {
        RaycastHit hit;

        // 2. DETECTION SOL & ALIGNEMENT
        // On tire le rayon depuis le bas du cube
        Vector3 rayStart = transform.position;
        bool rayHit = Physics.Raycast(rayStart, -transform.up, out hit, groundCheckDist + (transform.localScale.y / 2f), walkableLayers);

        // On ne peut �tre "Grounded" que si on ne monte pas � toute vitesse
        Vector3 localV = transform.InverseTransformDirection(rb.linearVelocity);
        isGrounded = rayHit && localV.y <= 0.1f;

        if (rayHit)
        {
            currentUp = hit.normal;
            rb.AddForce(-currentUp * gravityPower, ForceMode.Acceleration);

            Quaternion targetRot = Quaternion.FromToRotation(transform.up, currentUp) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 20f * Time.fixedDeltaTime);

            if (transform.parent != hit.transform) transform.SetParent(hit.transform);
        }
        else
        {
            // Gravit� normale si on est en l'air
            rb.AddForce(-currentUp * gravityPower, ForceMode.Acceleration);

            // Si on tombe loin du cube, on est attir� vers le centre (0,0,0)
            if (!Physics.Raycast(transform.position, -transform.up, 5f, walkableLayers))
            {
                rb.AddForce((Vector3.zero - transform.position).normalized * 20f);
                transform.SetParent(null);
            }
        }

        // 3. RESET DU SAUT
        if (isGrounded) jumpsRemaining = 1;

        // 4. MOUVEMENT (A/D Uniquement)
        float xInput = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) xInput = 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) xInput = -1;

        Vector3 moveVel = transform.right * xInput * moveSpeed;
        Vector3 verticalVel = Vector3.Project(rb.linearVelocity, transform.up);
        rb.linearVelocity = moveVel + verticalVel;

        // 5. ANTI-VIBRATION (Lock Z)
        Vector3 localVelFinal = transform.InverseTransformDirection(rb.linearVelocity);
        localVelFinal.z = 0;
        rb.linearVelocity = transform.TransformDirection(localVelFinal);
    }
}