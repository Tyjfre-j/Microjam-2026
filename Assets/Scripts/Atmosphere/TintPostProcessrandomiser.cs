using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Use high-definition for HDRP

public class NeonBloomController : MonoBehaviour
{
    public Volume volume;
    [Tooltip("Define the 'top light colors' here (e.g., bright cyans, pinks, yellows)")]
    public Gradient neonGradient;
    public float changeSpeed = 0.5f;

    private Bloom bloom;

    void Start()
    {
        // Try to find the Bloom override in the profile
        if (volume.profile.TryGet<Bloom>(out bloom))
        {
            bloom.tint.overrideState = true;
        }
        else
        {
            Debug.LogError("No Bloom override found on the Volume Profile!");
        }
    }

    void Update()
    {
        if (bloom != null)
        {
            // Calculate a value between 0 and 1 based on time
            float t = Mathf.PingPong(Time.time * changeSpeed, 1f);
            
            // Apply the color from your gradient to the Bloom tint
            bloom.tint.value = neonGradient.Evaluate(t);
        }
    }
}
