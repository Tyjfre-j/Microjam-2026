using System.Collections.Generic;
using UnityEngine;

public class CubeVisual : MonoBehaviour
{
    [Header("Prefab (Only if Rebuild is ON)")]
    [SerializeField] private GameObject cubePiecePrefab;

    [Header("Manual Setup")]
    [Tooltip("UNCHECK THIS if you placed cubes manually in the scene")]
    public bool rebuildOnStart = false;
    [Tooltip("Drag your 8 manual cube pieces here")]
    public GameObject[] pieces = new GameObject[8];

    [Header("Face Materials")]
    public Material meatMat; public Material spaceMat;
    public Material ruinsMat; public Material dreamMat;
    public Material clinicMat; public Material forestMat;

    [Header("Layout")]
    [SerializeField] private float pieceOffset = 0.5f;

    private CubeState cubeState;
    private GameObject cubePiecesRoot;

    public Transform PiecesRoot => (cubePiecesRoot != null) ? cubePiecesRoot.transform : transform;

    private void Awake() => cubeState = GetComponent<CubeState>();

    private void Start()
    {
        // Only build if the box is checked
        if (rebuildOnStart)
        {
            BuildPieces();
        }

        ApplyStickersFromState();
    }

    public void BuildPieces()
    {
        if (cubePiecePrefab == null) return;

        if (cubePiecesRoot == null)
        {
            Transform existing = transform.Find("CubePieces");
            cubePiecesRoot = existing != null ? existing.gameObject : new GameObject("CubePieces");
            cubePiecesRoot.transform.SetParent(transform, false);
        }
        foreach (Transform child in cubePiecesRoot.transform) Destroy(child.gameObject);

        pieces = new GameObject[8];
        Vector3[] pos = {
            new Vector3(-pieceOffset,-pieceOffset,-pieceOffset), new Vector3(-pieceOffset,-pieceOffset,pieceOffset),
            new Vector3(-pieceOffset,pieceOffset,-pieceOffset), new Vector3(-pieceOffset,pieceOffset,pieceOffset),
            new Vector3(pieceOffset,-pieceOffset,-pieceOffset), new Vector3(pieceOffset,-pieceOffset,pieceOffset),
            new Vector3(pieceOffset,pieceOffset,-pieceOffset), new Vector3(pieceOffset,pieceOffset,pieceOffset)
        };

        for (int i = 0; i < 8; i++)
        {
            pieces[i] = Instantiate(cubePiecePrefab, cubePiecesRoot.transform);
            pieces[i].transform.localPosition = pos[i];
            if (pieces[i].GetComponent<CubePiece>() == null) pieces[i].AddComponent<CubePiece>();
        }
    }

    public void ApplyStickersFromState()
    {
        if (cubeState == null || pieces == null || pieces.Length == 0) return;

        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            CubePiece cp = piece.GetComponent<CubePiece>();
            if (cp == null) continue;

            Vector3 local = transform.InverseTransformPoint(piece.transform.position);
            int x = Sign(local.x); int y = Sign(local.y); int z = Sign(local.z);

            if (x > 0) UpdateFace(cp, CubePiece.Face.PosX, 3, x, y, z); else UpdateFace(cp, CubePiece.Face.NegX, 2, x, y, z);
            if (y > 0) UpdateFace(cp, CubePiece.Face.PosY, 4, x, y, z); else UpdateFace(cp, CubePiece.Face.NegY, 5, x, y, z);
            if (z > 0) UpdateFace(cp, CubePiece.Face.PosZ, 0, x, y, z); else UpdateFace(cp, CubePiece.Face.NegZ, 1, x, y, z);
        }
    }

    private void UpdateFace(CubePiece cp, CubePiece.Face face, int fIdx, int x, int y, int z)
    {
        int tileIdx = GetTileIndexForFace(fIdx, x, y, z);
        Material m = cubeState.tiles[tileIdx] switch
        {
            CubeState.TileColor.Red => meatMat,
            CubeState.TileColor.Blue => spaceMat,
            CubeState.TileColor.Orange => ruinsMat,
            CubeState.TileColor.Yellow => dreamMat,
            CubeState.TileColor.White => clinicMat,
            CubeState.TileColor.Green => forestMat,
            _ => meatMat
        };
        cp.SetSticker(face, cubeState.tiles[tileIdx], m);
    }

    public void SyncStateFromPieces()
    {
        if (cubeState == null || pieces == null) return;
        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            CubePiece cp = piece.GetComponent<CubePiece>();
            Vector3 local = transform.InverseTransformPoint(piece.transform.position);
            int xS = Sign(local.x); int yS = Sign(local.y); int zS = Sign(local.z);

            if (xS > 0) cubeState.tiles[GetTileIndexForFace(3, xS, yS, zS)] = cp.GetStickerFacingWorld(transform.right);
            else cubeState.tiles[GetTileIndexForFace(2, xS, yS, zS)] = cp.GetStickerFacingWorld(-transform.right);
            if (yS > 0) cubeState.tiles[GetTileIndexForFace(4, xS, yS, zS)] = cp.GetStickerFacingWorld(transform.up);
            else cubeState.tiles[GetTileIndexForFace(5, xS, yS, zS)] = cp.GetStickerFacingWorld(-transform.up);
            if (zS > 0) cubeState.tiles[GetTileIndexForFace(0, xS, yS, zS)] = cp.GetStickerFacingWorld(transform.forward);
            else cubeState.tiles[GetTileIndexForFace(1, xS, yS, zS)] = cp.GetStickerFacingWorld(-transform.forward);
        }
    }

    private int GetTileIndexForFace(int f, int x, int y, int z)
    {
        bool t = (f == 4) ? z < 0 : (f == 5) ? z > 0 : y > 0;
        bool r = (f == 0) ? x > 0 : (f == 1) ? x < 0 : (f == 2) ? z > 0 : (f == 3) ? z < 0 : x > 0;
        return (f * 4) + (t ? 0 : 2) + (r ? 1 : 0);
    }
    private int Sign(float v) => v >= 0f ? 1 : -1;
}