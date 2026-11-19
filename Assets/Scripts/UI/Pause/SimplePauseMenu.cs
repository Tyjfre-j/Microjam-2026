using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePauseMenu : MonoBehaviour
{
    public GameObject menuPanel; // Drag your Black background here
    private bool isPaused = false;

    void Update()
    {
        // Toggle with Escape
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        menuPanel.SetActive(true);
        Time.timeScale = 0f; // This freezes the cube and player
        // Optional: Change camera focus or add blur here
    }

    public void Resume()
    {
        isPaused = false;
        menuPanel.SetActive(false);
        Time.timeScale = 1f; // This resumes physics
    }

    public void Quit()
    {
        Application.Quit();
    }
}