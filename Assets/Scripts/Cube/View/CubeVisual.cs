using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CubeVisual : MonoBehaviour
{
    [Header("Prefab (Optional)")]
    [FormerlySerializedAs("cubePiecePrefab")]
    [SerializeField, Tooltip("Optional: used only if you call BuildPieces() to spawn cubelets.")]
    private GameObject cubePiecePrefab;

    [Header("Surreal Dimensions (Animators)")]
    public RuntimeAnimatorController meatAnim;   // Red
    public RuntimeAnimatorController spaceAnim;  // Blue
    public RuntimeAnimatorController ruinsAnim;  // Orange
    public RuntimeAnimatorController dreamAnim;  // Yellow
    public RuntimeAnimatorController clinicAnim; // White
    public RuntimeAnimatorController forestAnim; // Green
    [Header("Surreal Animated Sheets")]
    public Sprite[] meatSheet;   // Drag the sliced meat PNG here
    public Sprite[] spaceSheet;  // Drag the sliced space PNG here
    public Sprite[] ruinsSheet;  // Drag the sliced ruins PNG here
    [Header("Layout")]
    [FormerlySerializedAs("pieceOffset")]
    [SerializeField, Tooltip("Half-size offset for spawning pieces when BuildPieces() is used.")]
    private float pieceOffset = 0.5f;

    [Header("Existing Pieces (Required)")]
    [FormerlySerializedAs("existingPiecesRoot")]
    [SerializeField, Tooltip("Root that contains the 8 cube pieces (e.g., CubeHolder/Cube).")]
    private Transform cubePiecesRootTransform;
    [FormerlySerializedAs("reparentExistingToCubeVisual")]
    [SerializeField, Tooltip("Reparent pieces under this GameObject at runtime (Play Mode only). Recommended OFF for prebuilt cube prefabs.")]
    private bool reparentPiecesToThis = false;

    [Header("Debug")]

    private CubeState cubeState;
    private GameObject cubePiecesRoot;
    public GameObject[] pieces;

    /// <summary>Transform space used to evaluate piece positions.</summary>
    public Transform PiecesRoot => cubePiecesRootTransform != null ? cubePiecesRootTransform : transform;

    private void Awake()
    {
        cubeState = GetComponent<CubeState>();
    }

    private void Start()
    {
        CacheExistingPieces();
        ApplyStickersFromState();
    }

    private void CacheExistingPieces()
    {
        if (cubePiecesRootTransform == null)
        {
            pieces = new GameObject[0];
            return;
        }

        cubePiecesRoot = cubePiecesRootTransform.gameObject;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in cubePiecesRootTransform)
        {
            children.Add(child);
        }

        List<GameObject> found = new List<GameObject>(children.Count);
        bool canReparent = reparentPiecesToThis && Application.isPlaying;
        if (canReparent && cubePiecesRootTransform != transform)
        {
            canReparent = false;
        }
        if (reparentPiecesToThis && !Application.isPlaying) { }

        foreach (Transform child in children)
        {
            if (canReparent)
            {
                child.SetParent(transform, true);
            }
            if (child.GetComponent<CubePiece>() == null) { child.gameObject.AddComponent<CubePiece>(); }
            found.Add(child.gameObject);
        }

        pieces = found.ToArray();

        if (pieces.Length != 8) { }
    }

    /// <summary>Refresh the pieces array from the assigned Existing Pieces Root.</summary>
    public void RefreshExistingPieces()
    {
        CacheExistingPieces();
    }

    public void BuildPieces()
    {
        if (cubePiecesRoot == null)
        {
            Transform existing = transform.Find("CubePieces");
            cubePiecesRoot = existing != null ? existing.gameObject : new GameObject("CubePieces");
            cubePiecesRoot.transform.SetParent(transform, false);
        }

        foreach (Transform child in cubePiecesRoot.transform) Destroy(child.gameObject);

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

        pieces = new GameObject[8];
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject piece = Instantiate(cubePiecePrefab, cubePiecesRoot.transform);
            piece.transform.localPosition = positions[i];
            if (piece.GetComponent<CubePiece>() == null) piece.AddComponent<CubePiece>();
            pieces[i] = piece;
        }
    }

    public void ApplyStickersFromState()
    {
        if (cubeState == null || cubeState.tiles == null || cubeState.tiles.Length != 24) return;
        EnsureValidPiecesCache();
        if (pieces == null || pieces.Length == 0) return;

        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            CubePiece cubePiece = piece.GetComponent<CubePiece>();
            if (cubePiece == null) continue;
            Vector3 local = PiecesRoot.InverseTransformPoint(piece.transform.position);
            int xSign = Sign(local.x); int ySign = Sign(local.y); int zSign = Sign(local.z);

            // Interior faces to black
            cubePiece.SetFaceColor(CubePiece.Face.PosX, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.NegX, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.PosY, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.NegY, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.PosZ, Color.black);
            cubePiece.SetFaceColor(CubePiece.Face.NegZ, Color.black);

            if (xSign > 0) AssignAnimation(cubePiece, CubePiece.Face.PosX, 3, xSign, ySign, zSign);
            else AssignAnimation(cubePiece, CubePiece.Face.NegX, 2, xSign, ySign, zSign);

            if (ySign > 0) AssignAnimation(cubePiece, CubePiece.Face.PosY, 4, xSign, ySign, zSign);
            else AssignAnimation(cubePiece, CubePiece.Face.NegY, 5, xSign, ySign, zSign);

            if (zSign > 0) AssignAnimation(cubePiece, CubePiece.Face.PosZ, 0, xSign, ySign, zSign);
            else AssignAnimation(cubePiece, CubePiece.Face.NegZ, 1, xSign, ySign, zSign);
        }
    }

    public void ApplyStartupFaceMeshVisibilityByMaterial()
    {
        if (pieces == null || pieces.Length == 0)
        {
            CacheExistingPieces();
        }

        if (pieces == null) return;

        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;

            foreach (Transform child in piece.transform)
            {
                if (child == null || !IsFaceName(child.name)) continue;

                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;

                Material faceMaterial = meshRenderer.sharedMaterial != null
                    ? meshRenderer.sharedMaterial
                    : meshRenderer.material;
                meshRenderer.enabled = faceMaterial != null;
            }
        }
    }

    public void DisableAllFaceMeshes()
    {
        EnsureValidPiecesCache();
        if (pieces == null || pieces.Length == 0) return;

        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;

            foreach (Transform child in piece.transform)
            {
                if (child == null || !IsFaceName(child.name)) continue;

                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;
                meshRenderer.enabled = false;
            }
        }
    }

    private void AssignAnimation(CubePiece cp, CubePiece.Face face, int faceIdx, int x, int y, int z)
    {
        int tileIdx = GetTileIndexForFace(faceIdx, x, y, z);
        CubeState.TileColor tileColor = cubeState.tiles[tileIdx];

        // This finds which corner of the 2x2 face this piece is (0, 1, 2, or 3)
        int quadrantID = tileIdx % 4;

        // Pick the sheet based on the color from CubeState
        Sprite[] currentSheet = tileColor switch
        {
            CubeState.TileColor.Red => meatSheet,
            CubeState.TileColor.Blue => spaceSheet,
            CubeState.TileColor.Orange => ruinsSheet,
            // ... add the rest
            _ => meatSheet
        };

        cp.SetSticker(face, tileColor, currentSheet, quadrantID);
    }

    // ALL OF MARTE'S ORIGINAL METHODS BELOW
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

    private int GetTileIndexForFace(int faceIndex, int xSign, int ySign, int zSign)
    {
        bool top; bool right;
        switch (faceIndex)
        {
            case 0: top = ySign > 0; right = xSign > 0; break;
            case 1: top = ySign > 0; right = xSign < 0; break;
            case 2: top = ySign > 0; right = zSign > 0; break;
            case 3: top = ySign > 0; right = zSign < 0; break;
            case 4: top = zSign < 0; right = xSign > 0; break;
            case 5: top = zSign > 0; right = xSign > 0; break;
            default: top = true; right = true; break;
        }
        return (faceIndex * 4) + ((top ? 0 : 2) + (right ? 1 : 0));
    }

    private static int Sign(float value) => value >= 0f ? 1 : -1;

    private static bool IsFaceName(string faceName)
    {
        return string.Equals(faceName, "front", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(faceName, "back", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(faceName, "left", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(faceName, "right", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(faceName, "up", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(faceName, "down", System.StringComparison.OrdinalIgnoreCase);
    }

    public void SyncStateFromPieces()
    {
        if (cubeState == null || cubeState.tiles == null || cubeState.tiles.Length != 24) return;
        EnsureValidPiecesCache();
        if (pieces == null || pieces.Length == 0) return;

        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            CubePiece cubePiece = piece.GetComponent<CubePiece>();
            if (cubePiece == null) continue;
            Vector3 local = PiecesRoot.InverseTransformPoint(piece.transform.position);
            int xSign = Sign(local.x); int ySign = Sign(local.y); int zSign = Sign(local.z);

            if (xSign > 0) cubeState.tiles[GetTileIndexForFace(3, xSign, ySign, zSign)] = cubePiece.GetStickerFacingWorld(PiecesRoot.right);
            else cubeState.tiles[GetTileIndexForFace(2, xSign, ySign, zSign)] = cubePiece.GetStickerFacingWorld(-PiecesRoot.right);

            if (ySign > 0) cubeState.tiles[GetTileIndexForFace(4, xSign, ySign, zSign)] = cubePiece.GetStickerFacingWorld(PiecesRoot.up);
            else cubeState.tiles[GetTileIndexForFace(5, xSign, ySign, zSign)] = cubePiece.GetStickerFacingWorld(-PiecesRoot.up);

            if (zSign > 0) cubeState.tiles[GetTileIndexForFace(0, xSign, ySign, zSign)] = cubePiece.GetStickerFacingWorld(PiecesRoot.forward);
            else cubeState.tiles[GetTileIndexForFace(1, xSign, ySign, zSign)] = cubePiece.GetStickerFacingWorld(-PiecesRoot.forward);
        }
    }

    private void EnsureValidPiecesCache()
    {
        if (pieces == null || pieces.Length == 0)
        {
            CacheExistingPieces();
            return;
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null)
            {
                CacheExistingPieces();
                return;
            }
        }
    }
}
