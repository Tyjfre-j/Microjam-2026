using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Hover / press feedback for a text button on the original pause panel.</summary>
public class PauseButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Graphic label;
    public Graphic marker;
    public Color normal = new Color(0.9f, 0.9f, 1f);
    public Color hover = new Color(0.25f, 0.9f, 1f);

    private float h, press;
    private bool over, down;

    public void OnPointerEnter(PointerEventData e) => over = true;
    public void OnPointerExit(PointerEventData e) { over = false; down = false; }
    public void OnPointerDown(PointerEventData e) => down = true;
    public void OnPointerUp(PointerEventData e) => down = false;

    private void OnDisable() { over = down = false; h = 0f; Apply(); }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        h = Mathf.Lerp(h, over ? 1f : 0f, 1f - Mathf.Exp(-dt * 16f));
        press = Mathf.Lerp(press, down ? 1f : 0f, 1f - Mathf.Exp(-dt * 25f));
        Apply();
    }

    private void Apply()
    {
        transform.localScale = Vector3.one * (1f + 0.08f * h - 0.05f * press);
        if (label != null) label.color = Color.Lerp(normal, hover, h);
        if (marker != null)
        {
            var c = hover; c.a = h; marker.color = c;
            marker.rectTransform.localScale = new Vector3(Mathf.Lerp(0.2f, 1f, h), 1f, 1f);
        }
    }
}
