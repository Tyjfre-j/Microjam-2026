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
    private float lastKeyTime;
    private string lastKeyLabel = "";
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
<<<<<<< HEAD
=======
            rotationAnimator.OnRotationComplete += HandleRotationComplete;
>>>>>>> origin/dev
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
<<<<<<< HEAD
=======
            rotationAnimator.OnRotationComplete -= HandleRotationComplete;
>>>>>>> origin/dev
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

        if (Keyboard.current == null) { return; }

        if (Keyboard.current.uKey.wasPressedThisFrame) { RegisterKey("U"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.U); }
        if (Keyboard.current.jKey.wasPressedThisFrame) { RegisterKey("U'"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.UPrime); }

        if (Keyboard.current.dKey.wasPressedThisFrame) { RegisterKey("D"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.D); }
        if (Keyboard.current.cKey.wasPressedThisFrame) { RegisterKey("D'"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.DPrime); }

        if (Keyboard.current.lKey.wasPressedThisFrame) { RegisterKey("L"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.L); }
        if (Keyboard.current.kKey.wasPressedThisFrame) { RegisterKey("L'"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.LPrime); }

        if (Keyboard.current.rKey.wasPressedThisFrame) { RegisterKey("R"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.R); }
<<<<<<< HEAD
        if (Keyboard.current.eKey.wasPressedThisFrame) { RegisterKey("R'"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.RPrime); }
=======
        if (Keyboard.current.qKey.wasPressedThisFrame) { RegisterKey("R'"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.RPrime); }
>>>>>>> origin/dev

        if (Keyboard.current.fKey.wasPressedThisFrame) { RegisterKey("F"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.F); }
        if (Keyboard.current.gKey.wasPressedThisFrame) { RegisterKey("F'"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.FPrime); }

        if (Keyboard.current.bKey.wasPressedThisFrame) { RegisterKey("B"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.B); }
        if (Keyboard.current.nKey.wasPressedThisFrame) { RegisterKey("B'"); rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.BPrime); }

        UpdateOverlay();
    }

    private void RegisterKey(string label)
    {
        lastKeyLabel = label;
        lastKeyTime = Time.unscaledTime;
        if (showDebugLogs)
        {
            // Log removed per project request.
        }
    }

    private void UpdateOverlay()
    {
        if (debugText == null) { return; }
        string status = Keyboard.current == null ? "Keyboard: NULL" : "Keyboard: OK";
        float age = Time.unscaledTime - lastKeyTime;
        string last = string.IsNullOrEmpty(lastKeyLabel) ? "None" : $"{lastKeyLabel} ({age:0.0}s ago)";
        debugText.text = $"Rotation Input\n{status}\nLast Key: {last}";
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

<<<<<<< HEAD
=======
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

>>>>>>> origin/dev
    // --- ADDED ---
    // --- CHANGED ---
    private void HandlePlatformsRespawned()
    {
        isInputEnabled = true;
    }
<<<<<<< HEAD
=======

    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }
>>>>>>> origin/dev
}
