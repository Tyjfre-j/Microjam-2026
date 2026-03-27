using UnityEngine;

public class GravityBody : MonoBehaviour
{
    public float gravityForce = 20f;
    public float rotationSpeed = 15f;
    public LayerMask walkableLayer;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, -transform.up, out hit, 5f, walkableLayer))
        {
            Vector3 gravityUp = hit.normal; 

           
            rb.AddForce(-gravityUp * gravityForce);

          
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}