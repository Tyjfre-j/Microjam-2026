using UnityEngine;

using System.Collections;



public class VibrateAndRotate : MonoBehaviour

{

    private Vector3 initialPosition;

    private Quaternion initialRotation;

    private bool isAnimating = false;

       [Header("Rotation Settings")]
    [Tooltip("Degrees per second on each axis")]
    public Vector3 rotationSpeed = new Vector3(0, 5f, 0); 



    void Start()

    {

        // Store the starting state

        initialPosition = transform.position;

        initialRotation = transform.rotation;

    }


/*
public System.Collections.IEnumerator StartAnimation()
{
   if (isAnimating) yield break;

   yield return StartCoroutine(AnimationSequence());
}



public System.Collections.IEnumerator AnimationSequence()

    {

        isAnimating = true;



        // --- PHASE 1: Vibrate ---

        float vibrateDuration = 0.5f;

        float vibrateMagnitude = 0.01f;

        float elapsed = 0f;



        while (elapsed < vibrateDuration)

        {

            float x = Random.Range(-1f, 1f) * vibrateMagnitude;

            float y = Random.Range(-1f, 1f) * vibrateMagnitude;

           

            transform.position = initialPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null;

        }



        // --- PHASE 2: Fast Rotate Z + Position Fluctuation ---

        float actionDuration = 2.0f;

        float rotationSpeed = 100f; // Degrees per second

        float yFluctuationMagnitude = 0.5f;

        float yFluctuationSpeed = 1f;

        elapsed = 0f;



        while (elapsed < actionDuration)

        {

            // Fast Rotation on Z

            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);



            // Z Position Fluctuation (Sine wave)

            float newY = initialPosition.y + Mathf.Sin(Time.time * yFluctuationSpeed) * yFluctuationMagnitude;

            transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);



            elapsed += Time.deltaTime;

            yield return null;

        }



        // --- PHASE 3: Return to Initial State ---

        // We lerp back quickly so it doesn't "snap" instantly

        float returnDuration = 0.3f;

        elapsed = 0f;

        Vector3 currentPos = transform.position;

        Quaternion currentRot = transform.rotation;



        while (elapsed < returnDuration)

        {

            float t = elapsed / returnDuration;

            transform.position = Vector3.Lerp(currentPos, initialPosition, t);
            transform.rotation = Quaternion.Slerp(currentRot, initialRotation, t);

            elapsed += Time.deltaTime;

            yield return null;

        }



        // Ensure exact precision at the end

        transform.position = initialPosition;

        transform.rotation = initialRotation;

       

        isAnimating = false;

    }*/

    
    void Update()
    {
        // Space.Self ensures it rotates around its own center, 
        // not the world's coordinates.
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        Debug.Log("CubeEnabled");
    }



}