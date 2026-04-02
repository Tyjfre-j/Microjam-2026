using UnityEngine;
using UnityEditor;
using System.IO;

public class FinalMeshBaker : MonoBehaviour
{
    [ContextMenu("FORCE BAKE ALL MESHES")]
    public void Bake()
    {
        // 1. Create the folder if it doesn't exist
        string folderPath = "Assets/BakedMeshes";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.ImportAsset(folderPath);
        }

        // 2. Find every mesh filter in the children
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();
        int count = 0;

        foreach (MeshFilter mf in filters)
        {
            if (mf.sharedMesh == null) continue;

            // 3. Create a REAL copy of the mesh data
            Mesh meshCopy = Instantiate(mf.sharedMesh);

            // 4. Create a unique name
            string meshName = mf.gameObject.name + "_" + mf.gameObject.GetInstanceID() + ".asset";
            string fullPath = folderPath + "/" + meshName;

            // 5. Save the file to your hard drive
            AssetDatabase.CreateAsset(meshCopy, fullPath);

            // 6. Tell the object to use the new file instead of the temporary one
            mf.sharedMesh = meshCopy;
            count++;
        }

        // 7. Refresh Unity so the files appear
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"SUCCESS: {count} meshes forced into Assets/BakedMeshes!");
    }
}