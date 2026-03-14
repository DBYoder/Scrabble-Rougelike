// LexiconTooltip.cs — Singleton tooltip panel that follows the mouse cursor.
// Shown by LexiconCardHover when the player hovers over a lexicon card.
using UnityEngine;
using UnityEngine.UI;

public class LexiconTooltip : MonoBehaviour
{
    public static LexiconTooltip Instance { get; private set; }

    public GameObject panel;
    public Text       bodyText;

    private RectTransform _canvasRT;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) _canvasRT = canvas.GetComponent<RectTransform>();

        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || _canvasRT == null) return;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, Input.mousePosition, null, out localPos);

        // Offset slightly so the tooltip doesn't sit directly under the pointer
        panel.GetComponent<RectTransform>().localPosition =
            new Vector3(localPos.x + 16f, localPos.y - 16f, 0f);
    }

    public void Show(string text)
    {
        if (panel == null) return;
        if (bodyText != null) bodyText.text = text;
        panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
