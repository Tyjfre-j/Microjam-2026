using UnityEngine;

public class MaterializeManager : MonoBehaviour
{
    public Material transitionMat;
    public float speed = 1.0f;
    
    // Start this very low (e.g., -50) so the objects are invisible at the start
    private float currentVal = 26f; 

    void Start()
    {
        // Ensure it starts invisible
        transitionMat.SetFloat("_AppearAmount", currentVal);
    }

    void Update()
    {
        if (currentVal >-30f) // Adjust 50 based on your scene size
        {
            currentVal -= Time.deltaTime * speed;
            transitionMat.SetFloat("_AppearAmount", currentVal);
        }
    }
}