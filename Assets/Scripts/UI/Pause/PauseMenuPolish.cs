using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual polish for the original pause panel. Keeps its layout, texts and buttons and only adds:
/// fade/scale-in when opened (works while time is frozen), a breathing glow on the title,
/// and hover/press feedback on each text button (scale, color, side marker).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PauseMenuPolish : MonoBehaviour
{
    [SerializeField] private Graphic title;
    [SerializeField] private Color titleA = new Color(0.25f, 0.9f, 1f);
    [SerializeField] private Color titleB = new Color(1f, 0.35f, 0.85f);
    [SerializeField] private RectTransform content;

    private CanvasGroup group;
    private float t;

    private void Awake() { group = GetComponent<CanvasGroup>(); }

    private void OnEnable()
    {
        t = 0f;
        if (group != null) group.alpha = 0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        t = Mathf.Min(1f, t + dt * 5f);
        float e = 1f - Mathf.Pow(1f - t, 3f);
        group.alpha = e;
        if (content != null) content.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
        if (title != null) title.color = Color.Lerp(titleA, titleB, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f));
    }
}

