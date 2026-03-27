using System.Collections;
using UnityEngine;

public class CubeScrambler : MonoBehaviour
{
    [SerializeField] private int scrambleMoves = 8;

    private PivotCubeController pivotController;
    private CubeState cubeState;
    private (Vector3 axis, string name)[] moves;

    private void Awake()
    {
        pivotController = GetComponent<PivotCubeController>();
        cubeState = GetComponent<CubeState>();

        if (pivotController == null)
        {
            Debug.LogError("[CubeScrambler] Missing PivotCubeController on CubeManager.");
            return;
        }

        if (cubeState == null)
        {
            Debug.LogError("[CubeScrambler] Missing CubeState on CubeManager.");
            return;
        }

        Transform p = pivotController.pivot;
        moves = new (Vector3, string)[]
        {
            (p.right,   "front"),
            (p.right,   "back"),
            (p.up,      "top"),
            (p.up,      "bottom"),
            (p.forward, "right"),
            (p.forward, "left"),
        };
    }

    private IEnumerator Start()
    {
        if (pivotController == null || cubeState == null || moves == null || moves.Length == 0)
        {
            yield break;
        }

        cubeState.InitSolvedState();
        yield return StartCoroutine(ScrambleRoutine(scrambleMoves));

        Debug.Log("Scramble complete");
        cubeState.DebugPrintState();
    }

    /// <summary>Scramble the cube by performing a sequence of random moves.</summary>
    public IEnumerator ScrambleRoutine(int numMoves)
    {
        for (int i = 0; i < numMoves; i++)
        {
            int index = Random.Range(0, moves.Length);
            (Vector3 axis, string name) move = moves[index];

            StartCoroutine(pivotController.RotateSequence(move.axis, move.name));
            yield return new WaitUntil(() => !pivotController.IsRotating);
        }
    }
}
