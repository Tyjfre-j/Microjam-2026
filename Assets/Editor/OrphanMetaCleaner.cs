using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class OrphanMetaCleaner : EditorWindow
{
    [MenuItem("Tools/Project/Clean Orphan Meta Files")]
    public static void CleanOrphanMetaFiles()
    {
        string assetsPath = Application.dataPath;
        string[] metaFiles = Directory.GetFiles(assetsPath, "*.meta", SearchOption.AllDirectories);
        List<string> removed = new List<string>();

        foreach (string metaFullPath in metaFiles)
        {
            string assetFullPath = metaFullPath.Substring(0, metaFullPath.Length - 5);
            bool assetExists = File.Exists(assetFullPath) || Directory.Exists(assetFullPath);
            if (assetExists) continue;

            string assetRelativePath = "Assets" + assetFullPath.Substring(assetsPath.Length).Replace("\\", "/");
            FileUtil.DeleteFileOrDirectory(metaFullPath);
            removed.Add(assetRelativePath);
        }

        AssetDatabase.Refresh();

        if (removed.Count == 0)
        {
            Debug.Log("[OrphanMetaCleaner] No orphan .meta files found.");
        }
        else
        {
            Debug.Log($"[OrphanMetaCleaner] Removed {removed.Count} orphan .meta file(s).");
        }
    }
}
