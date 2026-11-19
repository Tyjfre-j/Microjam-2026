using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Neon overlay for the main menu (CutScene). Replaces the flat 3D text buttons with animated
/// neon buttons that fire the SAME ObjectButton events (so the intro cutscene flow is unchanged).
/// Adds fade-in from black, keyboard/mouse navigation, subtle camera parallax and a footer.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Existing 3D menu")]
    [SerializeField] private GameObject menuRoot;          // mainmenufix
    [SerializeField] private ObjectButton newGameButton;   // fires PivotCubeControllerZ.NewGame
    [SerializeField] private ObjectButton2 quitButton;
    [SerializeField] private GameObject[] hideOnStart;     // 3D NEWGAME / SETTINGS / QUITGAME meshes

    [Header("Juice")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private float parallax = 0.6f;

    private RectTransform canvasRt;
    private CanvasGroup root, fade;
    private NeonButton[] buttons;
    private int focus;
    private float introT, leaveT = -1f;
    private Vector3 camBase;
    private Text hint;

    private void Awake()
    {
        if (menuRoot == null) menuRoot = GameObject.Find("mainmenufix");
        if (newGameButton == null && menuRoot != null) newGameButton = menuRoot.GetComponentInChildren<ObjectButton>(true);
        if (quitButton == null && menuRoot != null) quitButton = menuRoot.GetComponentInChildren<ObjectButton2>(true);
        if (menuCamera == null) menuCamera = Camera.main;
        if (menuCamera != null) camBase = menuCamera.transform.localPosition;
        if (hideOnStart != null) foreach (var g in hideOnStart) if (g != null) g.SetActive(false);

        canvasRt = NeonUI.CreateCanvas(transform, 40);
        Build();
    }

    private void Build()
    {
        var rootGo = new GameObject("Menu", typeof(RectTransform), typeof(CanvasGroup));
        rootGo.transform.SetParent(transform, false);
        NeonUI.Stretch((RectTransform)rootGo.transform, Vector2.zero, Vector2.zero);
        root = rootGo.GetComponent<CanvasGroup>();

        // Soft vignette so buttons read over the starfield.
        var vig = NeonUI.Img("Vignette", rootGo.transform, NeonUI.Gradient, new Color(0.18f, 0.05f, 0.32f, 0.55f));
        NeonUI.Stretch(vig.rectTransform, Vector2.zero, Vector2.zero);

        var col = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        col.transform.SetParent(rootGo.transform, false);
        var crt = (RectTransform)col.transform;
        NeonUI.Anchor(crt, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(460, 200));
        crt.pivot = new Vector2(0.5f, 0.5f);
        var vl = col.GetComponent<VerticalLayoutGroup>();
        vl.spacing = 18; vl.childAlignment = TextAnchor.UpperCenter; vl.childControlWidth = false; vl.childControlHeight = false;

        buttons = new[]
        {
            NeonUI.MenuButton(col.transform, "NEW GAME", "ENTER", NeonUI.Cyan, new Vector2(440, 76), StartGame),
            NeonUI.MenuButton(col.transform, "QUIT", "ESC", NeonUI.Pink, new Vector2(440, 76), QuitGame),
        };
        for (int i = 0; i < buttons.Length; i++) { int k = i; buttons[i].onHover = () => focus = k; }

        hint = NeonUI.Label(rootGo.transform, "W / S  or  ARROWS  to choose   ·   ENTER to confirm", 20, NeonUI.TextLo);
        NeonUI.Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 48), new Vector2(1000, 30));
        var credit = NeonUI.Label(rootGo.transform, "MICROJAM 2026", 18, new Color(1, 1, 1, 0.35f), TextAnchor.MiddleRight, true);
        NeonUI.Anchor(credit.rectTransform, new Vector2(1f, 0f), new Vector2(-40, 40), new Vector2(400, 30));

        // Fade-from-black overlay (top-most).
        var fadeImg = NeonUI.Img("Fade", transform, null, Color.black);
        NeonUI.Stretch(fadeImg.rectTransform, Vector2.zero, Vector2.zero);
        fade = fadeImg.gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        fade.alpha = 1f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        introT = Mathf.Min(1f, introT + dt / 1.2f);
        fade.alpha = 1f - NeonUI.EaseOutCubic(introT);

        if (leaveT >= 0f)
        {
            leaveT = Mathf.Min(1f, leaveT + dt / 0.35f);
            root.alpha = 1f - leaveT;
            root.transform.localScale = Vector3.one * (1f + leaveT * 0.05f);
            if (leaveT >= 1f) gameObject.SetActive(false);
            return;
        }

        // Staggered entrance.
        for (int i = 0; i < buttons.Length; i++)
        {
            float t = NeonUI.EaseOutBack(Mathf.Clamp01((introT - 0.25f - i * 0.12f) / 0.5f));
            var rt = (RectTransform)buttons[i].transform;
            var cg = buttons[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = buttons[i].gameObject.AddComponent<CanvasGroup>();
            cg.alpha = Mathf.Clamp01(t);
            // Layout group owns position and NeonButton owns scale; the entrance is a staggered fade.
            buttons[i].Focused = i == focus;
        }
        hint.color = new Color(NeonUI.TextLo.r, NeonUI.TextLo.g, NeonUI.TextLo.b, 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 2f));

        var kb = Keyboard.current;
        if (kb != null && introT > 0.4f)
        {
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) focus = (focus + 1) % buttons.Length;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) focus = (focus + buttons.Length - 1) % buttons.Length;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) buttons[focus].Press();
            else if (kb.escapeKey.wasPressedThisFrame) buttons[buttons.Length - 1].Press();
        }

        // Gentle mouse parallax on the menu camera.
        if (menuCamera != null && Mouse.current != null)
        {
            Vector2 m = Mouse.current.position.ReadValue();
            Vector2 n = new Vector2(m.x / Mathf.Max(1, Screen.width) - 0.5f, m.y / Mathf.Max(1, Screen.height) - 0.5f);
            Vector3 target = camBase + new Vector3(n.x, n.y, 0f) * parallax;
            menuCamera.transform.localPosition = Vector3.Lerp(menuCamera.transform.localPosition, target, 1f - Mathf.Exp(-dt * 4f));
        }
    }

    private void StartGame()
    {
        if (leaveT >= 0f) return;
        leaveT = 0f;
        if (menuCamera != null) menuCamera.transform.localPosition = camBase;
        if (newGameButton != null) newGameButton.onClick?.Invoke();
    }

    private void QuitGame()
    {
        if (quitButton != null) quitButton.onClick?.Invoke();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
