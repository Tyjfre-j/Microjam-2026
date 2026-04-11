using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CubeAutoSolver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RotationAnimator rotationAnimator;
    [SerializeField] private RotationAnimatorInput rotationAnimatorInput;
    [SerializeField] private PlayerController playerController;

    [Header("Debug")]
    [SerializeField] private bool allowShortcut = true;
    [SerializeField] private Key solveShortcutKey = Key.F7;
    [SerializeField] private bool showDebugLogs = true;

    private readonly List<RotationAnimator.RotationType> moveHistory = new List<RotationAnimator.RotationType>();
    private Coroutine solveRoutine;
    private bool isReplaying;

    public bool IsSolving => solveRoutine != null;
    public int RecordedMoveCount => moveHistory.Count;

    private void Awake()
    {
        if (rotationAnimator == null) rotationAnimator = GetComponent<RotationAnimator>();
        if (rotationAnimatorInput == null) rotationAnimatorInput = GetComponent<RotationAnimatorInput>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

    private void OnEnable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationCommitted += HandleRotationCommitted;
        }
    }

    private void OnDisable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationCommitted -= HandleRotationCommitted;
        }
    }

    private void Update()
    {
        if (!allowShortcut) return;
        if (TryWasPressedThisFrame(solveShortcutKey))
        {
            StartSolve();
        }
    }

    private bool TryWasPressedThisFrame(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        try
        {
            return keyboard[key].wasPressedThisFrame;
        }
        catch (System.ArgumentOutOfRangeException)
        {
            solveShortcutKey = Key.F7;
            Log("Invalid shortcut key value detected in inspector; reset to F7.");
            return false;
        }
    }

    [ContextMenu("Auto Solve/Start Solve")]
    public void StartSolve()
    {
        if (solveRoutine != null) return;
        if (rotationAnimator == null)
        {
            Log("Cannot solve: RotationAnimator is missing.");
            return;
        }
        if (moveHistory.Count == 0)
        {
            Log("No recorded moves to replay.");
            return;
        }

        solveRoutine = StartCoroutine(SolveByReplay());
    }

    [ContextMenu("Auto Solve/Clear History")]
    public void ClearHistory()
    {
        moveHistory.Clear();
        Log("Auto-solver history cleared.");
    }

    private void HandleRotationCommitted(RotationAnimator.RotationType type)
    {
        if (isReplaying) return;
        moveHistory.Add(type);
    }

    private IEnumerator SolveByReplay()
    {
        isReplaying = true;
        rotationAnimatorInput?.SetInputEnabled(false);
        playerController?.Freeze();

        while (rotationAnimator.isAnimating)
        {
            yield return null;
        }

        while (moveHistory.Count > 0)
        {
            int lastIndex = moveHistory.Count - 1;
            RotationAnimator.RotationType lastMove = moveHistory[lastIndex];
            moveHistory.RemoveAt(lastIndex);

            RotationAnimator.RotationType inverse = InverseOf(lastMove);
            bool completed = false;
            System.Action onComplete = () => completed = true;
            rotationAnimator.OnRotationComplete += onComplete;
            rotationAnimator.AnimateAndApplyRotation(inverse);

            float elapsed = 0f;
            const float timeout = 5f;
            while (!completed && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            rotationAnimator.OnRotationComplete -= onComplete;

            if (!completed)
            {
                Log("Auto-solver stopped: rotation timeout.");
                break;
            }
        }

        isReplaying = false;
        solveRoutine = null;
        rotationAnimatorInput?.SetInputEnabled(true);
        playerController?.Unfreeze();
    }

    private static RotationAnimator.RotationType InverseOf(RotationAnimator.RotationType move)
    {
        switch (move)
        {
            case RotationAnimator.RotationType.U: return RotationAnimator.RotationType.UPrime;
            case RotationAnimator.RotationType.UPrime: return RotationAnimator.RotationType.U;
            case RotationAnimator.RotationType.D: return RotationAnimator.RotationType.DPrime;
            case RotationAnimator.RotationType.DPrime: return RotationAnimator.RotationType.D;
            case RotationAnimator.RotationType.R: return RotationAnimator.RotationType.RPrime;
            case RotationAnimator.RotationType.RPrime: return RotationAnimator.RotationType.R;
            case RotationAnimator.RotationType.L: return RotationAnimator.RotationType.LPrime;
            case RotationAnimator.RotationType.LPrime: return RotationAnimator.RotationType.L;
            case RotationAnimator.RotationType.F: return RotationAnimator.RotationType.FPrime;
            case RotationAnimator.RotationType.FPrime: return RotationAnimator.RotationType.F;
            case RotationAnimator.RotationType.B: return RotationAnimator.RotationType.BPrime;
            case RotationAnimator.RotationType.BPrime: return RotationAnimator.RotationType.B;
            default: return move;
        }
    }

    private void Log(string msg)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[{GetType().Name}] {msg}");
        }
    }
}