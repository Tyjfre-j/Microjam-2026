using UnityEngine;

public class OscillatingPlatform : MonoBehaviour
{
    public float speed = 2.0f;
    public float distance = 1.0f;
    public float timeOffset = 0.0f;

    private Vector3 localStartPos;

    void Start()
    {
        localStartPos = transform.localPosition;
    }

    void FixedUpdate()
    {
        // Using FixedUpdate to stop the player from lagging/jittering
        float movement = Mathf.Sin((Time.time + timeOffset) * speed) * distance;
        transform.localPosition = localStartPos + new Vector3(0, movement, 0);
    }
}