using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    private const int CubeSize = 2;

    [Header("References")]
    [SerializeField] private Transform cubeRoot;

    [Header("Grid Settings")]
    [SerializeField, Tooltip("Distance between adjacent cube pieces in local space.")]
    private float cellSize = 1f;
    [SerializeField, Tooltip("Optional local offset for the grid center.")]
    private Vector3 gridOriginOffset = Vector3.zero;

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Seconds for a single 90° rotation animation.")]
    private float rotationDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private readonly CubePiece[,,] cube = new CubePiece[CubeSize, CubeSize, CubeSize];
    private bool isRotating;
    private Coroutine activeRotationRoutine;

    /// <summary>True while a layer rotation is in progress.</summary>
    public bool IsRotating => isRotating;

    /// <summary>Invoked when a rotation starts (axis, layerIndex, clockwise).</summary>
    public event System.Action<Axis, int, bool> OnRotationStart;
    /// <summary>Invoked when a rotation completes (axis, layerIndex, clockwise).</summary>
    public event System.Action<Axis, int, bool> OnRotationComplete;

    private void Awake()
    {
        if (cubeRoot == null) cubeRoot = transform;
        CachePiecesFromChildren();
    }

    /// <summary>Rotate a layer of the cube by 90 degrees.</summary>
    public void RotateLayer(Axis axis, int layerIndex, bool clockwise)
    {
        RotateLayer(axis, layerIndex, clockwise, null);
    }

    /// <summary>Rotate a layer of the cube by 90 degrees and invoke a callback when done.</summary>
    public void RotateLayer(Axis axis, int layerIndex, bool clockwise, System.Action onComplete)
    {
        if (isRotating) return;
        if (!IsValidLayer(layerIndex))
        {
            Log($"Invalid layer index {layerIndex}. Expected 0 or 1.");
            return;
        }

        List<CubePiece> layerPieces = GetLayerPieces(axis, layerIndex);
        if (layerPieces.Count != 4)
        {
            Log($"Layer selection returned {layerPieces.Count} pieces. Expected 4.");
            return;
        }

        OnRotationStart?.Invoke(axis, layerIndex, clockwise);
        activeRotationRoutine = StartCoroutine(RotateLayerRoutine(axis, layerIndex, clockwise, layerPieces, onComplete));
    }

    /// <summary>Rotate a layer instantly (no animation).</summary>
    public void RotateLayerImmediate(Axis axis, int layerIndex, bool clockwise)
    {
        if (isRotating) return;
        if (!IsValidLayer(layerIndex))
        {
            Log($"Invalid layer index {layerIndex}. Expected 0 or 1.");
            return;
        }

        List<CubePiece> layerPieces = GetLayerPieces(axis, layerIndex);
        if (layerPieces.Count != 4)
        {
            Log($"Layer selection returned {layerPieces.Count} pieces. Expected 4.");
            return;
        }

        OnRotationStart?.Invoke(axis, layerIndex, clockwise);
        ApplyRotationImmediate(axis, layerIndex, clockwise, layerPieces);
        OnRotationComplete?.Invoke(axis, layerIndex, clockwise);
    }

    /// <summary>Rebuild the grid array from CubePiece components in the hierarchy.</summary>
    public void RebuildGridFromPieces()
    {
        CachePiecesFromChildren();
    }

    /// <summary>
    /// Validates that the authoritative gameplay cube root has exactly 8 direct-child
    /// pieces with unique in-range grid positions.
    /// </summary>
    public bool ValidateAuthoritativeCubeLayout(out string error)
    {
        error = string.Empty;

        if (cubeRoot == null)
        {
            error = "cubeRoot is null.";
            return false;
        }

        if (TryValidateDirectChildLayout(cubeRoot, out error))
        {
            return true;
        }

        if (TryFindAuthoritativeCubeRoot(out Transform resolvedRoot, out string resolveError))
        {
            string previousName = cubeRoot != null ? cubeRoot.name : "null";
            cubeRoot = resolvedRoot;
            CachePiecesFromChildren();
            Log($"Auto-resolved cubeRoot from '{previousName}' to '{cubeRoot.name}'.");
            error = string.Empty;
            return true;
        }

        if (!string.IsNullOrEmpty(resolveError))
        {
            error = $"{error} {resolveError}";
        }

        return false;
    }

    private bool TryValidateDirectChildLayout(Transform root, out string error)
    {
        error = string.Empty;
        if (root == null)
        {
            error = "cubeRoot is null.";
            return false;
        }

        int pieceCount = 0;
        HashSet<Vector3Int> uniqueSlots = new HashSet<Vector3Int>();

        foreach (Transform child in root)
        {
            if (child == null) continue;

            CubePiece piece = child.GetComponent<CubePiece>();
            if (piece == null) continue;

            pieceCount++;
            Vector3Int pos = piece.GridPosition;
            if (!IsValidGridPosition(pos))
            {
                error = $"Piece '{piece.name}' has out-of-range grid position {pos}.";
                return false;
            }

            if (!uniqueSlots.Add(pos))
            {
                error = $"Duplicate grid slot detected at {pos}.";
                return false;
            }
        }

        if (pieceCount != 8)
        {
            error = $"Found {pieceCount} direct child pieces under cubeRoot; expected 8.";
            return false;
        }

        if (uniqueSlots.Count != 8)
        {
            error = $"Found {uniqueSlots.Count} unique grid slots; expected 8.";
            return false;
        }

        return true;
    }

    private bool TryFindAuthoritativeCubeRoot(out Transform resolvedRoot, out string error)
    {
        resolvedRoot = null;
        error = "";

        if (cubeRoot == null)
        {
            error = "Cannot auto-resolve cubeRoot because current root is null.";
            return false;
        }

        Transform[] candidates = cubeRoot.GetComponentsInChildren<Transform>(true);
        int bestDepth = int.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null) continue;

            if (!TryValidateDirectChildLayout(candidate, out _))
            {
                continue;
            }

            int depth = GetDepthFromRoot(cubeRoot, candidate);
            if (depth >= 0 && depth < bestDepth)
            {
                bestDepth = depth;
                resolvedRoot = candidate;
            }
        }

        if (resolvedRoot == null)
        {
            error = "No descendant transform with exactly 8 unique direct-child CubePiece entries was found.";
            return false;
        }

        return true;
    }

    private static int GetDepthFromRoot(Transform root, Transform candidate)
    {
        if (root == null || candidate == null) return -1;

        int depth = 0;
        Transform current = candidate;
        while (current != null)
        {
            if (current == root)
            {
                return depth;
            }

            current = current.parent;
            depth++;
        }

        return -1;
    }

    private IEnumerator RotateLayerRoutine(Axis axis, int layerIndex, bool clockwise, List<CubePiece> layerPieces, System.Action onComplete)
    {
        isRotating = true;

        GameObject pivot = new GameObject("LayerPivot");
        pivot.transform.SetParent(cubeRoot, false);
        pivot.transform.localPosition = GetLayerCenterLocal(axis, layerIndex);
        pivot.transform.localRotation = Quaternion.identity;

        for (int i = 0; i < layerPieces.Count; i++)
        {
            if (layerPieces[i] == null) continue;
            layerPieces[i].CachedTransform.SetParent(pivot.transform, true);
        }

        Quaternion startRot = pivot.transform.localRotation;
        float angle = clockwise ? 90f : -90f;
        Vector3 axisLocal = AxisToVector(axis);
        Quaternion endRot = startRot * Quaternion.AngleAxis(angle, axisLocal);

        float t = 0f;
        if (rotationDuration <= 0.0001f)
        {
            pivot.transform.localRotation = endRot;
        }
        else
        {
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, rotationDuration);
                pivot.transform.localRotation = Quaternion.Slerp(startRot, endRot, Mathf.Clamp01(t));
                yield return null;
            }
            pivot.transform.localRotation = endRot;
        }

        ApplyRotationImmediate(axis, layerIndex, clockwise, layerPieces, pivot);
        onComplete?.Invoke();
        OnRotationComplete?.Invoke(axis, layerIndex, clockwise);
    }

    private void CachePiecesFromChildren()
    {
        for (int x = 0; x < CubeSize; x++)
        {
            for (int y = 0; y < CubeSize; y++)
            {
                for (int z = 0; z < CubeSize; z++)
                {
                    cube[x, y, z] = null;
                }
            }
        }

        if (cubeRoot == null) cubeRoot = transform;

        int count = 0;
        bool[] occupied = new bool[CubeSize * CubeSize * CubeSize];

        foreach (Transform child in cubeRoot)
        {
            CubePiece piece = child.GetComponent<CubePiece>();
            if (piece == null) continue;

            Vector3Int pos = piece.GridPosition;

            int slot = ToLinearIndex(pos);
            bool gridPosIsUsable = IsValidGridPosition(pos) && slot >= 0 && slot < occupied.Length && !occupied[slot];

            if (!gridPosIsUsable)
            {
                // Recover slot from piece local position to avoid a dead cube when inspector grid data is stale.
                Vector3Int recovered = GetClosestFreeGridSlot(piece.CachedTransform.localPosition, occupied);
                if (!IsValidGridPosition(recovered))
                {
                    Log($"Piece '{piece.name}' could not be assigned to a free grid slot.");
                    continue;
                }

                if (!IsValidGridPosition(pos))
                {
                    Log($"Piece '{piece.name}' had invalid grid position {pos}. Recovered as {recovered}.");
                }
                else
                {
                    Log($"Grid slot {pos} already occupied. Recovered '{piece.name}' as {recovered}.");
                }

                pos = recovered;
                slot = ToLinearIndex(pos);
                piece.SetGridPosition(pos);
            }

            if (slot < 0 || slot >= occupied.Length || occupied[slot])
            {
                continue;
            }

            cube[pos.x, pos.y, pos.z] = piece;
            occupied[slot] = true;
            SnapPieceTransform(piece);
            count++;
        }

        if (count != 8)
        {
            Log($"Cached {count} pieces. Expected 8 for a 2x2x2 cube.");
        }
    }

    private void ApplyRotationToGrid(Axis axis, int layerIndex, bool clockwise)
    {
        CubePiece[,,] original = (CubePiece[,,])cube.Clone();

        for (int x = 0; x < CubeSize; x++)
        {
            for (int y = 0; y < CubeSize; y++)
            {
                for (int z = 0; z < CubeSize; z++)
                {
                    bool inLayer = axis == Axis.X ? x == layerIndex
                        : axis == Axis.Y ? y == layerIndex
                        : z == layerIndex;

                    if (!inLayer) continue;

                    Vector3Int from = new Vector3Int(x, y, z);
                    Vector3Int to = RotateGridPosition(from, axis, clockwise);
                    cube[to.x, to.y, to.z] = original[x, y, z];
                    if (cube[to.x, to.y, to.z] != null)
                    {
                        cube[to.x, to.y, to.z].SetGridPosition(to);
                    }
                }
            }
        }
    }

    private Vector3Int RotateGridPosition(Vector3Int pos, Axis axis, bool clockwise)
    {
        int sx = IndexToSign(pos.x);
        int sy = IndexToSign(pos.y);
        int sz = IndexToSign(pos.z);

        if (axis == Axis.X)
        {
            int ny = clockwise ? -sz : sz;
            int nz = clockwise ? sy : -sy;
            return new Vector3Int(pos.x, SignToIndex(ny), SignToIndex(nz));
        }
        if (axis == Axis.Y)
        {
            int nx = clockwise ? sz : -sz;
            int nz = clockwise ? -sx : sx;
            return new Vector3Int(SignToIndex(nx), pos.y, SignToIndex(nz));
        }

        int nxZ = clockwise ? -sy : sy;
        int nyZ = clockwise ? sx : -sx;
        return new Vector3Int(SignToIndex(nxZ), SignToIndex(nyZ), pos.z);
    }

    private void SnapPieceTransform(CubePiece piece)
    {
        if (piece == null) return;
        piece.CachedTransform.localPosition = GetLocalPositionFromGrid(piece.GridPosition);
        piece.CachedTransform.localRotation = SnapToRightAngles(piece.CachedTransform.localRotation);
    }

    private List<CubePiece> GetLayerPieces(Axis axis, int layerIndex)
    {
        List<CubePiece> pieces = new List<CubePiece>(4);
        for (int x = 0; x < CubeSize; x++)
        {
            for (int y = 0; y < CubeSize; y++)
            {
                for (int z = 0; z < CubeSize; z++)
                {
                    if (axis == Axis.X && x != layerIndex) continue;
                    if (axis == Axis.Y && y != layerIndex) continue;
                    if (axis == Axis.Z && z != layerIndex) continue;
                    CubePiece piece = cube[x, y, z];
                    if (piece != null) pieces.Add(piece);
                }
            }
        }
        return pieces;
    }

    private Vector3 GetLayerCenterLocal(Axis axis, int layerIndex)
    {
        float center = (CubeSize - 1) * 0.5f;
        float value = (layerIndex - center) * cellSize;
        Vector3 local = gridOriginOffset;
        if (axis == Axis.X) local.x += value;
        if (axis == Axis.Y) local.y += value;
        if (axis == Axis.Z) local.z += value;
        return local;
    }

    private Vector3 GetLocalPositionFromGrid(Vector3Int gridPos)
    {
        float center = (CubeSize - 1) * 0.5f;
        float x = (gridPos.x - center) * cellSize;
        float y = (gridPos.y - center) * cellSize;
        float z = (gridPos.z - center) * cellSize;
        return new Vector3(x, y, z) + gridOriginOffset;
    }

    private Vector3Int GetClosestFreeGridSlot(Vector3 pieceLocalPosition, bool[] occupied)
    {
        float bestDist = float.MaxValue;
        Vector3Int best = new Vector3Int(-1, -1, -1);

        for (int x = 0; x < CubeSize; x++)
        {
            for (int y = 0; y < CubeSize; y++)
            {
                for (int z = 0; z < CubeSize; z++)
                {
                    Vector3Int candidate = new Vector3Int(x, y, z);
                    int slot = ToLinearIndex(candidate);
                    if (slot < 0 || slot >= occupied.Length || occupied[slot]) continue;

                    Vector3 target = GetLocalPositionFromGrid(candidate);
                    float dist = (target - pieceLocalPosition).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = candidate;
                    }
                }
            }
        }

        return best;
    }

    private int ToLinearIndex(Vector3Int pos)
    {
        if (!IsValidGridPosition(pos)) return -1;
        return (pos.x * CubeSize * CubeSize) + (pos.y * CubeSize) + pos.z;
    }

    private Vector3 AxisToVector(Axis axis)
    {
        return axis == Axis.X ? Vector3.right : axis == Axis.Y ? Vector3.up : Vector3.forward;
    }

    private bool IsValidLayer(int layerIndex) => layerIndex >= 0 && layerIndex < CubeSize;

    private bool IsValidGridPosition(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < CubeSize
            && pos.y >= 0 && pos.y < CubeSize
            && pos.z >= 0 && pos.z < CubeSize;
    }

    private int IndexToSign(int index) => index == 0 ? -1 : 1;

    private int SignToIndex(int sign) => sign < 0 ? 0 : 1;

    private Quaternion SnapToRightAngles(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;
        return Quaternion.Euler(euler);
    }

    private void ApplyRotationImmediate(Axis axis, int layerIndex, bool clockwise, List<CubePiece> layerPieces, GameObject pivot = null)
    {
        if (pivot == null)
        {
            pivot = new GameObject("LayerPivot");
            pivot.transform.SetParent(cubeRoot, false);
            pivot.transform.localPosition = GetLayerCenterLocal(axis, layerIndex);
            pivot.transform.localRotation = Quaternion.identity;

            for (int i = 0; i < layerPieces.Count; i++)
            {
                if (layerPieces[i] == null) continue;
                layerPieces[i].CachedTransform.SetParent(pivot.transform, true);
            }

            float angle = clockwise ? 90f : -90f;
            Vector3 axisLocal = AxisToVector(axis);
            pivot.transform.localRotation = Quaternion.AngleAxis(angle, axisLocal);
        }

        for (int i = 0; i < layerPieces.Count; i++)
        {
            if (layerPieces[i] == null) continue;
            layerPieces[i].CachedTransform.SetParent(cubeRoot, true);
        }

        ApplyRotationToGrid(axis, layerIndex, clockwise);

        for (int i = 0; i < layerPieces.Count; i++)
        {
            if (layerPieces[i] == null) continue;
            SnapPieceTransform(layerPieces[i]);
        }

        Destroy(pivot);
        isRotating = false;
    }


    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
