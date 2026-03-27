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
    public GameObject[] pieces;

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
            if (piece.GetComponent<CubePiece>() == null)
            {
                piece.AddComponent<CubePiece>();
            }
            pieces[i] = piece;
        }
    }

    /// <summary>Apply the cube face colors to each piece face.</summary>
    public void ApplyFaceColorsFromState()
    {
        ApplyStickersFromState();
    }

    /// <summary>Apply stickers to each cube piece based on CubeState.</summary>
    public void ApplyStickersFromState()
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

        foreach (GameObject piece in pieces)
        {
            if (piece == null) { continue; }
            CubePiece cubePiece = piece.GetComponent<CubePiece>();
            if (cubePiece == null) { continue; }

            Vector3 local = transform.InverseTransformPoint(piece.transform.position);
            int xSign = Sign(local.x);
            int ySign = Sign(local.y);
            int zSign = Sign(local.z);

            // Clear all stickers to black for interior faces.
            cubePiece.SetFaceColor(CubePiece.Face.PosX, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.NegX, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.PosY, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.NegY, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.PosZ, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.NegZ, Color.black);

            if (xSign > 0)
            {
                int idx = GetTileIndexForFace(3, xSign, ySign, zSign);
                cubePiece.SetSticker(CubePiece.Face.PosX, cubeState.tiles[idx]);
            }
            else
            {
                int idx = GetTileIndexForFace(2, xSign, ySign, zSign);
                cubePiece.SetSticker(CubePiece.Face.NegX, cubeState.tiles[idx]);
            }

            if (ySign > 0)
            {
                int idx = GetTileIndexForFace(4, xSign, ySign, zSign);
                cubePiece.SetSticker(CubePiece.Face.PosY, cubeState.tiles[idx]);
            }
            else
            {
                int idx = GetTileIndexForFace(5, xSign, ySign, zSign);
                cubePiece.SetSticker(CubePiece.Face.NegY, cubeState.tiles[idx]);
            }

            if (zSign > 0)
            {
                int idx = GetTileIndexForFace(0, xSign, ySign, zSign);
                cubePiece.SetSticker(CubePiece.Face.PosZ, cubeState.tiles[idx]);
            }
            else
            {
                int idx = GetTileIndexForFace(1, xSign, ySign, zSign);
                cubePiece.SetSticker(CubePiece.Face.NegZ, cubeState.tiles[idx]);
            }
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

    public static Color ToColor(CubeState.TileColor tile)
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

    private static int GetTileIndexForFace(int faceIndex, int xSign, int ySign, int zSign)
    {
        bool top;
        bool right;

        switch (faceIndex)
        {
            case 0: // Front (+Z)
                top = ySign > 0;
                right = xSign > 0;
                break;
            case 1: // Back (-Z)
                top = ySign > 0;
                right = xSign < 0;
                break;
            case 2: // Left (-X)
                top = ySign > 0;
                right = zSign > 0;
                break;
            case 3: // Right (+X)
                top = ySign > 0;
                right = zSign < 0;
                break;
            case 4: // Top (+Y)
                top = zSign < 0;
                right = xSign > 0;
                break;
            case 5: // Bottom (-Y)
                top = zSign > 0;
                right = xSign > 0;
                break;
            default:
                top = true;
                right = true;
                break;
        }

        int tile = (top ? 0 : 2) + (right ? 1 : 0);
        return (faceIndex * 4) + tile;
    }

    private static int Sign(float value)
    {
        return value >= 0f ? 1 : -1;
    }

    /// <summary>Read outward stickers from the pieces and update CubeState.</summary>
    public void SyncStateFromPieces()
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

        for (int i = 0; i < cubeState.tiles.Length; i++)
        {
            cubeState.tiles[i] = CubeState.TileColor.Red;
        }

        foreach (GameObject piece in pieces)
        {
            if (piece == null) { continue; }
            CubePiece cubePiece = piece.GetComponent<CubePiece>();
            if (cubePiece == null) { continue; }

            Vector3 local = transform.InverseTransformPoint(piece.transform.position);
            int xSign = Sign(local.x);
            int ySign = Sign(local.y);
            int zSign = Sign(local.z);

            if (xSign > 0)
            {
                int idx = GetTileIndexForFace(3, xSign, ySign, zSign);
                cubeState.tiles[idx] = cubePiece.GetStickerFacingWorld(transform.right, transform);
            }
            else
            {
                int idx = GetTileIndexForFace(2, xSign, ySign, zSign);
                cubeState.tiles[idx] = cubePiece.GetStickerFacingWorld(-transform.right, transform);
            }

            if (ySign > 0)
            {
                int idx = GetTileIndexForFace(4, xSign, ySign, zSign);
                cubeState.tiles[idx] = cubePiece.GetStickerFacingWorld(transform.up, transform);
            }
            else
            {
                int idx = GetTileIndexForFace(5, xSign, ySign, zSign);
                cubeState.tiles[idx] = cubePiece.GetStickerFacingWorld(-transform.up, transform);
            }

            if (zSign > 0)
            {
                int idx = GetTileIndexForFace(0, xSign, ySign, zSign);
                cubeState.tiles[idx] = cubePiece.GetStickerFacingWorld(transform.forward, transform);
            }
            else
            {
                int idx = GetTileIndexForFace(1, xSign, ySign, zSign);
                cubeState.tiles[idx] = cubePiece.GetStickerFacingWorld(-transform.forward, transform);
            }
        }
    }
}
