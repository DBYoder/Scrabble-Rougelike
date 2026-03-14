// LexiconCardHover.cs — Attached to instantiated lexicon card GameObjects.
// Calls LexiconTooltip.Show/Hide when the pointer enters or leaves the card.
using UnityEngine;
using UnityEngine.EventSystems;

public class LexiconCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string effectText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        LexiconTooltip.Instance?.Show(effectText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        LexiconTooltip.Instance?.Hide();
    }
}
