using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tiny shared kit for the neon menus (main / pause / win): procedural sprites,
/// panels, labels, keycaps, animated buttons and easing. No textures or prefabs needed.
/// </summary>
public static class NeonUI
{
    public static readonly Color Ink     = new Color(0.035f, 0.03f, 0.09f, 0.94f);
    public static readonly Color InkSoft = new Color(0.07f, 0.06f, 0.15f, 0.92f);
    public static readonly Color TextHi  = new Color(0.96f, 0.96f, 1f);
    public static readonly Color TextLo  = new Color(0.62f, 0.62f, 0.78f);
    public static readonly Color Cyan    = new Color(0.25f, 0.9f, 1f);
    public static readonly Color Pink    = new Color(1f, 0.35f, 0.85f);
    public static readonly Color Violet  = new Color(0.62f, 0.45f, 1f);

    private static Sprite s_Rounded, s_Ring, s_Soft, s_Gradient;
    private static Font s_Font;

    public static Sprite Rounded => s_Rounded != null ? s_Rounded : (s_Rounded = MakeRounded(64, 18, 0f));
    public static Sprite Ring => s_Ring != null ? s_Ring : (s_Ring = MakeRounded(64, 18, 3f));
    public static Sprite Soft => s_Soft != null ? s_Soft : (s_Soft = MakeSoftCircle(64));
    public static Sprite Gradient => s_Gradient != null ? s_Gradient : (s_Gradient = MakeVerticalGradient());
    public static Font Font => s_Font != null ? s_Font : (s_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    // ── Canvas ────────────────────────────────────────────────────
    public static RectTransform CreateCanvas(Transform host, int sortingOrder)
    {
        // Adding a Canvas replaces Transform with RectTransform, so work through the GameObject.
        GameObject go = host.gameObject;
        var canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            es.transform.SetParent(go.transform, false);
        }
        return (RectTransform)go.transform;
    }

    // ── Widgets ───────────────────────────────────────────────────
    public static Image Img(string n, Transform parent, Sprite s, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = s; img.color = c; img.raycastTarget = false;
        if (s == Rounded || s == Ring) img.type = Image.Type.Sliced;
        return img;
    }

    public static RectTransform Panel(string n, Transform parent, Color c) => Img(n, parent, Rounded, c).rectTransform;

    public static Image Border(RectTransform parent, Color c)
    {
        var img = Img("Border", parent, Ring, c);
        Stretch(img.rectTransform, new Vector2(-2, -2), new Vector2(2, 2));
        return img;
    }

    public static Text Label(Transform parent, string s, int size, Color c, TextAnchor align = TextAnchor.MiddleCenter, bool bold = false)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, Vector2.zero, Vector2.zero);
        var t = go.GetComponent<Text>();
        t.font = Font; t.text = s; t.fontSize = size; t.color = c; t.alignment = align; t.raycastTarget = false;
        t.supportRichText = true;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = go.GetComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.55f); sh.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    public static RectTransform Keycap(Transform parent, string key, float width)
    {
        var kc = Panel("Key_" + key, parent, new Color(0.14f, 0.13f, 0.24f, 1f));
        kc.sizeDelta = new Vector2(width, 34);
        var shade = Img("Shade", kc, Rounded, new Color(0, 0, 0, 0.35f));
        Anchor(shade.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -3), new Vector2(width, 8));
        shade.transform.SetAsFirstSibling();
        Border(kc, new Color(1, 1, 1, 0.18f));
        Label(kc, key, 19, TextHi, TextAnchor.MiddleCenter, true);
        return kc;
    }

    /// <summary>Big menu button: dark pill, accent border, label, optional keycap; animated hover/press.</summary>
    public static NeonButton MenuButton(Transform parent, string label, string key, Color accent, Vector2 size, System.Action onClick)
    {
        var rt = Panel("Btn_" + label, parent, InkSoft);
        rt.sizeDelta = size;
        var glow = Img("Glow", rt, Soft, new Color(accent.r, accent.g, accent.b, 0f));
        Stretch(glow.rectTransform, new Vector2(-40, -30), new Vector2(40, 30));
        glow.transform.SetAsFirstSibling();
        var fill = Img("Fill", rt, Rounded, new Color(accent.r, accent.g, accent.b, 0f));
        Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);
        var border = Border(rt, new Color(accent.r, accent.g, accent.b, 0.45f));
        var bar = Img("Bar", rt, Rounded, accent);
        Anchor(bar.rectTransform, new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(5, size.y * 0.5f));
        var text = Label(rt, label, 30, TextHi, TextAnchor.MiddleLeft, true);
        Stretch(text.rectTransform, new Vector2(34, 0), new Vector2(-70, 0));
        if (!string.IsNullOrEmpty(key))
        {
            var kc = Keycap(rt, key, Mathf.Max(40, 18 + key.Length * 12));
            Anchor(kc, new Vector2(1, 0.5f), new Vector2(-16, 0), kc.sizeDelta);
        }
        var img = rt.GetComponent<Image>(); img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        var nb = rt.gameObject.AddComponent<NeonButton>();
        nb.Init(accent, glow, fill, border, bar, text, onClick);
        b.onClick.AddListener(nb.Press);
        return nb;
    }

    public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    public static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = min; rt.offsetMax = max;
    }

    // ── Easing ────────────────────────────────────────────────────
    public static float EaseOutCubic(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3f); }
    public static float EaseOutBack(float t) { t = Mathf.Clamp01(t); const float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }

    // ── Procedural sprites ────────────────────────────────────────
    private static Sprite MakeRounded(int size, int radius, float ringWidth)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0);
            float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0);
            float inside = radius - Mathf.Sqrt(dx * dx + dy * dy);
            float edge = Mathf.Min(Mathf.Min(x + 0.5f, size - x - 0.5f), Mathf.Min(y + 0.5f, size - y - 0.5f));
            float d = (dx > 0 && dy > 0) ? inside : edge;
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
            tex.SetPixel(0, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 1.6f)));
        }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f));
    }
}

/// <summary>Animated state for NeonUI.MenuButton (hover glow, slide, press punch, keyboard focus).</summary>
public class NeonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool Focused { get; set; }
    public System.Action onHover;

    private Color accent;
    private Image glow, fill, border, bar;
    private Text text;
    private System.Action onClick;
    private float hover, punch;
    private RectTransform rt;

    public void Init(Color a, Image g, Image f, Image b, Image br, Text t, System.Action click)
    {
        accent = a; glow = g; fill = f; border = b; bar = br; text = t; onClick = click;
        rt = (RectTransform)transform;
    }

    public void Press()
    {
        punch = 1f;
        onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData e) { Focused = true; onHover?.Invoke(); }
    public void OnPointerExit(PointerEventData e) { Focused = false; }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        hover = Mathf.Lerp(hover, Focused ? 1f : 0f, 1f - Mathf.Exp(-dt * 16f));
        punch = Mathf.MoveTowards(punch, 0f, dt * 4f);
        float s = 1f + hover * 0.04f - Mathf.Sin(punch * Mathf.PI) * 0.05f;
        rt.localScale = new Vector3(s, s, 1f);
        glow.color = new Color(accent.r, accent.g, accent.b, hover * 0.35f + punch * 0.4f);
        fill.color = new Color(accent.r, accent.g, accent.b, hover * 0.12f + punch * 0.2f);
        border.color = new Color(accent.r, accent.g, accent.b, Mathf.Lerp(0.45f, 1f, hover));
        bar.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(5f, 9f, hover), bar.rectTransform.sizeDelta.y);
        text.rectTransform.offsetMin = new Vector2(34f + hover * 10f, 0f);
        text.color = Color.Lerp(NeonUI.TextHi, Color.white, hover);
    }
}
