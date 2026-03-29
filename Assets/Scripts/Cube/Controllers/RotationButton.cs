using UnityEngine;
using UnityEngine.InputSystem;

public class RotationButton : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private RotationAnimator rotationAnimator;
    [SerializeField] private RotationAnimator.RotationType rotationType = RotationAnimator.RotationType.U;

    [Header("Interaction")]
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private string playerTag = "Player";

    [Header("Highlight")]
    [SerializeField] private Renderer highlightRenderer;
    [SerializeField] private Color highlightColor = new Color(1f, 0.7f, 0.1f);
    [SerializeField] private bool useEmission = true;

    private bool isPlayerInRange;

    /// <summary>Current rotation type for this button.</summary>
    public RotationAnimator.RotationType RotationType
    {
        get => rotationType;
        set => rotationType = value;
    }

    private void Awake()
    {
        if (rotationAnimator == null)
        {
            rotationAnimator = FindAnyObjectByType<RotationAnimator>();
        }
    }

    private void OnEnable()
    {
        if (interactAction != null) { interactAction.action.Enable(); }
        SetHighlight(false);
    }

    private void OnDisable()
    {
        if (interactAction != null) { interactAction.action.Disable(); }
        SetHighlight(false);
    }

    private void Update()
    {
        if (!isPlayerInRange) { return; }
        if (rotationAnimator == null || rotationAnimator.isAnimating) { return; }

        if (interactAction != null && interactAction.action.WasPressedThisFrame())
        {
            rotationAnimator.AnimateAndApplyRotation(rotationType);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) { return; }
        isPlayerInRange = true;
        SetHighlight(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) { return; }
        isPlayerInRange = false;
        SetHighlight(false);
    }

    private void SetHighlight(bool isOn)
    {
        if (highlightRenderer == null) { return; }

        Material mat = highlightRenderer.material;
        Color color = isOn ? highlightColor : Color.black;

        if (mat.HasProperty("_BaseColor")) { mat.SetColor("_BaseColor", isOn ? highlightColor : mat.GetColor("_BaseColor")); }
        if (mat.HasProperty("_Color")) { mat.SetColor("_Color", isOn ? highlightColor : mat.GetColor("_Color")); }

        if (useEmission && mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", isOn ? highlightColor : Color.black);
            if (isOn) { mat.EnableKeyword("_EMISSION"); } else { mat.DisableKeyword("_EMISSION"); }
        }
    }
}
