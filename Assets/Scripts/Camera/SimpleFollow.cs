using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public Transform target; // Drag Player here

    void LateUpdate()
    {
        if (target != null)
        {
            // Smoothly follow the player's position
            transform.position = Vector3.Lerp(transform.position, target.position, 0.1f);
            // Match the player's orientation so the screen stays "upright"
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, 0.1f);
        }
    }
}