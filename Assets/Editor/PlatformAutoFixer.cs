using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlatformAutoFixer : EditorWindow
{
    private Transform piecesRoot;
    private bool includeInactive = true;

    [MenuItem("Tools/Cube/Fix Platform Alignment")]
    public static void Open()
    {
        GetWindow<PlatformAutoFixer>("Fix Platform Alignment");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Platform Alignment Fixer", EditorStyles.boldLabel);
        piecesRoot = (Transform)EditorGUILayout.ObjectField("Pieces Root", piecesRoot, typeof(Transform), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        GUILayout.Space(8f);

        if (GUILayout.Button("Fix Platforms"))
        {
            FixPlatforms();
        }
    }

    private void FixPlatforms()
    {
        if (piecesRoot == null)
        {
            Debug.LogWarning("[PlatformAutoFixer] Assign a Pieces Root first.");
            return;
        }

        int fixedCount = 0;
        int skippedCount = 0;

        foreach (Transform piece in piecesRoot)
        {
            if (piece == null) continue;
            Transform[] all = piece.GetComponentsInChildren<Transform>(includeInactive);
            foreach (Transform t in all)
            {
                if (t == null || !IsPlatformParent(t)) continue;
                if (!TryGetFaceNameFromPivotHierarchy(t, out string faceName))
                {
                    skippedCount++;
                    Debug.LogWarning($"[PlatformAutoFixer] Skipped '{t.name}' (no face name in hierarchy).");
                    continue;
                }

                Vector3 localNormal = FaceLocalNormal(faceName);
                Vector3 localTangent = FaceLocalTangent(faceName);
                Vector3 worldNormal = piece.TransformDirection(localNormal);
                Vector3 worldTangent = piece.TransformDirection(localTangent);

                Undo.RecordObject(t, "Fix Platform Alignment");
                t.rotation = Quaternion.LookRotation(worldTangent, worldNormal);

                foreach (Transform child in t)
                {
                    Undo.RecordObject(child, "Fix Platform Child Rotation");
                    child.localRotation = Quaternion.identity;
                }

                fixedCount++;
            }
        }

        Debug.Log($"[PlatformAutoFixer] Fixed {fixedCount} platform parent(s). Skipped {skippedCount}.");
    }

    private bool IsPlatformParent(Transform t)
    {
        string name = t.name.ToUpperInvariant();
        return name.Contains("PL") && name.Contains("PARENT");
    }

    private bool TryGetFaceNameFromPivotHierarchy(Transform t, out string faceName)
    {
        Transform current = t;
        while (current != null)
        {
            if (TryGetFaceNameFromPivot(current.name, out faceName))
            {
                return true;
            }
            current = current.parent;
        }

        faceName = null;
        return false;
    }

    private bool TryGetFaceNameFromPivot(string name, out string faceName)
    {
        string compact = name.Replace(" ", "").ToUpperInvariant();
        if (compact.StartsWith("PIVOT-")) compact = compact.Substring(6);
        if (compact == "FRONT") { faceName = "FRONT"; return true; }
        if (compact == "BACK") { faceName = "BACK"; return true; }
        if (compact == "UP") { faceName = "UP"; return true; }
        if (compact == "DOWN") { faceName = "DOWN"; return true; }
        if (compact == "LEFT") { faceName = "LEFT"; return true; }
        if (compact == "RIGHT") { faceName = "RIGHT"; return true; }

        faceName = null;
        return false;
    }

    private Vector3 FaceLocalNormal(string faceName)
    {
        switch (faceName)
        {
            case "FRONT": return Vector3.forward;
            case "BACK": return Vector3.back;
            case "UP": return Vector3.up;
            case "DOWN": return Vector3.down;
            case "LEFT": return Vector3.left;
            case "RIGHT": return Vector3.right;
            default: return Vector3.up;
        }
    }

    private Vector3 FaceLocalTangent(string faceName)
    {
        switch (faceName)
        {
            case "FRONT":
            case "BACK":
                return Vector3.right;
            case "LEFT":
            case "RIGHT":
                return Vector3.forward;
            case "UP":
            case "DOWN":
                return Vector3.forward;
            default:
                return Vector3.forward;
        }
    }
}
