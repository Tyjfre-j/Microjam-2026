using UnityEngine;

public class TimeAndSpaceDisable : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float timeLimit = 5f; // "X" amount of time
    
    private bool spaceWasPressed = false;
    private float timer;

    void Start()
    {
        // Initialize the timer with your time limit
        timer = timeLimit;
    }

    void Update()
    {
        // 1. Always check for Space press to "arm" the trigger
        if (Input.GetKeyDown(KeyCode.Space))
        {
            spaceWasPressed = true;
        }

        // 2. Always count down from the start of the scene
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else // 3. Once timer hits 0, check if space was ever pressed
        {
            if (spaceWasPressed)
            {
                gameObject.SetActive(false);
            }
        }
    }
}