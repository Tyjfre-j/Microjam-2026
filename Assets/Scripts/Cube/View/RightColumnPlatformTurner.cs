using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Rotates platform parents on the right column when a key is pressed (local space).</summary>
public class RightColumnPlatformTurner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private RotationAnimator rotationAnimator;

    [Header("Selection")]
    [SerializeField, Tooltip("Local X threshold to select the right column pieces.")]
    private float rightColumnThreshold = 0.1f;
    [SerializeField, Tooltip("If true, select the right column by sorting X and taking the top layer size.")]
    private bool useSortedLayerSelection = true;

    [Header("Rotation")]
    [SerializeField, Tooltip("Degrees to rotate platforms by on each press.")]
    private float rotationStep = 90f;
    [SerializeField, Tooltip("Exact local Euler rotation to apply to the PL parent.")]
    private Vector3 targetLocalEuler = new Vector3(0f, 90f, -90f);
    [SerializeField, Tooltip("Exact local Euler to apply when PL parent belongs to the FRONT face.")]
    private Vector3 frontFaceEuler = new Vector3(-90f, 90f, 0f);
    [SerializeField, Tooltip("Exact local Euler to apply when PL parent belongs to the RIGHT face.")]
    private Vector3 rightFaceEuler = new Vector3(-90f, 180f, 0f);
    [SerializeField, Tooltip("Exact local Euler to apply when PL parent belongs to the DOWN face.")]
    private Vector3 downFaceEuler = new Vector3(90f, -90f, 0f);
    [SerializeField, Tooltip("Exact local Euler to apply when PL parent belongs to the BACK face.")]
    private Vector3 backFaceEuler = new Vector3(90f, -90f, 0f);

    [Header("Input")]
    [SerializeField] private Key triggerKey = Key.E;
    [SerializeField, Tooltip("If true, rotate after RotationAnimator completes instead of on key press.")]
    private bool rotateAfterAnimator = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField, Tooltip("Log child local rotations after reset.")]
    private bool logChildLocalRotations = false;

    private Coroutine pendingResetRoutine;

    private void Update()
    {
        if (Keyboard.current == null) { return; }
        if (rotateAfterAnimator) { return; }
        if (Keyboard.current[triggerKey].wasPressedThisFrame)
        {
            RotateRightColumnPlatforms();
        }
    }

    private void Awake()
    {
        if (rotationAnimator == null) { rotationAnimator = GetComponent<RotationAnimator>(); }
    }

    private void OnEnable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationComplete += HandleRotationComplete;
        }
    }

    private void OnDisable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationComplete -= HandleRotationComplete;
        }
    }

    private void HandleRotationComplete()
    {
        if (!rotateAfterAnimator) { return; }
        RotateRightColumnPlatforms();
    }

    /// <summary>Rotate platform parents on the right column once (local space).</summary>
    public void RotateRightColumnPlatforms()
    {
        if (piecesRoot == null) { return; }

        int rotated = 0;
        Transform[] pieceTransforms = piecesRoot.GetComponentsInChildren<Transform>(true);
        HashSet<Transform> platformParents = new HashSet<Transform>();

        List<Transform> rightColumnPieces = useSortedLayerSelection
            ? GetRightColumnPiecesSorted(pieceTransforms)
            : null;

        // Collect platform parents under pieces that are in the right column.
        foreach (Transform t in pieceTransforms)
        {
            if (t.parent != piecesRoot) { continue; } // Only direct children = cube pieces.

            if (useSortedLayerSelection)
            {
                if (rightColumnPieces == null || !rightColumnPieces.Contains(t)) { continue; }
            }
            else
            {
                float localX = piecesRoot.InverseTransformPoint(t.position).x;
                if (localX <= rightColumnThreshold) { continue; }
            }

            foreach (Transform child in t.GetComponentsInChildren<Transform>(true))
            {
                if (!IsPlatformParent(child)) { continue; }
                platformParents.Add(child);
            }
        }

        foreach (Transform p in platformParents)
        {
            List<Transform> children = new List<Transform>(p.childCount);
            for (int i = 0; i < p.childCount; i++)
            {
                children.Add(p.GetChild(i));
            }

            Vector3 finalEuler = targetLocalEuler;
            if (IsFrontFaceParent(p))
            {
                finalEuler = frontFaceEuler;
            }
            else if (IsRightFaceParent(p))
            {
                finalEuler = rightFaceEuler;
            }
            else if (IsDownFaceParent(p))
            {
                finalEuler = downFaceEuler;
            }
            else if (IsBackFaceParent(p))
            {
                finalEuler = backFaceEuler;
            }
            p.localRotation = Quaternion.Euler(finalEuler);

            rotated++;
        }

        // Force all descendants to show 0,0,0 local rotation after Unity finishes the frame.
        if (pendingResetRoutine != null)
        {
            StopCoroutine(pendingResetRoutine);
        }
        pendingResetRoutine = StartCoroutine(ResetDescendantsEndOfFrame(platformParents));

        if (showDebugLogs)
        {
            Debug.Log($"[RightColumnPlatformTurner] Rotated {rotated} platform parent(s) by {rotationStep} degrees.");
        }
    }

    private IEnumerator ResetDescendantsEndOfFrame(HashSet<Transform> platformParents)
    {
        yield return new WaitForEndOfFrame();

        foreach (Transform p in platformParents)
        {
            if (p == null) { continue; }

            Transform[] descendants = p.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i] == null || descendants[i] == p) { continue; }
                // Keep specific children at 90,0,0; all others reset to 0,0,0.
                if (ShouldKeep90ForFace(p, descendants[i]))
                {
                    descendants[i].localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    descendants[i].localRotation = Quaternion.identity;
                }
            }

            if (logChildLocalRotations)
            {
                for (int i = 0; i < descendants.Length; i++)
                {
                    Transform d = descendants[i];
                    if (d == null || d == p) { continue; }
                    Debug.Log($"[RightColumnPlatformTurner] Child '{d.name}' localEuler={d.localEulerAngles} (parent='{p.name}')");
                }
            }
        }
    }

    private static bool IsPlatformParent(Transform t)
    {
        if (t == null) { return false; }
        if (IsPlatformParentName(t.name)) { return true; }

        int childCount = t.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = t.GetChild(i);
            if (child == null) { continue; }
            if (IsPlatformChildName(child.name)) { return true; }
        }

        return false;
    }

    private static bool IsPlatformParentName(string name)
    {
        if (string.IsNullOrEmpty(name)) { return false; }
        string upper = name.ToUpperInvariant();
        if (upper.StartsWith("PL ")) { return true; }
        if (upper.StartsWith("PL_")) { return true; }
        if (upper.StartsWith("PL")) { return true; }
        if (upper.Contains("PARENT")) { return true; }
        return false;
    }

    private static bool IsPlatformChildName(string name)
    {
        if (string.IsNullOrEmpty(name)) { return false; }
        string upper = name.ToUpperInvariant();
        if (upper.StartsWith("PLATFORM")) { return true; }
        if (upper.StartsWith("PLAT")) { return true; }
        return false;
    }

    private static bool IsPlatform29(Transform t)
    {
        if (t == null) { return false; }
        string n = t.name;
        if (string.IsNullOrEmpty(n)) { return false; }
        // Match both "PLATFORM  (29)" and "PLATFORM (29)" variants.
        return n.Contains("PLATFORM") && n.Contains("(29)");
    }

    private static bool IsPlatform16(Transform t)
    {
        if (t == null) { return false; }
        string n = t.name;
        if (string.IsNullOrEmpty(n)) { return false; }
        // Match both "PLATFORM  (16)" and "PLATFORM (16)" variants.
        return n.Contains("PLATFORM") && n.Contains("(16)");
    }

    private static bool IsPlatform46(Transform t)
    {
        if (t == null) { return false; }
        string n = t.name;
        if (string.IsNullOrEmpty(n)) { return false; }
        // Match both "PLATFORM  (46)" and "PLATFORM (46)" variants.
        return n.Contains("PLATFORM") && n.Contains("(46)");
    }

    private bool IsRightFaceParent(Transform platformParent)
    {
        Transform current = platformParent != null ? platformParent.parent : null;
        while (current != null && current != piecesRoot)
        {
            string n = current.name;
            if (!string.IsNullOrEmpty(n))
            {
                string upper = n.ToUpperInvariant();
                if (upper == "RIGHT" || upper.Contains(" RIGHT") || upper.Contains("RIGHT "))
                {
                    return true;
                }
            }
            current = current.parent;
        }
        return false;
    }

    private bool IsDownFaceParent(Transform platformParent)
    {
        Transform current = platformParent != null ? platformParent.parent : null;
        while (current != null && current != piecesRoot)
        {
            string n = current.name;
            if (!string.IsNullOrEmpty(n))
            {
                string upper = n.ToUpperInvariant();
                if (upper == "DOWN" || upper.Contains(" DOWN") || upper.Contains("DOWN "))
                {
                    return true;
                }
            }
            current = current.parent;
        }
        return false;
    }

    private bool ShouldKeep90ForFace(Transform platformParent, Transform child)
    {
        if (platformParent == null || child == null) { return false; }
        if (IsPlatform16(child) || IsPlatform29(child) || IsPlatform46(child)) { return true; }
        if (IsRightFaceParent(platformParent))
        {
            return IsPlatform16(child) || IsPlatform29(child) || IsPlatform46(child);
        }
        if (IsDownFaceParent(platformParent))
        {
            return IsPlatform16(child) || IsPlatform29(child) || IsPlatform46(child);
        }
        if (IsFrontFaceParent(platformParent))
        {
            return IsPlatform16(child) || IsPlatform29(child);
        }
        return false;
    }

    private bool IsFrontFaceParent(Transform platformParent)
    {
        Transform current = platformParent != null ? platformParent.parent : null;
        while (current != null && current != piecesRoot)
        {
            string n = current.name;
            if (!string.IsNullOrEmpty(n))
            {
                string upper = n.ToUpperInvariant();
                if (upper == "FRONT" || upper.Contains(" FRONT") || upper.Contains("FRONT ") || upper.Contains("FORWARD"))
                {
                    return true;
                }
            }
            current = current.parent;
        }
        return false;
    }

    private bool IsBackFaceParent(Transform platformParent)
    {
        Transform current = platformParent != null ? platformParent.parent : null;
        while (current != null && current != piecesRoot)
        {
            string n = current.name;
            if (!string.IsNullOrEmpty(n))
            {
                string upper = n.ToUpperInvariant();
                if (upper == "BACK" || upper.Contains(" BACK") || upper.Contains("BACK "))
                {
                    return true;
                }
            }
            current = current.parent;
        }
        return false;
    }
    private List<Transform> GetRightColumnPiecesSorted(Transform[] allTransforms)
    {
        List<Transform> pieces = new List<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t == null) { continue; }
            if (t.parent != piecesRoot) { continue; }
            pieces.Add(t);
        }

        int pieceCount = pieces.Count;
        if (pieceCount == 0) { return pieces; }

        int grid = Mathf.RoundToInt(Mathf.Pow(pieceCount, 1f / 3f));
        if (grid < 1) { grid = 1; }
        int layerSize = Mathf.Max(1, pieceCount / grid);

        pieces.Sort((a, b) =>
        {
            float ax = piecesRoot.InverseTransformPoint(a.position).x;
            float bx = piecesRoot.InverseTransformPoint(b.position).x;
            return ax.CompareTo(bx);
        });

        int startIndex = Mathf.Max(0, pieces.Count - layerSize);
        List<Transform> rightColumn = new List<Transform>(layerSize);
        for (int i = startIndex; i < pieces.Count; i++)
        {
            rightColumn.Add(pieces[i]);
        }

        return rightColumn;
    }
}
