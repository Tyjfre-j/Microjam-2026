using UnityEngine;
using UnityEngine.UI; // Required for the Image component

public class NeonImageController : MonoBehaviour
{
    public Image targetImage;
    [Tooltip("Define your 'light' neon colors here")]
    public Gradient neonGradient;
    public float changeSpeed = 0.5f;

    void Start()
    {
        // If you didn't drag the image in, try to find it on this object
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    void Update()
    {
        if (targetImage != null)
        {
            // Calculate a value between 0 and 1 based on time
            float t = Mathf.PingPong(Time.time * changeSpeed, 1f);
            
            // Apply the color from the gradient directly to the Image component
            targetImage.color = neonGradient.Evaluate(t);
        }
    }
}