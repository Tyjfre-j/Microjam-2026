using UnityEngine;

public class CubeState : MonoBehaviour
{
    public enum TileColor
    {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        White
    }

    public TileColor[] tiles = new TileColor[24];

    private const int FaceCount = 6;
    private const int TilesPerFace = 4;

    private void Start()
    {
        InitSolvedState();
        DebugPrintState();
    }

    /// <summary>Initialize the cube to a solved state.</summary>
    public void InitSolvedState()
    {
        if (tiles == null || tiles.Length != FaceCount * TilesPerFace)
        {
            tiles = new TileColor[FaceCount * TilesPerFace];
        }

        for (int face = 0; face < FaceCount; face++)
        {
            TileColor faceColor = (TileColor)face;
            int startIndex = face * TilesPerFace;

            for (int i = 0; i < TilesPerFace; i++)
            {
                tiles[startIndex + i] = faceColor;
            }
        }
    }

    /// <summary>Return true when all faces have matching tiles.</summary>
    public bool IsSolved()
    {
        if (tiles == null || tiles.Length != FaceCount * TilesPerFace)
        {
            return false;
        }

        for (int face = 0; face < FaceCount; face++)
        {
            int startIndex = face * TilesPerFace;
            TileColor faceColor = tiles[startIndex];

            for (int i = 1; i < TilesPerFace; i++)
            {
                if (tiles[startIndex + i] != faceColor)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Log the cube's tiles to the console, face by face.</summary>
    public void DebugPrintState()
    {
        if (tiles == null || tiles.Length != FaceCount * TilesPerFace)
        {
            Debug.LogWarning("[CubeState] Tiles array is missing or has an invalid size.");
            return;
        }

        for (int face = 0; face < FaceCount; face++)
        {
            int startIndex = face * TilesPerFace;
            string faceLabel = ((TileColor)face).ToString();

            string line = string.Format(
                "[CubeState] Face {0} ({1}): {2}, {3}, {4}, {5}",
                face,
                faceLabel,
                tiles[startIndex + 0],
                tiles[startIndex + 1],
                tiles[startIndex + 2],
                tiles[startIndex + 3]
            );

            Debug.Log(line);
        }
    }
}
