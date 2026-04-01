using System.Collections.Generic;
using UnityEngine;

public class CubeSetupUtility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cubeRoot;

    [Header("Grid Settings")]
    [SerializeField, Tooltip("Optional local offset for the grid center.")]
    private Vector3 gridOriginOffset = Vector3.zero;

    [Header("Scan")]
    [SerializeField, Tooltip("If true, include inactive children when scanning.")]
    private bool includeInactive = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private const int CubeSize = 2;

    private void Awake()
    {
        if (cubeRoot == null) cubeRoot = transform;
    }

    /// <summary>Auto-assign GridPosition on CubePiece components from local position.</summary>
    [ContextMenu("Auto Assign Grid Positions")]
    public void AutoAssignGridPositions()
    {
        if (cubeRoot == null) cubeRoot = transform;

        CubePiece[] pieces = cubeRoot.GetComponentsInChildren<CubePiece>(includeInactive);
        if (pieces.Length == 0)
        {
            Log("Found 0 CubePiece components. Check cubeRoot assignment.");
            return;
        }

        int assigned = 0;
        for (int i = 0; i < pieces.Length; i++)
        {
            CubePiece piece = pieces[i];
            if (piece == null) continue;

            Vector3 local = cubeRoot.InverseTransformPoint(piece.transform.position) - gridOriginOffset;
            Vector3Int grid = new Vector3Int(local.x >= 0f ? 1 : 0, local.y >= 0f ? 1 : 0, local.z >= 0f ? 1 : 0);
            piece.SetGridPosition(grid);
            assigned++;
        }

        Log($"Assigned GridPosition for {assigned} CubePiece objects.");
    }

    /// <summary>Verify grid positions are unique and match the expected 2x2x2 layout.</summary>
    [ContextMenu("Verify Grid Positions")]
    public void VerifyGridPositions()
    {
        if (cubeRoot == null) cubeRoot = transform;

        HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
        List<Vector3Int> duplicates = new List<Vector3Int>();
        int total = 0;
        int invalid = 0;

        CubePiece[] pieces = cubeRoot.GetComponentsInChildren<CubePiece>(includeInactive);
        if (pieces.Length == 0)
        {
            Log("Found 0 CubePiece components. Check cubeRoot assignment.");
            return;
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            CubePiece piece = pieces[i];
            if (piece == null) continue;

            Vector3Int pos = piece.GridPosition;
            total++;

            if (!IsValidGridPosition(pos))
            {
                invalid++;
                Log($"Invalid GridPosition {pos} on '{piece.name}'. Expected values 0 or 1.");
                continue;
            }

            if (!seen.Add(pos))
            {
                duplicates.Add(pos);
            }
        }

        int expected = CubeSize * CubeSize * CubeSize;
        bool countOk = total == expected;
        bool dupOk = duplicates.Count == 0;
        bool invalidOk = invalid == 0;

        if (!countOk) { Log($"Found {total} CubePiece components. Expected {expected}."); }
        if (!dupOk) { Log($"Duplicate GridPosition entries: {string.Join(", ", duplicates)}"); }

        if (countOk && dupOk && invalidOk)
        {
            Log("GridPosition verification passed (unique and in range).");
        }
    }

    private bool IsValidGridPosition(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < CubeSize
            && pos.y >= 0 && pos.y < CubeSize
            && pos.z >= 0 && pos.z < CubeSize;
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
