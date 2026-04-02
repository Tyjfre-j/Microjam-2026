using UnityEngine;
using UnityEngine.InputSystem; // Using New Input System
using UnityEngine.SceneManagement;

public class QuickMenuManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject menuPanel; // Drag your Canvas or Main Panel here

    private bool isPaused = false;

    void Start()
    {
        // Start with menu hidden
        menuPanel.SetActive(false);
    }

    void Update()
    {
        // Toggle with Escape key
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else OpenMenu();
        }
    }

    public void OpenMenu()
    {
        isPaused = true;
        menuPanel.SetActive(true);
        Time.timeScale = 0f; // Freeze the cube and player movement

        // Unlock mouse to click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        menuPanel.SetActive(false);
        Time.timeScale = 1f; // Back to normal speed

        // Lock mouse back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}