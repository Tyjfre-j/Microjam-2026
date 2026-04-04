using System.Collections.Generic;
using UnityEngine;

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

    [Header("Buttons")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Vector3 buttonLocalScale = new Vector3(0.2f, 0.08f, 0.2f);
    // How high above the tagged platform the button floats
    [SerializeField] private float buttonSurfaceOffset = 0.3f;

    [Header("Labels")]
    [SerializeField] private bool showLabels = true;
    [SerializeField] private float labelFontSize = 32f;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Vector3 labelLocalOffset = new Vector3(0f, 0.04f, 0f);

    [Header("Behavior")]
    [SerializeField] private bool lockDuringRotation = true;
    [SerializeField] private bool lockUntilPlatformsRespawned = true;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private FaceChildrenRemover faceChildrenRemover;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showOnScreenDebug = false;
    [SerializeField] private bool showFaceDetectionDebug = false;
    [SerializeField] private bool showActionResolutionDebug = false;

    // ── Internal ─────────────────────────────────────────────────
    private readonly List<GameObject> buttons = new List<GameObject>(8);
    private Transform buttonsRoot;
    private CubePiece[] cachedPieces;
    private bool isLocked;
    private bool isDirty = true;
    private OrientationCache lastOrientation;
    private int lastTriggerFrame = -1;
    private string debugLine = "";
    private int lastDebugFaceIndex = -999;
    private Vector3 lastDebugForwardLocal = Vector3.zero;
    private float lastDebugDot = 0f;
<<<<<<< HEAD
=======
    private readonly HashSet<int> warnedMissingAnchorFaces = new HashSet<int>();
>>>>>>> origin/dev

    private static readonly Vector3[] LocalFaceNormals =
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right,
        Vector3.up,
        Vector3.down
    };

    // ── Unity ─────────────────────────────────────────────────────
    private void Awake()
    {
<<<<<<< HEAD
        if (rotationAnimator == null) rotationAnimator = GetComponent<RotationAnimator>();
=======
        if (rotationAnimator == null)
        {
            rotationAnimator = GetComponent<RotationAnimator>();
        }
>>>>>>> origin/dev
        if (cubeRoot == null)         cubeRoot         = transform;
        if (targetCamera == null)     targetCamera     = Camera.main;
        if (playerController == null)   playerController   = FindAnyObjectByType<PlayerController>();
        if (faceChildrenRemover == null) faceChildrenRemover = FindAnyObjectByType<FaceChildrenRemover>();

        cachedPieces = cubeRoot.GetComponentsInChildren<CubePiece>(true);
        EnsureButtonsRoot();
    }

    private void OnEnable()
    {
<<<<<<< HEAD
        if (rotationAnimator != null && lockDuringRotation)
        {
            rotationAnimator.OnRotationStart    += HandleRotationStart;
            rotationAnimator.OnRotationComplete += HandleRotationComplete;
        }
=======
        BindRotationAnimatorEvents(rotationAnimator);
>>>>>>> origin/dev
        if (faceChildrenRemover != null && lockUntilPlatformsRespawned)
        {
            faceChildrenRemover.OnPlatformsRespawned += HandlePlatformsRespawned;
        }
    }

    private void OnDisable()
    {
<<<<<<< HEAD
        if (rotationAnimator != null && lockDuringRotation)
        {
            rotationAnimator.OnRotationStart    -= HandleRotationStart;
            rotationAnimator.OnRotationComplete -= HandleRotationComplete;
        }
=======
        UnbindRotationAnimatorEvents(rotationAnimator);
>>>>>>> origin/dev
        if (faceChildrenRemover != null && lockUntilPlatformsRespawned)
        {
            faceChildrenRemover.OnPlatformsRespawned -= HandlePlatformsRespawned;
        }
    }

    private void Update()
    {
        if (rotationAnimator == null || cubeRoot == null) return;

        if (lockDuringRotation && rotationAnimator.isAnimating)
        {
            SetButtonsActive(false);
            debugLine = "Face: (rotating)";
            return;
        }
        if (isLocked)
        {
            SetButtonsActive(false);
            debugLine = "Face: (locked)";
            return;
        }

        if (!TryGetOrientation(out OrientationCache orientation))
        {
            SetButtonsActive(false);
            debugLine = "Face: NONE";
            return;
        }

        bool changed = isDirty || !OrientationEquals(lastOrientation, orientation);
        if (changed)
        {
            if (!RebuildButtons(orientation, out string rebuildReason))
            {
                SetButtonsActive(false);
                debugLine = $"Face: {GetFaceName(orientation.faceIndex)} ({rebuildReason})";
                Log($"Rebuild failed: {rebuildReason}");
                return;
            }
            lastOrientation = orientation;
            isDirty         = false;
            Log($"Rebuilt buttons for face {GetFaceName(orientation.faceIndex)}");
        }

        SetButtonsActive(true);
        debugLine = $"Face: {GetFaceName(orientation.faceIndex)}";
    }

    // ── Public API ────────────────────────────────────────────────
    public void TriggerAction(ButtonAction action)
    {
        if (Time.frameCount == lastTriggerFrame)          return;
        if (isLocked)                                     return;
<<<<<<< HEAD
        if (rotationAnimator == null)                     return;
        if (rotationAnimator.isAnimating)                 return;
=======
        if (rotationAnimator != null && rotationAnimator.isAnimating) return;
>>>>>>> origin/dev

        if (!TryGetOrientation(out OrientationCache orientation)) return;

        if (!TryResolveRotation(action, orientation,
                out CubeManager.Axis axis,
                out int layerIndex,
                out bool clockwise))
            return;

        if (!TryMapToRotationType(axis, layerIndex, clockwise,
                out RotationAnimator.RotationType type))
            return;

        if (showActionResolutionDebug)
        {
            Debug.Log($"[CubeRotationButtonSystem] Face={GetFaceName(orientation.faceIndex)} Action={action} -> Axis={axis} Layer={layerIndex} Clockwise={clockwise} Type={type}");
        }

        lastTriggerFrame = Time.frameCount;
<<<<<<< HEAD
        rotationAnimator.AnimateAndApplyRotation(type);
=======
        if (TryStartRotation(type, out RotationAnimator activeAnimator))
        {
            if (activeAnimator != rotationAnimator)
            {
                SetRotationAnimator(activeAnimator);
            }
            return;
        }

        if (showActionResolutionDebug)
        {
            Debug.LogWarning("[CubeRotationButtonSystem] Rotation request did not start. Check RotationAnimator/CubeManager/CubeRoot wiring.");
        }
    }

    private bool TryStartRotation(RotationAnimator.RotationType type, out RotationAnimator activeAnimator)
    {
        activeAnimator = null;

        if (TryStartWithAnimator(rotationAnimator, type, out activeAnimator))
        {
            return true;
        }

        RotationAnimator[] candidates = FindObjectsByType<RotationAnimator>(FindObjectsInactive.Include);

        for (int i = 0; i < candidates.Length; i++)
        {
            RotationAnimator candidate = candidates[i];
            if (candidate == null || candidate == rotationAnimator) continue;
            if (cubeRoot != null && candidate.transform != cubeRoot) continue;

            if (TryStartWithAnimator(candidate, type, out activeAnimator))
            {
                return true;
            }
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            RotationAnimator candidate = candidates[i];
            if (candidate == null || candidate == rotationAnimator) continue;

            if (TryStartWithAnimator(candidate, type, out activeAnimator))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryStartWithAnimator(
        RotationAnimator animator,
        RotationAnimator.RotationType type,
        out RotationAnimator activeAnimator)
    {
        activeAnimator = null;
        if (animator == null) return false;
        if (animator.isAnimating) return false;

        animator.AnimateAndApplyRotation(type);
        if (!animator.isAnimating) return false;

        activeAnimator = animator;
        return true;
    }

    private void SetRotationAnimator(RotationAnimator newAnimator)
    {
        if (newAnimator == rotationAnimator) return;

        UnbindRotationAnimatorEvents(rotationAnimator);
        rotationAnimator = newAnimator;
        BindRotationAnimatorEvents(rotationAnimator);
    }

    private void BindRotationAnimatorEvents(RotationAnimator animator)
    {
        if (!lockDuringRotation || animator == null) return;
        animator.OnRotationStart += HandleRotationStart;
        animator.OnRotationComplete += HandleRotationComplete;
    }

    private void UnbindRotationAnimatorEvents(RotationAnimator animator)
    {
        if (!lockDuringRotation || animator == null) return;
        animator.OnRotationStart -= HandleRotationStart;
        animator.OnRotationComplete -= HandleRotationComplete;
>>>>>>> origin/dev
    }

    /// <summary>Resolve a button action for a specific face basis (debug/testing).</summary>
    public bool TryResolveActionForTest(
        Vector3 forwardLocal,
        Vector3 rightLocal,
        Vector3 upLocal,
        ButtonAction action,
        out CubeManager.Axis axis,
        out int layerIndex,
        out bool clockwise,
        out RotationAnimator.RotationType type)
    {
        axis = CubeManager.Axis.Y;
        layerIndex = 0;
        clockwise = true;
        type = RotationAnimator.RotationType.U;

        OrientationCache orientation;
        orientation.forwardLocal = forwardLocal;
        orientation.rightLocal = rightLocal;
        orientation.upLocal = upLocal;
        orientation.faceIndex = 0;

        if (!TryResolveRotation(action, orientation, out axis, out layerIndex, out clockwise))
            return false;

        return TryMapToRotationType(axis, layerIndex, clockwise, out type);
    }

    // ── Rotation Events ───────────────────────────────────────────
    private void HandleRotationStart()
    {
        isLocked = true;
        SetButtonsActive(false);
        playerController?.Freeze();
    }

    private void HandleRotationComplete()
    {
<<<<<<< HEAD
        if (lockUntilPlatformsRespawned && faceChildrenRemover != null)
=======
        // In startup-only rebuild mode, FaceChildrenRemover does not emit per-rotation respawn events.
        // Do not wait for OnPlatformsRespawned in that mode or buttons/player can stay locked.
        if (lockUntilPlatformsRespawned
            && faceChildrenRemover != null
            && !faceChildrenRemover.StartupOnlyRebuild)
>>>>>>> origin/dev
        {
            return;
        }

        UnlockAndUnfreeze();
    }

    private void HandlePlatformsRespawned()
    {
        if (!lockUntilPlatformsRespawned) return;
        UnlockAndUnfreeze();
    }

    private void UnlockAndUnfreeze()
    {
        isLocked = false;
        isDirty  = true;
        playerController?.Unfreeze();
    }

    // ── Button Root ───────────────────────────────────────────────
    private void EnsureButtonsRoot()
    {
        if (buttonsRoot != null) return;
        Transform existing = cubeRoot.Find("RotationButtonsRoot");
        if (existing != null) { buttonsRoot = existing; return; }

        GameObject root = new GameObject("RotationButtonsRoot");
        root.transform.SetParent(cubeRoot, false);
        buttonsRoot = root.transform;
    }

    // ── Rebuild ───────────────────────────────────────────────────
    private bool RebuildButtons(OrientationCache orientation, out string reason)
    {
        reason = "";
        EnsureButtonsRoot();

        if (!TryGetFacePieces(orientation.forwardLocal, out List<CubePiece> facePieces))
        {
            reason = "no face pieces";
            return false;
        }
        if (facePieces.Count != 4)
        {
            reason = $"face pieces != 4 ({facePieces.Count})";
            return false;
        }

        // Find ButtonAnchor-tagged platforms across the 4 face pieces
        // and sort them into the 4 corners (multiple anchors per corner allowed)
        if (!TryGetAnchorsByCorner(facePieces, orientation, out List<Transform>[] cornerAnchors))
        {
            reason = "no ButtonAnchor platforms found — tag 2 platforms per corner with 'ButtonAnchor'";
<<<<<<< HEAD
            Debug.LogWarning($"[CubeRotationButtonSystem] {reason}");
=======
            if (warnedMissingAnchorFaces.Add(orientation.faceIndex))
            {
                Debug.LogWarning($"[CubeRotationButtonSystem] {reason}");
            }
>>>>>>> origin/dev
            return false;
        }

        Corner[] corners = { Corner.TopLeft, Corner.TopRight, Corner.BottomLeft, Corner.BottomRight };

        for (int i = 0; i < 4; i++)
        {
            if (cornerAnchors[i] == null || cornerAnchors[i].Count < 2)
            {
                Log($"Corner {corners[i]} needs 2 ButtonAnchor platforms (found {cornerAnchors[i]?.Count ?? 0}), skipping.");
                continue;
            }

            ButtonAction actionA = GetActionForCorner(corners[i], true);
            ButtonAction actionB = GetActionForCorner(corners[i], false);

            // Pick two distinct anchors for this corner (stable assignment)
            Transform anchorA;
            Transform anchorB;
            PickTwoAnchors(cornerAnchors[i], out anchorA, out anchorB);

            Vector3 worldPosA = anchorA.position + Vector3.up * buttonSurfaceOffset;
            Vector3 worldPosB = anchorB.position + Vector3.up * buttonSurfaceOffset;
            Vector3 localPosA = cubeRoot.InverseTransformPoint(worldPosA);
            Vector3 localPosB = cubeRoot.InverseTransformPoint(worldPosB);

            ConfigureButton(i * 2,     localPosA, actionA, GetLabel(actionA));
            ConfigureButton(i * 2 + 1, localPosB, actionB, GetLabel(actionB));
        }

<<<<<<< HEAD
=======
        warnedMissingAnchorFaces.Remove(orientation.faceIndex);
>>>>>>> origin/dev
        reason = "ok";
        return true;
    }

    // ── Anchor Detection ──────────────────────────────────────────
    // Searches all children of the 4 front-facing small cube pieces for
    // GameObjects tagged "ButtonAnchor", then assigns each to the corner
    // it spatially belongs to (TopLeft / TopRight / BottomLeft / BottomRight).
    private bool TryGetAnchorsByCorner(
        List<CubePiece> facePieces,
        OrientationCache orientation,
        out List<Transform>[] cornerAnchors)
    {
        // [0]=TopLeft  [1]=TopRight  [2]=BottomLeft  [3]=BottomRight
        cornerAnchors = new List<Transform>[4];
        for (int i = 0; i < 4; i++) cornerAnchors[i] = new List<Transform>(4);

        // Collect every ButtonAnchor-tagged child across all 4 face pieces
        // but only on the currently visible face (front/back/left/right/up/down).
        string faceName = GetFaceName(orientation.faceIndex);
        List<Transform> anchors = new List<Transform>(8);
        foreach (CubePiece piece in facePieces)
        {
            Transform[] children = piece.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.CompareTag("ButtonAnchor") && IsAnchorOnFace(child, faceName))
                    anchors.Add(child);
            }
        }

        if (anchors.Count == 0)
        {
            Log("No ButtonAnchor-tagged objects found in face pieces.");
            return false;
        }

        Log($"Found {anchors.Count} ButtonAnchor(s) on this face.");

        // Face center in world space
        Vector3 faceCenter = Vector3.zero;
        foreach (CubePiece piece in facePieces)
            faceCenter += piece.transform.position;
        faceCenter /= facePieces.Count;

        // World-space directions for right and up on this face
        Vector3 rightWorld = cubeRoot.TransformDirection(orientation.rightLocal).normalized;
        Vector3 upWorld    = cubeRoot.TransformDirection(orientation.upLocal).normalized;

        // Ideal direction toward each corner from face center
        Vector3[] cornerDirs = {
            (-rightWorld + upWorld).normalized,  // TopLeft
            ( rightWorld + upWorld).normalized,  // TopRight
            (-rightWorld - upWorld).normalized,  // BottomLeft
            ( rightWorld - upWorld).normalized   // BottomRight
        };

        // Assign each anchor to the closest corner direction (multiple per corner allowed)
        foreach (Transform anchor in anchors)
        {
            Vector3 dir = (anchor.position - faceCenter).normalized;
            float bestScore = -999f;
            int bestCorner = -1;
            for (int c = 0; c < 4; c++)
            {
                float score = Vector3.Dot(dir, cornerDirs[c]);
                if (score > bestScore) { bestScore = score; bestCorner = c; }
            }
            if (bestCorner >= 0)
            {
                cornerAnchors[bestCorner].Add(anchor);
                Log($"Corner {(Corner)bestCorner} += {anchor.name}");
            }
        }

        // Succeed if at least one corner has any anchors
        int assigned = 0;
        for (int i = 0; i < 4; i++) assigned += cornerAnchors[i].Count;
        return assigned > 0;
    }

    private static void PickTwoAnchors(List<Transform> list, out Transform a, out Transform b)
    {
        a = list[0];
        b = list[1];

        if (list.Count <= 2) return;

        int iA = Random.Range(0, list.Count);
        int iB = Random.Range(0, list.Count - 1);
        if (iB >= iA) iB++;

        a = list[iA];
        b = list[iB];
    }

    // ── Button Config ─────────────────────────────────────────────
    private void ConfigureButton(int index, Vector3 localPos, ButtonAction action, string label)
    {
        GameObject button = GetOrCreateButton(index);
        button.transform.SetParent(buttonsRoot, false);
        button.transform.localPosition = localPos;
        button.transform.localRotation = Quaternion.identity;
        button.transform.localScale    = buttonLocalScale;

        CubeRotationButtonTrigger trigger = button.GetComponent<CubeRotationButtonTrigger>();
        if (trigger == null)
            trigger = button.AddComponent<CubeRotationButtonTrigger>();

        trigger.SetAction(action);

        if (showLabels)
            SetLabel(button.transform, label);
    }

    private GameObject GetOrCreateButton(int index)
    {
        while (buttons.Count <= index)
            buttons.Add(null);

        if (buttons[index] != null) return buttons[index];

        GameObject button = buttonPrefab != null
            ? Instantiate(buttonPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        button.name = $"RotationButton_{index}";
        button.transform.SetParent(buttonsRoot, false);
        button.transform.localScale = buttonLocalScale;

        // 3D trigger collider only — no 2D needed in a 3D game
        BoxCollider col = button.GetComponent<BoxCollider>();
        if (col == null) col = button.AddComponent<BoxCollider>();
        col.isTrigger = true;

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
            if (tm == null) tm = existing.gameObject.AddComponent<TextMesh>();
        }

        tm.text      = label;
        tm.fontSize  = Mathf.RoundToInt(labelFontSize);
        tm.color     = labelColor;
        tm.anchor    = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        Transform t = tm.transform;
        t.localPosition = labelLocalOffset;
        t.localRotation = Quaternion.identity;
        t.localScale    = Vector3.one * 0.1f;
    }

    private void SetButtonsActive(bool active)
    {
        if (buttonsRoot != null && buttonsRoot.gameObject.activeSelf != active)
            buttonsRoot.gameObject.SetActive(active);
    }

    // ── Orientation ───────────────────────────────────────────────
    private bool TryGetOrientation(out OrientationCache orientation)
    {
        orientation = default;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || cubeRoot == null) return false;

        Vector3 toCamera  = (targetCamera.transform.position - cubeRoot.position).normalized;
        float   bestDot   = -999f;
        int     bestFace  = -1;
        Vector3 bestLocal = Vector3.forward;

        for (int i = 0; i < LocalFaceNormals.Length; i++)
        {
            Vector3 worldNormal = cubeRoot.TransformDirection(LocalFaceNormals[i]).normalized;
            float   dot         = Vector3.Dot(worldNormal, toCamera);
            if (dot > bestDot) { bestDot = dot; bestFace = i; bestLocal = LocalFaceNormals[i]; }
        }

        if (bestFace < 0 || bestDot < 0.2f) return false;

        if (showFaceDetectionDebug && (bestFace != lastDebugFaceIndex || Vector3.Dot(bestLocal, lastDebugForwardLocal) < 0.99f))
        {
            lastDebugFaceIndex = bestFace;
            lastDebugForwardLocal = bestLocal;
            lastDebugDot = bestDot;
            Debug.Log($"[CubeRotationButtonSystem] Camera sees face: {GetFaceName(bestFace)} (dot={bestDot:0.00})");
        }

        Vector3 faceNormalWorld = cubeRoot.TransformDirection(bestLocal).normalized;
        Vector3 rightWorld      = Vector3.ProjectOnPlane(targetCamera.transform.right, faceNormalWorld);
        if (rightWorld.sqrMagnitude < 0.001f)
            rightWorld = Vector3.ProjectOnPlane(cubeRoot.right, faceNormalWorld);
        rightWorld.Normalize();

        Vector3 rightLocal = SnapToAxis(cubeRoot.InverseTransformDirection(rightWorld), bestLocal);
        Vector3 upLocal    = Vector3.Cross(bestLocal, rightLocal);
        if (upLocal.sqrMagnitude < 0.001f)
            upLocal = SnapToAxis(cubeRoot.InverseTransformDirection(targetCamera.transform.up), bestLocal);
        upLocal = upLocal.normalized;

        Vector3 upWorld = cubeRoot.TransformDirection(upLocal);
        if (Vector3.Dot(upWorld, targetCamera.transform.up) < 0f)
        {
            upLocal   = -upLocal;
            rightLocal = -rightLocal;
        }

        orientation.forwardLocal = bestLocal;
        orientation.rightLocal   = rightLocal;
        orientation.upLocal      = upLocal;
        orientation.faceIndex    = bestFace;
        return true;
    }

    // ── Rotation Resolution ───────────────────────────────────────
    private bool TryResolveRotation(
        ButtonAction action,
        OrientationCache orientation,
        out CubeManager.Axis axis,
        out int layerIndex,
        out bool clockwise)
    {
        axis       = CubeManager.Axis.Y;
        layerIndex = 0;
        clockwise  = true;

        Vector3 axisLocal, targetLocal;
        int     sideSign;
        bool    wantIncrease;

        switch (action)
        {
            case ButtonAction.RightColumnUp:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal;   sideSign =  1; wantIncrease = true;  break;
            case ButtonAction.LeftColumnUp:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal;   sideSign = -1; wantIncrease = true;  break;
            case ButtonAction.RightColumnDown:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal;   sideSign =  1; wantIncrease = false; break;
            case ButtonAction.LeftColumnDown:
                axisLocal = orientation.rightLocal; targetLocal = orientation.upLocal;   sideSign = -1; wantIncrease = false; break;
            case ButtonAction.TopLineLeft:
                axisLocal = orientation.upLocal;    targetLocal = orientation.rightLocal; sideSign =  1; wantIncrease = false; break;
            case ButtonAction.TopLineRight:
                axisLocal = orientation.upLocal;    targetLocal = orientation.rightLocal; sideSign =  1; wantIncrease = true;  break;
            case ButtonAction.BottomLineLeft:
                axisLocal = orientation.upLocal;    targetLocal = orientation.rightLocal; sideSign = -1; wantIncrease = false; break;
            case ButtonAction.BottomLineRight:
                axisLocal = orientation.upLocal;    targetLocal = orientation.rightLocal; sideSign = -1; wantIncrease = true;  break;
            default:
                return false;
        }

        if (!TryLocalAxisToAxis(axisLocal, out axis, out int axisSign)) return false;

        layerIndex = LayerIndexForAxisSide(axisSign, sideSign);
        clockwise  = ComputeClockwise(axisLocal, orientation.forwardLocal, targetLocal, wantIncrease);
        // Back face needs an extra flip because its forward vector is inverted.
        if (Vector3.Dot(orientation.forwardLocal, Vector3.back) > 0.9f)
        {
            clockwise = !clockwise;
        }
        return true;
    }

    private static bool TryMapToRotationType(
        CubeManager.Axis axis,
        int layerIndex,
        bool clockwise,
        out RotationAnimator.RotationType type)
    {
        type = RotationAnimator.RotationType.U;
        if (axis == CubeManager.Axis.Y && layerIndex == 1) { type = clockwise ? RotationAnimator.RotationType.U      : RotationAnimator.RotationType.UPrime; return true; }
        if (axis == CubeManager.Axis.Y && layerIndex == 0) { type = clockwise ? RotationAnimator.RotationType.D      : RotationAnimator.RotationType.DPrime; return true; }
        if (axis == CubeManager.Axis.X && layerIndex == 1) { type = clockwise ? RotationAnimator.RotationType.R      : RotationAnimator.RotationType.RPrime; return true; }
        if (axis == CubeManager.Axis.X && layerIndex == 0) { type = clockwise ? RotationAnimator.RotationType.L      : RotationAnimator.RotationType.LPrime; return true; }
        if (axis == CubeManager.Axis.Z && layerIndex == 1) { type = clockwise ? RotationAnimator.RotationType.F      : RotationAnimator.RotationType.FPrime; return true; }
        if (axis == CubeManager.Axis.Z && layerIndex == 0) { type = clockwise ? RotationAnimator.RotationType.B      : RotationAnimator.RotationType.BPrime; return true; }
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────
    private bool TryGetFacePieces(Vector3 forwardLocal, out List<CubePiece> facePieces)
    {
        facePieces = new List<CubePiece>(4);
        if (cachedPieces == null || cachedPieces.Length == 0)
            cachedPieces = cubeRoot.GetComponentsInChildren<CubePiece>(true);

        if (!TryLocalAxisToAxis(forwardLocal, out CubeManager.Axis axis, out int axisSign))
            return false;

        int layerIndex = axisSign > 0 ? 1 : 0;

        foreach (CubePiece piece in cachedPieces)
        {
            if (piece == null) continue;
            Vector3Int pos = piece.GridPosition;
            if (axis == CubeManager.Axis.X && pos.x == layerIndex) facePieces.Add(piece);
            if (axis == CubeManager.Axis.Y && pos.y == layerIndex) facePieces.Add(piece);
            if (axis == CubeManager.Axis.Z && pos.z == layerIndex) facePieces.Add(piece);
        }

        return facePieces.Count == 4;
    }

    private static bool TryLocalAxisToAxis(Vector3 localAxis, out CubeManager.Axis axis, out int sign)
    {
        axis = CubeManager.Axis.X;
        sign = 1;
        Vector3 n  = localAxis.normalized;
        float   ax = Mathf.Abs(n.x);
        float   ay = Mathf.Abs(n.y);
        float   az = Mathf.Abs(n.z);

        if (ax > ay && ax > az) { axis = CubeManager.Axis.X; sign = n.x >= 0f ? 1 : -1; return true; }
        if (ay > ax && ay > az) { axis = CubeManager.Axis.Y; sign = n.y >= 0f ? 1 : -1; return true; }
        axis = CubeManager.Axis.Z; sign = n.z >= 0f ? 1 : -1; return true;
    }

    private static int LayerIndexForAxisSide(int axisSign, int sideSign)
    {
        if (axisSign > 0) return sideSign > 0 ? 1 : 0;
        return sideSign > 0 ? 0 : 1;
    }

    private static bool ComputeClockwise(Vector3 axisLocal, Vector3 forwardLocal, Vector3 targetLocal, bool wantIncrease)
    {
        Vector3    p      = (forwardLocal + targetLocal).normalized;
        Quaternion rotCW  = Quaternion.AngleAxis( 90f, axisLocal);
        Quaternion rotCCW = Quaternion.AngleAxis(-90f, axisLocal);
        float dotCW       = Vector3.Dot(rotCW  * p, targetLocal);
        float dotCCW      = Vector3.Dot(rotCCW * p, targetLocal);
        return wantIncrease ? dotCW > dotCCW : dotCW < dotCCW;
    }

    private static Vector3 SnapToAxis(Vector3 localDir, Vector3 forwardLocal)
    {
        Vector3[] axes = { Vector3.right, -Vector3.right, Vector3.up, -Vector3.up, Vector3.forward, -Vector3.forward };
        float     bestDot = -999f;
        Vector3   best    = Vector3.right;

        foreach (Vector3 a in axes)
        {
            if (Vector3.Dot(a,  forwardLocal) > 0.9f) continue;
            if (Vector3.Dot(a, -forwardLocal) > 0.9f) continue;
            float dot = Vector3.Dot(localDir.normalized, a);
            if (dot > bestDot) { bestDot = dot; best = a; }
        }
        return best.normalized;
    }

    private static ButtonAction GetActionForCorner(Corner corner, bool isPrimary)
    {
        switch (corner)
        {
            case Corner.TopLeft:     return isPrimary ? ButtonAction.RightColumnUp   : ButtonAction.BottomLineLeft;
            case Corner.TopRight:    return isPrimary ? ButtonAction.LeftColumnUp    : ButtonAction.BottomLineRight;
            case Corner.BottomLeft:  return isPrimary ? ButtonAction.RightColumnDown : ButtonAction.TopLineLeft;
            case Corner.BottomRight: return isPrimary ? ButtonAction.LeftColumnDown  : ButtonAction.TopLineRight;
            default:                 return ButtonAction.RightColumnUp;
        }
    }

    private static string GetLabel(ButtonAction action)
    {
        switch (action)
        {
            case ButtonAction.RightColumnUp:    return "R↑";
            case ButtonAction.BottomLineLeft:   return "←Bot";
            case ButtonAction.LeftColumnUp:     return "L↑";
            case ButtonAction.BottomLineRight:  return "Bot→";
            case ButtonAction.RightColumnDown:  return "R↓";
            case ButtonAction.TopLineLeft:      return "←Top";
            case ButtonAction.LeftColumnDown:   return "L↓";
            case ButtonAction.TopLineRight:     return "Top→";
            default:                            return "Move";
        }
    }

    private static string GetFaceName(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0: return "front";
            case 1: return "back";
            case 2: return "left";
            case 3: return "right";
            case 4: return "up";
            case 5: return "down";
            default: return "";
        }
    }

    private static bool IsAnchorOnFace(Transform anchor, string faceName)
    {
        if (anchor == null || string.IsNullOrEmpty(faceName)) return false;
        Transform t = anchor;
        while (t != null)
        {
            if (string.Equals(t.name, faceName, System.StringComparison.OrdinalIgnoreCase))
                return true;
            t = t.parent;
        }
        return false;
    }

    private static bool OrientationEquals(OrientationCache a, OrientationCache b)
    {
        return a.faceIndex == b.faceIndex
            && Vector3.Dot(a.forwardLocal, b.forwardLocal) > 0.99f
            && Vector3.Dot(a.rightLocal,   b.rightLocal)   > 0.99f
            && Vector3.Dot(a.upLocal,      b.upLocal)      > 0.99f;
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug || string.IsNullOrEmpty(debugLine)) return;
        GUI.Label(new Rect(10f, 10f, 400f, 22f), $"[Buttons] {debugLine}");
    }
}
