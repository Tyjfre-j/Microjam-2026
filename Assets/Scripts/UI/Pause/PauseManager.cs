using UnityEngine;
using UnityEngine.InputSystem; // Using New Input System
using UnityEngine.SceneManagement;

public class QuickMenuManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject menuPanel; // Drag your Canvas or Main Panel here

    [Header("Scenes")]
    [SerializeField] private int mainMenuBuildIndex = 0;

    public static bool IsPaused { get; private set; }

    private bool isPaused = false;
    private PlayerController player;

    void Start()
    {
        // Start with menu hidden
        menuPanel.SetActive(false);
        IsPaused = false;
        player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    void OnDestroy()
    {
        if (isPaused) { Time.timeScale = 1f; AudioListener.pause = false; }
        IsPaused = false;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Toggle with Escape (ESC closes the face-teleport menu first if it's open)
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !FaceTeleportUI.IsOpen)
        {
            if (isPaused) Resume();
            else OpenMenu();
        }
    }

    public void OpenMenu()
    {
        isPaused = true;
        IsPaused = true;
        menuPanel.SetActive(true);
        Time.timeScale = 0f; // Freeze the cube and player movement
        AudioListener.pause = true;
        if (player != null && player.isActiveAndEnabled) player.SetInputEnabled(false);

        // Unlock mouse to click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        IsPaused = false;
        menuPanel.SetActive(false);
        Time.timeScale = 1f; // Back to normal speed
        AudioListener.pause = false;
        if (player != null && player.isActiveAndEnabled) player.SetInputEnabled(true);
        // The game is played with the mouse free (HUD buttons), so the cursor is no longer locked here.
    }

    public void RestartLevel()
    {
        ResetTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Button hooks used by the pause panel (RESTART / MAINMENU)
    public void Restart() => RestartLevel();

    public void QuitToMenu()
    {
        ResetTime();
        SceneManager.LoadScene(mainMenuBuildIndex);
    }

    public void QuitGame()
    {
        ResetTime();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResetTime()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
    }
}
