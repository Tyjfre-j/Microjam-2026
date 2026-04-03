using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class PivotCubeController : MonoBehaviour
{
    public Transform pivot;        // Drag your "CubeCenter" Empty GameObject here
    [SerializeField] private CubeManager cubeManager;
    public float rotationSpeed = 400f; 
    private bool isRotating = false;
    public bool IsRotating => isRotating;

 // Change these at the top of your class:
public List<GameObject> activeLayer = new List<GameObject>(); // Changed to public
public string currentLayerName; // Add this line

    void Update()
    {
        // Safety check: Make sure you assigned the pivot in the Inspector
        if (pivot == null || isRotating) return;

        if (cubeManager == null) cubeManager = GetComponentInParent<CubeManager>();
        if (cubeManager == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame) RotateLayer("front");
        if (Keyboard.current.kKey.wasPressedThisFrame) RotateLayer("back");

        if (Keyboard.current.tKey.wasPressedThisFrame) RotateLayer("top");
        if (Keyboard.current.bKey.wasPressedThisFrame) RotateLayer("bottom");

        if (Keyboard.current.rKey.wasPressedThisFrame) RotateLayer("right");
        if (Keyboard.current.lKey.wasPressedThisFrame) RotateLayer("left");
    }

    public void RotateLayer(string layerName)
    {
        if (cubeManager == null) return;
        currentLayerName = layerName;
        isRotating = true;
        activeLayer.Clear();

        bool clockwise = true;
        CubeManager.Axis axis;
        int layerIndex;

        switch (layerName)
        {
            case "front":
                axis = CubeManager.Axis.X;
                layerIndex = 0;
                break;
            case "back":
                axis = CubeManager.Axis.X;
                layerIndex = 1;
                break;
            case "top":
                axis = CubeManager.Axis.Y;
                layerIndex = 1;
                break;
            case "bottom":
                axis = CubeManager.Axis.Y;
                layerIndex = 0;
                break;
            case "right":
                axis = CubeManager.Axis.Z;
                layerIndex = 0;
                break;
            case "left":
                axis = CubeManager.Axis.Z;
                layerIndex = 1;
                break;
            default:
                isRotating = false;
                return;
        }

        cubeManager.RotateLayer(axis, layerIndex, clockwise, () => { isRotating = false; });
    }
}
