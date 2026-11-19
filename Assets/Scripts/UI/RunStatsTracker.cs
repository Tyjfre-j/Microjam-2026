using UnityEngine;

/// <summary>Counts play time (excluding pauses) and player-made turns for the win screen.</summary>
public class RunStatsTracker : MonoBehaviour
{
    private RotationAnimator rotationAnimator;
    private PlayerController player;

    private void Awake()
    {
        RunStats.Reset();
        player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        rotationAnimator = FindAnyObjectByType<RotationAnimator>();
        if (rotationAnimator != null) rotationAnimator.OnRotationStart += CountMove;
    }

    private void OnDestroy()
    {
        if (rotationAnimator != null) rotationAnimator.OnRotationStart -= CountMove;
    }

    // Only player-made turns count (the startup shuffle runs while the player is hidden).
    private void CountMove()
    {
        if (player != null && player.gameObject.activeInHierarchy) RunStats.Moves++;
    }

    private void Update()
    {
        if (player != null && player.gameObject.activeInHierarchy) RunStats.Seconds += Time.deltaTime; // timeScale 0 while paused
    }
}
