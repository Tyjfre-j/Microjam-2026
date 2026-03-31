using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CubeSetupAuditor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform piecesRoot;

    [Header("Validation")]
    [SerializeField, Tooltip("Min dot for axis alignment checks (1 = perfect).")]
    private float axisDotThreshold = 0.9f;
    [SerializeField] private bool includeInactive = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private static readonly string[] FaceNames = { "FRONT", "BACK", "UP", "DOWN", "LEFT", "RIGHT" };

    /// <summary>Audit cube piece hierarchy, face mesh orientation, and platform parent alignment.</summary>
    [ContextMenu("Audit Cube Setup")]
    public void AuditCubeSetup()
    {
        if (piecesRoot == null)
        {
            piecesRoot = transform;
        }

        int issueCount = 0;
        StringBuilder report = new StringBuilder();
        report.AppendLine($"=== Cube Setup Audit: {piecesRoot.name} ===");

        foreach (Transform piece in piecesRoot)
        {
            if (piece == null) continue;
            report.AppendLine($"\n[Piece] {piece.name}");

            Dictionary<string, List<Transform>> faces = new Dictionary<string, List<Transform>>();
            List<Transform> platformParents = new List<Transform>();

            Transform[] all = piece.GetComponentsInChildren<Transform>(includeInactive);
            foreach (Transform t in all)
            {
                if (t == null) continue;
                if (TryGetFaceName(t.name, out string faceName))
                {
                    if (!faces.TryGetValue(faceName, out List<Transform> list))
                    {
                        list = new List<Transform>();
                        faces[faceName] = list;
                    }
                    list.Add(t);
                }

                if (IsPlatformParent(t))
                {
                    platformParents.Add(t);
                }
            }

            foreach (string faceName in FaceNames)
            {
                faces.TryGetValue(faceName, out List<Transform> faceList);
                int count = faceList == null ? 0 : faceList.Count;
                if (count == 0)
                {
                    issueCount++;
                    report.AppendLine($"  ⚠ Missing face: {faceName}");
                }
                else if (count > 1)
                {
                    issueCount++;
                    report.AppendLine($"  ⚠ Multiple faces named {faceName}: {count}");
                }
            }

            foreach (KeyValuePair<string, List<Transform>> kvp in faces)
            {
                Vector3 expectedLocal = FaceLocalNormal(kvp.Key);
                foreach (Transform face in kvp.Value)
                {
                    Vector3 localForward = piece.InverseTransformDirection(face.forward).normalized;
                    float dot = Vector3.Dot(localForward, expectedLocal);
                    if (dot < axisDotThreshold)
                    {
                        issueCount++;
                        report.AppendLine($"  ⚠ Face '{face.name}' forward misaligned. dot={dot:F2} expected {expectedLocal}");
                    }
                }
            }

            foreach (Transform plParent in platformParents)
            {
                if (!TryGetFaceNameFromHierarchy(plParent, out string faceName))
                {
                    issueCount++;
                    report.AppendLine($"  ⚠ Platform parent '{plParent.name}' not under a named face (front/back/up/down/left/right).");
                    continue;
                }

                Vector3 expectedLocal = FaceLocalNormal(faceName);
                Vector3 localUp = piece.InverseTransformDirection(plParent.up).normalized;
                float dot = Vector3.Dot(localUp, expectedLocal);
                if (dot < axisDotThreshold)
                {
                    issueCount++;
                    report.AppendLine($"  ⚠ Platform '{plParent.name}' up misaligned. dot={dot:F2} expected {expectedLocal}");
                }

                foreach (Transform child in plParent)
                {
                    if (Quaternion.Angle(child.localRotation, Quaternion.identity) > 0.1f)
                    {
                        issueCount++;
                        report.AppendLine($"  ⚠ Platform child '{child.name}' has localRotation != identity.");
                        break;
                    }
                }
            }
        }

        report.AppendLine($"\n=== Audit Complete: {issueCount} issue(s) found ===");
        Log(report.ToString());
    }

    private bool TryGetFaceNameFromHierarchy(Transform t, out string faceName)
    {
        Transform current = t;
        while (current != null)
        {
            if (TryGetFaceName(current.name, out faceName))
            {
                return true;
            }
            current = current.parent;
        }

        faceName = null;
        return false;
    }

    private bool TryGetFaceName(string name, out string faceName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            faceName = null;
            return false;
        }

        string compact = name.Replace(" ", "").ToUpperInvariant();
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
            default: return Vector3.forward;
        }
    }

    private bool IsPlatformParent(Transform t)
    {
        if (t == null) return false;
        string name = t.name.ToUpperInvariant();
        return name.Contains("PL") && name.Contains("PARENT");
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
