// TileCardDrag.cs — Added to hand tile cards at runtime by TileHandUI.RefreshHand().
// Registers pointer-down intent with DragManager; threshold + drop handled in DragManager.Update().
using UnityEngine;
using UnityEngine.EventSystems;

public class TileCardDrag : MonoBehaviour, IPointerDownHandler
{
    public TileInstance tile;

    public void OnPointerDown(PointerEventData data)
    {
        var dm = DragManager.Instance ?? DragManager.GetOrCreate();
        if (dm == null || dm.IsDragging) return;
        if (GameManager.Instance?.CurrentState != GameState.Placement) return;
        if (TileHandUI.Instance != null && TileHandUI.Instance.IsInRedrawMode) return;

        dm.BeginPendingFromHand(tile, gameObject);
    }
}
