using UnityEngine;
using UnityEngine.Events;

public class ObjectButton2 : MonoBehaviour
{
    // UnityEvents allow you to drag and drop functions in the Inspector, 
    // just like a UI Button.
    public UnityEvent onClick;

    [SerializeField] private Color hoverColor = Color.gray;
    private Color originalColor;
    private Renderer objRenderer;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null) originalColor = objRenderer.material.color;
    }

    // Triggered when the mouse enters the collider
    void OnMouseEnter()
    {
        if (objRenderer == null) return;
        objRenderer.material.color = hoverColor;
    }

    // Triggered when the mouse leaves the collider
    void OnMouseExit()
    {
        if (objRenderer == null) return;
        objRenderer.material.color = originalColor;
    }

    // Triggered when the mouse clicks the collider
    void OnMouseDown()
    {
        // This invokes whatever function you link in the Inspector
            if (onClick != null)
        {
            onClick.Invoke();
        }
    }

    public void exit()
    {
        Application.Quit();
    }
}
