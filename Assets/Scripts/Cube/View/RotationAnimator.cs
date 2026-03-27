using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationAnimator : MonoBehaviour
{
    public enum RotationType
    {
        U, UPrime, D, DPrime, R, RPrime, L, LPrime, F, FPrime, B, BPrime
    }

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private bool syncStateFromVisuals = true;
    public bool isAnimating { get; private set; }

    private CubeVisual cubeVisual;
    private CubeRotations cubeRotations;

    private void Awake()
    {
        cubeVisual = GetComponent<CubeVisual>();
        cubeRotations = GetComponent<CubeRotations>();

        if (cubeVisual == null)
        {
            Debug.LogError("[RotationAnimator] Missing CubeVisual on CubeManager.");
        }

        if (cubeRotations == null)
        {
            Debug.LogError("[RotationAnimator] Missing CubeRotations on CubeManager.");
        }
    }

    /// <summary>Animate a move and then apply the logical rotation.</summary>
    public void AnimateAndApplyRotation(RotationType type)
    {
        if (isAnimating || cubeVisual == null || cubeRotations == null || cubeVisual.pieces == null)
        {
            return;
        }

        if (!TryGetRotationData(type, out int[] pieceIndices, out Vector3 axis, out float angle, out System.Action applyRotation))
        {
            Debug.LogError($"[RotationAnimator] RotationType {type} not implemented.");
            return;
        }

        StartCoroutine(AnimateAndApplyCoroutine(pieceIndices, axis, angle, animationDuration, applyRotation));
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
        if (syncStateFromVisuals)
        {
            cubeVisual.SyncStateFromPieces();
        }
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
        pivot.transform.SetParent(cubeVisual.transform, true);

        foreach (int idx in pieceIndices)
        {
            cubeVisual.pieces[idx].transform.SetParent(pivot.transform, true);
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
            cubeVisual.pieces[idx].transform.SetParent(cubeVisual.transform, true);
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
        List<int> indices = new List<int>(4);
        for (int i = 0; i < cubeVisual.pieces.Length; i++)
        {
            Transform t = cubeVisual.pieces[i].transform;
            Vector3 local = cubeVisual.transform.InverseTransformPoint(t.position);
            float value = axis == AxisFilter.X ? local.x : axis == AxisFilter.Y ? local.y : local.z;

            if (sign > 0 && value > 0f)
            {
                indices.Add(i);
            }
            else if (sign < 0 && value < 0f)
            {
                indices.Add(i);
            }
        }

        return indices.ToArray();
    }
}
