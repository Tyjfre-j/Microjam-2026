using UnityEditor;
using UnityEngine;

public static class RemoveMissingScriptsTool
{
    private const string MenuPath = "Tools/Cube/Remove Missing Scripts (Selected Root)";

    [MenuItem(MenuPath)]
    private static void RemoveMissingScripts()
    {
        if (Selection.activeTransform == null)
        {
            Debug.LogWarning("[RemoveMissingScriptsTool] Select a root in the Hierarchy.");
            return;
        }

        Transform root = Selection.activeTransform;
        int removedCount = 0;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) { continue; }
            removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }

        Debug.Log($"[RemoveMissingScriptsTool] Removed {removedCount} missing script component(s) under '{root.name}'.");
    }
}
