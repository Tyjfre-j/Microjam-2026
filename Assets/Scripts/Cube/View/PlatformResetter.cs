using System.Collections.Generic;
using UnityEngine;

public class PlatformResetter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private bool includeInactive = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private readonly Dictionary<Transform, Quaternion> cachedLocalRotations = new Dictionary<Transform, Quaternion>();

    private void Awake()
    {
        if (piecesRoot == null)
        {
            piecesRoot = transform;
        }

        CachePlatformRotations();
    }

    /// <summary>Reset cached platform parent and child local rotations back to their initial state.</summary>
    [ContextMenu("Reset Platforms")]
    public void ResetPlatforms()
    {
        if (cachedLocalRotations.Count == 0)
        {
            CachePlatformRotations();
        }

        int restored = 0;
        foreach (KeyValuePair<Transform, Quaternion> kvp in cachedLocalRotations)
        {
            if (kvp.Key == null) continue;
            kvp.Key.localRotation = kvp.Value;
            restored++;
        }

        Log($"Reset {restored} cached platform transforms.");
    }

    /// <summary>Re-scan the hierarchy and cache current local rotations for platform parents and children.</summary>
    [ContextMenu("Cache Platforms")]
    public void CachePlatforms()
    {
        CachePlatformRotations();
    }

    private void CachePlatformRotations()
    {
        cachedLocalRotations.Clear();

        if (piecesRoot == null)
        {
            piecesRoot = transform;
        }

        foreach (Transform piece in piecesRoot)
        {
            if (piece == null) continue;
            Transform[] all = piece.GetComponentsInChildren<Transform>(includeInactive);
            foreach (Transform t in all)
            {
                if (t == null) continue;
                if (!IsPlatformParentOrChild(t)) continue;
                if (!cachedLocalRotations.ContainsKey(t))
                {
                    cachedLocalRotations[t] = t.localRotation;
                }
            }
        }

        Log($"Cached {cachedLocalRotations.Count} platform transforms.");
    }

    private bool IsPlatformParentOrChild(Transform t)
    {
        if (t == null) return false;
        if (IsPlatformParent(t)) return true;
        return t.parent != null && IsPlatformParent(t.parent);
    }

    private bool IsPlatformParent(Transform t)
    {
        string name = t.name.ToUpperInvariant();
        return name.Contains("PL") && name.Contains("PARENT");
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
