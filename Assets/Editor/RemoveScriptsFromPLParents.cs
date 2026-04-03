using UnityEditor;
using UnityEngine;

public static class RemoveScriptsFromPLParents
{
    [MenuItem("Tools/Cube/Remove All Scripts From PL- Parents")]
    private static void RemoveAllScripts()
    {
        Transform root = Selection.activeTransform;
        if (root == null)
        {
            Debug.LogWarning("[RemoveScriptsFromPLParents] Select a root GameObject (e.g., CubeHolder) and try again.");
            return;
        }

        int removed = 0;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == null) { continue; }
            if (!t.name.StartsWith("PL-")) { continue; }

            MonoBehaviour[] scripts = t.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script == null) { continue; }
                Undo.DestroyObjectImmediate(script);
                removed++;
            }
        }

        Debug.Log($"[RemoveScriptsFromPLParents] Removed {removed} script component(s).");
    }
}
