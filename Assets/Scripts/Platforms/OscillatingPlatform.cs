using UnityEngine;

public class OscillatingPlatform : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 0.8f;
    [SerializeField] private float distance = 0.1f;
    [SerializeField] private float timeOffset = 0.0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Vector3 localStartPos;

    private void Start()
    {
        localStartPos = transform.localPosition;
    }

    private void FixedUpdate()
    {
        // Using FixedUpdate to keep the player stable on moving platforms.
        float movement = Mathf.Sin((Time.time + timeOffset) * speed) * distance;
        transform.localPosition = localStartPos + new Vector3(0f, movement, 0f);
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
