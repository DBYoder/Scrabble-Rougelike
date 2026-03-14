// GridCellInteract.cs — Added to every grid cell by GridUI.BuildGrid().
// Registers pointer-down on unsubmitted turn tiles so DragManager can move them.
// Also serves as the raycast identifier used by DragManager.TryDropAtCursor().
using UnityEngine;
using UnityEngine.EventSystems;

public class GridCellInteract : MonoBehaviour, IPointerDownHandler
{
    public int cellX;
    public int cellY;

    public void OnPointerDown(PointerEventData data)
    {
        var dm = DragManager.Instance ?? DragManager.GetOrCreate();
        if (dm == null || dm.IsDragging) return;
        if (GameManager.Instance?.CurrentState != GameState.Placement) return;
        if (!GridManager.Instance.IsTurnCell(cellX, cellY)) return;

        dm.BeginPendingFromGrid(cellX, cellY);
    }
}
