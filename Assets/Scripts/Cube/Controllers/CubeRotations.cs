using System.Collections.Generic;
using UnityEngine;

public class CubeRotations : MonoBehaviour
{
    private enum Axis
    {
        X,
        Y,
        Z
    }

    private static readonly Vector3Int[] FaceNormals =
    {
        new Vector3Int(0, 0, 1),   // Front
        new Vector3Int(0, 0, -1),  // Back
        new Vector3Int(-1, 0, 0),  // Left
        new Vector3Int(1, 0, 0),   // Right
        new Vector3Int(0, 1, 0),   // Top
        new Vector3Int(0, -1, 0),  // Bottom
    };

    private static readonly Vector3Int[] FaceRight =
    {
        new Vector3Int(1, 0, 0),   // Front
        new Vector3Int(-1, 0, 0),  // Back
        new Vector3Int(0, 0, 1),   // Left
        new Vector3Int(0, 0, -1),  // Right
        new Vector3Int(1, 0, 0),   // Top
        new Vector3Int(1, 0, 0),   // Bottom
    };

    private static readonly Vector3Int[] FaceUp =
    {
        new Vector3Int(0, 1, 0),   // Front
        new Vector3Int(0, 1, 0),   // Back
        new Vector3Int(0, 1, 0),   // Left
        new Vector3Int(0, 1, 0),   // Right
        new Vector3Int(0, 0, -1),  // Top
        new Vector3Int(0, 0, 1),   // Bottom
    };

    private static readonly Dictionary<Vector3Int, int> NormalToFace = new Dictionary<Vector3Int, int>
    {
        { new Vector3Int(0, 0, 1), 0 },
        { new Vector3Int(0, 0, -1), 1 },
        { new Vector3Int(-1, 0, 0), 2 },
        { new Vector3Int(1, 0, 0), 3 },
        { new Vector3Int(0, 1, 0), 4 },
        { new Vector3Int(0, -1, 0), 5 },
    };

    private CubeState cubeState;

    private void Awake()
    {
        cubeState = GetComponent<CubeState>();
        // Error log removed per project request.
    }

    /// <summary>Rotate the top layer clockwise (U).</summary>
    public void RotateU()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Y, 1, -1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate U")]
    private void TestRotateU()
    {
        RotateU();
    }

    /// <summary>Rotate the top layer counterclockwise (U').</summary>
    public void RotateUPrime()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Y, 1, 1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate U'")]
    private void TestRotateUPrime()
    {
        RotateUPrime();
    }

    /// <summary>Rotate the bottom layer clockwise (D).</summary>
    public void RotateD()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Y, -1, 1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate D")]
    private void TestRotateD()
    {
        RotateD();
    }

    /// <summary>Rotate the bottom layer counterclockwise (D').</summary>
    public void RotateDPrime()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Y, -1, -1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate D'")]
    private void TestRotateDPrime()
    {
        RotateDPrime();
    }

    /// <summary>Rotate the right layer clockwise (R).</summary>
    public void RotateR()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.X, 1, -1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate R")]
    private void TestRotateR()
    {
        RotateR();
    }

    /// <summary>Rotate the right layer counterclockwise (R').</summary>
    public void RotateRPrime()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.X, 1, 1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate R'")]
    private void TestRotateRPrime()
    {
        RotateRPrime();
    }

    /// <summary>Rotate the left layer clockwise (L).</summary>
    public void RotateL()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.X, -1, 1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate L")]
    private void TestRotateL()
    {
        RotateL();
    }

    /// <summary>Rotate the left layer counterclockwise (L').</summary>
    public void RotateLPrime()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.X, -1, -1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate L'")]
    private void TestRotateLPrime()
    {
        RotateLPrime();
    }

    /// <summary>Rotate the front layer clockwise (F).</summary>
    public void RotateF()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Z, 1, -1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate F")]
    private void TestRotateF()
    {
        RotateF();
    }

    /// <summary>Rotate the front layer counterclockwise (F').</summary>
    public void RotateFPrime()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Z, 1, 1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate F'")]
    private void TestRotateFPrime()
    {
        RotateFPrime();
    }

    /// <summary>Rotate the back layer clockwise (B).</summary>
    public void RotateB()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Z, -1, 1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate B")]
    private void TestRotateB()
    {
        RotateB();
    }

    /// <summary>Rotate the back layer counterclockwise (B').</summary>
    public void RotateBPrime()
    {
        if (cubeState == null) { return; }
        RotateLayer(Axis.Z, -1, -1);
        cubeState.DebugPrintState();
    }

    [ContextMenu("Test/Rotate B'")]
    private void TestRotateBPrime()
    {
        RotateBPrime();
    }

    private void RotateLayer(Axis axis, int layerCoord, int direction)
    {
        if (cubeState == null || cubeState.tiles == null || cubeState.tiles.Length != 24)
        {
            return;
        }

        CubeState.TileColor[] newTiles = (CubeState.TileColor[])cubeState.tiles.Clone();

        for (int i = 0; i < cubeState.tiles.Length; i++)
        {
            int face = i / 4;
            int tile = i % 4;
            int uSign = (tile == 0 || tile == 2) ? -1 : 1;
            int vSign = (tile == 0 || tile == 1) ? 1 : -1;

            Vector3Int n = FaceNormals[face];
            Vector3Int right = FaceRight[face];
            Vector3Int up = FaceUp[face];
            Vector3Int cubeletPos = n + (uSign * right) + (vSign * up);

            int axisValue = axis == Axis.X ? cubeletPos.x : axis == Axis.Y ? cubeletPos.y : cubeletPos.z;
            if (axisValue != layerCoord)
            {
                continue;
            }

            Vector3Int nRot = RotateVector(n, axis, direction);
            Vector3Int rightRot = RotateVector(right, axis, direction);
            Vector3Int upRot = RotateVector(up, axis, direction);
            Vector3Int posRot = nRot + (uSign * rightRot) + (vSign * upRot);

            int newFace = NormalToFace[nRot];
            Vector3Int faceRight = FaceRight[newFace];
            Vector3Int faceUp = FaceUp[newFace];

            int newUSign = Sign(Dot(posRot, faceRight));
            int newVSign = Sign(Dot(posRot, faceUp));
            int newIndex = GetTileIndex(newFace, newUSign, newVSign);

            newTiles[newIndex] = cubeState.tiles[i];
        }

        cubeState.tiles = newTiles;
    }

    private static Vector3Int RotateVector(Vector3Int v, Axis axis, int direction)
    {
        if (direction > 0)
        {
            return axis switch
            {
                Axis.X => new Vector3Int(v.x, -v.z, v.y),
                Axis.Y => new Vector3Int(v.z, v.y, -v.x),
                Axis.Z => new Vector3Int(-v.y, v.x, v.z),
                _ => v,
            };
        }

        return axis switch
        {
            Axis.X => new Vector3Int(v.x, v.z, -v.y),
            Axis.Y => new Vector3Int(-v.z, v.y, v.x),
            Axis.Z => new Vector3Int(v.y, -v.x, v.z),
            _ => v,
        };
    }

    private static int GetTileIndex(int face, int uSign, int vSign)
    {
        int tile = (vSign > 0 ? 0 : 2) + (uSign > 0 ? 1 : 0);
        return (face * 4) + tile;
    }

    private static int Dot(Vector3Int a, Vector3Int b)
    {
        return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
    }

    private static int Sign(int value)
    {
        return value >= 0 ? 1 : -1;
    }
}
