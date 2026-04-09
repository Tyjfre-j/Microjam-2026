using UnityEngine;

public class EnemyHorizontalPatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2.0f;      // How fast it moves
    public float distance = 1.0f;   // How far it goes left and right from start
    public float timeOffset = 0.0f; // To desync different enemies

    private Vector3 localStartPos;

    void Start()
    {
        // Remember where the spawner placed me on the platform
        localStartPos = transform.localPosition;
    }

    void Update()
    {
        // 1. Calculate the horizontal offset using a Sine wave
        float xMovement = Mathf.Sin((Time.time + timeOffset) * speed) * distance;

        // 2. Apply the movement to the LOCAL X axis
        // This ensures he stays on his platform even if the cube rotates
        transform.localPosition = localStartPos + new Vector3(xMovement, 0, 0);

        // 3. Optional: Flip the sprite based on direction
        if (xMovement > 0.01f) transform.localRotation = Quaternion.Euler(0, 0, 0);
        else if (xMovement < -0.01f) transform.localRotation = Quaternion.Euler(0, 180, 0);
    }
}