using System;
using System.Collections.Generic;
using UnityEngine;

public class CubePiece : MonoBehaviour
{
    public enum Face { PosX, NegX, PosY, NegY, PosZ, NegZ }

    private readonly Dictionary<Face, Renderer> faceRenderers = new Dictionary<Face, Renderer>();
    private readonly Dictionary<Face, Animator> faceAnimators = new Dictionary<Face, Animator>(); // NEW
    private readonly Dictionary<Face, CubeState.TileColor> stickers = new Dictionary<Face, CubeState.TileColor>();

    private void Awake()
    {
        CacheFaceComponents(Face.PosX, "Face_+X");
        CacheFaceComponents(Face.NegX, "Face_-X");
        CacheFaceComponents(Face.PosY, "Face_+Y");
        CacheFaceComponents(Face.NegY, "Face_-Y");
        CacheFaceComponents(Face.PosZ, "Face_+Z");
        CacheFaceComponents(Face.NegZ, "Face_-Z");
    }

    // Inside CubePiece.cs
    public void SetSticker(Face face, CubeState.TileColor color, Sprite[] animationSheet, int quadrantID)
    {
        // Find the child object for the face (e.g. Face_+Z)
        Transform t = transform.Find("Face_" + face.ToString().Replace("Pos", "+").Replace("Neg", "-"));
        if (t != null)
        {
            // Add or Get the animator
            QuadrantAnimator qAnim = t.GetComponent<QuadrantAnimator>();
            if (qAnim == null) qAnim = t.gameObject.AddComponent<QuadrantAnimator>();

            // Hide the 3D Mesh so the Sprite shows
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            // Add SpriteRenderer if missing
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.material = new Material(Shader.Find("Sprites/Default")); // Or your custom unlit shader

            qAnim.Setup(animationSheet, quadrantID);
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
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = face;
            }
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

    private void ApplySticker(Face face)
    {
        if (!faceRenderers.TryGetValue(face, out Renderer renderer) || renderer == null) return;

        Color color = CubeVisual.ToColor(GetSticker(face));
        Material mat = renderer.material;
        if (mat.HasProperty("_BaseColor")) { mat.SetColor("_BaseColor", color); }
        if (mat.HasProperty("_Color")) { mat.SetColor("_Color", color); }
    }

    private void CacheFaceComponents(Face face, string name)
    {
        Transform t = transform.Find(name);
        if (t == null) return;

        Renderer r = t.GetComponent<Renderer>();
        if (r != null) faceRenderers[face] = r;

        Animator a = t.GetComponent<Animator>(); // NEW
        if (a != null) faceAnimators[face] = a;
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