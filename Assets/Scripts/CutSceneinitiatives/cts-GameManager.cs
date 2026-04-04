using UnityEngine;
using System.Collections;

public class AnimationSequenceHandler : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public GameObject cameraToEnable;
    public Light lightToChange;

    [Header("Settings")]
    public Color targetColor = Color.red;
    public string animationStateName; // The name of the animation clip
    public float switchInterval = 0.5f; // How fast it switches colors
    private Color initialColor;
    int moves;

    void Start()
    {
        // Optional: Start the sequence automatically
        StartCoroutine(ExecuteSequence());
    }

    IEnumerator ExecuteSequence()
    {
        // 1. Wait for the animation to finish
        // We yield until the current state is no longer playing or has finished its loop
        if (animator != null)
        {
            // Wait a frame to ensure the animator has started
            yield return null; 
            
            float duration = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(duration);
        }

        // 2. Enable the Camera
        if (cameraToEnable != null)
        {
            cameraToEnable.SetActive(true);
            Debug.Log("Camera Enabled");
        }
        yield return new WaitForSeconds(1f);

        // 3. Change Light Color (Triggered by camera enablement)
        if (lightToChange != null)
        {
        bool isTargetColor = false;
        int lightswitch =0;
            
            while ((true) && (lightswitch<10)) // This will run forever
            {
                // Toggle the color
                lightToChange.color = isTargetColor ? initialColor : targetColor;
                
                // Flip the boolean for the next loop
                isTargetColor = !isTargetColor;

                // Wait before switching again
                yield return new WaitForSeconds(switchInterval);
                lightswitch = lightswitch + 1;
            }
        }
    }
}