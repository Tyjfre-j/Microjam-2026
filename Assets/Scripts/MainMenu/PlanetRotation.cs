using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Degrees per second on each axis")]
    public Vector3 rotationSpeed = new Vector3(0, 5f, 0); 

    void Update()
    {
        // Space.Self ensures it rotates around its own center, 
        // not the world's coordinates.
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}