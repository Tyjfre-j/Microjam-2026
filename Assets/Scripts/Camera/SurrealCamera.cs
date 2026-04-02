using UnityEngine;

public class SurrealCamera : MonoBehaviour
{
    public Transform target;        // Drag your Player here
    public Vector3 offset = new Vector3(0, 2, -7); // X=Side, Y=Above, Z=Distance Behind
    public float smoothSpeed = 0.125f;
    public float rotationSmooth = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Calculate the position we WANT the camera to be in
        // TransformPoint converts our local offset into world space relative to the player
        Vector3 desiredPosition = target.TransformPoint(offset);

        // 2. Smoothly move the camera to that position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 3. Smoothly rotate the camera to match the player's orientation
        // This ensures the "Up" of the screen matches the "Up" of the player's head
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotationSmooth * Time.deltaTime);
    }
}