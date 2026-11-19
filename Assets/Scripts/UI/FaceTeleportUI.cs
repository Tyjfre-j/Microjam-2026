using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Self-building neon HUD + "teleport to face" menu.
/// HUD: current-face badge, controls strip with keycaps, FACES button, contextual
/// "press E" prompt that tracks the button the player stands on, toasts, teleport flash
/// and a big title card when arriving on a face.
/// Menu (Tab / button): cube-net of the 6 faces with mouse + keyboard (arrows/WASD + Enter,
/// 1-6) navigation, staggered pop-in, hover/selection states, close button, ESC.
/// </summary>
public class FaceTeleportUI : MonoBehaviour
{
    [SerializeField] private CubeCameraRig rig;
    [SerializeField] private Key toggleKey = Key.Tab;

    public static bool IsOpen { get; private set; }
    public static bool Exists { get; private set; }

    // ── Palette ───────────────────────────────────────────────────
    private static readonly string[] Names = { "FRONT", "BACK", "RIGHT", "LEFT", "TOP", "BOTTOM" };
    private static readonly Color[] Accents =
    {
        new Color(0.20f, 0.90f, 1.00f), new Color(1.00f, 0.32f, 0.85f), new Color(0.45f, 1.00f, 0.55f),
        new Color(1.00f, 0.76f, 0.28f), new Color(0.66f, 0.56f, 1.00f), new Color(1.00f, 0.46f, 0.36f)
    };
    private static readonly Color Ink      = new Color(0.035f, 0.03f, 0.09f, 0.94f);
    private static readonly Color InkSoft  = new Color(0.07f, 0.06f, 0.15f, 0.92f);
    private static readonly Color TextHi   = new Color(0.96f, 0.96f, 1f);
    private static readonly Color TextLo   = new Color(0.62f, 0.62f, 0.78f);
    private static readonly Color ColCyan  = new Color(0.25f, 0.9f, 1f);
    private static readonly Color ColPink  = new Color(1f, 0.35f, 0.85f);

    // Cube-net layout (col,row) — row 0 is the top.
    private static readonly Vector2Int[] NetPos =
    {
        new Vector2Int(1, 1), new Vector2Int(3, 1), new Vector2Int(2, 1),
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 2)
    };

    // ── State ─────────────────────────────────────────────────────
    private bool open;
    private float openT;
    private int selected;
    private float flashT = 1f;
    private float cardT = 1f;
    private float toastT = 1f;
    private float badgeBump;
    private CubeCameraRig.Face shownFace = (CubeCameraRig.Face)(-1);
    private Sprite rounded, ring, soft, gradient;
    private Font font;
    private PlayerController player;
    private RectTransform canvasRt;

    // ── Widgets ───────────────────────────────────────────────────
    private CanvasGroup panelGroup;
    private RectTransform panel;
    private readonly Tile[] tiles = new Tile[6];
    private Text currentLabel;
    private Image badgeDot, badgeBar; private Text badgeText; private RectTransform badge;
    private RectTransform facesBtn; private Image facesBtnBorder;
    private RectTransform prompt; private CanvasGroup promptGroup; private Text promptText; private Image promptAccent; private Text promptKey;
    private Image flash; private CanvasGroup card; private Text cardTitle, cardSub; private Image cardLine;
    private CanvasGroup toast; private Text toastText;
    private CanvasGroup controls;

    private class Tile
    {
        public RectTransform rt; public Image bg, border, glow; public Text name, here; public float hover, pop, sel;
    }

    // ── Lifecycle ─────────────────────────────────────────────────
    private void Awake()
    {
        Exists = true;
        if (rig == null) rig = FindAnyObjectByType<CubeCameraRig>();
        player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rounded = MakeRounded(64, 18, 0f);
        ring = MakeRounded(64, 18, 3f);
        soft = MakeSoftCircle(64);
        gradient = MakeVerticalGradient();
        Build();
        SetOpen(false, true);
    }

    private void OnDestroy() { Exists = false; IsOpen = false; }
    private void OnEnable() { if (rig != null) rig.OnFaceChanged += HandleFaceChanged; }
    private void OnDisable() { if (rig != null) rig.OnFaceChanged -= HandleFaceChanged; }

    private void Start() { if (rig != null) RefreshBadge(rig.CurrentFace); }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (rig != null && rig.CurrentFace != shownFace) { shownFace = rig.CurrentFace; RefreshBadge(shownFace); }
        HandleInput();

        // Menu open/close: fade + slide + scale with ease-out-back.
        openT = Mathf.MoveTowards(openT, open ? 1f : 0f, dt * (open ? 5f : 7f));
        float e = open ? EaseOutBack(openT) : EaseOutCubic(openT);
        panelGroup.alpha = Mathf.Clamp01(openT * 1.4f);
        panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, e);
        panel.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-40f, 0f, e));
        panelGroup.blocksRaycasts = open;
        panelGroup.interactable = open;
        IsOpen = open;

        AnimateTiles(dt);
        AnimateHud(dt);
        UpdatePrompt(dt);
    }

    // ── Input ─────────────────────────────────────────────────────
    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (QuickMenuManager.IsPaused) return;
        if (kb[toggleKey].wasPressedThisFrame) { SetOpen(!open); return; }
        if (!open) return;
        if (kb.escapeKey.wasPressedThisFrame) { SetOpen(false); return; }

        Key[] nums = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
        for (int i = 0; i < 6; i++) if (kb[nums[i]].wasPressedThisFrame) { Pick(i); return; }

        Vector2Int move = Vector2Int.zero;
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) move.x = -1;
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) move.x = 1;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) move.y = -1;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) move.y = 1;
        if (move != Vector2Int.zero) Navigate(move);

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Pick(selected);
    }

    private void Navigate(Vector2Int dir)
    {
        Vector2Int target = NetPos[selected] + dir;
        // Wrap horizontally along the middle row (it's a ring around the cube).
        if (target.y == 1) target.x = (target.x + 4) % 4;
        for (int i = 0; i < 6; i++) if (NetPos[i] == target) { selected = i; tiles[i].pop = 0.6f; return; }
        // Moving vertically from a side face: go to top/bottom.
        if (dir.y != 0) selected = dir.y < 0 ? 4 : 5;
    }

    private void SetOpen(bool value, bool instant = false)
    {
        if (value && rig != null && rig.IsTransitioning) return;
        open = value;
        if (instant) openT = value ? 1f : 0f;
        if (player != null && player.isActiveAndEnabled) player.SetInputEnabled(!value);
        if (value)
        {
            int cur = rig != null ? (int)rig.CurrentFace : 0;
            selected = cur;
            if (currentLabel != null) currentLabel.text = "CURRENTLY ON  <color=#" + ColorUtility.ToHtmlStringRGB(Accents[cur]) + ">" + Names[cur] + "</color>";
            for (int i = 0; i < 6; i++) tiles[i].pop = -0.06f * StaggerOrder(i); // staggered pop-in
        }
    }

    public void Open() => SetOpen(true);

    private static int StaggerOrder(int i) { int[] o = { 1, 3, 2, 0, 4, 5 }; return o[i]; }

    private void Pick(int i)
    {
        if (rig == null) return;
        if ((int)rig.CurrentFace == i) { ShowToast("You're already on " + Names[i]); tiles[i].pop = 0.5f; return; }
        if (rig.IsTransitioning) return;
        if (!rig.GoToFace((CubeCameraRig.Face)i))
        {
            ShowToast("Wait for the cube to stop rotating");
            return;
        }
        open = false;
        if (player != null) player.SetInputEnabled(true);
    }

    private void HandleFaceChanged(CubeCameraRig.Face f)
    {
        RefreshBadge(f);
        flashT = 0f; cardT = 0f; badgeBump = 1f;
        Color a = Accents[(int)f];
        cardTitle.text = Names[(int)f];
        cardTitle.color = TextHi;
        cardLine.color = a;
        cardSub.text = "FACE " + ((int)f + 1) + " / 6";
        flash.color = new Color(a.r, a.g, a.b, 0f);
    }

    private void RefreshBadge(CubeCameraRig.Face f)
    {
        Color a = Accents[(int)f];
        badgeText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(TextLo) + ">FACE</color>   " + Names[(int)f];
        badgeDot.color = a;
        badgeBar.color = a;
    }

    private void ShowToast(string msg) { toastText.text = msg; toastT = 0f; }

    // ── Animation ─────────────────────────────────────────────────
    private void AnimateTiles(float dt)
    {
        float time = Time.unscaledTime;
        int cur = rig != null ? (int)rig.CurrentFace : -1;
        for (int i = 0; i < 6; i++)
        {
            Tile t = tiles[i];
            if (open) t.pop = Mathf.Min(1f, t.pop + dt * 4f);
            float p = Mathf.Clamp01(t.pop);
            t.sel = Mathf.MoveTowards(t.sel, i == selected ? 1f : 0f, dt * 8f);
            float target = 1f + 0.06f * Mathf.Max(t.hover, t.sel);
            float s = EaseOutBack(p) * target;
            t.rt.localScale = new Vector3(s, s, 1f);

            Color a = Accents[i];
            bool isCur = i == cur;
            float pulse = 0.6f + 0.4f * Mathf.Sin(time * 4f + i);
            t.border.color = new Color(a.r, a.g, a.b, isCur ? pulse : Mathf.Lerp(0.35f, 1f, Mathf.Max(t.hover, t.sel)));
            t.glow.color = new Color(a.r, a.g, a.b, 0.35f * Mathf.Max(t.sel, t.hover) + (isCur ? 0.12f : 0f));
            t.bg.color = Color.Lerp(new Color(a.r * 0.12f, a.g * 0.12f, a.b * 0.16f, 0.96f), new Color(a.r * 0.28f, a.g * 0.28f, a.b * 0.32f, 1f), Mathf.Max(t.hover, t.sel));
            t.here.enabled = isCur;
            t.name.color = isCur ? a : TextHi;
        }
    }

    private void AnimateHud(float dt)
    {
        float time = Time.unscaledTime;

        // FACES button breathing border.
        facesBtnBorder.color = new Color(ColCyan.r, ColCyan.g, ColCyan.b, open ? 1f : 0.55f + 0.25f * Mathf.Sin(time * 2.2f));

        // Badge bump on face change.
        badgeBump = Mathf.MoveTowards(badgeBump, 0f, dt * 2.5f);
        badge.localScale = Vector3.one * (1f + 0.12f * EaseOutCubic(badgeBump));
        badgeDot.rectTransform.localScale = Vector3.one * (0.85f + 0.15f * Mathf.Sin(time * 3f));

        // Controls hint fades while the menu is open.
        controls.alpha = Mathf.MoveTowards(controls.alpha, open ? 0f : 0.9f, dt * 4f);

        // Teleport flash.
        flashT = Mathf.Min(1f, flashT + dt * 2.2f);
        Color fc = flash.color; fc.a = (1f - EaseOutCubic(flashT)) * 0.55f; flash.color = fc;

        // Arrival title card: slide in, hold, fade.
        cardT = Mathf.Min(1f, cardT + dt / 1.8f);
        float inT = Mathf.Clamp01(cardT / 0.2f), outT = Mathf.Clamp01((cardT - 0.7f) / 0.3f);
        card.alpha = inT * (1f - outT);
        ((RectTransform)card.transform).anchoredPosition = new Vector2(0f, 120f + Mathf.Lerp(-30f, 0f, EaseOutCubic(inT)) + outT * 20f);
        cardLine.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(0f, 260f, EaseOutCubic(inT)), 4f);

        // Toast.
        toastT = Mathf.Min(1f, toastT + dt / 1.6f);
        float tIn = Mathf.Clamp01(toastT / 0.12f), tOut = Mathf.Clamp01((toastT - 0.75f) / 0.25f);
        toast.alpha = tIn * (1f - tOut);
        ((RectTransform)toast.transform).anchoredPosition = new Vector2(0f, 150f + EaseOutCubic(tIn) * 10f);
    }

    private void UpdatePrompt(float dt)
    {
        CubeRotationButtonTrigger near = null;
        if (!open)
        {
            var list = CubeRotationButtonTrigger.Active;
            for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].IsPlayerNearby) { near = list[i]; break; }
        }

        promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, near != null ? 1f : 0f, dt * 8f);
        if (near == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(near.transform.position);
        if (sp.z < 0f) { promptGroup.alpha = 0f; return; }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out Vector2 local);
        float bob = Mathf.Sin(Time.unscaledTime * 4f) * 3f;
        prompt.anchoredPosition = Vector2.Lerp(prompt.anchoredPosition, local + new Vector2(0f, 70f + bob), 1f - Mathf.Exp(-dt * 20f));

        Color a = near.IsColumn ? ColCyan : ColPink;
        promptAccent.color = a;
        promptKey.text = near.InteractKey.ToString().ToUpperInvariant();
        promptText.text = near.Describe();
        promptText.color = TextHi;
    }

    // ── Construction ──────────────────────────────────────────────
    private void Build()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            es.transform.SetParent(transform, false);
        }

        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvas.pixelPerfect = false;
        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        canvasRt = (RectTransform)transform;

        BuildHud();
        BuildMenu();
        BuildOverlays();
    }

    private void BuildHud()
    {
        // Current face badge (top-left).
        badge = Panel("FaceBadge", transform, InkSoft);
        Anchor(badge, new Vector2(0, 1), new Vector2(40, -40), new Vector2(300, 64));
        Border(badge, new Color(1, 1, 1, 0.12f));
        badgeBar = Img("Accent", badge, rounded, Color.white);
        Anchor(badgeBar.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(6, 40));
        badgeDot = Img("Dot", badge, soft, Color.white);
        Anchor(badgeDot.rectTransform, new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(22, 22));
        badgeText = Label(badge, "", 26, TextHi, TextAnchor.MiddleLeft);
        badgeText.supportRichText = true; badgeText.fontStyle = FontStyle.Bold;
        Stretch(badgeText.rectTransform, new Vector2(52, 0), new Vector2(-12, 0));

        // FACES button (top-right).
        facesBtn = Panel("FacesButton", transform, InkSoft);
        Anchor(facesBtn, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(250, 64));
        facesBtnBorder = Border(facesBtn, ColCyan);
        var icon = MiniNet(facesBtn, 26f, ColCyan);
        Anchor(icon, new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(34, 26));
        var lbl = Label(facesBtn, "FACES", 26, TextHi, TextAnchor.MiddleLeft);
        lbl.fontStyle = FontStyle.Bold;
        Stretch(lbl.rectTransform, new Vector2(70, 0), new Vector2(-80, 0));
        var kc = Keycap(facesBtn, "TAB", 54);
        Anchor(kc, new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(54, 34));
        var b = AddButton(facesBtn.gameObject, () => SetOpen(!open));
        Hoverable(facesBtn.gameObject, 1.05f);

        // Controls strip (bottom-left).
        var strip = new GameObject("Controls", typeof(RectTransform), typeof(CanvasGroup), typeof(HorizontalLayoutGroup));
        strip.transform.SetParent(transform, false);
        var srt = (RectTransform)strip.transform;
        Anchor(srt, new Vector2(0, 0), new Vector2(40, 36), new Vector2(900, 44));
        var hl = strip.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 22; hl.childAlignment = TextAnchor.MiddleLeft; hl.childControlWidth = false; hl.childControlHeight = false;
        controls = strip.GetComponent<CanvasGroup>();
        controls.blocksRaycasts = false;
        ControlHint(srt, new[] { "A", "D" }, "MOVE");
        ControlHint(srt, new[] { "SPACE" }, "JUMP");
        ControlHint(srt, new[] { "E" }, "ROTATE");
        ControlHint(srt, new[] { "TAB" }, "FACES");

        // Interaction prompt (follows the button in screen space).
        prompt = Panel("Prompt", transform, Ink);
        prompt.anchorMin = prompt.anchorMax = new Vector2(0.5f, 0.5f);
        prompt.pivot = new Vector2(0.5f, 0f);
        prompt.sizeDelta = new Vector2(320, 52);
        promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
        promptGroup.blocksRaycasts = false; promptGroup.alpha = 0f;
        promptAccent = Border(prompt, ColCyan);
        var pk = Keycap(prompt, "E", 40);
        Anchor(pk, new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(40, 36));
        promptKey = pk.GetComponentInChildren<Text>();
        promptText = Label(prompt, "", 21, TextHi, TextAnchor.MiddleLeft);
        promptText.fontStyle = FontStyle.Bold;
        Stretch(promptText.rectTransform, new Vector2(62, 0), new Vector2(-12, 0));
        var tail = Img("Tail", prompt, soft, new Color(1, 1, 1, 0.35f));
        Anchor(tail.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -16), new Vector2(10, 10));
        tail.rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void ControlHint(RectTransform parent, string[] keys, string label)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        var hl = go.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 6; hl.childAlignment = TextAnchor.MiddleLeft; hl.childControlWidth = false; hl.childControlHeight = false;
        float w = 0;
        foreach (var k in keys)
        {
            float kw = Mathf.Max(38, 16 + k.Length * 13);
            var kc = Keycap((RectTransform)go.transform, k, kw);
            kc.sizeDelta = new Vector2(kw, 34);
            w += kw + 6;
        }
        var t = Label(go.transform, label, 20, TextLo, TextAnchor.MiddleLeft);
        t.rectTransform.sizeDelta = new Vector2(label.Length * 13 + 8, 34);
        ((RectTransform)go.transform).sizeDelta = new Vector2(w + label.Length * 13 + 8, 40);
    }

    private void BuildMenu()
    {
        var root = new GameObject("TeleportMenu", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        Stretch((RectTransform)root.transform, Vector2.zero, Vector2.zero);
        panelGroup = root.GetComponent<CanvasGroup>();

        var dim = Img("Backdrop", root.transform, null, new Color(0.01f, 0f, 0.04f, 0.7f));
        Stretch(dim.rectTransform, Vector2.zero, Vector2.zero);
        dim.raycastTarget = true;
        AddButton(dim.gameObject, () => SetOpen(false)).transition = Selectable.Transition.None;
        var vign = Img("Vignette", root.transform, gradient, new Color(0.25f, 0.05f, 0.4f, 0.35f));
        Stretch(vign.rectTransform, Vector2.zero, Vector2.zero);
        vign.raycastTarget = false;

        panel = Panel("Window", root.transform, Ink);
        Anchor(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 820));
        Border(panel, new Color(0.62f, 0.45f, 1f, 0.75f));
        var sheen = Img("Sheen", panel, gradient, new Color(0.5f, 0.35f, 1f, 0.12f));
        Stretch(sheen.rectTransform, new Vector2(4, 4), new Vector2(-4, -4));
        sheen.raycastTarget = false;
        AddButton(panel.gameObject, () => { }).transition = Selectable.Transition.None; // swallow backdrop clicks

        var title = Label(panel, "T E L E P O R T", 46, TextHi, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(800, 60));
        var titleLine = Img("TitleLine", panel, rounded, new Color(0.62f, 0.45f, 1f, 0.8f));
        Anchor(titleLine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -108), new Vector2(120, 3));
        currentLabel = Label(panel, "", 22, TextLo, TextAnchor.MiddleCenter);
        currentLabel.supportRichText = true;
        Anchor(currentLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(800, 34));

        // Close button.
        var close = Panel("Close", panel, InkSoft);
        Anchor(close, new Vector2(1, 1), new Vector2(-22, -22), new Vector2(46, 46));
        Border(close, new Color(1, 1, 1, 0.25f));
        var x = Label(close, "X", 24, TextLo, TextAnchor.MiddleCenter); x.fontStyle = FontStyle.Bold;
        AddButton(close.gameObject, () => SetOpen(false));
        Hoverable(close.gameObject, 1.12f);

        const float tile = 156f, gap = 16f;
        float gridW = 4 * tile + 3 * gap;
        Vector2 origin = new Vector2(-gridW / 2f + tile / 2f, tile + gap - 46f);
        for (int i = 0; i < 6; i++)
        {
            int idx = i;
            var t = new Tile();
            t.rt = Panel(Names[i], panel, Color.black);
            t.bg = t.rt.GetComponent<Image>();
            Anchor(t.rt, new Vector2(0.5f, 0.5f), origin + new Vector2(NetPos[i].x * (tile + gap), -NetPos[i].y * (tile + gap)), new Vector2(tile, tile));
            t.glow = Img("Glow", t.rt, soft, Color.clear);
            Stretch(t.glow.rectTransform, new Vector2(-30, -30), new Vector2(30, 30));
            t.glow.transform.SetAsFirstSibling();
            t.glow.raycastTarget = false;
            t.border = Border(t.rt, Accents[i]);

            var mini = MiniNet(t.rt, 44f, Accents[i]);
            Anchor(mini, new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(44, 44));
            HighlightMini(mini, i);

            t.name = Label(t.rt, Names[i], 24, TextHi, TextAnchor.MiddleCenter);
            t.name.fontStyle = FontStyle.Bold;
            Anchor(t.name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(tile, 30));

            var num = Keycap(t.rt, (i + 1).ToString(), 30);
            Anchor(num, new Vector2(0, 1), new Vector2(10, -10), new Vector2(30, 28));

            t.here = Label(t.rt, "HERE", 15, Accents[i], TextAnchor.MiddleCenter);
            t.here.fontStyle = FontStyle.Bold;
            Anchor(t.here.rectTransform, new Vector2(1, 1), new Vector2(-10, -12), new Vector2(54, 22));

            AddButton(t.rt.gameObject, () => Pick(idx)).transition = Selectable.Transition.None;
            var hv = t.rt.gameObject.AddComponent<HoverState>();
            hv.onEnter = () => { t.hover = 1f; selected = idx; };
            hv.onExit = () => t.hover = 0f;
            tiles[i] = t;
        }

        // Footer keycap hints.
        var foot = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        foot.transform.SetParent(panel, false);
        var frt = (RectTransform)foot.transform;
        Anchor(frt, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(720, 40));
        var fhl = foot.GetComponent<HorizontalLayoutGroup>();
        fhl.spacing = 26; fhl.childAlignment = TextAnchor.MiddleCenter; fhl.childControlWidth = false; fhl.childControlHeight = false;
        ControlHint(frt, new[] { "1-6" }, "JUMP TO");
        ControlHint(frt, new[] { "ARROWS" }, "SELECT");
        ControlHint(frt, new[] { "ENTER" }, "GO");
        ControlHint(frt, new[] { "ESC" }, "CLOSE");
    }

    private void BuildOverlays()
    {
        flash = Img("Flash", transform, gradient, new Color(1, 1, 1, 0));
        Stretch(flash.rectTransform, Vector2.zero, Vector2.zero);
        flash.raycastTarget = false;

        var cardGo = new GameObject("ArrivalCard", typeof(RectTransform), typeof(CanvasGroup));
        cardGo.transform.SetParent(transform, false);
        card = cardGo.GetComponent<CanvasGroup>(); card.alpha = 0f; card.blocksRaycasts = false;
        var crt = (RectTransform)cardGo.transform;
        Anchor(crt, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(800, 160));
        cardSub = Label(crt, "", 22, TextLo, TextAnchor.MiddleCenter);
        Anchor(cardSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(800, 30));
        cardTitle = Label(crt, "", 86, TextHi, TextAnchor.MiddleCenter);
        cardTitle.fontStyle = FontStyle.Bold;
        var outline = cardTitle.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, 0.5f); outline.effectDistance = new Vector2(2, -2);
        Anchor(cardTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(800, 100));
        cardLine = Img("Line", crt, rounded, Color.white);
        Anchor(cardLine.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(0, 4));

        var toastGo = Panel("Toast", transform, Ink);
        toast = toastGo.gameObject.AddComponent<CanvasGroup>(); toast.alpha = 0f; toast.blocksRaycasts = false;
        Anchor(toastGo, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(560, 54));
        Border(toastGo, new Color(1f, 0.6f, 0.3f, 0.8f));
        toastText = Label(toastGo, "", 22, TextHi, TextAnchor.MiddleCenter);
    }

    // ── Widget helpers ────────────────────────────────────────────
    private RectTransform Panel(string n, Transform parent, Color c)
    {
        var img = Img(n, parent, rounded, c);
        img.type = Image.Type.Sliced;
        return img.rectTransform;
    }

    private Image Img(string n, Transform parent, Sprite s, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = s; img.color = c; img.raycastTarget = false;
        if (s == rounded || s == ring) img.type = Image.Type.Sliced;
        return img;
    }

    private Image Border(RectTransform parent, Color c)
    {
        var img = Img("Border", parent, ring, c);
        Stretch(img.rectTransform, new Vector2(-2, -2), new Vector2(2, 2));
        return img;
    }

    private RectTransform Keycap(RectTransform parent, string key, float width)
    {
        var kc = Panel("Key_" + key, parent, new Color(0.14f, 0.13f, 0.24f, 1f));
        kc.sizeDelta = new Vector2(width, 34);
        var shade = Img("Shade", kc, rounded, new Color(0, 0, 0, 0.35f));
        Anchor(shade.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -3), new Vector2(width, 8));
        shade.transform.SetAsFirstSibling();
        Border(kc, new Color(1, 1, 1, 0.18f));
        var t = Label(kc, key, 19, TextHi, TextAnchor.MiddleCenter);
        t.fontStyle = FontStyle.Bold;
        return kc;
    }

    private RectTransform MiniNet(RectTransform parent, float size, Color c)
    {
        var go = new GameObject("MiniGrid", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        float cell = size / 2f - 2f;
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 2; x++)
        {
            var sq = Img("c", rt, rounded, new Color(c.r, c.g, c.b, 0.85f));
            sq.type = Image.Type.Simple;
            Anchor(sq.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((x - 0.5f) * (cell + 3f), (y - 0.5f) * (cell + 3f)), new Vector2(cell, cell));
        }
        return rt;
    }

    private void HighlightMini(RectTransform mini, int face)
    {
        // Tiny flourish: each face's mini-grid has a different lit cell so tiles read as distinct.
        int lit = face % 4;
        int k = 0;
        foreach (Transform c in mini)
        {
            var img = c.GetComponent<Image>();
            Color col = img.color; col.a = k == lit ? 1f : 0.35f; img.color = col;
            k++;
        }
    }

    private Text Label(Transform parent, string s, int size, Color c, TextAnchor align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, Vector2.zero, Vector2.zero);
        var t = go.GetComponent<Text>();
        t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = align; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = go.GetComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.55f); sh.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = min; rt.offsetMax = max;
    }

    private static Button AddButton(GameObject go, UnityEngine.Events.UnityAction a)
    {
        var img = go.GetComponent<Image>(); if (img != null) img.raycastTarget = true;
        var b = go.GetComponent<Button>();
        if (b == null) b = go.AddComponent<Button>();
        b.onClick.AddListener(a);
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.25f); cb.pressedColor = new Color(0.75f, 0.75f, 0.8f); cb.fadeDuration = 0.08f; b.colors = cb;
        return b;
    }

    private static void Hoverable(GameObject go, float scale)
    {
        var h = go.AddComponent<HoverState>();
        var rt = (RectTransform)go.transform;
        h.onTick = v => rt.localScale = Vector3.one * Mathf.Lerp(1f, scale, v);
    }

    private static float EaseOutCubic(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3f); }
    private static float EaseOutBack(float t) { t = Mathf.Clamp01(t); const float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }

    // ── Procedural sprites ────────────────────────────────────────
    private static Sprite MakeRounded(int size, int radius, float ringWidth)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0);
            float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0);
            float inside = radius - Mathf.Sqrt(dx * dx + dy * dy);
            float edgeX = Mathf.Min(x + 0.5f, size - x - 0.5f), edgeY = Mathf.Min(y + 0.5f, size - y - 0.5f);
            float d = (dx > 0 && dy > 0) ? inside : Mathf.Min(edgeX, edgeY);
            float a = Mathf.Clamp01(d + 0.5f);
            if (ringWidth > 0f) a *= Mathf.Clamp01(ringWidth - d + 0.5f);
            px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(px); tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static Sprite MakeSoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
            float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
            px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(px); tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite MakeVerticalGradient()
    {
        const int h = 64;
        var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        {
            float t = y / (h - 1f);
            float a = Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 1.6f); // bright at top/bottom edges
            tex.SetPixel(0, y, new Color(1, 1, 1, a));
        }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Pointer hover helper with smoothed 0..1 value.</summary>
    private class HoverState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public System.Action onEnter, onExit;
        public System.Action<float> onTick;
        private float target, value;
        public void OnPointerEnter(PointerEventData e) { target = 1f; onEnter?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { target = 0f; onExit?.Invoke(); }
        private void Update()
        {
            if (onTick == null) return;
            value = Mathf.Lerp(value, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
            onTick(value);
        }
    }
}
