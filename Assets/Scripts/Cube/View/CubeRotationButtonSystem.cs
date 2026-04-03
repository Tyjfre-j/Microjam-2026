using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CubeRotationButtonSystem : MonoBehaviour
{
    public enum ButtonAction
    {
        RightColumnUp,
        BottomLineLeft,
        LeftColumnUp,
        BottomLineRight,
        RightColumnDown,
        TopLineLeft,
        LeftColumnDown,
        TopLineRight
    }

    private enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    private struct OrientationCache
    {
        public Vector3 forwardLocal;
        public Vector3 rightLocal;
        public Vector3 upLocal;
        public int faceIndex;
    }

    [Header("References")]
    [SerializeField] private RotationAnimator rotationAnimator;
    [SerializeField] private Transform cubeRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private InputActionReference interactAction;

    [Header("Buttons")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Vector3 buttonLocalScale = new Vector3(0.2f, 0.08f, 0.2f);
    [SerializeField] private float buttonSurfaceOffset = 0.06f;
    [SerializeField] private float cornerInset = 0.12f;
    [SerializeField] private bool addBoxCollider = true;
    [SerializeField] private bool addBoxCollider2D = true;

    [Header("Labels")]
    [SerializeField] private bool showLabels = true;
    [SerializeField] private float labelFontSize = 32f;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Vector3 labelLocalOffset = new Vector3(0f, 0.04f, 0f);

    [Header("Behavior")]
    [SerializeField] private bool lockDuringRotation = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private readonly List<GameObject> buttons = new List<GameObject>(8);
    private Transform buttonsRoot;
    private CubePiece[] cachedPieces;
    private bool isLocked;
    private bool isDirty = true;
    private OrientationCache lastOrientation;
    private int lastTriggerFrame = -1;

    private static readonly Vector3[] LocalFaceNormals =
    {
        Vector3.forward, // Front
        Vector3.back,    // Back
        Vector3.left,    // Left
        Vector3.right,   // Right
        Vector3.up,      // Top
        Vector3.down     // Bottom
    };

    private void Awake()
    {
        if (rotationAnimator == null) { rotationAnimator = GetComponent<RotationAnimator>(); }
        if (cubeRoot == null) { cubeRoot = transform; }
        if (targetCamera == null) { targetCamera = Camera.main; }

        cachedPieces = cubeRoot.GetComponentsInChildren<CubePiece>(true);
        EnsureButtonsRoot();
    }

    private void OnEnable()
    {
        if (rotationAnimator != null && lockDuringRotation)
        {
            rotationAnimator.OnRotationStart += HandleRotationStart;
            rotationAnimator.OnRotationComplete += HandleRotationComplete;
        }

        if (interactAction != null)
        {
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (rotationAnimator != null && lockDuringRotation)
        {
            rotationAnimator.OnRotationStart -= HandleRotationStart;
            rotationAnimator.OnRotationComplete -= HandleRotationComplete;
        }

        if (interactAction != null)
        {
            interactAction.action.Disable();
        }
    }

    private void Update()
    {
        if (rotationAnimator == null || cubeRoot == null) { return; }
        if (lockDuringRotation && rotationAnimator.isAnimating)
        {
            SetButtonsActive(false);
            return;
        }

        if (!TryGetOrientation(out OrientationCache orientation))
        {
            SetButtonsActive(false);
            return;
        }

        bool changed = isDirty || !OrientationEquals(lastOrientation, orientation);
        if (changed)
        {
            if (!RebuildButtons(orientation))
            {
                SetButtonsActive(false);
                return;
            }
            lastOrientation = orientation;
            isDirty = false;
        }

        SetButtonsActive(true);
    }

    /// <summary>Execute a button action using the current camera-facing orientation.</summary>
    public void TriggerAction(ButtonAction action)
    {
        if (Time.frameCount == lastTriggerFrame) { return; }
        if (isLocked) { return; }
        if (rotationAnimator == null || rotationAnimator.isAnimating) { return; }

        if (!TryGetOrientation(out OrientationCache orientation)) { return; }

        if (!TryResolveRotation(action, orientation, out CubeManager.Axis axis, out int layerIndex, out bool clockwise))
        {
            return;
        }

        if (!TryMapToRotationType(axis, layerIndex, clockwise, out RotationAnimator.RotationType type))
        {
            return;
        }

        lastTriggerFrame = Time.frameCount;
        rotationAnimator.AnimateAndApplyRotation(type);
    }

    private void HandleRotationStart()
    {
        isLocked = true;
        SetButtonsActive(false);
    }

    private void HandleRotationComplete()
    {
        isLocked = false;
        isDirty = true;
    }

    private void EnsureButtonsRoot()
    {
        if (buttonsRoot != null) { return; }
        Transform existing = cubeRoot.Find("RotationButtonsRoot");
        if (existing != null)
        {
            buttonsRoot = existing;
            return;
        }

        GameObject root = new GameObject("RotationButtonsRoot");
        root.transform.SetParent(cubeRoot, false);
        buttonsRoot = root.transform;
    }

    private bool RebuildButtons(OrientationCache orientation)
    {
        EnsureButtonsRoot();
        if (!TryGetFacePieces(orientation.forwardLocal, out List<CubePiece> facePieces)) { return false; }
        if (facePieces.Count != 4) { return false; }

        Vector3 faceCenterLocal = Vector3.zero;
        for (int i = 0; i < facePieces.Count; i++)
        {
            faceCenterLocal += cubeRoot.InverseTransformPoint(facePieces[i].transform.position);
        }
        faceCenterLocal /= facePieces.Count;

        float halfRight = 0f;
        float halfUp = 0f;
        for (int i = 0; i < facePieces.Count; i++)
        {
            Vector3 local = cubeRoot.InverseTransformPoint(facePieces[i].transform.position);
            Vector3 offset = local - faceCenterLocal;
            halfRight = Mathf.Max(halfRight, Mathf.Abs(Vector3.Dot(offset, orientation.rightLocal)));
            halfUp = Mathf.Max(halfUp, Mathf.Abs(Vector3.Dot(offset, orientation.upLocal)));
        }

        if (halfRight <= 0f || halfUp <= 0f)
        {
            return false;
        }

        PlaceCornerButtons(Corner.TopLeft, -1, 1, orientation, faceCenterLocal, halfRight, halfUp, 0);
        PlaceCornerButtons(Corner.TopRight, 1, 1, orientation, faceCenterLocal, halfRight, halfUp, 2);
        PlaceCornerButtons(Corner.BottomLeft, -1, -1, orientation, faceCenterLocal, halfRight, halfUp, 4);
        PlaceCornerButtons(Corner.BottomRight, 1, -1, orientation, faceCenterLocal, halfRight, halfUp, 6);

        return true;
    }

    private void PlaceCornerButtons(
        Corner corner,
        int rightSign,
        int upSign,
        OrientationCache orientation,
        Vector3 faceCenterLocal,
        float halfRight,
        float halfUp,
        int startIndex)
    {
        Vector3 basePos = faceCenterLocal
            + (orientation.rightLocal * rightSign * halfRight)
            + (orientation.upLocal * upSign * halfUp)
            + (orientation.forwardLocal * buttonSurfaceOffset);

        Vector3 posA = basePos + (-orientation.rightLocal * rightSign * cornerInset);
        Vector3 posB = basePos + (-orientation.upLocal * upSign * cornerInset);

        ButtonAction actionA = GetActionForCorner(corner, true);
        ButtonAction actionB = GetActionForCorner(corner, false);

        ConfigureButton(startIndex, posA, actionA, GetLabel(actionA));
        ConfigureButton(startIndex + 1, posB, actionB, GetLabel(actionB));
    }

    private void ConfigureButton(int index, Vector3 localPos, ButtonAction action, string label)
    {
        GameObject button = GetOrCreateButton(index);
        button.transform.SetParent(buttonsRoot, false);
        button.transform.localPosition = localPos;
        button.transform.localRotation = Quaternion.identity;
        button.transform.localScale = buttonLocalScale;

        CubeRotationButtonTrigger trigger = button.GetComponent<CubeRotationButtonTrigger>();
        if (trigger == null)
        {
            trigger = button.AddComponent<CubeRotationButtonTrigger>();
        }
        trigger.SetAction(action);
        trigger.SetInteractAction(interactAction);

        if (showLabels)
        {
            SetLabel(button.transform, label);
        }
    }

    private GameObject GetOrCreateButton(int index)
    {
        while (buttons.Count <= index)
        {
            buttons.Add(null);
        }

        if (buttons[index] != null) { return buttons[index]; }

        GameObject button = buttonPrefab != null ? Instantiate(buttonPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.name = $"RotationButton_{index}";
        button.transform.SetParent(buttonsRoot, false);
        button.transform.localScale = buttonLocalScale;

        if (addBoxCollider)
        {
            BoxCollider col = button.GetComponent<BoxCollider>();
            if (col == null) { col = button.AddComponent<BoxCollider>(); }
            col.isTrigger = true;
        }

        if (addBoxCollider2D)
        {
            BoxCollider2D col2D = button.GetComponent<BoxCollider2D>();
            if (col2D == null) { col2D = button.AddComponent<BoxCollider2D>(); }
            col2D.isTrigger = true;
        }

        buttons[index] = button;
        return button;
    }

    private void SetLabel(Transform button, string label)
    {
        Transform existing = button.Find("Label");
        TextMesh tm;

        if (existing == null)
        {
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(button, false);
            tm = labelGO.AddComponent<TextMesh>();
        }
        else
        {
            tm = existing.GetComponent<TextMesh>();
            if (tm == null) { tm = existing.gameObject.AddComponent<TextMesh>(); }
        }

        tm.text = label;
        tm.fontSize = Mathf.RoundToInt(labelFontSize);
        tm.color = labelColor;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        Transform t = tm.transform;
        t.localPosition = labelLocalOffset;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one * 0.1f;
    }

    private void SetButtonsActive(bool active)
    {
        if (buttonsRoot != null && buttonsRoot.gameObject.activeSelf != active)
        {
            buttonsRoot.gameObject.SetActive(active);
        }
    }

    private bool TryGetOrientation(out OrientationCache orientation)
    {
        orientation = default;
        if (targetCamera == null) { targetCamera = Camera.main; }
        if (targetCamera == null || cubeRoot == null) { return false; }

        Vector3 toCamera = (targetCamera.transform.position - cubeRoot.position).normalized;
        float bestDot = -999f;
        int bestFace = -1;
        Vector3 bestLocal = Vector3.forward;

        for (int i = 0; i < LocalFaceNormals.Length; i++)
        {
            Vector3 worldNormal = cubeRoot.TransformDirection(LocalFaceNormals[i]).normalized;
            float dot = Vector3.Dot(worldNormal, toCamera);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = i;
                bestLocal = LocalFaceNormals[i];
            }
        }

        if (bestFace < 0 || bestDot < 0.2f)
        {
            return false;
        }

        Vector3 faceNormalWorld = cubeRoot.TransformDirection(bestLocal).normalized;
        Vector3 rightWorld = Vector3.ProjectOnPlane(targetCamera.transform.right, faceNormalWorld);
        if (rightWorld.sqrMagnitude < 0.001f)
        {
            rightWorld = Vector3.ProjectOnPlane(cubeRoot.right, faceNormalWorld);
        }
        rightWorld.Normalize();

        Vector3 rightLocal = SnapToAxis(cubeRoot.InverseTransformDirection(rightWorld), bestLocal);
        Vector3 upLocal = Vector3.Cross(bestLocal, rightLocal);
        if (upLocal.sqrMagnitude < 0.001f)
        {
            upLocal = SnapToAxis(cubeRoot.InverseTransformDirection(targetCamera.transform.up), bestLocal);
        }
        upLocal = upLocal.normalized;

        Vector3 upWorld = cubeRoot.TransformDirection(upLocal);
        if (Vector3.Dot(upWorld, targetCamera.transform.up) < 0f)
        {
            upLocal = -upLocal;
            rightLocal = -rightLocal;
        }

        orientation.forwardLocal = bestLocal;
        orientation.rightLocal = rightLocal;
        orientation.upLocal = upLocal;
        orientation.faceIndex = bestFace;
        return true;
    }

    private static Vector3 SnapToAxis(Vector3 localDir, Vector3 forwardLocal)
    {
        Vector3[] axes =
        {
            Vector3.right, -Vector3.right,
            Vector3.up, -Vector3.up,
            Vector3.forward, -Vector3.forward
        };

        float bestDot = -999f;
        Vector3 best = Vector3.right;
        for (int i = 0; i < axes.Length; i++)
        {
            if (Vector3.Dot(axes[i], forwardLocal) > 0.9f || Vector3.Dot(axes[i], -forwardLocal) > 0.9f)
            {
                continue;
            }
            float dot = Vector3.Dot(localDir.normalized, axes[i]);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = axes[i];
            }
        }
        return best.normalized;
    }

    private bool TryGetFacePieces(Vector3 forwardLocal, out List<CubePiece> facePieces)
    {
        facePieces = new List<CubePiece>(4);
        if (cachedPieces == null || cachedPieces.Length == 0)
        {
            cachedPieces = cubeRoot.GetComponentsInChildren<CubePiece>(true);
        }

        if (!TryLocalAxisToAxis(forwardLocal, out CubeManager.Axis axis, out int axisSign))
        {
            return false;
        }

        int layerIndex = axisSign > 0 ? 1 : 0;

        for (int i = 0; i < cachedPieces.Length; i++)
        {
            CubePiece piece = cachedPieces[i];
            if (piece == null) { continue; }
            Vector3Int pos = piece.GridPosition;
            if (axis == CubeManager.Axis.X && pos.x == layerIndex) { facePieces.Add(piece); }
            if (axis == CubeManager.Axis.Y && pos.y == layerIndex) { facePieces.Add(piece); }
            if (axis == CubeManager.Axis.Z && pos.z == layerIndex) { facePieces.Add(piece); }
        }

        return facePieces.Count == 4;
    }

    private static bool TryLocalAxisToAxis(Vector3 localAxis, out CubeManager.Axis axis, out int sign)
    {
        axis = CubeManager.Axis.X;
        sign = 1;

        Vector3 n = localAxis.normalized;
        float ax = Mathf.Abs(n.x);
        float ay = Mathf.Abs(n.y);
        float az = Mathf.Abs(n.z);

        if (ax > ay && ax > az)
        {
            axis = CubeManager.Axis.X;
            sign = n.x >= 0f ? 1 : -1;
            return true;
        }
        if (ay > ax && ay > az)
        {
            axis = CubeManager.Axis.Y;
            sign = n.y >= 0f ? 1 : -1;
            return true;
        }

        axis = CubeManager.Axis.Z;
        sign = n.z >= 0f ? 1 : -1;
        return true;
    }

    private static int LayerIndexForAxisSide(int axisSign, int sideSign)
    {
        if (axisSign > 0)
        {
            return sideSign > 0 ? 1 : 0;
        }
        return sideSign > 0 ? 0 : 1;
    }

    private bool TryResolveRotation(
        ButtonAction action,
        OrientationCache orientation,
        out CubeManager.Axis axis,
        out int layerIndex,
        out bool clockwise)
    {
        axis = CubeManager.Axis.Y;
        layerIndex = 0;
        clockwise = true;

        Vector3 axisLocal;
        Vector3 targetLocal;
        int sideSign;
        bool wantIncrease;

        switch (action)
        {
            case ButtonAction.RightColumnUp:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal; sideSign = 1; wantIncrease = true; break;
            case ButtonAction.LeftColumnUp:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal; sideSign = -1; wantIncrease = true; break;
            case ButtonAction.RightColumnDown:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal; sideSign = 1; wantIncrease = false; break;
            case ButtonAction.LeftColumnDown:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal; sideSign = -1; wantIncrease = false; break;
            case ButtonAction.TopLineLeft:
                axisLocal = orientation.upLocal; targetLocal = orientation.rightLocal; sideSign = 1; wantIncrease = false; break;
            case ButtonAction.TopLineRight:
                axisLocal = orientation.upLocal; targetLocal = orientation.rightLocal; sideSign = 1; wantIncrease = true; break;
            case ButtonAction.BottomLineLeft:
                axisLocal = orientation.upLocal; targetLocal = orientation.rightLocal; sideSign = -1; wantIncrease = false; break;
            case ButtonAction.BottomLineRight:
                axisLocal = orientation.upLocal; targetLocal = orientation.rightLocal; sideSign = -1; wantIncrease = true; break;
            default:
                return false;
        }

        if (!TryLocalAxisToAxis(axisLocal, out axis, out int axisSign))
        {
            return false;
        }

        layerIndex = LayerIndexForAxisSide(axisSign, sideSign);
        clockwise = ComputeClockwise(axisLocal, orientation.forwardLocal, targetLocal, wantIncrease);
        return true;
    }

    private static bool ComputeClockwise(Vector3 axisLocal, Vector3 forwardLocal, Vector3 targetLocal, bool wantIncrease)
    {
        Vector3 p = (forwardLocal + targetLocal).normalized;
        Quaternion rotCW = Quaternion.AngleAxis(90f, axisLocal);
        Quaternion rotCCW = Quaternion.AngleAxis(-90f, axisLocal);
        float dotCW = Vector3.Dot(rotCW * p, targetLocal);
        float dotCCW = Vector3.Dot(rotCCW * p, targetLocal);
        return wantIncrease ? dotCW > dotCCW : dotCW < dotCCW;
    }

    private static bool TryMapToRotationType(
        CubeManager.Axis axis,
        int layerIndex,
        bool clockwise,
        out RotationAnimator.RotationType type)
    {
        type = RotationAnimator.RotationType.U;
        if (axis == CubeManager.Axis.Y && layerIndex == 1) { type = clockwise ? RotationAnimator.RotationType.U : RotationAnimator.RotationType.UPrime; return true; }
        if (axis == CubeManager.Axis.Y && layerIndex == 0) { type = clockwise ? RotationAnimator.RotationType.D : RotationAnimator.RotationType.DPrime; return true; }
        if (axis == CubeManager.Axis.X && layerIndex == 1) { type = clockwise ? RotationAnimator.RotationType.R : RotationAnimator.RotationType.RPrime; return true; }
        if (axis == CubeManager.Axis.X && layerIndex == 0) { type = clockwise ? RotationAnimator.RotationType.L : RotationAnimator.RotationType.LPrime; return true; }
        if (axis == CubeManager.Axis.Z && layerIndex == 1) { type = clockwise ? RotationAnimator.RotationType.F : RotationAnimator.RotationType.FPrime; return true; }
        if (axis == CubeManager.Axis.Z && layerIndex == 0) { type = clockwise ? RotationAnimator.RotationType.B : RotationAnimator.RotationType.BPrime; return true; }
        return false;
    }

    private static ButtonAction GetActionForCorner(Corner corner, bool isPrimary)
    {
        switch (corner)
        {
            case Corner.TopLeft:
                return isPrimary ? ButtonAction.RightColumnUp : ButtonAction.BottomLineLeft;
            case Corner.TopRight:
                return isPrimary ? ButtonAction.LeftColumnUp : ButtonAction.BottomLineRight;
            case Corner.BottomLeft:
                return isPrimary ? ButtonAction.RightColumnDown : ButtonAction.TopLineLeft;
            case Corner.BottomRight:
                return isPrimary ? ButtonAction.LeftColumnDown : ButtonAction.TopLineRight;
            default:
                return ButtonAction.RightColumnUp;
        }
    }

    private static string GetLabel(ButtonAction action)
    {
        switch (action)
        {
            case ButtonAction.RightColumnUp: return "R↑";
            case ButtonAction.BottomLineLeft: return "←Bot";
            case ButtonAction.LeftColumnUp: return "L↑";
            case ButtonAction.BottomLineRight: return "Bot→";
            case ButtonAction.RightColumnDown: return "R↓";
            case ButtonAction.TopLineLeft: return "←Top";
            case ButtonAction.LeftColumnDown: return "L↓";
            case ButtonAction.TopLineRight: return "Top→";
            default: return "Move";
        }
    }

    private static bool OrientationEquals(OrientationCache a, OrientationCache b)
    {
        return a.faceIndex == b.faceIndex
            && Vector3.Dot(a.forwardLocal, b.forwardLocal) > 0.99f
            && Vector3.Dot(a.rightLocal, b.rightLocal) > 0.99f
            && Vector3.Dot(a.upLocal, b.upLocal) > 0.99f;
    }
}
