using UnityEditor;
using UnityEngine;

public class AssignOscillatingPlatformTool : EditorWindow
{
    private bool includeInactive = true;
    private bool includePrefabsInProject = true;
    private bool includeOpenScenes = true;
    private Transform sceneRootOverride;

    [MenuItem("Tools/Platforms/Assign Oscillating Platform")]
    private static void Open()
    {
        GetWindow<AssignOscillatingPlatformTool>("Assign Oscillating Platform");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign OscillatingPlatform to platforms starting with \"up\" or \"down\"", EditorStyles.boldLabel);

        includeOpenScenes = EditorGUILayout.Toggle("Scan Open Scenes", includeOpenScenes);
        includePrefabsInProject = EditorGUILayout.Toggle("Scan Prefabs", includePrefabsInProject);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        sceneRootOverride = (Transform)EditorGUILayout.ObjectField("Scene Root (Optional)", sceneRootOverride, typeof(Transform), true);

        EditorGUILayout.Space();

        if (GUILayout.Button("Assign OscillatingPlatform"))
        {
            int added = 0;
            int scanned = 0;

            if (includeOpenScenes)
            {
                if (sceneRootOverride != null)
                {
                    Transform[] children = sceneRootOverride.GetComponentsInChildren<Transform>(includeInactive);
                    for (int i = 0; i < children.Length; i++)
                    {
                        GameObject go = children[i].gameObject;
                        if (!includeInactive && !go.activeInHierarchy) continue;
                        if (!NameMatches(go.name)) continue;
                        scanned++;
                        if (go.GetComponent<OscillatingPlatform>() == null)
                        {
                            Undo.AddComponent<OscillatingPlatform>(go);
                            added++;
                        }
                    }
                }
                else
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
                        if (go.GetComponent<OscillatingPlatform>() == null)
                        {
                            Undo.AddComponent<OscillatingPlatform>(go);
                            added++;
                        }
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
                        if (go.GetComponent<OscillatingPlatform>() == null)
                        {
                            Undo.AddComponent<OscillatingPlatform>(go);
                            added++;
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
            }

            Debug.Log($"[AssignOscillatingPlatformTool] Scanned {scanned} objects. Added {added} components.");
        }
    }

    private bool NameMatches(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.StartsWith("up/down", System.StringComparison.OrdinalIgnoreCase);
    }
}
