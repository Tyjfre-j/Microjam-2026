<<<<<<< HEAD
using UnityEngine;

public class FinalCubeCamera : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 2, 10);
    public float positionSmooth = 15f;
    public float rotationSmooth = 10f;

    void LateUpdate()
    {
        if (player == null) return;

        // 1. On calcule la position désirée de manière plus robuste
        // On utilise la rotation du joueur pour définir la direction, 
        // mais on force la distance pour éviter le zoom.
        Vector3 targetPosition = player.position + (player.right * offset.x) + (player.up * offset.y) + (player.forward * offset.z);

        // 2. Déplacement fluide
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmooth * Time.deltaTime);

        // 3. Rotation fluide
        // On veut que la caméra regarde toujours le joueur, avec le "Up" du joueur
        Quaternion targetRotation = Quaternion.LookRotation(player.position - transform.position, player.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmooth * Time.deltaTime);
    }
}
=======
using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cubeCenter;
    [SerializeField] private PlayerController playerController;

    [Header("Camera Settings")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float transitionDuration = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool isTransitioning;
    private Coroutine transitionCoroutine;

    private readonly Vector3[] faceNormals =
    {
        Vector3.forward,  // 0 Front
        Vector3.back,     // 1 Back
        Vector3.left,     // 2 Left
        Vector3.right,    // 3 Right
        Vector3.up,       // 4 Top
        Vector3.down      // 5 Bottom
    };

    private readonly Vector3[] faceUps =
    {
        Vector3.up,        // Front
        Vector3.up,        // Back
        Vector3.up,        // Left
        Vector3.up,        // Right
        Vector3.forward,   // Top
        Vector3.back       // Bottom
    };

    private void Start()
    {
        SnapToFace(0);
    }

    /// <summary>Switch the camera to a face index with a smooth transition.</summary>
    public void SwitchToFace(int faceIndex)
    {
        if (isTransitioning) { return; }
        faceIndex = Mathf.Clamp(faceIndex, 0, faceNormals.Length - 1);
        transitionCoroutine = StartCoroutine(SwitchFaceCoroutine(faceIndex));
    }

    private IEnumerator SwitchFaceCoroutine(int faceIndex)
    {
        isTransitioning = true;
        playerController?.Freeze();

        GetFacePose(faceIndex, out Vector3 targetPos, out Quaternion targetRot);
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(startPos, targetPos, eased);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, eased);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        playerController?.Unfreeze();

        isTransitioning = false;
        Log($"Switched to face {faceIndex}");
    }

    private void SnapToFace(int faceIndex)
    {
        GetFacePose(faceIndex, out Vector3 pos, out Quaternion rot);
        transform.position = pos;
        transform.rotation = rot;
        Log($"Snapped to face {faceIndex}");
    }

    private void GetFacePose(int faceIndex, out Vector3 position, out Quaternion rotation)
    {
        Vector3 center = cubeCenter != null ? cubeCenter.position : Vector3.zero;
        Vector3 normal = faceNormals[faceIndex].normalized;
        Vector3 up = faceUps[faceIndex].normalized;

        position = center + (normal * distance);
        rotation = Quaternion.LookRotation(-normal, up);
    }

    private void Log(string msg)
    {
        if (showDebugLogs)
        {
            // Log removed per project request.
        }
    }
}
>>>>>>> origin/dev
