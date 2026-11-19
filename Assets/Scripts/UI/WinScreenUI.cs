using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Neon victory overlay for the Win scene: letter-by-letter "SOLVED!" title, confetti burst,
/// run stats (time / moves) counting up, and Play Again / Main Menu / Quit buttons that
/// appear once the existing "you won" clip has played (auto-return is disabled).
/// </summary>
public class WinScreenUI : MonoBehaviour
{
    [SerializeField] private int menuBuildIndex = 0;
    [SerializeField] private int gameBuildIndex = 1;
    [SerializeField] private uwinscreen clipPlayer;
    [SerializeField] private float buttonsDelay = 1.6f;

    private static readonly Color[] Confetti =
    {
        new Color(0.25f, 0.9f, 1f), new Color(1f, 0.35f, 0.85f), new Color(0.45f, 1f, 0.55f),
        new Color(1f, 0.76f, 0.28f), new Color(0.66f, 0.56f, 1f), new Color(1f, 0.46f, 0.36f)
    };

    private class Piece { public RectTransform rt; public Vector2 v; public float spin, life; }

    private readonly List<Piece> pieces = new List<Piece>(160);
    private readonly List<Text> letters = new List<Text>();
    private RectTransform confettiRoot;
    private CanvasGroup statsGroup, buttonsGroup, fade;
    private Text timeText, movesText, subtitle;
    private NeonButton[] buttons;
    private int focus;
    private float t;
    private bool leaving;

    private void Awake()
    {
        if (clipPlayer == null) clipPlayer = FindAnyObjectByType<uwinscreen>();
        if (clipPlayer != null)
        {
            clipPlayer.autoReturn = false;
            buttonsDelay = 1.6f;
        }
        Time.timeScale = 1f;
        NeonUI.CreateCanvas(transform, 70);
        Build();

    }

    private void Build()
    {
        var vig = NeonUI.Img("Vignette", transform, NeonUI.Gradient, new Color(0.25f, 0.06f, 0.4f, 0.5f));
        NeonUI.Stretch(vig.rectTransform, Vector2.zero, Vector2.zero);

        confettiRoot = new GameObject("Confetti", typeof(RectTransform)).GetComponent<RectTransform>();
        confettiRoot.SetParent(transform, false);
        NeonUI.Stretch(confettiRoot, Vector2.zero, Vector2.zero);

        // Title letters.
        const string word = "SOLVED!";
        var row = new GameObject("Title", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        row.SetParent(transform, false);
        NeonUI.Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0, 2000), new Vector2(900, 150));
        row.pivot = new Vector2(0.5f, 0.5f);
        var hl = row.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 6; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = false; hl.childControlHeight = false;
        for (int i = 0; i < word.Length; i++)
        {
            var cell = new GameObject("L" + i, typeof(RectTransform)).GetComponent<RectTransform>();
            cell.SetParent(row, false);
            cell.sizeDelta = new Vector2(word[i] == '!' ? 50 : 104, 150);
            var l = NeonUI.Label(cell, word[i].ToString(), 130, Color.white, TextAnchor.MiddleCenter, true);
            var o = l.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(Confetti[i % 6].r, Confetti[i % 6].g, Confetti[i % 6].b, 0.6f);
            o.effectDistance = new Vector2(3, -3);
            l.color = Color.Lerp(NeonUI.Cyan, NeonUI.Pink, i / (word.Length - 1f));
            l.transform.localScale = Vector3.zero;
            letters.Add(l);
        }

        subtitle = NeonUI.Label(transform, "THE CUBE IS WHOLE AGAIN", 26, NeonUI.TextLo, TextAnchor.MiddleCenter, true);
        NeonUI.Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 2000), new Vector2(900, 40));
        subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Stats cards.
        var stats = new GameObject("Stats", typeof(RectTransform), typeof(CanvasGroup), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        stats.SetParent(transform, false);
        NeonUI.Anchor(stats, new Vector2(0.5f, 0.5f), new Vector2(0, 390), new Vector2(620, 120));
        stats.pivot = new Vector2(0.5f, 0.5f);
        var shl = stats.GetComponent<HorizontalLayoutGroup>();
        shl.spacing = 20; shl.childAlignment = TextAnchor.MiddleCenter; shl.childControlWidth = false; shl.childControlHeight = false;
        statsGroup = stats.GetComponent<CanvasGroup>();
        timeText = StatCard(stats, "TIME", NeonUI.Cyan);
        movesText = StatCard(stats, "MOVES", NeonUI.Pink);

        // Buttons.
        var col = new GameObject("Buttons", typeof(RectTransform), typeof(CanvasGroup), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        col.SetParent(transform, false);
        NeonUI.Anchor(col, new Vector2(0.5f, 0.5f), new Vector2(0, 255), new Vector2(1120, 80));
        col.pivot = new Vector2(0.5f, 0.5f);
        var vl = col.GetComponent<HorizontalLayoutGroup>();
        vl.spacing = 14; vl.childAlignment = TextAnchor.MiddleCenter; vl.childControlWidth = false; vl.childControlHeight = false;
        buttonsGroup = col.GetComponent<CanvasGroup>();
        Vector2 size = new Vector2(350, 66);
        buttons = new[]
        {
            NeonUI.MenuButton(col, "PLAY AGAIN", "ENTER", NeonUI.Cyan, size, () => Go(gameBuildIndex)),
            NeonUI.MenuButton(col, "MAIN MENU", "M", NeonUI.Violet, size, () => Go(menuBuildIndex)),
            NeonUI.MenuButton(col, "QUIT", "ESC", NeonUI.Pink, size, Quit),
        };
        for (int i = 0; i < buttons.Length; i++) { int k = i; buttons[i].onHover = () => focus = k; }

        var fadeImg = NeonUI.Img("Fade", transform, null, Color.black);
        NeonUI.Stretch(fadeImg.rectTransform, Vector2.zero, Vector2.zero);
        fade = fadeImg.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f; fade.blocksRaycasts = false;
    }

    private Text StatCard(RectTransform parent, string label, Color accent)
    {
        var card = NeonUI.Panel(label, parent, NeonUI.InkSoft);
        card.sizeDelta = new Vector2(290, 110);
        NeonUI.Border(card, new Color(accent.r, accent.g, accent.b, 0.7f));
        var l = NeonUI.Label(card, label, 18, NeonUI.TextLo, TextAnchor.UpperCenter, true);
        l.rectTransform.offsetMax = new Vector2(0, -12);
        var v = NeonUI.Label(card, "--", 46, accent, TextAnchor.LowerCenter, true);
        v.rectTransform.offsetMin = new Vector2(0, 14);
        return v;
    }

    private void Burst(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var img = NeonUI.Img("c", confettiRoot, NeonUI.Rounded, Confetti[Random.Range(0, Confetti.Length)]);
            img.type = Image.Type.Simple;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Random.Range(8f, 16f), Random.Range(14f, 26f));
            rt.anchoredPosition = new Vector2(Random.Range(-120f, 120f), 420f);
            float ang = Random.Range(20f, 160f) * Mathf.Deg2Rad;
            float spd = Random.Range(500f, 1300f);
            pieces.Add(new Piece { rt = rt, v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd, spin = Random.Range(-720f, 720f), life = Random.Range(2.5f, 4.5f) });
        }
    }

    private float waitT, revealAt = -1f;
    private Image clipImage;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Phase 1: let the original "YOU WON" clip play, then fade it out and reveal the overlay.
        if (revealAt < 0f)
        {
            waitT += dt;
            if (waitT > 0.05f)
            {
                float len = clipPlayer != null ? clipPlayer.ClipLength : 0f;
                revealAt = Mathf.Clamp(len, 0f, 4f);
                if (clipPlayer != null) clipImage = clipPlayer.GetComponent<Image>();
            }
            HideOverlay();
            return;
        }
        if (waitT < revealAt) { waitT += dt; HideOverlay(); return; }
        if (t == 0f) Burst(140);
        if (clipImage != null)
        {
            // Original "YOU WON" clip stays fully visible (kept as the hero of the screen).
        }
        t += dt;

        // Letters pop in one by one, then wave.
        for (int i = 0; i < letters.Count; i++)
        {
            float lt = Mathf.Clamp01((t - 0.15f - i * 0.08f) / 0.4f);
            float s = NeonUI.EaseOutBack(lt);
            var tr = letters[i].transform;
            tr.localScale = Vector3.one * s;
            tr.localPosition = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 3f + i * 0.6f) * 8f * lt, 0f);
            tr.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 2f + i) * 3f * lt);
        }
        subtitle.color = new Color(NeonUI.TextLo.r, NeonUI.TextLo.g, NeonUI.TextLo.b, Mathf.Clamp01((t - 0.8f) / 0.5f));

        // Stats count up.
        float st = Mathf.Clamp01((t - 0.9f) / 0.9f);
        statsGroup.alpha = Mathf.Clamp01((t - 0.8f) / 0.3f);
        float secs = RunStats.Seconds * NeonUI.EaseOutCubic(st);
        int si = Mathf.FloorToInt(secs);
        timeText.text = RunStats.Seconds > 0f ? $"{si / 60:00}:{si % 60:00}" : "--:--";
        movesText.text = Mathf.RoundToInt(RunStats.Moves * NeonUI.EaseOutCubic(st)).ToString();

        // Confetti physics.
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            var p = pieces[i];
            p.v.y -= 1400f * dt;
            p.v *= 1f - 0.9f * dt;
            p.rt.anchoredPosition += p.v * dt;
            p.rt.localRotation *= Quaternion.Euler(0, 0, p.spin * dt);
            p.rt.localScale = new Vector3(Mathf.Cos(Time.unscaledTime * 6f + i), 1f, 1f); // flutter
            p.life -= dt;
            if (p.life <= 0f) { Destroy(p.rt.gameObject); pieces.RemoveAt(i); }
        }
        if (t > 1.2f && t - dt <= 1.2f) Burst(60); // second smaller burst

        // Buttons after the clip.
        float bt = Mathf.Clamp01((t - buttonsDelay) / 0.4f);
        buttonsGroup.alpha = bt;
        buttonsGroup.interactable = buttonsGroup.blocksRaycasts = bt > 0.5f;
        if (bt > 0.5f && !leaving)
        {
            for (int i = 0; i < buttons.Length; i++) buttons[i].Focused = i == focus;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) focus = (focus + 1) % buttons.Length;
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) focus = (focus + buttons.Length - 1) % buttons.Length;
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) buttons[focus].Press();
                if (kb.mKey.wasPressedThisFrame) buttons[1].Press();
                if (kb.escapeKey.wasPressedThisFrame) buttons[2].Press();
            }
        }

        if (leaving) fade.alpha = Mathf.MoveTowards(fade.alpha, 1f, dt * 3f);
    }

    private void HideOverlay()
    {
        for (int i = 0; i < letters.Count; i++) letters[i].transform.localScale = Vector3.zero;
        statsGroup.alpha = 0f;
        buttonsGroup.alpha = 0f; buttonsGroup.interactable = buttonsGroup.blocksRaycasts = false;
        subtitle.color = Color.clear;
    }

    private void Go(int buildIndex)
    {
        if (leaving) return;
        leaving = true;
        StartCoroutine(LoadAfterFade(buildIndex));
    }

    private System.Collections.IEnumerator LoadAfterFade(int buildIndex)
    {
        while (fade.alpha < 1f) yield return null;
        SceneManager.LoadScene(buildIndex);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
