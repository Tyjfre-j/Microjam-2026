using System;
using System.Collections.Generic;
using UnityEngine;

public class CubePiece : MonoBehaviour
{
    public enum Face { PosX, NegX, PosY, NegY, PosZ, NegZ }

    [Header("Grid Data")]
    [SerializeField] private Vector3Int gridPosition;
    private Transform cachedTransform;

    private readonly Dictionary<Face, Renderer> faceRenderers = new Dictionary<Face, Renderer>();
    private readonly Dictionary<Face, CubeState.TileColor> stickers = new Dictionary<Face, CubeState.TileColor>();

    private void Awake()
    {
        cachedTransform = transform;
        // Keep your original face naming
        CacheFaceComponents(Face.PosX, "Face_+X");
        CacheFaceComponents(Face.NegX, "Face_-X");
        CacheFaceComponents(Face.PosY, "Face_+Y");
        CacheFaceComponents(Face.NegY, "Face_-Y");
        CacheFaceComponents(Face.PosZ, "Face_+Z");
        CacheFaceComponents(Face.NegZ, "Face_-Z");
    }

    public Transform CachedTransform => cachedTransform != null ? cachedTransform : transform;
    public Vector3Int GridPosition => gridPosition;
    public void SetGridPosition(Vector3Int position) => gridPosition = position;

    // FIXED: Now takes Material (Option 2) and keeps your logic
    public void SetSticker(Face face, CubeState.TileColor color, Material mat)
    {
        stickers[face] = color;
        if (faceRenderers.TryGetValue(face, out Renderer renderer))
        {
            renderer.material = mat;
        }
    }

    public CubeState.TileColor GetSticker(Face face)
    {
        return stickers.TryGetValue(face, out CubeState.TileColor color) ? color : CubeState.TileColor.Red;
    }

    public CubeState.TileColor GetStickerFacingWorld(Vector3 worldDir)
    {
        Face bestFace = Face.PosZ;
        float bestDot = -1f;
        foreach (Face face in Enum.GetValues(typeof(Face)))
        {
            Vector3 localDir = FaceToLocalDir(face);
            Vector3 worldDirFromPiece = transform.TransformDirection(localDir);
            float dot = Vector3.Dot(worldDirFromPiece.normalized, worldDir.normalized);
            if (dot > bestDot) { bestDot = dot; bestFace = face; }
        }
        return GetSticker(bestFace);
    }

    public void SetFaceColor(Face face, Color color)
    {
        if (faceRenderers.TryGetValue(face, out Renderer renderer) && renderer != null)
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_BaseColor")) { mat.SetColor("_BaseColor", color); }
            if (mat.HasProperty("_Color")) { mat.SetColor("_Color", color); }
        }
    }

    private void CacheFaceComponents(Face face, string name)
    {
        Transform t = transform.Find(name);
        if (t == null) return;
        Renderer r = t.GetComponent<Renderer>();
        if (r != null) faceRenderers[face] = r;
    }

    private static Vector3 FaceToLocalDir(Face face)
    {
        return face switch
        {
            Face.PosX => Vector3.right,
            Face.NegX => Vector3.left,
            Face.PosY => Vector3.up,
            Face.NegY => Vector3.down,
            Face.PosZ => Vector3.forward,
            Face.NegZ => Vector3.back,
            _ => Vector3.forward,
        };
    }
}