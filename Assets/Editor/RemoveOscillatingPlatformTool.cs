using UnityEditor;
using UnityEngine;

public class RemoveOscillatingPlatformTool : EditorWindow
{
    private bool includeInactive = true;
    private bool includePrefabsInProject = true;
    private bool includeOpenScenes = true;
    private string[] namePrefixes = new[] { "up", "down" };

    [MenuItem("Tools/Platforms/Remove Oscillating Platform")]
    private static void Open()
    {
        GetWindow<RemoveOscillatingPlatformTool>("Remove Oscillating Platform");
    }

    private void OnGUI()
    {
        GUILayout.Label("Remove OscillatingPlatform from named platforms", EditorStyles.boldLabel);

        includeOpenScenes = EditorGUILayout.Toggle("Scan Open Scenes", includeOpenScenes);
        includePrefabsInProject = EditorGUILayout.Toggle("Scan Prefabs", includePrefabsInProject);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        EditorGUILayout.Space();
        GUILayout.Label("Name Prefixes (case-insensitive)", EditorStyles.label);

        int newSize = Mathf.Clamp(EditorGUILayout.IntField("Count", namePrefixes.Length), 1, 10);
        if (newSize != namePrefixes.Length)
        {
            System.Array.Resize(ref namePrefixes, newSize);
            for (int i = 0; i < namePrefixes.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(namePrefixes[i]))
                {
                    namePrefixes[i] = "up";
                }
            }
        }

        for (int i = 0; i < namePrefixes.Length; i++)
        {
            namePrefixes[i] = EditorGUILayout.TextField($"Prefix {i + 1}", namePrefixes[i]);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Remove OscillatingPlatform"))
        {
            int removed = 0;
            int scanned = 0;

            if (includeOpenScenes)
            {
                Object[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (Object obj in allObjects)
                {
                    GameObject go = obj as GameObject;
                    if (go == null) continue;
                    if (EditorUtility.IsPersistent(go)) continue;
                    if (!includeInactive && !go.activeInHierarchy) continue;
                    if (!NameMatches(go.name)) continue;
                    scanned++;
                    OscillatingPlatform osc = go.GetComponent<OscillatingPlatform>();
                    if (osc != null)
                    {
                        Undo.DestroyObjectImmediate(osc);
                        removed++;
                    }
                }
            }

            if (includePrefabsInProject)
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    Transform[] children = prefab.GetComponentsInChildren<Transform>(includeInactive);
                    for (int i = 0; i < children.Length; i++)
                    {
                        GameObject go = children[i].gameObject;
                        if (!NameMatches(go.name)) continue;
                        scanned++;
                        OscillatingPlatform osc = go.GetComponent<OscillatingPlatform>();
                        if (osc != null)
                        {
                            Undo.DestroyObjectImmediate(osc);
                            removed++;
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
            }

            Debug.Log($"[RemoveOscillatingPlatformTool] Scanned {scanned} objects. Removed {removed} components.");
        }
    }

    private bool NameMatches(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        for (int i = 0; i < namePrefixes.Length; i++)
        {
            string prefix = namePrefixes[i];
            if (string.IsNullOrWhiteSpace(prefix)) continue;
            if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
