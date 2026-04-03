using UnityEditor;
using UnityEngine;

public static class CubePieceBuilder
{
    private const float FaceOffset = 0.51f;

    [MenuItem("Tools/Cube Platformer/Create CubePiece Prefab Root")]
    private static void CreateCubePiece()
    {
        GameObject root = new GameObject("CubePiece");
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = Vector3.one;

        CreateFace(root.transform, "Face_+X", new Vector3(FaceOffset, 0f, 0f), new Vector3(0f, -90f, 0f));
        CreateFace(root.transform, "Face_-X", new Vector3(-FaceOffset, 0f, 0f), new Vector3(0f, 90f, 0f));
        CreateFace(root.transform, "Face_+Y", new Vector3(0f, FaceOffset, 0f), new Vector3(90f, 0f, 0f));
        CreateFace(root.transform, "Face_-Y", new Vector3(0f, -FaceOffset, 0f), new Vector3(-90f, 0f, 0f));
        CreateFace(root.transform, "Face_+Z", new Vector3(0f, 0f, FaceOffset), new Vector3(0f, 180f, 0f));
        CreateFace(root.transform, "Face_-Z", new Vector3(0f, 0f, -FaceOffset), new Vector3(0f, 0f, 0f));

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Create CubePiece");
    }

    private static void CreateFace(Transform parent, string name, Vector3 localPos, Vector3 localEuler)
    {
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = name;
        face.transform.SetParent(parent, false);
        face.transform.localPosition = localPos;
        face.transform.localRotation = Quaternion.Euler(localEuler);
    }
}
