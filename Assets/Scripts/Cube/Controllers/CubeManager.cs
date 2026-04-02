using System;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Hierarchy")]
    public Transform piecesRoot; // Drag 'CubeHolder' here
    public List<CubePiece> pieces = new List<CubePiece>(); // Drag your 8 cubes here

    private CubeState cubeState;

    private void Awake()
    {
        cubeState = GetComponent<CubeState>();
        // Automatically find pieces if list is empty
        if (pieces.Count == 0) CachePieces();
    }

    public void CachePieces()
    {
        pieces.Clear();
        Transform root = (piecesRoot != null) ? piecesRoot : transform;
        foreach (Transform child in root)
        {
            CubePiece p = child.GetComponent<CubePiece>();
            if (p != null) pieces.Add(p);
        }
        Debug.Log($"[CubeManager] Cached {pieces.Count} pieces.");
    }

    // THIS IS THE FUNCTION THAT WAS FAILING
    public void RotateLayer(Axis axis, int layerCoord, bool clockwise, System.Action onComplete)
    {
        if (pieces.Count != 8)
        {
            Debug.LogError("[CubeManager] Cannot rotate. Need 8 pieces, found " + pieces.Count);
            return;
        }

        List<CubePiece> selected = new List<CubePiece>();

        foreach (var p in pieces)
        {
            // Convert world position to local position relative to the root
            Vector3 localPos = piecesRoot.InverseTransformPoint(p.transform.position);
            float val = (axis == Axis.X) ? localPos.x : (axis == Axis.Y) ? localPos.y : localPos.z;

            // FIX: Use a buffer (0.1f) instead of checking for exact 0.5
            // If we want the 'Positive' layer (1), we look for anything > 0.1
            // If we want the 'Negative' layer (-1), we look for anything < -0.1
            if (layerCoord > 0 && val > 0.1f) selected.Add(p);
            else if (layerCoord < 0 && val < -0.1f) selected.Add(p);
        }

        if (selected.Count != 4)
        {
            Debug.LogWarning($"[CubeManager] Layer selection returned {selected.Count} pieces. Expected 4. Check cube positions!");
            return;
        }

        // Move is valid, logic would go here to update CubeState
        onComplete?.Invoke();
    }

    // Helper for logs
    public void Log(string msg) => Debug.Log("[CubeManager] " + msg);
}