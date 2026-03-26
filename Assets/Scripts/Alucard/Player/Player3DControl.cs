using UnityEngine;

public class Player3DControl : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float jumpForce = 10f;
    public LayerMask walkableLayer;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
     
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        
        isGrounded = Physics.Raycast(transform.position, -transform.up, 1.2f, walkableLayer);

      
        float h = Input.GetAxis("Horizontal");
        
        Vector3 moveDir = transform.right * h * moveSpeed;

        
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        localVelocity.x = h * moveSpeed;
        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }
}