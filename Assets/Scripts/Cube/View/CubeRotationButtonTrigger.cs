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
    [SerializeField] private bool showDebugLogs = false;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;

    private Transform _playerTransform;
    private bool _playerNearby;
    private GUIStyle _promptStyle;
    // --- ADDED ---
    private Collider _buttonCollider;
    private bool _loggedMissingPlayer;
    private bool _loggedMissingSystem;
    private float _nextResolveTime;
    private Camera _cam;
    private string _cachedPrompt;

    private void Awake()
    {
        if (buttonSystem == null)
            buttonSystem = GetComponentInParent<CubeRotationButtonSystem>();

        // --- ADDED ---
        _buttonCollider = GetComponent<Collider>();

        ResolvePlayerTransform();
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            // Throttle expensive scene searches to ~4x per second.
            if (Time.unscaledTime < _nextResolveTime) return;
            _nextResolveTime = Time.unscaledTime + 0.25f;
            ResolvePlayerTransform();
            if (_playerTransform == null)
            {
                if (showDebugLogs && !_loggedMissingPlayer)
                {
                    Debug.LogWarning($"[CubeRotationButtonTrigger] No player found for '{name}' (tag='{playerTag}').");
                    _loggedMissingPlayer = true;
                }
                return;
            }

            _loggedMissingPlayer = false;
        }

        if (buttonSystem == null)
        {
            if (showDebugLogs && !_loggedMissingSystem)
            {
                Debug.LogWarning($"[CubeRotationButtonTrigger] No CubeRotationButtonSystem linked for '{name}'.");
                _loggedMissingSystem = true;
            }
            return;
        }

        _loggedMissingSystem = false;

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
            if (showDebugLogs)
            {
                Debug.Log($"[CubeRotationButtonTrigger] Interaction on '{name}' -> action '{action}'.");
            }
            buttonSystem.TriggerAction(action);
        }
    }

    private void OnGUI()
    {
        // The polished HUD draws the prompt when present; this IMGUI box is only a fallback.
        if (FaceTeleportUI.Exists) return;
        if (!showPrompt || !_playerNearby || !isActiveAndEnabled) return;

        // Build style once
        if (_promptStyle == null)
        {
            _promptStyle = new GUIStyle(GUI.skin.box);
            _promptStyle.fontSize = 18;
            _promptStyle.normal.textColor = Color.white;
            _promptStyle.alignment = TextAnchor.MiddleCenter;
        }

        // Project button world pos to screen
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;
        Vector3 screenPos = _cam.WorldToScreenPoint(transform.position);
        if (screenPos.z < 0) return; // behind camera

        if (_cachedPrompt == null) _cachedPrompt = $"[{interactKey}] {GetLabel()}";

        // Flip Y for GUI coords
        float guiY = Screen.height - screenPos.y - 40f;
        GUI.Box(new Rect(screenPos.x - 40f, guiY, 80f, 28f), _cachedPrompt, _promptStyle);
    }

    // ── Public API ───────────────────────────────────────────────
    public bool IsPlayerNearby => _playerNearby && isActiveAndEnabled;
    public CubeRotationButtonSystem.ButtonAction Action => action;
    public Key InteractKey => interactKey;

    /// <summary>All enabled triggers (lets the HUD find the one the player stands on without scene searches).</summary>
    public static readonly System.Collections.Generic.List<CubeRotationButtonTrigger> Active =
        new System.Collections.Generic.List<CubeRotationButtonTrigger>(16);

    private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    private void OnDisable() { Active.Remove(this); _playerNearby = false; }

    /// <summary>Human readable description used by the HUD prompt.</summary>
    public string Describe()
    {
        switch (action)
        {
            case CubeRotationButtonSystem.ButtonAction.RightColumnUp:   return "Right column  UP";
            case CubeRotationButtonSystem.ButtonAction.RightColumnDown: return "Right column  DOWN";
            case CubeRotationButtonSystem.ButtonAction.LeftColumnUp:    return "Left column  UP";
            case CubeRotationButtonSystem.ButtonAction.LeftColumnDown:  return "Left column  DOWN";
            case CubeRotationButtonSystem.ButtonAction.TopLineLeft:     return "Top row  LEFT";
            case CubeRotationButtonSystem.ButtonAction.TopLineRight:    return "Top row  RIGHT";
            case CubeRotationButtonSystem.ButtonAction.BottomLineLeft:  return "Bottom row  LEFT";
            default:                                                    return "Bottom row  RIGHT";
        }
    }

    public bool IsColumn =>
        action == CubeRotationButtonSystem.ButtonAction.RightColumnUp || action == CubeRotationButtonSystem.ButtonAction.RightColumnDown ||
        action == CubeRotationButtonSystem.ButtonAction.LeftColumnUp  || action == CubeRotationButtonSystem.ButtonAction.LeftColumnDown;

    public void SetAction(CubeRotationButtonSystem.ButtonAction newAction)
    {
        action = newAction;
        _cachedPrompt = null;
    }

    public void SetButtonSystem(CubeRotationButtonSystem system)
    {
        buttonSystem = system;
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

    private void ResolvePlayerTransform()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null)
        {
            _playerTransform = playerGO.transform;
            return;
        }

        PlayerController player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }
}
