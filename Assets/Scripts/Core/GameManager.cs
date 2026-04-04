using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private CubeScrambler cubeScrambler;
    [SerializeField] private PlayerController playerController;

    [Header("UI")]
    [SerializeField] private GameObject winPanel;

    [Header("Debug")]
    [SerializeField, Tooltip("Allow forcing win with a keyboard shortcut in Play Mode.")]
    private bool allowDebugWinShortcut = false;
    [SerializeField] private Key debugWinKey = Key.F8;
    [SerializeField] private Key debugResetKey = Key.F9;

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    private bool winChecksEnabled;

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
        if (cubeScrambler == null) { cubeScrambler = FindAnyObjectByType<CubeScrambler>(); }
        if (playerController == null) { playerController = FindAnyObjectByType<PlayerController>(); }
    }

    private void OnEnable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationComplete += CheckWin;
        }

        if (cubeScrambler != null)
        {
            cubeScrambler.OnStartupShuffleCompleted += HandleStartupShuffleCompleted;
        }
    }

    private void OnDisable()
    {
        if (rotationAnimator != null)
        {
            rotationAnimator.OnRotationComplete -= CheckWin;
        }

        if (cubeScrambler != null)
        {
            cubeScrambler.OnStartupShuffleCompleted -= HandleStartupShuffleCompleted;
        }
    }

    private void Start()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        RefreshWinCheckAvailability();
    }

    private void Update()
    {
        if (!allowDebugWinShortcut) { return; }
        if (Keyboard.current == null) { return; }

        if (TryWasPressedThisFrame(ref debugWinKey, Key.F8))
        {
            ForceWinForTesting();
        }
        else if (TryWasPressedThisFrame(ref debugResetKey, Key.F9))
        {
            ResetWinForTesting();
        }
    }

    /// <summary>Check for a solved cube and trigger win state.</summary>
    public void CheckWin()
    {
        if (!winChecksEnabled) { return; }
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
            playerController?.Freeze();

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

    private void HandleStartupShuffleCompleted()
    {
        winChecksEnabled = true;
        if (currentState != GameState.Won)
        {
            SetState(GameState.Playing);
        }
        CheckWin();
    }

    private void RefreshWinCheckAvailability()
    {
        if (cubeScrambler == null)
        {
            winChecksEnabled = true;
            return;
        }

        winChecksEnabled = cubeScrambler.IsStartupShuffleComplete;
    }

    private static bool TryWasPressedThisFrame(ref Key configuredKey, Key fallbackKey)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        try
        {
            return keyboard[configuredKey].wasPressedThisFrame;
        }
        catch (System.ArgumentOutOfRangeException)
        {
            configuredKey = fallbackKey;
            return false;
        }
    }

    [ContextMenu("Debug/Force Win")]
    public void ForceWinForTesting()
    {
        SetState(GameState.Won);
    }

    [ContextMenu("Debug/Reset Win")]
    public void ResetWinForTesting()
    {
        currentState = GameState.Playing;
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        playerController?.Unfreeze();
    }
}
