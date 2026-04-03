using UnityEngine;
using UnityEngine.InputSystem;

public class CubeRotationButtonTrigger : MonoBehaviour
{
    [SerializeField] private CubeRotationButtonSystem buttonSystem;
    [SerializeField] private CubeRotationButtonSystem.ButtonAction action;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private InputActionReference interactAction;

    private bool isPlayerInside;

    private void Awake()
    {
        if (buttonSystem == null)
        {
            buttonSystem = GetComponentInParent<CubeRotationButtonSystem>();
        }
    }

    private void OnEnable()
    {
        if (interactAction != null) { interactAction.action.Enable(); }
    }

    private void OnDisable()
    {
        if (interactAction != null) { interactAction.action.Disable(); }
    }

    private void Update()
    {
        if (!isPlayerInside) { return; }
        if (interactAction == null) { return; }
        if (interactAction.action.WasPressedThisFrame())
        {
            buttonSystem?.TriggerAction(action);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) { return; }
        isPlayerInside = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) { return; }
        isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) { return; }
        isPlayerInside = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) { return; }
        isPlayerInside = false;
    }

    private bool IsPlayer(Component other)
    {
        if (other == null) { return false; }
        return other.CompareTag(playerTag);
    }

    /// <summary>Assign the action this trigger should execute.</summary>
    public void SetAction(CubeRotationButtonSystem.ButtonAction newAction)
    {
        action = newAction;
    }

    /// <summary>Assign the input action used to interact with this button.</summary>
    public void SetInteractAction(InputActionReference actionRef)
    {
        interactAction = actionRef;
    }
}
