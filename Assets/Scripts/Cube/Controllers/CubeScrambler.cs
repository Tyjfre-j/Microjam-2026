using UnityEngine;
using System.Collections.Generic;

public class CubeScrambler : MonoBehaviour
{
    [SerializeField] private int scrambleMoves = 10;

    private CubeState cubeState;
    private CubeRotations cubeRotations;
    private CubeVisual cubeVisual;
    private CubeManager cubeManager;

    private struct Move
    {
        public CubeManager.Axis axis;
        public int layerIndex;
        public bool clockwise;
        public System.Action applyState;
    }

    private List<Move> moves;

    private void Awake()
    {
        cubeState = GetComponent<CubeState>();
        cubeRotations = GetComponent<CubeRotations>();
        cubeVisual = GetComponent<CubeVisual>();
        cubeManager = GetComponent<CubeManager>();

        if (cubeState == null) { return; }
        if (cubeRotations == null) { return; }
        if (cubeManager == null) { return; }

        moves = new List<Move>
        {
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
        };
    }

    private void Start()
    {
        if (cubeRotations == null || cubeState == null || cubeManager == null || moves == null || moves.Count == 0)
        {
            return;
        }

        cubeState.InitSolvedState();
        Scramble(scrambleMoves);

        cubeState.DebugPrintState();

        if (cubeVisual != null)
        {
            cubeVisual.ApplyStickersFromState();
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
}
