using UnityEngine;

public sealed class PlaneRotationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform frontTrigger;
    [SerializeField] private GameObject plane;

    [Header("Rotation Settings")]
    [Tooltip("The rotation the plane takes when 'Upfront'")]
    [SerializeField] private Vector3 upfrontRotationEuler = new Vector3(-90, 0, 0);
    [SerializeField] private float transitionSpeed = 5f;
    
    private Quaternion targetUpfrontRotation;

    void Start()
    {
        // Pre-calculate the upfront rotation once at the start
        targetUpfrontRotation = Quaternion.Euler(upfrontRotationEuler);
    }

    void Update()
    {
        // Check if the plane's current position is mathematically between the camera and trigger
        if (IsBetween(mainCamera.position, frontTrigger.position, plane.transform.position))
        {
            // Rotate towards the "Upfront" state
            plane.transform.rotation = Quaternion.Slerp(
                plane.transform.rotation, 
                targetUpfrontRotation, 
                Time.deltaTime * transitionSpeed
            );
        }
        // Else block removed: Plane will now stay in its last rotation state
    }

    private bool IsBetween(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 direction = end - start;
        float totalDistance = direction.magnitude;
        direction.Normalize();

        Vector3 projectPoint = point - start;
        float dotProduct = Vector3.Dot(projectPoint, direction);

        return dotProduct > 0 && dotProduct < totalDistance;
    }
}