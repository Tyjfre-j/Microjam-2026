using UnityEngine;
using UnityEngine.Events;

public class FaceManager : MonoBehaviour
{
    public static FaceManager Instance { get; private set; }

    public enum Direction
    {
        Left,
        Right,
        Up,
        Down
    }

    [Header("State")]
    [SerializeField] private int currentFace = 0;

    [Header("Events")]
    public UnityEvent<int> OnFaceChanged = new UnityEvent<int>();

    private int[,] adjacency;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        BuildAdjacency();
    }

    /// <summary>Return the current active face index.</summary>
    public int GetCurrentFace()
    {
        return currentFace;
    }

    /// <summary>Get the adjacent face for a given face and direction.</summary>
    public int GetAdjacentFace(int face, Direction dir)
    {
        if (adjacency == null)
        {
            BuildAdjacency();
        }

        int dirIndex = (int)dir;
        return adjacency[face, dirIndex];
    }

    /// <summary>Set the current face and fire the OnFaceChanged event.</summary>
    public void SetCurrentFace(int faceIndex)
    {
        if (currentFace == faceIndex) { return; }
        currentFace = faceIndex;
        OnFaceChanged?.Invoke(currentFace);
    }

    private void BuildAdjacency()
    {
        // Face order: 0=Front 1=Back 2=Left 3=Right 4=Top 5=Bottom
        // Direction order: Left=0 Right=1 Up=2 Down=3
        adjacency = new int[6, 4];

        // Front
        adjacency[0, (int)Direction.Left] = 2;
        adjacency[0, (int)Direction.Right] = 3;
        adjacency[0, (int)Direction.Up] = 4;
        adjacency[0, (int)Direction.Down] = 5;

        // Back
        adjacency[1, (int)Direction.Left] = 3;
        adjacency[1, (int)Direction.Right] = 2;
        adjacency[1, (int)Direction.Up] = 4;
        adjacency[1, (int)Direction.Down] = 5;

        // Left
        adjacency[2, (int)Direction.Left] = 1;
        adjacency[2, (int)Direction.Right] = 0;
        adjacency[2, (int)Direction.Up] = 4;
        adjacency[2, (int)Direction.Down] = 5;

        // Right
        adjacency[3, (int)Direction.Left] = 0;
        adjacency[3, (int)Direction.Right] = 1;
        adjacency[3, (int)Direction.Up] = 4;
        adjacency[3, (int)Direction.Down] = 5;

        // Top
        adjacency[4, (int)Direction.Left] = 2;
        adjacency[4, (int)Direction.Right] = 3;
        adjacency[4, (int)Direction.Up] = 1;
        adjacency[4, (int)Direction.Down] = 0;

        // Bottom
        adjacency[5, (int)Direction.Left] = 2;
        adjacency[5, (int)Direction.Right] = 3;
        adjacency[5, (int)Direction.Up] = 0;
        adjacency[5, (int)Direction.Down] = 1;
    }
}
