using UnityEditor;
using UnityEngine;

public static class FaceScriptRemover
{
    private const string MenuItemPath = "Tools/Cube/Remove Scripts From Faces (Selected Root)";

    [MenuItem(MenuItemPath)]
    public static void RemoveScriptsFromFaces()
    {
        Transform root = Selection.activeTransform;
        if (root == null)
        {
            Debug.LogWarning("[FaceScriptRemover] Select a cube root or face parent in the Hierarchy.");
            return;
        }

        int removed = 0;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (!IsFaceName(t.name)) { continue; }

            MonoBehaviour[] scripts = t.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script == null) { continue; }
                Undo.DestroyObjectImmediate(script);
                removed++;
            }
        }

        Debug.Log($"[FaceScriptRemover] Removed {removed} script component(s) from faces under '{root.name}'.");
    }

    private static bool IsFaceName(string name)
    {
        return name == "Front" ||
               name == "Back" ||
               name == "left" ||
               name == "right" ||
               name == "up" ||
               name == "down" ||
               name == "Face_+X" ||
               name == "Face_-X" ||
               name == "Face_+Y" ||
               name == "Face_-Y" ||
               name == "Face_+Z" ||
               name == "Face_-Z";
    }
}
