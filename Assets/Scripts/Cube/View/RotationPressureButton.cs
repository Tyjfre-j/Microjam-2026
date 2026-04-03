using UnityEngine;

public class RotationPressureButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RotationAnimator rotationAnimator;

    [Header("Rotation")]
    [SerializeField] private RotationAnimator.RotationType rotationType = RotationAnimator.RotationType.U;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Tooltip("If true, can trigger when player is already on the button (OnTriggerStay).")]
    private bool allowTriggerOnStay = false;
    [SerializeField, Tooltip("Re-arm only after the player leaves the trigger.")]
    private bool rearmOnExit = true;
    [SerializeField, Tooltip("Minimum seconds between triggers.")]
    private float cooldownSeconds = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool isArmed = true;
    private bool isPlayerOnButton = false;
    private float lastTriggerTime = -999f;

    private void Awake()
    {
        if (rotationAnimator == null)
        {
            rotationAnimator = GetComponentInParent<RotationAnimator>();
        }
    }

    private void OnEnable()
    {
        isArmed = true;
        isPlayerOnButton = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) { return; }
        isPlayerOnButton = true;
        TryTrigger();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!allowTriggerOnStay) { return; }
        if (!IsPlayer(other)) { return; }
        isPlayerOnButton = true;
        TryTrigger();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) { return; }
        isPlayerOnButton = false;
        if (rearmOnExit)
        {
            isArmed = true;
        }
    }

    private void TryTrigger()
    {
        if (!isArmed) { return; }
        if (rotationAnimator == null) { Log("Missing RotationAnimator reference."); return; }
        if (rotationAnimator.isAnimating) { return; }
        if (!isPlayerOnButton) { return; }

        float age = Time.time - lastTriggerTime;
        if (age < cooldownSeconds) { return; }

        isArmed = false;
        lastTriggerTime = Time.time;
        rotationAnimator.AnimateAndApplyRotation(rotationType);
        Log($"Triggered rotation: {rotationType}");
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) { return false; }
        return other.CompareTag(playerTag);
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
