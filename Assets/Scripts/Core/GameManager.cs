using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,
        Playing,
        Won
    }

    [Header("References")]
    [SerializeField] private CubeState cubeState;
    [SerializeField] private RotationAnimator rotationAnimator;
    [SerializeField] private PlayerController playerController;

    [Header("UI")]
    [SerializeField] private GameObject winPanel;

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (cubeState == null) { cubeState = FindAnyObjectByType<CubeState>(); }
        if (rotationAnimator == null) { rotationAnimator = FindAnyObjectByType<RotationAnimator>(); }
        if (playerController == null) { playerController = FindAnyObjectByType<PlayerController>(); }
    }

    private void OnEnable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationComplete += CheckWin;
        }
    }

    private void OnDisable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationComplete -= CheckWin;
        }
    }

    private void Start()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    /// <summary>Check for a solved cube and trigger win state.</summary>
    public void CheckWin()
    {
        if (currentState != GameState.Playing) { return; }
        if (cubeState == null) { return; }

        if (cubeState.IsSolved())
        {
            SetState(GameState.Won);
        }
    }

    /// <summary>Set the current game state.</summary>
    public void SetState(GameState state)
    {
        if (currentState == state) { return; }
        currentState = state;

        if (currentState == GameState.Won)
        {
            if (playerController != null)
            {
                playerController.SetInputEnabled(false);
            }

            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
        }
    }

    /// <summary>Restart the current scene.</summary>
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
