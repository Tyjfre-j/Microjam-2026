using UnityEngine;

public class UpAndBackPlatform : MonoBehaviour
{
    public float speed = 2.0f;
    public float distance = 1.0f;
    public float timeOffset = 0.0f;

    private Vector3 localStartPos;

    private void Start()
    {
        localStartPos = transform.localPosition;
    }

    private void FixedUpdate()
    {
        // Smooth oscillation like OscillatingPlatform, but clamped to [0, distance].
        float movement = (Mathf.Sin((Time.time + timeOffset) * speed) * 0.5f + 0.5f) * distance;
        transform.localPosition = localStartPos + new Vector3(0f, movement, 0f);
    }
}
