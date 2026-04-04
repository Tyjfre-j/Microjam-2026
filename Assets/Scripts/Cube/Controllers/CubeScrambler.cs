using UnityEngine;
using System.Collections.Generic;
<<<<<<< HEAD

public class CubeScrambler : MonoBehaviour
{
    [SerializeField] private int scrambleMoves = 10;
=======
using System.Collections;

public class CubeScrambler : MonoBehaviour
{
    [SerializeField] private int scrambleMoves = 20;
    [SerializeField] private FaceChildrenRemover faceChildrenRemover;
    [SerializeField] private RotationAnimator rotationAnimator;
    [SerializeField] private RotationAnimatorInput rotationAnimatorInput;
    [SerializeField] private PlayerController playerController;
    [SerializeField, Tooltip("Optional spawn point used when enabling the player after startup rotations.")]
    private Transform playerSpawnPoint;
    [SerializeField, Tooltip("If enabled, the player is hidden during startup shuffle and appears only after rotations finish.")]
    private bool spawnPlayerAfterStartupRotations = true;
    [SerializeField] private int solvedRetryCount = 1;

    public event System.Action OnStartupShuffleCompleted;
    public bool IsStartupShuffleComplete { get; private set; }
>>>>>>> origin/dev

    private CubeState cubeState;
    private CubeRotations cubeRotations;
    private CubeVisual cubeVisual;
    private CubeManager cubeManager;

    private struct Move
    {
        public CubeManager.Axis axis;
        public int layerIndex;
        public bool clockwise;
<<<<<<< HEAD
=======
        public RotationAnimator.RotationType rotationType;
>>>>>>> origin/dev
        public System.Action applyState;
    }

    private List<Move> moves;

    private void Awake()
    {
        cubeState = GetComponent<CubeState>();
        cubeRotations = GetComponent<CubeRotations>();
        cubeVisual = GetComponent<CubeVisual>();
        cubeManager = GetComponent<CubeManager>();
<<<<<<< HEAD
=======
        if (faceChildrenRemover == null) faceChildrenRemover = GetComponent<FaceChildrenRemover>();
        if (faceChildrenRemover == null) faceChildrenRemover = FindAnyObjectByType<FaceChildrenRemover>();
        if (rotationAnimator == null) rotationAnimator = GetComponent<RotationAnimator>();
        if (rotationAnimatorInput == null) rotationAnimatorInput = GetComponent<RotationAnimatorInput>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
>>>>>>> origin/dev

        if (cubeState == null) { return; }
        if (cubeRotations == null) { return; }
        if (cubeManager == null) { return; }

        moves = new List<Move>
        {
<<<<<<< HEAD
            new Move { axis = CubeManager.Axis.Y, layerIndex = 1, clockwise = true,  applyState = cubeRotations.RotateU },
            new Move { axis = CubeManager.Axis.Y, layerIndex = 1, clockwise = false, applyState = cubeRotations.RotateUPrime },
            new Move { axis = CubeManager.Axis.Y, layerIndex = 0, clockwise = true,  applyState = cubeRotations.RotateD },
            new Move { axis = CubeManager.Axis.Y, layerIndex = 0, clockwise = false, applyState = cubeRotations.RotateDPrime },
            new Move { axis = CubeManager.Axis.X, layerIndex = 1, clockwise = true,  applyState = cubeRotations.RotateR },
            new Move { axis = CubeManager.Axis.X, layerIndex = 1, clockwise = false, applyState = cubeRotations.RotateRPrime },
            new Move { axis = CubeManager.Axis.X, layerIndex = 0, clockwise = true,  applyState = cubeRotations.RotateL },
            new Move { axis = CubeManager.Axis.X, layerIndex = 0, clockwise = false, applyState = cubeRotations.RotateLPrime },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 1, clockwise = true,  applyState = cubeRotations.RotateF },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 1, clockwise = false, applyState = cubeRotations.RotateFPrime },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 0, clockwise = true,  applyState = cubeRotations.RotateB },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 0, clockwise = false, applyState = cubeRotations.RotateBPrime },
=======
            new Move { axis = CubeManager.Axis.Y, layerIndex = 1, clockwise = true,  rotationType = RotationAnimator.RotationType.U,      applyState = cubeRotations.RotateU },
            new Move { axis = CubeManager.Axis.Y, layerIndex = 1, clockwise = false, rotationType = RotationAnimator.RotationType.UPrime, applyState = cubeRotations.RotateUPrime },
            new Move { axis = CubeManager.Axis.Y, layerIndex = 0, clockwise = true,  rotationType = RotationAnimator.RotationType.D,      applyState = cubeRotations.RotateD },
            new Move { axis = CubeManager.Axis.Y, layerIndex = 0, clockwise = false, rotationType = RotationAnimator.RotationType.DPrime, applyState = cubeRotations.RotateDPrime },
            new Move { axis = CubeManager.Axis.X, layerIndex = 1, clockwise = true,  rotationType = RotationAnimator.RotationType.R,      applyState = cubeRotations.RotateR },
            new Move { axis = CubeManager.Axis.X, layerIndex = 1, clockwise = false, rotationType = RotationAnimator.RotationType.RPrime, applyState = cubeRotations.RotateRPrime },
            new Move { axis = CubeManager.Axis.X, layerIndex = 0, clockwise = true,  rotationType = RotationAnimator.RotationType.L,      applyState = cubeRotations.RotateL },
            new Move { axis = CubeManager.Axis.X, layerIndex = 0, clockwise = false, rotationType = RotationAnimator.RotationType.LPrime, applyState = cubeRotations.RotateLPrime },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 1, clockwise = true,  rotationType = RotationAnimator.RotationType.F,      applyState = cubeRotations.RotateF },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 1, clockwise = false, rotationType = RotationAnimator.RotationType.FPrime, applyState = cubeRotations.RotateFPrime },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 0, clockwise = true,  rotationType = RotationAnimator.RotationType.B,      applyState = cubeRotations.RotateB },
            new Move { axis = CubeManager.Axis.Z, layerIndex = 0, clockwise = false, rotationType = RotationAnimator.RotationType.BPrime, applyState = cubeRotations.RotateBPrime },
>>>>>>> origin/dev
        };
    }

    private void Start()
    {
        if (cubeRotations == null || cubeState == null || cubeManager == null || moves == null || moves.Count == 0)
        {
            return;
        }

<<<<<<< HEAD
        cubeState.InitSolvedState();
        Scramble(scrambleMoves);

        cubeState.DebugPrintState();

        if (cubeVisual != null)
        {
            cubeVisual.ApplyStickersFromState();
=======
        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        IsStartupShuffleComplete = false;
        PreparePlayerForStartupShuffle();
        SetInputEnabled(false);

        cubeState.InitSolvedState();
        if (cubeVisual != null)
        {
            cubeVisual.ApplyStickersFromState();
            cubeVisual.ApplyStartupFaceMeshVisibilityByMaterial();
        }

        if (faceChildrenRemover != null)
        {
            faceChildrenRemover.ResetStartupBuildState();
            faceChildrenRemover.RemoveAllForStartup();
        }

        yield return AnimatedShuffleWithRetry(scrambleMoves, Mathf.Max(0, solvedRetryCount));

        if (faceChildrenRemover != null)
        {
            faceChildrenRemover.MarkStartupBuildCompleted();
            faceChildrenRemover.RespawnAllForStartup();
        }

        if (cubeVisual != null)
        {
            cubeVisual.SyncStateFromPieces();
            cubeVisual.DisableAllFaceMeshes();
        }

        cubeState.DebugPrintState();
        SetInputEnabled(true);
        SpawnPlayerAfterStartupShuffle();
        IsStartupShuffleComplete = true;
        OnStartupShuffleCompleted?.Invoke();
    }

    private IEnumerator AnimatedShuffleWithRetry(int numMoves, int retryCount)
    {
        yield return AnimatedShuffleSequence(numMoves);

        int retries = 0;
        while (numMoves > 0 && cubeState != null && cubeState.IsSolved() && retries < retryCount)
        {
            retries++;
            yield return AnimatedShuffleSequence(numMoves);
        }
    }

    private IEnumerator AnimatedShuffleSequence(int numMoves)
    {
        if (numMoves <= 0) yield break;

        for (int i = 0; i < numMoves; i++)
        {
            int index = Random.Range(0, moves.Count);
            Move move = moves[index];

            if (rotationAnimator == null)
            {
                cubeManager.RotateLayerImmediate(move.axis, move.layerIndex, move.clockwise);
                move.applyState?.Invoke();
                continue;
            }

            bool completed = false;
            System.Action handler = () => completed = true;
            rotationAnimator.OnRotationComplete += handler;
            rotationAnimator.AnimateAndApplyRotation(move.rotationType);

            float timeout = 4f;
            float elapsed = 0f;
            while (!completed && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            rotationAnimator.OnRotationComplete -= handler;

            if (!completed)
            {
                cubeManager.RotateLayerImmediate(move.axis, move.layerIndex, move.clockwise);
                move.applyState?.Invoke();
            }
>>>>>>> origin/dev
        }
    }

    /// <summary>Scramble the cube by performing a sequence of random moves.</summary>
    public void Scramble(int numMoves)
    {
        if (moves == null || moves.Count == 0 || cubeManager == null)
        {
            return;
        }

        for (int i = 0; i < numMoves; i++)
        {
            int index = Random.Range(0, moves.Count);
            Move move = moves[index];
            cubeManager.RotateLayerImmediate(move.axis, move.layerIndex, move.clockwise);
            move.applyState?.Invoke();
        }

        if (numMoves > 0 && cubeState.IsSolved())
        {
            // Rare edge case: re-scramble if we accidentally end up solved.
            for (int i = 0; i < numMoves; i++)
            {
                int index = Random.Range(0, moves.Count);
                Move move = moves[index];
                cubeManager.RotateLayerImmediate(move.axis, move.layerIndex, move.clockwise);
                move.applyState?.Invoke();
            }
        }

        if (cubeVisual != null)
        {
            cubeVisual.SyncStateFromPieces();
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        if (rotationAnimatorInput != null)
        {
            rotationAnimatorInput.SetInputEnabled(enabled);
        }
    }

    private void PreparePlayerForStartupShuffle()
    {
        if (!spawnPlayerAfterStartupRotations || playerController == null)
        {
            return;
        }

        playerController.Freeze();
        if (playerController.gameObject.activeSelf)
        {
            playerController.gameObject.SetActive(false);
        }
    }

    private void SpawnPlayerAfterStartupShuffle()
    {
        if (!spawnPlayerAfterStartupRotations || playerController == null)
        {
            return;
        }

        Transform playerTransform = playerController.transform;
        if (playerSpawnPoint != null)
        {
            playerTransform.SetPositionAndRotation(playerSpawnPoint.position, playerSpawnPoint.rotation);
        }

        if (!playerController.gameObject.activeSelf)
        {
            playerController.gameObject.SetActive(true);
        }

        playerController.Unfreeze();
    }
}
