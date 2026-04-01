using UnityEngine;

public class RotationAnimator : MonoBehaviour
{
    public enum RotationType
    {
        U, UPrime, D, DPrime, R, RPrime, L, LPrime, F, FPrime, B, BPrime
    }

    [Header("References")]
    [SerializeField] private CubeManager cubeManager;
    [SerializeField] private CubeRotations cubeRotations;
    [SerializeField] private CubeVisual cubeVisual;

    [Header("State Sync")]
    [SerializeField, Tooltip("After animation, sync CubeState from the rotated pieces.")]
    private bool syncCubeStateFromPieces = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    /// <summary>True while a rotation is in progress.</summary>
    public bool isAnimating => cubeManager != null && cubeManager.IsRotating;

    /// <summary>Invoked when a rotation starts.</summary>
    public event System.Action OnRotationStart;
    public event System.Action OnRotationComplete;

    private void Awake()
    {
        if (cubeManager == null) cubeManager = GetComponent<CubeManager>();
        if (cubeRotations == null) cubeRotations = GetComponent<CubeRotations>();
        if (cubeVisual == null) cubeVisual = GetComponent<CubeVisual>();
    }

    /// <summary>Animate a move and then apply the logical rotation.</summary>
    public void AnimateAndApplyRotation(RotationType type)
    {
        if (cubeManager == null) { Log("Missing CubeManager reference."); return; }
        if (isAnimating) { return; }

        if (!TryGetRotationData(type, out CubeManager.Axis axis, out int layerIndex, out bool clockwise, out System.Action applyRotation))
        {
            Log($"Unsupported rotation type: {type}");
            return;
        }

        OnRotationStart?.Invoke();
        cubeManager.RotateLayer(axis, layerIndex, clockwise, () =>
        {
            applyRotation?.Invoke();
            if (syncCubeStateFromPieces && cubeVisual != null)
            {
                cubeVisual.SyncStateFromPieces();
            }
            OnRotationComplete?.Invoke();
        });
    }

    private bool TryGetRotationData(
        RotationType type,
        out CubeManager.Axis axis,
        out int layerIndex,
        out bool clockwise,
        out System.Action applyRotation)
    {
        axis = CubeManager.Axis.Y;
        layerIndex = 0;
        clockwise = true;
        applyRotation = null;

        switch (type)
        {
            case RotationType.U:
                axis = CubeManager.Axis.Y; layerIndex = 1; clockwise = true; applyRotation = cubeRotations != null ? cubeRotations.RotateU : null; return true;
            case RotationType.UPrime:
                axis = CubeManager.Axis.Y; layerIndex = 1; clockwise = false; applyRotation = cubeRotations != null ? cubeRotations.RotateUPrime : null; return true;
            case RotationType.D:
                axis = CubeManager.Axis.Y; layerIndex = 0; clockwise = true; applyRotation = cubeRotations != null ? cubeRotations.RotateD : null; return true;
            case RotationType.DPrime:
                axis = CubeManager.Axis.Y; layerIndex = 0; clockwise = false; applyRotation = cubeRotations != null ? cubeRotations.RotateDPrime : null; return true;
            case RotationType.R:
                axis = CubeManager.Axis.X; layerIndex = 1; clockwise = true; applyRotation = cubeRotations != null ? cubeRotations.RotateR : null; return true;
            case RotationType.RPrime:
                axis = CubeManager.Axis.X; layerIndex = 1; clockwise = false; applyRotation = cubeRotations != null ? cubeRotations.RotateRPrime : null; return true;
            case RotationType.L:
                axis = CubeManager.Axis.X; layerIndex = 0; clockwise = true; applyRotation = cubeRotations != null ? cubeRotations.RotateL : null; return true;
            case RotationType.LPrime:
                axis = CubeManager.Axis.X; layerIndex = 0; clockwise = false; applyRotation = cubeRotations != null ? cubeRotations.RotateLPrime : null; return true;
            case RotationType.F:
                axis = CubeManager.Axis.Z; layerIndex = 1; clockwise = true; applyRotation = cubeRotations != null ? cubeRotations.RotateF : null; return true;
            case RotationType.FPrime:
                axis = CubeManager.Axis.Z; layerIndex = 1; clockwise = false; applyRotation = cubeRotations != null ? cubeRotations.RotateFPrime : null; return true;
            case RotationType.B:
                axis = CubeManager.Axis.Z; layerIndex = 0; clockwise = true; applyRotation = cubeRotations != null ? cubeRotations.RotateB : null; return true;
            case RotationType.BPrime:
                axis = CubeManager.Axis.Z; layerIndex = 0; clockwise = false; applyRotation = cubeRotations != null ? cubeRotations.RotateBPrime : null; return true;
            default:
                return false;
        }
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
