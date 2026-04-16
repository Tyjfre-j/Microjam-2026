using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RotationAnimatorInput : MonoBehaviour
{
    [SerializeField] private RotationAnimator rotationAnimator;
    // --- ADDED ---
    [SerializeField] private FaceChildrenRemover faceChildrenRemover;
    // --- ADDED ---
    [Header("Input Lock")]
    [SerializeField] private bool disableInputDuringRotation = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugOverlay = true;
    [SerializeField] private bool showDebugLogs = false;

    private Text debugText;
    // --- ADDED ---
    private bool isInputEnabled = true;

    private void Awake()
    {
        if (rotationAnimator == null)
        {
            rotationAnimator = GetComponent<RotationAnimator>();
        }
        if (faceChildrenRemover == null)
        {
            faceChildrenRemover = GetComponent<FaceChildrenRemover>();
        }

        if (showDebugOverlay)
        {
            CreateDebugOverlay();
        }
    }

    // --- ADDED ---
    private void OnEnable()
    {
        if (rotationAnimator != null && disableInputDuringRotation)
        {
            rotationAnimator.OnRotationStart += HandleRotationStart;
            rotationAnimator.OnRotationComplete += HandleRotationComplete;
        }
        if (faceChildrenRemover != null && disableInputDuringRotation)
        {
            faceChildrenRemover.OnPlatformsRespawned += HandlePlatformsRespawned;
        }
    }

    // --- ADDED ---
    private void OnDisable()
    {
        if (rotationAnimator != null && disableInputDuringRotation)
        {
            rotationAnimator.OnRotationStart -= HandleRotationStart;
            rotationAnimator.OnRotationComplete -= HandleRotationComplete;
        }
        if (faceChildrenRemover != null && disableInputDuringRotation)
        {
            faceChildrenRemover.OnPlatformsRespawned -= HandlePlatformsRespawned;
        }
    }

    private void Update()
    {
        if (!isInputEnabled) { return; }
        if (rotationAnimator == null || rotationAnimator.isAnimating) { return; }

        // Removed keyboard input handling to allow WASD for walking
        // Cube rotation now only via buttons

        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        if (debugText == null) { return; }
        string status = isInputEnabled ? "Input: Enabled" : "Input: Disabled";
        debugText.text = $"Rotation Input\n{status}\nInput via Buttons Only";
    }

    private void CreateDebugOverlay()
    {
        GameObject canvasGO = new GameObject("RotationInputDebugCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("RotationInputDebugText");
        textGO.transform.SetParent(canvasGO.transform, false);
        debugText = textGO.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.alignment = TextAnchor.UpperLeft;

        RectTransform rt = debugText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);
        rt.sizeDelta = new Vector2(320f, 80f);
    }

    // --- ADDED ---
    private void HandleRotationStart()
    {
        isInputEnabled = false;
    }

    private void HandleRotationComplete()
    {
        if (!disableInputDuringRotation)
        {
            return;
        }

        if (faceChildrenRemover == null)
        {
            isInputEnabled = true;
            return;
        }

        if (faceChildrenRemover.StartupOnlyRebuild && faceChildrenRemover.StartupBuildCompleted)
        {
            isInputEnabled = true;
        }
    }

    // --- ADDED ---
    // --- CHANGED ---
    private void HandlePlatformsRespawned()
    {
        isInputEnabled = true;
    }

    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }
}
