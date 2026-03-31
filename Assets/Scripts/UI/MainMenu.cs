using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private int gameSceneBuildIndex = 1;

    [Header("Background")]
    [SerializeField] private Transform rotatingCube;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 20f, 0f);

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private void Update()
    {
        if (rotatingCube != null)
        {
            rotatingCube.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>Load the game scene.</summary>
    public void Play()
    {
        if (gameSceneBuildIndex < 0 || gameSceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Log("Game scene build index is invalid.");
            return;
        }

        SceneManager.LoadScene(gameSceneBuildIndex);
    }

    /// <summary>Quit the application.</summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Log(string msg)
    {
        if (showDebugLogs)
        {
            // Log removed per project request.
        }
    }
}
