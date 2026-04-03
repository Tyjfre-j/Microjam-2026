using UnityEngine;
using UnityEngine.InputSystem;

public class CubeRotationButtonTrigger : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CubeRotationButtonSystem buttonSystem;
    [SerializeField] private CubeRotationButtonSystem.ButtonAction action;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Key interactKey = Key.E;
    // --- ADDED ---
    [SerializeField] private bool useButtonColliderBounds = true;
    [SerializeField] private float proximityRadius = 1.2f;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;

    private Transform _playerTransform;
    private bool _playerNearby;
    private GUIStyle _promptStyle;
    // --- ADDED ---
    private Collider _buttonCollider;

    private void Awake()
    {
        if (buttonSystem == null)
            buttonSystem = GetComponentInParent<CubeRotationButtonSystem>();

        // --- ADDED ---
        _buttonCollider = GetComponent<Collider>();

        // Find player once
        GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null) _playerTransform = playerGO.transform;
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        // --- CHANGED ---
        // Proximity check — use the button's own collider space when available
        if (useButtonColliderBounds && _buttonCollider != null)
        {
            Vector3 closest = _buttonCollider.ClosestPoint(_playerTransform.position);
            _playerNearby = (closest - _playerTransform.position).sqrMagnitude <= 0.0001f;
        }
        else
        {
            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            _playerNearby = dist <= proximityRadius;
        }

        if (_playerNearby && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            buttonSystem?.TriggerAction(action);
        }
    }

    private void OnGUI()
    {
        if (!showPrompt || !_playerNearby) return;

        // Build style once
        if (_promptStyle == null)
        {
            _promptStyle = new GUIStyle(GUI.skin.box);
            _promptStyle.fontSize = 18;
            _promptStyle.normal.textColor = Color.white;
            _promptStyle.alignment = TextAnchor.MiddleCenter;
        }

        // Project button world pos to screen
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z < 0) return; // behind camera

        // Flip Y for GUI coords
        float guiY = Screen.height - screenPos.y - 40f;
        GUI.Box(new Rect(screenPos.x - 40f, guiY, 80f, 28f),
                $"[{interactKey}] {GetLabel()}", _promptStyle);
    }

    // ── Public API ───────────────────────────────────────────────
    public void SetAction(CubeRotationButtonSystem.ButtonAction newAction)
    {
        action = newAction;
    }

    // Kept for compatibility — no longer needed but won't break anything
    public void SetInteractAction(UnityEngine.InputSystem.InputActionReference actionRef) { }

    private string GetLabel()
    {
        switch (action)
        {
            case CubeRotationButtonSystem.ButtonAction.RightColumnUp:   return "R↑";
            case CubeRotationButtonSystem.ButtonAction.BottomLineLeft:  return "←Bot";
            case CubeRotationButtonSystem.ButtonAction.LeftColumnUp:    return "L↑";
            case CubeRotationButtonSystem.ButtonAction.BottomLineRight: return "Bot→";
            case CubeRotationButtonSystem.ButtonAction.RightColumnDown: return "R↓";
            case CubeRotationButtonSystem.ButtonAction.TopLineLeft:     return "←Top";
            case CubeRotationButtonSystem.ButtonAction.LeftColumnDown:  return "L↓";
            case CubeRotationButtonSystem.ButtonAction.TopLineRight:    return "Top→";
            default: return "?";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
}
