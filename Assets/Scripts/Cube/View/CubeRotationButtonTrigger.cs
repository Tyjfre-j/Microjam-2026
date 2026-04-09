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
<<<<<<< HEAD
=======
    [SerializeField] private bool showDebugLogs = false;
>>>>>>> origin/dev

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;

    private Transform _playerTransform;
    private bool _playerNearby;
    private GUIStyle _promptStyle;
    // --- ADDED ---
    private Collider _buttonCollider;
<<<<<<< HEAD
=======
    private bool _loggedMissingPlayer;
    private bool _loggedMissingSystem;
>>>>>>> origin/dev

    private void Awake()
    {
        if (buttonSystem == null)
            buttonSystem = GetComponentInParent<CubeRotationButtonSystem>();

        // --- ADDED ---
        _buttonCollider = GetComponent<Collider>();

<<<<<<< HEAD
        // Find player once
        GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null) _playerTransform = playerGO.transform;
=======
        ResolvePlayerTransform();
>>>>>>> origin/dev
    }

    private void Update()
    {
<<<<<<< HEAD
        if (_playerTransform == null) return;
=======
        if (_playerTransform == null)
        {
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
>>>>>>> origin/dev

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
<<<<<<< HEAD
            buttonSystem?.TriggerAction(action);
=======
            if (showDebugLogs)
            {
                Debug.Log($"[CubeRotationButtonTrigger] Interaction on '{name}' -> action '{action}'.");
            }
            buttonSystem.TriggerAction(action);
>>>>>>> origin/dev
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

<<<<<<< HEAD
=======
    public void SetButtonSystem(CubeRotationButtonSystem system)
    {
        buttonSystem = system;
    }

>>>>>>> origin/dev
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
<<<<<<< HEAD
=======

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
>>>>>>> origin/dev
}
