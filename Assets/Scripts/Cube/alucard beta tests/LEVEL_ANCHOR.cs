using UnityEngine;

public class SmartLevelAnchor : MonoBehaviour
{
    private Vector3 mySurfaceDirection;

    void Start()
    {
        // 1. Figure out which side of the cube I am on (Front, Left, Top, etc.)
        // We do this by looking at the folder's starting position
        mySurfaceDirection = transform.localPosition.normalized;
    }

    void LateUpdate()
    {
        // 2. Only activate if we are facing the camera
        float dot = Vector3.Dot(transform.parent.forward, -Camera.main.transform.forward);

        if (dot > 0.4f)
        {
            // 3. THE MAGIC: Force the platforms to stay level with the world
            Vector3 worldUp = Vector3.up;
            Vector3 faceNormal = transform.parent.forward;

            // Handle Top/Bottom cases
            if (Mathf.Abs(Vector3.Dot(worldUp, faceNormal)) > 0.9f) worldUp = Vector3.forward;

            Vector3 projectedUp = Vector3.ProjectOnPlane(worldUp, faceNormal).normalized;

            // Apply the "Upright" rotation
            transform.rotation = Quaternion.LookRotation(-faceNormal, projectedUp);

            // Snap to 90 degrees so platforms aren't crooked
            Vector3 angles = transform.localRotation.eulerAngles;
            angles.z = Mathf.Round(angles.z / 90f) * 90f;
            transform.localRotation = Quaternion.Euler(0, 0, angles.z);
        }
        else
        {
            // If not facing camera, stay flat against the cube face
            transform.localRotation = Quaternion.identity;
        }

        // 4. Stay glued to the surface (Stop the "flying away" bug)
        transform.localPosition = mySurfaceDirection * 0.51f;
    }
}