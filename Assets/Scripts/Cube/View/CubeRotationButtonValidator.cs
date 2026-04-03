using System.Collections.Generic;
using UnityEngine;

public class CubeRotationButtonValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CubeRotationButtonSystem buttonSystem;

    [Header("Behavior")]
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        if (buttonSystem == null) buttonSystem = FindAnyObjectByType<CubeRotationButtonSystem>();
    }

    private void Start()
    {
        if (runOnStart)
        {
            RunValidation();
        }
    }

    /// <summary>Validate all faces and all 8 button actions against the expected movement directions.</summary>
    [ContextMenu("Run Button Mapping Validation")]
    public void RunValidation()
    {
        if (buttonSystem == null)
        {
            Log("Missing CubeRotationButtonSystem reference.");
            return;
        }

        List<string> failures = new List<string>();
        foreach (FaceBasis basis in GetAllFaceBases())
        {
            foreach (CubeRotationButtonSystem.ButtonAction action in System.Enum.GetValues(typeof(CubeRotationButtonSystem.ButtonAction)))
            {
                if (!buttonSystem.TryResolveActionForTest(
                        basis.forwardLocal,
                        basis.rightLocal,
                        basis.upLocal,
                        action,
                        out CubeManager.Axis axis,
                        out int layerIndex,
                        out bool clockwise,
                        out RotationAnimator.RotationType type))
                {
                    failures.Add($"{basis.name}:{action} -> failed to resolve.");
                    continue;
                }

                if (!ValidateActionOnFace(basis, action, axis, layerIndex, clockwise))
                {
                    failures.Add($"{basis.name}:{action} -> Axis={axis} Layer={layerIndex} Clockwise={clockwise} Type={type}");
                }
            }
        }

        if (failures.Count == 0)
        {
            Log("All button mappings validated successfully.");
        }
        else
        {
            Log($"Button mapping failures: {failures.Count}");
            for (int i = 0; i < failures.Count; i++)
            {
                Debug.LogWarning($"[CubeRotationButtonValidator] {failures[i]}");
            }
        }
    }

    private bool ValidateActionOnFace(
        FaceBasis basis,
        CubeRotationButtonSystem.ButtonAction action,
        CubeManager.Axis axis,
        int layerIndex,
        bool clockwise)
    {
        // Build the 4 face positions (2x2) in grid space.
        List<Vector3Int> facePositions = new List<Vector3Int>(4);
        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    Vector3Int sign = IndexToSign(pos);
                    if (Vector3.Dot(basis.forwardLocal, sign) > 0.5f)
                    {
                        facePositions.Add(pos);
                    }
                }
            }
        }

        if (facePositions.Count != 4) return false;

        // Pick the two positions that represent the target line/column.
        List<Vector3Int> targets = new List<Vector3Int>(2);
        for (int i = 0; i < facePositions.Count; i++)
        {
            Vector3Int pos = facePositions[i];
            Vector3Int sign = IndexToSign(pos);
            int rightSign = AxisSign(sign, basis.rightLocal);
            int upSign = AxisSign(sign, basis.upLocal);

            if (IsTargetLine(action, rightSign, upSign))
            {
                targets.Add(pos);
            }
        }

        if (targets.Count != 2) return false;

        // Validate each target position moves in the expected direction.
        for (int i = 0; i < targets.Count; i++)
        {
            Vector3Int pos = targets[i];
            if (!IsInLayer(pos, axis, layerIndex)) return false;

            Vector3Int rotated = RotateGridPosition(pos, axis, clockwise);
            Vector3Int rotatedSign = IndexToSign(rotated);
            int newRight = AxisSign(rotatedSign, basis.rightLocal);
            int newUp = AxisSign(rotatedSign, basis.upLocal);

            if (!MatchesExpectedOutcome(action, newRight, newUp))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTargetLine(CubeRotationButtonSystem.ButtonAction action, int rightSign, int upSign)
    {
        switch (action)
        {
            case CubeRotationButtonSystem.ButtonAction.RightColumnUp:
            case CubeRotationButtonSystem.ButtonAction.RightColumnDown:
                return rightSign > 0;
            case CubeRotationButtonSystem.ButtonAction.LeftColumnUp:
            case CubeRotationButtonSystem.ButtonAction.LeftColumnDown:
                return rightSign < 0;
            case CubeRotationButtonSystem.ButtonAction.TopLineLeft:
            case CubeRotationButtonSystem.ButtonAction.TopLineRight:
                return upSign > 0;
            case CubeRotationButtonSystem.ButtonAction.BottomLineLeft:
            case CubeRotationButtonSystem.ButtonAction.BottomLineRight:
                return upSign < 0;
            default:
                return false;
        }
    }

    private static bool MatchesExpectedOutcome(CubeRotationButtonSystem.ButtonAction action, int rightSign, int upSign)
    {
        switch (action)
        {
            case CubeRotationButtonSystem.ButtonAction.RightColumnUp:
            case CubeRotationButtonSystem.ButtonAction.LeftColumnUp:
                return upSign > 0;
            case CubeRotationButtonSystem.ButtonAction.RightColumnDown:
            case CubeRotationButtonSystem.ButtonAction.LeftColumnDown:
                return upSign < 0;
            case CubeRotationButtonSystem.ButtonAction.TopLineLeft:
            case CubeRotationButtonSystem.ButtonAction.BottomLineLeft:
                return rightSign < 0;
            case CubeRotationButtonSystem.ButtonAction.TopLineRight:
            case CubeRotationButtonSystem.ButtonAction.BottomLineRight:
                return rightSign > 0;
            default:
                return false;
        }
    }

    private static bool IsInLayer(Vector3Int pos, CubeManager.Axis axis, int layerIndex)
    {
        return axis == CubeManager.Axis.X ? pos.x == layerIndex
            : axis == CubeManager.Axis.Y ? pos.y == layerIndex
            : pos.z == layerIndex;
    }

    private static int AxisSign(Vector3Int sign, Vector3 axis)
    {
        if (Mathf.Abs(axis.x) > 0.5f) return axis.x > 0f ? sign.x : -sign.x;
        if (Mathf.Abs(axis.y) > 0.5f) return axis.y > 0f ? sign.y : -sign.y;
        return axis.z > 0f ? sign.z : -sign.z;
    }

    private static Vector3Int IndexToSign(Vector3Int pos)
    {
        return new Vector3Int(IndexToSign(pos.x), IndexToSign(pos.y), IndexToSign(pos.z));
    }

    private static int IndexToSign(int idx)
    {
        return idx == 0 ? -1 : 1;
    }

    private static Vector3Int RotateGridPosition(Vector3Int pos, CubeManager.Axis axis, bool clockwise)
    {
        int sx = IndexToSign(pos.x);
        int sy = IndexToSign(pos.y);
        int sz = IndexToSign(pos.z);

        if (axis == CubeManager.Axis.X)
        {
            int ny = clockwise ? -sz : sz;
            int nz = clockwise ? sy : -sy;
            return new Vector3Int(pos.x, SignToIndex(ny), SignToIndex(nz));
        }
        if (axis == CubeManager.Axis.Y)
        {
            int nx = clockwise ? sz : -sz;
            int nz = clockwise ? -sx : sx;
            return new Vector3Int(SignToIndex(nx), pos.y, SignToIndex(nz));
        }

        int nxZ = clockwise ? -sy : sy;
        int nyZ = clockwise ? sx : -sx;
        return new Vector3Int(SignToIndex(nxZ), SignToIndex(nyZ), pos.z);
    }

    private static int SignToIndex(int sign)
    {
        return sign < 0 ? 0 : 1;
    }

    private static IEnumerable<FaceBasis> GetAllFaceBases()
    {
        yield return BuildFaceBasis("front", Vector3.forward);
        yield return BuildFaceBasis("back", Vector3.back);
        yield return BuildFaceBasis("left", Vector3.left);
        yield return BuildFaceBasis("right", Vector3.right);
        yield return BuildFaceBasis("up", Vector3.up);
        yield return BuildFaceBasis("down", Vector3.down);
    }

    private static FaceBasis BuildFaceBasis(string name, Vector3 forwardLocal)
    {
        Vector3 camForward = -forwardLocal.normalized;
        Vector3 camUp = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(camForward, camUp)) > 0.95f)
        {
            camUp = Vector3.forward;
        }

        Vector3 rightLocal = Vector3.Cross(camUp, camForward).normalized;
        Vector3 upLocal = Vector3.Cross(camForward, rightLocal).normalized;

        return new FaceBasis
        {
            name = name,
            forwardLocal = forwardLocal,
            rightLocal = rightLocal,
            upLocal = upLocal
        };
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[CubeRotationButtonValidator] {msg}");
    }

    private struct FaceBasis
    {
        public string name;
        public Vector3 forwardLocal;
        public Vector3 rightLocal;
        public Vector3 upLocal;
    }
}
