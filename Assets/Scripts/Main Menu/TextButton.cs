using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI tmp;
    private Vector3 originalScale;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        originalScale = transform.localScale;
    }

    // When mouse enters
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. Make it solid white
        tmp.faceColor = new Color32(255, 255, 255, 255);
        // 2. Make it bigger
        transform.localScale = originalScale * 1.15f;
    }

    // When mouse leaves
    public void OnPointerExit(PointerEventData eventData)
    {
        // 1. Make it hollow again (0 alpha on the face)
        tmp.faceColor = new Color32(255, 255, 255, 0);
        // 2. Back to normal size
        transform.localScale = originalScale;
    }
}