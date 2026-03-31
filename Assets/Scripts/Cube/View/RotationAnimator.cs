using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RotationAnimator : MonoBehaviour
{
    public enum RotationType
    {
        U, UPrime, D, DPrime, R, RPrime, L, LPrime, F, FPrime, B, BPrime
    }

    [Header("Animation Settings")]
    [FormerlySerializedAs("animationDuration")]
    [SerializeField, Tooltip("Seconds for a single 90° rotation animation.")]
    private float rotationDuration = 0.3f;
    [FormerlySerializedAs("syncStateFromVisuals")]
    [SerializeField, Tooltip("After animation, sync CubeState from the rotated pieces.")]
    private bool syncCubeStateFromPieces = true;
    [FormerlySerializedAs("layerEpsilon")]
    [SerializeField, Tooltip("Treat values within this range as 0 when selecting layers.")]
    private float layerSelectionEpsilon = 0.001f;
    [Header("Post Rotation Alignment")]
    [SerializeField, Tooltip("Snap face meshes to their original world rotation after animation.")]
    private bool snapFacesAfterRotation = true;
    [SerializeField, Tooltip("Align platform parents to face normals after animation.")]
    private bool alignPlatformsAfterRotation = true;
    [SerializeField, Tooltip("Log face/platform alignment data after rotation.")]
    private bool debugAlignment = false;
    public bool isAnimating { get; private set; }

    private CubeVisual cubeVisual;
    private CubeRotations cubeRotations;
    public event System.Action OnRotationComplete;

    private void Awake()
    {
        cubeVisual = GetComponent<CubeVisual>();
        cubeRotations = GetComponent<CubeRotations>();
        // Error logs removed per project request.
    }

    /// <summary>Animate a move and then apply the logical rotation.</summary>
    public void AnimateAndApplyRotation(RotationType type)
    {
        if (isAnimating || cubeVisual == null || cubeRotations == null || cubeVisual.pieces == null)
        {
            return;
        }

        if (cubeVisual.pieces.Length != 8) { }

        if (!TryGetRotationData(type, out int[] pieceIndices, out Vector3 axis, out float angle, out System.Action applyRotation))
        {
            return;
        }

        StartCoroutine(AnimateAndApplyCoroutine(pieceIndices, axis, angle, rotationDuration, applyRotation));
    }

    [ContextMenu("Test/Rotate U")]
    private void TestRotateU() => AnimateAndApplyRotation(RotationType.U);

    [ContextMenu("Test/Rotate U'")]
    private void TestRotateUPrime() => AnimateAndApplyRotation(RotationType.UPrime);

    [ContextMenu("Test/Rotate D")]
    private void TestRotateD() => AnimateAndApplyRotation(RotationType.D);

    [ContextMenu("Test/Rotate D'")]
    private void TestRotateDPrime() => AnimateAndApplyRotation(RotationType.DPrime);

    [ContextMenu("Test/Rotate L")]
    private void TestRotateL() => AnimateAndApplyRotation(RotationType.L);

    [ContextMenu("Test/Rotate L'")]
    private void TestRotateLPrime() => AnimateAndApplyRotation(RotationType.LPrime);

    [ContextMenu("Test/Rotate R")]
    private void TestRotateR() => AnimateAndApplyRotation(RotationType.R);

    [ContextMenu("Test/Rotate R'")]
    private void TestRotateRPrime() => AnimateAndApplyRotation(RotationType.RPrime);

    [ContextMenu("Test/Rotate F")]
    private void TestRotateF() => AnimateAndApplyRotation(RotationType.F);

    [ContextMenu("Test/Rotate F'")]
    private void TestRotateFPrime() => AnimateAndApplyRotation(RotationType.FPrime);

    [ContextMenu("Test/Rotate B")]
    private void TestRotateB() => AnimateAndApplyRotation(RotationType.B);

    [ContextMenu("Test/Rotate B'")]
    private void TestRotateBPrime() => AnimateAndApplyRotation(RotationType.BPrime);

    /// <summary>Animate a rotation of the specified cube pieces.</summary>
    public void AnimateRotation(int[] pieceIndices, Vector3 axis, float angle, float duration)
    {
        if (isAnimating) { return; }
        StartCoroutine(AnimateRotationCoroutine(pieceIndices, axis, angle, duration));
    }

    private IEnumerator AnimateAndApplyCoroutine(int[] pieceIndices, Vector3 axis, float angle, float duration, System.Action applyRotation)
    {
        yield return AnimateRotationCoroutine(pieceIndices, axis, angle, duration);

        applyRotation?.Invoke();
        if (syncCubeStateFromPieces)
        {
            cubeVisual.SyncStateFromPieces();
        }
        OnRotationComplete?.Invoke();
    }

    private IEnumerator AnimateRotationCoroutine(int[] pieceIndices, Vector3 axis, float angle, float duration)
    {
        isAnimating = true;

        GameObject pivot = new GameObject("RotationPivot");
        Vector3 center = Vector3.zero;
        foreach (int idx in pieceIndices)
        {
            center += cubeVisual.pieces[idx].transform.position;
        }
        center /= pieceIndices.Length;
        pivot.transform.position = center;
        pivot.transform.SetParent(cubeVisual.PiecesRoot, true);

        List<Transform> faceMeshes = new List<Transform>(pieceIndices.Length * 2);
        Dictionary<Transform, Vector3> faceUpLocal = new Dictionary<Transform, Vector3>(pieceIndices.Length * 2);
        Dictionary<Transform, Transform> faceToPiece = new Dictionary<Transform, Transform>(pieceIndices.Length * 2);
        List<Transform> platformParents = new List<Transform>(pieceIndices.Length * 2);
        Dictionary<Transform, Vector3> platformForwardLocal = new Dictionary<Transform, Vector3>(pieceIndices.Length * 2);
        Dictionary<Transform, Transform> platformToPiece = new Dictionary<Transform, Transform>(pieceIndices.Length * 2);

        foreach (int idx in pieceIndices)
        {
            Transform piece = cubeVisual.pieces[idx].transform;
            piece.SetParent(pivot.transform, true);
            CollectTargets(piece, faceMeshes, faceUpLocal, faceToPiece, platformParents, platformForwardLocal, platformToPiece);
        }

        Quaternion startRot = pivot.transform.rotation;
        Quaternion endRot = startRot * Quaternion.AngleAxis(angle, axis);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            pivot.transform.rotation = Quaternion.Slerp(startRot, endRot, Mathf.Clamp01(t));
            yield return null;
        }

        pivot.transform.rotation = endRot;

        foreach (int idx in pieceIndices)
        {
            cubeVisual.pieces[idx].transform.SetParent(cubeVisual.PiecesRoot, true);
        }

        if (snapFacesAfterRotation)
        {
            for (int i = 0; i < faceMeshes.Count; i++)
            {
                Transform face = faceMeshes[i];
                if (face == null) continue;
                AlignFaceMesh(face, faceUpLocal, faceToPiece);
            }
        }

        if (alignPlatformsAfterRotation)
        {
            for (int i = 0; i < platformParents.Count; i++)
            {
                Transform platformParent = platformParents[i];
                if (platformParent == null) continue;
                FixPlatformAlignment(platformParent, platformForwardLocal, platformToPiece);
            }
        }

        Destroy(pivot);
        isAnimating = false;
    }


    private bool TryGetRotationData(RotationType type, out int[] pieceIndices, out Vector3 axis, out float angle, out System.Action applyRotation)
    {
        pieceIndices = null;
        axis = Vector3.zero;
        angle = 90f;
        applyRotation = null;

        switch (type)
        {
            case RotationType.U:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Y, 1);
                axis = cubeVisual.transform.up;
                angle = 90f;
                applyRotation = cubeRotations.RotateU;
                return true;
            case RotationType.UPrime:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Y, 1);
                axis = cubeVisual.transform.up;
                angle = -90f;
                applyRotation = cubeRotations.RotateUPrime;
                return true;
            case RotationType.D:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Y, -1);
                axis = cubeVisual.transform.up;
                angle = 90f;
                applyRotation = cubeRotations.RotateD;
                return true;
            case RotationType.DPrime:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Y, -1);
                axis = cubeVisual.transform.up;
                angle = -90f;
                applyRotation = cubeRotations.RotateDPrime;
                return true;
            case RotationType.L:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.X, -1);
                axis = cubeVisual.transform.right;
                angle = 90f;
                applyRotation = cubeRotations.RotateL;
                return true;
            case RotationType.LPrime:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.X, -1);
                axis = cubeVisual.transform.right;
                angle = -90f;
                applyRotation = cubeRotations.RotateLPrime;
                return true;
            case RotationType.R:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.X, 1);
                axis = cubeVisual.transform.right;
                angle = 90f;
                applyRotation = cubeRotations.RotateR;
                return true;
            case RotationType.RPrime:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.X, 1);
                axis = cubeVisual.transform.right;
                angle = -90f;
                applyRotation = cubeRotations.RotateRPrime;
                return true;
            case RotationType.F:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Z, 1);
                axis = cubeVisual.transform.forward;
                angle = 90f;
                applyRotation = cubeRotations.RotateF;
                return true;
            case RotationType.FPrime:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Z, 1);
                axis = cubeVisual.transform.forward;
                angle = -90f;
                applyRotation = cubeRotations.RotateFPrime;
                return true;
            case RotationType.B:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Z, -1);
                axis = cubeVisual.transform.forward;
                angle = 90f;
                applyRotation = cubeRotations.RotateB;
                return true;
            case RotationType.BPrime:
                pieceIndices = GetPiecesByAxisSign(AxisFilter.Z, -1);
                axis = cubeVisual.transform.forward;
                angle = -90f;
                applyRotation = cubeRotations.RotateBPrime;
                return true;
            default:
                return false;
        }
    }

    private enum AxisFilter { X, Y, Z }

    private int[] GetPiecesByAxisSign(AxisFilter axis, int sign)
    {
        List<(int index, float value)> values = new List<(int, float)>(cubeVisual.pieces.Length);
        for (int i = 0; i < cubeVisual.pieces.Length; i++)
        {
            Transform t = cubeVisual.pieces[i].transform;
            Vector3 local = cubeVisual.PiecesRoot.InverseTransformPoint(t.position);
            float v = axis == AxisFilter.X ? local.x : axis == AxisFilter.Y ? local.y : local.z;
            if (Mathf.Abs(v) < layerSelectionEpsilon) { v = 0f; }
            values.Add((i, v));
        }

        values.Sort((a, b) => a.value.CompareTo(b.value));

        int takeCount = Mathf.Min(4, values.Count);
        int startIndex = sign > 0 ? Mathf.Max(0, values.Count - takeCount) : 0;

        List<int> indices = new List<int>(takeCount);
        for (int i = 0; i < takeCount; i++)
        {
            indices.Add(values[startIndex + i].index);
        }

        if (indices.Count != 4) { }

        return indices.ToArray();
    }

    private void CollectTargets(
        Transform root,
        List<Transform> faceMeshes,
        Dictionary<Transform, Vector3> faceUpLocal,
        Dictionary<Transform, Transform> faceToPiece,
        List<Transform> platformParents,
        Dictionary<Transform, Vector3> platformForwardLocal,
        Dictionary<Transform, Transform> platformToPiece)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (IsFaceMeshName(t.name))
            {
                if (!faceMeshes.Contains(t))
                {
                    faceMeshes.Add(t);
                    faceUpLocal[t] = root.InverseTransformDirection(t.up);
                    faceToPiece[t] = root;
                }
                continue;
            }

            if (IsPlatformParent(t))
            {
                if (!platformParents.Contains(t))
                {
                    platformParents.Add(t);
                    platformForwardLocal[t] = root.InverseTransformDirection(t.forward);
                    platformToPiece[t] = root;
                }
            }
        }
    }

    private bool IsFaceMeshName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        string compact = name.Replace(" ", "").ToUpperInvariant();
        if (compact == "FACEMESH") return true;
        if (compact == "FRONT") return true;
        if (compact == "BACK") return true;
        if (compact == "UP") return true;
        if (compact == "DOWN") return true;
        if (compact == "LEFT") return true;
        if (compact == "RIGHT") return true;
        return false;
    }

    private bool IsPlatformParent(Transform t)
    {
        if (t == null) return false;
        string name = t.name.ToUpperInvariant();
        return name.Contains("PL") && name.Contains("PARENT");
    }

    private void FixPlatformAlignment(
        Transform plParent,
        Dictionary<Transform, Vector3> platformForwardLocal,
        Dictionary<Transform, Transform> platformToPiece)
    {
        if (plParent == null) return;
        if (!platformToPiece.TryGetValue(plParent, out Transform piece) || piece == null) return;

        Vector3 faceNormal = GetFaceNormal(plParent, piece);

        Vector3 referenceForward = platformForwardLocal.TryGetValue(plParent, out Vector3 storedForwardLocal)
            ? piece.TransformDirection(storedForwardLocal)
            : plParent.forward;

        Vector3 forward = Vector3.ProjectOnPlane(referenceForward, faceNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            Vector3 faceTangent = GetFaceTangent(plParent, piece);
            forward = Vector3.ProjectOnPlane(faceTangent, faceNormal);
        }
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(piece.up, faceNormal);
        }
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, faceNormal);
        }

        plParent.rotation = Quaternion.LookRotation(forward.normalized, faceNormal);

        foreach (Transform child in plParent)
        {
            child.localRotation = Quaternion.identity;
        }

        if (debugAlignment)
        {
            Debug.Log($"[RotationAnimator] Platform '{plParent.name}' normal={faceNormal} forward={forward.normalized}");
        }
    }

    private void AlignFaceMesh(
        Transform face,
        Dictionary<Transform, Vector3> faceUpLocal,
        Dictionary<Transform, Transform> faceToPiece)
    {
        if (face == null || cubeVisual == null) return;
        if (!faceToPiece.TryGetValue(face, out Transform piece) || piece == null) return;

        Vector3 faceNormal = GetFaceNormal(face, piece);

        Vector3 preferredUp = faceUpLocal.TryGetValue(face, out Vector3 storedUpLocal)
            ? piece.TransformDirection(storedUpLocal)
            : face.up;
        Vector3 up = Vector3.ProjectOnPlane(preferredUp, faceNormal);
        if (up.sqrMagnitude < 0.0001f)
        {
            up = Vector3.ProjectOnPlane(piece.right, faceNormal);
        }
        if (up.sqrMagnitude < 0.0001f)
        {
            up = Vector3.up;
        }

        face.rotation = Quaternion.LookRotation(faceNormal, up.normalized);

        if (debugAlignment)
        {
            Debug.Log($"[RotationAnimator] Face '{face.name}' normal={faceNormal} up={up.normalized}");
        }
    }

    private Vector3 GetFaceNormal(Transform target, Transform piece)
    {
        Vector3 localNormal = GetFaceLocalNormalFromPivotHierarchy(target);
        return SnapToAxis(piece.TransformDirection(localNormal));
    }

    private Vector3 GetFaceTangent(Transform target, Transform piece)
    {
        Transform current = target;
        while (current != null)
        {
            string name = current.name.ToUpperInvariant();
            if (name.Contains("FRONT") || name.Contains("BACK")) return piece.TransformDirection(Vector3.right);
            if (name.Contains("LEFT") || name.Contains("RIGHT")) return piece.TransformDirection(Vector3.forward);
            if (name.Contains("UP") || name.Contains("DOWN")) return piece.TransformDirection(Vector3.forward);
            current = current.parent;
        }
        return piece.TransformDirection(Vector3.forward);
    }

    private Vector3 GetFaceLocalNormalFromPivotHierarchy(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            string name = current.name.ToUpperInvariant();
            if (name.StartsWith("PIVOT-")) name = name.Substring(6);
            if (name == "FRONT") return Vector3.forward;
            if (name == "BACK") return Vector3.back;
            if (name == "UP") return Vector3.up;
            if (name == "DOWN") return Vector3.down;
            if (name == "LEFT") return Vector3.left;
            if (name == "RIGHT") return Vector3.right;
            current = current.parent;
        }
        return Vector3.forward;
    }

    private Vector3 SnapToAxis(Vector3 direction)
    {
        Vector3 d = direction.normalized;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y) && Mathf.Abs(d.x) > Mathf.Abs(d.z))
            return new Vector3(Mathf.Sign(d.x), 0f, 0f);
        if (Mathf.Abs(d.y) > Mathf.Abs(d.x) && Mathf.Abs(d.y) > Mathf.Abs(d.z))
            return new Vector3(0f, Mathf.Sign(d.y), 0f);
        return new Vector3(0f, 0f, Mathf.Sign(d.z));
    }
}
