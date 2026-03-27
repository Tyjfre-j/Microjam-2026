using System.Collections.Generic;
using UnityEngine;

public class CubeVisual : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject cubePiecePrefab;

    [Header("Layout")]
    [SerializeField] private float pieceOffset = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool rebuildOnStart = true;
    [SerializeField] private bool logMissingFaces = false;

    private CubeState cubeState;
    private GameObject cubePiecesRoot;
    private GameObject[] pieces;

    private void Awake()
    {
        cubeState = GetComponent<CubeState>();
        if (cubeState == null)
        {
            Debug.LogError("[CubeVisual] Missing CubeState on CubeManager.");
        }
    }

    private void Start()
    {
        if (rebuildOnStart)
        {
            BuildPieces();
        }

        ApplyFaceColorsFromState();
    }

    /// <summary>Instantiate 8 corner pieces into a 2×2×2 grid.</summary>
    public void BuildPieces()
    {
        if (cubePiecePrefab == null)
        {
            Debug.LogError("[CubeVisual] CubePiece prefab is not assigned.");
            return;
        }

        if (cubePiecesRoot == null)
        {
            Transform existing = transform.Find("CubePieces");
            cubePiecesRoot = existing != null ? existing.gameObject : new GameObject("CubePieces");
            cubePiecesRoot.transform.SetParent(transform, false);
        }

        foreach (Transform child in cubePiecesRoot.transform)
        {
            Destroy(child.gameObject);
        }

        List<Vector3> positions = new List<Vector3>
        {
            new Vector3(-pieceOffset, -pieceOffset, -pieceOffset),
            new Vector3(-pieceOffset, -pieceOffset,  pieceOffset),
            new Vector3(-pieceOffset,  pieceOffset, -pieceOffset),
            new Vector3(-pieceOffset,  pieceOffset,  pieceOffset),
            new Vector3( pieceOffset, -pieceOffset, -pieceOffset),
            new Vector3( pieceOffset, -pieceOffset,  pieceOffset),
            new Vector3( pieceOffset,  pieceOffset, -pieceOffset),
            new Vector3( pieceOffset,  pieceOffset,  pieceOffset),
        };

        pieces = new GameObject[positions.Count];

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject piece = Instantiate(cubePiecePrefab, cubePiecesRoot.transform);
            piece.transform.localPosition = positions[i];
            piece.transform.localRotation = Quaternion.identity;
            pieces[i] = piece;
        }
    }

    /// <summary>Apply the cube face colors to each piece face.</summary>
    public void ApplyFaceColorsFromState()
    {
        if (cubeState == null || cubeState.tiles == null || cubeState.tiles.Length != 24)
        {
            Debug.LogError("[CubeVisual] CubeState tiles are missing or invalid.");
            return;
        }

        if (pieces == null || pieces.Length == 0)
        {
            return;
        }

        Color front = ToColor(cubeState.tiles[0]);
        Color back = ToColor(cubeState.tiles[4]);
        Color left = ToColor(cubeState.tiles[8]);
        Color right = ToColor(cubeState.tiles[12]);
        Color top = ToColor(cubeState.tiles[16]);
        Color bottom = ToColor(cubeState.tiles[20]);

        foreach (GameObject piece in pieces)
        {
            if (piece == null) { continue; }

            ApplyColorToFace(piece, "Face_+Z", front);
            ApplyColorToFace(piece, "Face_-Z", back);
            ApplyColorToFace(piece, "Face_-X", left);
            ApplyColorToFace(piece, "Face_+X", right);
            ApplyColorToFace(piece, "Face_+Y", top);
            ApplyColorToFace(piece, "Face_-Y", bottom);
        }
    }

    private void ApplyColorToFace(GameObject piece, string faceName, Color color)
    {
        Transform face = piece.transform.Find(faceName);
        if (face == null)
        {
            if (logMissingFaces)
            {
                Debug.LogWarning($"[CubeVisual] Missing face '{faceName}' on {piece.name}");
            }
            return;
        }

        Renderer renderer = face.GetComponent<Renderer>();
        if (renderer == null) { return; }
        Material mat = renderer.material;
        if (mat.HasProperty("_BaseColor")) { mat.SetColor("_BaseColor", color); }
        if (mat.HasProperty("_Color")) { mat.SetColor("_Color", color); }
    }

    private static Color ToColor(CubeState.TileColor tile)
    {
        return tile switch
        {
            CubeState.TileColor.Red => new Color(0.80f, 0.20f, 0.20f),
            CubeState.TileColor.Orange => new Color(0.85f, 0.45f, 0.05f),
            CubeState.TileColor.Yellow => new Color(0.92f, 0.80f, 0.20f),
            CubeState.TileColor.Green => new Color(0.20f, 0.55f, 0.30f),
            CubeState.TileColor.Blue => new Color(0.15f, 0.35f, 0.65f),
            CubeState.TileColor.White => new Color(0.92f, 0.92f, 0.92f),
            _ => Color.magenta,
        };
    }
}
