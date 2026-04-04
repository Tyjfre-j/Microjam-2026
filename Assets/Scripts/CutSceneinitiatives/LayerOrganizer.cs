using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem; 
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PivotCubeControllerZ : MonoBehaviour
{
    public Transform pivot;        // Drag your "CubeCenter" Empty GameObject here
    public string cubeTag = "Cube";
    public float rotationSpeed = 400f; 
    public float maxIntensity = 5f;
    public float duration = 1.0f;
    private bool isRotating = false;
    public bool IsRotating => isRotating;
    public int moves = 20;
    public Vector3 orbitSpeed = new Vector3(0, 5f, 0); 
    public Animator clip1;
    public Animator clip2;
    public Animator clip3;
    public Image actor1;
    public Image actor2;
    public Image actor3;
    public Light myLight;
    public GameObject camera1;
    public GameObject cameraToEnable1;
    public GameObject cameraToEnable2;
    public GameObject mainmenu;
    public GameObject cuby;
    public Light lightToChange;
    public RectTransform imageRectTransform;
    public float durationw = 1.5f;

 // Change these at the top of your class:
    public List<GameObject> activeLayer = new List<GameObject>(); // Changed to public
    public string currentLayerName; // Add this line
    private string[] layerNames = { "front", "back", "top", "bottom", "right", "left" };
    
    [Header("Settings")]
    public Color targetColor = Color.red;
    public string animationStateName; // The name of the animation clip
    public float switchInterval = 0.5f; // How fast it switches colors
    private Color initialColor;





    public void Start()
    {
        //NewGame();
    }







       public void NewGame()
    {
        mainmenu.SetActive(false);
        // Optional: Start the sequence automatically
        StartCoroutine(ExecuteSequence());
    }

   // void Update()
    //{
        // Safety check: Make sure you assigned the pivot in the Inspector
       // if (pivot == null || isRotating) return;

        // Using pivot's local axes (Right, Up, Forward) 
        // This allows the cube to work even if it's tilted!
       // if (Keyboard.current.fKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.right, "front"));
       // if (Keyboard.current.kKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.right, "back"));
        
       // if (Keyboard.current.tKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.up, "top"));
        //if (Keyboard.current.bKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.up, "bottom"));
            
        //if (Keyboard.current.rKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.forward, "right"));
       // if (Keyboard.current.lKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.forward, "left"));

       // if (Keyboard.current.sKey.wasPressedThisFrame) StartCoroutine(ScrambleCube(20));
   // }


    public System.Collections.IEnumerator ExecuteSequence()
    {
        TriggerMove();
        // 1. Wait for the animation to finish
        // We yield until the current state is no longer playing or has finished its loop
        if (clip1 != null)
        {
            // Wait a frame to ensure the animator has started
            yield return null; 
            
            float duration = clip1.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(duration);
        }
        actor1.enabled = false;
        clip1.enabled = false;
        clip2.enabled = true;

        if (clip2 != null)
        {

            // Wait a frame to ensure the animator has started
            yield return null; 
            
            float duration = clip2.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(duration);

        }
        actor2.enabled = false;
        clip2.enabled = false;
        clip3.enabled = true;

                if (cameraToEnable1 != null)
        {
            cameraToEnable1.SetActive(true);
            Debug.Log("Camera Enabled");
        }
        camera1.SetActive(false);

                if (clip3 != null)
        {
            clip2.enabled = true;
            // Wait a frame to ensure the animator has started
            yield return null; 
            
            float duration = clip3.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(duration);
        }
        actor3.enabled = false;





        float elapsed = 0;

        // Phase 1: Fade In
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            myLight.intensity = Mathf.Lerp(0, maxIntensity, elapsed / duration);
            yield return null;
        }

        elapsed = 0; // Reset timer for fade out

        // Phase 2: Fade Out
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            myLight.intensity = Mathf.Lerp(maxIntensity, 0, elapsed / duration);
            yield return null;
        }
        cuby.SetActive(false);

        myLight.intensity = 0; // Ensure it ends exactly at 0

        SceneManager.LoadScene("COULI");
     //mainmenu.SetActive(true) ;

        /*
        // 2. Enable the Camera
        if (cameraToEnable2 != null)
        {
            cameraToEnable2.SetActive(true);
            Debug.Log("Camera Enabled");
        }
        cameraToEnable1.SetActive(false);
        actor3.enabled = false;

        //cuby.StartAnimation();
        StartCoroutine(CubyShaffle());
        cuby.transform.Rotate(orbitSpeed * Time.deltaTime, Space.Self);
        */
    }





          /// <summary>
    /// Picks a random side and rotates it multiple times.
    /// </summary>
    public System.Collections.IEnumerator ScrambleCube(int moves)
    {
        // Increase speed temporarily for the scramble if you want it faster
        float originalSpeed = rotationSpeed;
        rotationSpeed = 800f; 
        float switchInterval = 1f;

        for (int i = 0; i < moves; i++)
        {
            // Pick a random layer name
            string randomLayer = layerNames[Random.Range(0, layerNames.Length)];
            float rotatedAmount = 0;
            
            // Determine the axis based on the chosen layer
            Vector3 axis = Vector3.zero;
            if (randomLayer == "front" || randomLayer == "back") axis = pivot.right;
            else if (randomLayer == "top" || randomLayer == "bottom") axis = pivot.up;
            else if (randomLayer == "right" || randomLayer == "left") axis = pivot.forward;

            // Randomize direction (Clockwise or Counter-clockwise)
            if (Random.value > 0.5f) axis = -axis;

            // Start the rotation and WAIT for it to finish before the next move
            yield return StartCoroutine(RotateSequence(axis, randomLayer));

            if (i == 19)
            {
                mainmenu.SetActive(true); 
            }
            // 4. Optional: Tiny buffer to let the engine settle 
            // (prevents frame-perfect physics glitches if your RotateSequence is complex)
            yield return new WaitForFixedUpdate();
        }

        rotationSpeed = originalSpeed; // Reset speed
    }


    public System.Collections.IEnumerator RotateSequence(Vector3 axis, string layerName)
    {
        currentLayerName = layerName; // <--- ADD THIS LINE
        isRotating = true;
        activeLayer.Clear();

      GameObject[] allCubes = GameObject.FindGameObjectsWithTag(cubeTag) ;
        
        foreach (GameObject cube in allCubes)
        {
            // We check the position RELATIVE to the pivot
            Vector3 relativePos = pivot.InverseTransformPoint(cube.transform.position);
            
            if (layerName == "front" && relativePos.x <= -0.1f) activeLayer.Add(cube);
            else if (layerName == "back" && relativePos.x >= 0.1f) activeLayer.Add(cube);
            else if (layerName == "top" && relativePos.y >= 0.1f) activeLayer.Add(cube);
            else if (layerName == "bottom" && relativePos.y <= -0.1f) activeLayer.Add(cube);
            else if (layerName == "right" && relativePos.z <= -0.1f) activeLayer.Add(cube);
            else if (layerName == "left" && relativePos.z >= 0.1f) activeLayer.Add(cube);
        }

        float rotatedAmount = 0;
        while (rotatedAmount < 90f)
        {
            float step = rotationSpeed * Time.deltaTime;
            if (rotatedAmount + step > 90f) step = 90f - rotatedAmount;

            foreach (GameObject cube in activeLayer)
            {
                // NOW ROTATING AROUND THE PIVOT POSITION
                cube.transform.RotateAround(pivot.position, axis, step);
            }
            rotatedAmount += step;
            yield return null;
        }

        // Snap Logic using the Pivot's local space
        foreach (GameObject cube in activeLayer)
        {
            Vector3 localP = pivot.InverseTransformPoint(cube.transform.position);
            localP = new Vector3(Mathf.Round(localP.x * 2f) / 2f, Mathf.Round(localP.y * 2f) / 2f, Mathf.Round(localP.z * 2f) / 2f);
            cube.transform.position = pivot.TransformPoint(localP);
            
            // Snap rotation to 90 degree increments relative to pivot
            cube.transform.rotation = Quaternion.Euler(
                Mathf.Round(cube.transform.eulerAngles.x / 90) * 90,
                Mathf.Round(cube.transform.eulerAngles.y / 90) * 90,
                Mathf.Round(cube.transform.eulerAngles.z / 90) * 90
            );
        }

    isRotating = false;
    }




        public void TriggerMove()
    {
         // Stop any current move to prevent conflicts
        StartCoroutine(SmoothMove(Vector2.zero));
    }

    public IEnumerator SmoothMove(Vector2 targetPosition)
    {
        Vector2 startPosition = imageRectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < durationw)
        {
            // Calculate progress (0 to 1)
            float t = elapsed / durationw;
            
            // SmoothStep makes the start and end feel more natural (easing)
            t = Mathf.SmoothStep(0f, 1f, t);

            imageRectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it snaps exactly to 0,0,0 at the end
        imageRectTransform.anchoredPosition = targetPosition;
    }




    /*
    public System.Collections.IEnumerator CubyShaffle()
    {
        yield return StartCoroutine(cuby.StartAnimation());
        yield return StartCoroutine(ScrambleCube(moves));
    }
    */

}


