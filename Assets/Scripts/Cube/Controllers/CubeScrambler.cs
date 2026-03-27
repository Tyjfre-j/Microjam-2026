using UnityEngine;
using System.Collections.Generic;

public class CubeScrambler : MonoBehaviour
{
    [SerializeField] private int scrambleMoves = 10;

    private CubeState cubeState;
    private CubeRotations cubeRotations;
    private List<System.Action> rotations;

    private void Awake()
    {
        cubeState = GetComponent<CubeState>();
        cubeRotations = GetComponent<CubeRotations>();

        if (cubeState == null)
        {
            Debug.LogError("[CubeScrambler] Missing CubeState on CubeManager.");
            return;
        }

        if (cubeRotations == null)
        {
            Debug.LogError("[CubeScrambler] Missing CubeRotations on CubeManager.");
            return;
        }

        rotations = new List<System.Action>
        {
            cubeRotations.RotateU,
            cubeRotations.RotateUPrime,
            cubeRotations.RotateD,
            cubeRotations.RotateDPrime,
            cubeRotations.RotateR,
            cubeRotations.RotateRPrime,
            cubeRotations.RotateL,
            cubeRotations.RotateLPrime,
            cubeRotations.RotateF,
            cubeRotations.RotateFPrime,
            cubeRotations.RotateB,
            cubeRotations.RotateBPrime,
        };
    }

    private void Start()
    {
        if (cubeRotations == null || cubeState == null || rotations == null || rotations.Count == 0)
        {
            return;
        }

        cubeState.InitSolvedState();
        Scramble(scrambleMoves);

        Debug.Log($"Cube scrambled with {scrambleMoves} moves");
        cubeState.DebugPrintState();
    }

    /// <summary>Scramble the cube by performing a sequence of random moves.</summary>
    public void Scramble(int numMoves)
    {
        if (rotations == null || rotations.Count == 0)
        {
            return;
        }

        for (int i = 0; i < numMoves; i++)
        {
            int index = Random.Range(0, rotations.Count);
            rotations[index]?.Invoke();
        }

        if (numMoves > 0 && cubeState.IsSolved())
        {
            // Rare edge case: re-scramble if we accidentally end up solved.
            for (int i = 0; i < numMoves; i++)
            {
                int index = Random.Range(0, rotations.Count);
                rotations[index]?.Invoke();
            }
        }
    }
}
