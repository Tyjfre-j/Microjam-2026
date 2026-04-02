using UnityEngine;

public class FaceRotationPresets : MonoBehaviour
{
    [Header("Face Rotations (Type the Euler angles here)")]
    public Vector3 ifFacingFront = new Vector3(0, 0, 0);
    public Vector3 ifFacingBack = new Vector3(0, 180, 0);
    public Vector3 ifFacingLeft = new Vector3(0, -90, 0);
    public Vector3 ifFacingRight = new Vector3(0, 90, 0);
    public Vector3 ifFacingTop = new Vector3(90, 0, 0);
    public Vector3 ifFacingBottom = new Vector3(-90, 0, 0);

    [Header("Settings")]
    public float snapSpeed = 15f;

    void LateUpdate()
    {
        // 1. Find which way the PARENT cube piece is looking in the world
        Vector3 worldForward = transform.parent.forward;
        Vector3 worldUp = transform.parent.up;
        Vector3 worldRight = transform.parent.right;

        // 2. Decide which preset to use based on the most dominant direction
        // We check if the piece is pointing mostly Up, Down, Left, etc.
        Vector3 targetEuler = ifFacingFront;

        // Check X-axis (Left/Right)
        if (Vector3.Dot(worldForward, Vector3.right) > 0.5f) targetEuler = ifFacingRight;
        else if (Vector3.Dot(worldForward, Vector3.left) > 0.5f) targetEuler = ifFacingLeft;

        // Check Y-axis (Top/Bottom)
        else if (Vector3.Dot(worldForward, Vector3.up) > 0.5f) targetEuler = ifFacingTop;
        else if (Vector3.Dot(worldForward, Vector3.down) > 0.5f) targetEuler = ifFacingBottom;

        // Check Z-axis (Front/Back)
        else if (Vector3.Dot(worldForward, Vector3.forward) > 0.5f) targetEuler = ifFacingFront;
        else if (Vector3.Dot(worldForward, Vector3.back) > 0.5f) targetEuler = ifFacingBack;

        // 3. Apply the rotation LOCALLY
        // This ensures it spins around its center pivot and doesn't fly away
        Quaternion targetRot = Quaternion.Euler(targetEuler);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * snapSpeed);
    }
}