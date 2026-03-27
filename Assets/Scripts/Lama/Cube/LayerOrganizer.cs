using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class PivotCubeController : MonoBehaviour
{
    public Transform pivot;        // Drag your "CubeCenter" Empty GameObject here
    public string cubeTag = "Cube";
    public float rotationSpeed = 400f; 
    private bool isRotating = false;

 // Change these at the top of your class:
public List<GameObject> activeLayer = new List<GameObject>(); // Changed to public
public string currentLayerName; // Add this line

    void Update()
    {
        // Safety check: Make sure you assigned the pivot in the Inspector
        if (pivot == null || isRotating) return;

        // Using pivot's local axes (Right, Up, Forward) 
        // This allows the cube to work even if it's tilted!
        if (Keyboard.current.fKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.right, "front"));
        if (Keyboard.current.kKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.right, "back"));
        
        if (Keyboard.current.tKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.up, "top"));
        if (Keyboard.current.bKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.up, "bottom"));
            
        if (Keyboard.current.rKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.forward, "right"));
        if (Keyboard.current.lKey.wasPressedThisFrame) StartCoroutine(RotateSequence(pivot.forward, "left"));
    }

    System.Collections.IEnumerator RotateSequence(Vector3 axis, string layerName)
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
}