using UnityEngine;

public class RelativeCubeRotator : MonoBehaviour
{
    public Transform target; // The Cube
    public float transitionSpeed = 10f;
    public float distance = 5f;

    private Quaternion targetRotation;

    void Start()
    {
        // Initialize at the current rotation
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // Rotate 90 degrees relative to the CURRENT target rotation
        if (Input.GetKeyDown(KeyCode.UpArrow))
            targetRotation *= Quaternion.Euler(90, 0, 0);
            
        if (Input.GetKeyDown(KeyCode.DownArrow))
            targetRotation *= Quaternion.Euler(-90, 0, 0);
            
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            targetRotation *= Quaternion.Euler(0, 90, 0);
            
        if (Input.GetKeyDown(KeyCode.RightArrow))
            targetRotation *= Quaternion.Euler(0, -90, 0);

        // Smooth Interpolation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);

        // Pivot logic: Keep the camera at 'distance' from the cube
        transform.position = target.position - (transform.forward * distance);
    }
}