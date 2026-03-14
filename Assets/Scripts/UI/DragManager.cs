// DragManager.cs — Singleton managing tile drag-and-drop.
// Self-bootstrapping: if not placed in the scene by SceneBuilder, it creates
// itself (including the ghost tile UI) when first requested by TileCardDrag.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragManager : MonoBehaviour
{
    public static DragManager Instance { get; private set; }

    [Header("Ghost Tile (wired by SceneBuilder or created at runtime)")]
    public Image         ghostImage;
    public Text          ghostText;
    public RectTransform handAreaRT; // Drop zone — dragging a grid tile here returns it to hand

    private const float DragThreshold = 8f; // screen pixels before drag activates

    // ── Active drag ───────────────────────────────────────────────────────────
    public TileInstance DraggedTile { get; private set; }
    public bool         IsDragging  => DraggedTile != null;

    private bool       _fromGrid;
    private int        _srcX, _srcY;
    private GameObject _srcHandCard;

    // ── Pending (pointer down, waiting to cross movement threshold) ───────────
    private bool         _pendingFromGrid;
    private int          _pendingGridX, _pendingGridY;
    private TileInstance _pendingTile;
    private GameObject   _pendingCard;
    private Vector2      _dragStartScreenPos;
    private bool HasPending => _pendingTile != null || _pendingFromGrid;

    private RectTransform _canvasRT;
    private static readonly Color InvalidFlash = new Color(0.937f, 0.584f, 0.616f, 0.6f);
    private static readonly Color ValidHover   = new Color(0.58f,  0.76f,  0.88f,  0.7f);

    // Tracks which cell is currently highlighted during a drag
    private int _hoverX = -1, _hoverY = -1;

    // ── Bootstrap ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns the existing instance, or creates one on-the-fly if the scene
    /// was not rebuilt after the drag system was introduced.
    /// </summary>
    public static DragManager GetOrCreate()
    {
        if (Instance != null) return Instance;

        // Find the root canvas to parent ourselves to
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;

        var go = new GameObject("[DragManager]", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling(); // render above all panels
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var dm = go.AddComponent<DragManager>(); // Awake fires here
        dm.BuildGhost();
        return dm;
    }

    private void BuildGhost()
    {
        var ghostGo = new GameObject("GhostTile", typeof(RectTransform));
        ghostGo.transform.SetParent(transform, false);
        var grt = ghostGo.GetComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot     = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(80f, 110f);

        ghostImage               = ghostGo.AddComponent<Image>();
        ghostImage.color         = new Color(0.988f, 0.867f, 0.737f, 0.82f);
        ghostImage.raycastTarget = false;

        var txtGo = new GameObject("GhostLetter", typeof(RectTransform));
        txtGo.transform.SetParent(ghostGo.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        ghostText                    = txtGo.AddComponent<Text>();
        ghostText.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ghostText.fontSize           = 36;
        ghostText.fontStyle          = FontStyle.Bold;
        ghostText.color              = new Color(0.22f, 0.18f, 0.20f);
        ghostText.alignment          = TextAnchor.MiddleCenter;
        ghostText.horizontalOverflow = HorizontalWrapMode.Overflow;
        ghostText.verticalOverflow   = VerticalWrapMode.Overflow;
        ghostText.raycastTarget      = false;

        ghostGo.SetActive(false);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Find canvas RT for coordinate conversion
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) _canvasRT = canvas.GetComponent<RectTransform>();

        if (ghostImage != null) ghostImage.gameObject.SetActive(false);
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (IsDragging)
        {
            MoveGhostToCursor();
            UpdateDragHover();
            if (!Input.GetMouseButton(0))
                TryDropAtCursor();
            return;
        }

        if (!HasPending) return;

        if (!Input.GetMouseButton(0))
        {
            ClearPending(); // released before threshold — was a click, not drag
            return;
        }

        if (Vector2.Distance(Input.mousePosition, _dragStartScreenPos) >= DragThreshold)
        {
            if (_pendingFromGrid) ActivateDragFromGrid();
            else                  ActivateDragFromHand();
        }
    }

    // ── Begin pending ─────────────────────────────────────────────────────────
    public void BeginPendingFromHand(TileInstance tile, GameObject card)
    {
        _pendingTile        = tile;
        _pendingCard        = card;
        _pendingFromGrid    = false;
        _dragStartScreenPos = Input.mousePosition;
    }

    public void BeginPendingFromGrid(int x, int y)
    {
        _pendingFromGrid    = true;
        _pendingGridX       = x;
        _pendingGridY       = y;
        _pendingTile        = null;
        _dragStartScreenPos = Input.mousePosition;
    }

    // ── Activate ──────────────────────────────────────────────────────────────
    private void ActivateDragFromHand()
    {
        DraggedTile  = _pendingTile;
        _fromGrid    = false;
        _srcHandCard = _pendingCard;
        ClearPending();
        SetCardAlpha(_srcHandCard, 0.35f);
        ShowGhost(DraggedTile);
    }

    private void ActivateDragFromGrid()
    {
        int gx = _pendingGridX, gy = _pendingGridY;
        ClearPending();
        var tile = GridManager.Instance.RemoveTurnTile(gx, gy);
        if (tile == null) return; // already removed by click handler

        DraggedTile = tile;
        _fromGrid   = true;
        _srcX       = gx;
        _srcY       = gy;
        ShowGhost(tile);
        GridUI.Instance?.RefreshGrid();
        GridUI.Instance?.RefreshPreview();
    }

    // ── Hover highlight ───────────────────────────────────────────────────────
    private void UpdateDragHover()
    {
        var results   = new List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current)
                        { position = Input.mousePosition };
        EventSystem.current.RaycastAll(eventData, results);

        GridCellInteract hovered = null;
        foreach (var r in results)
        {
            hovered = r.gameObject.GetComponent<GridCellInteract>();
            if (hovered != null) break;
        }

        int newX = hovered != null ? hovered.cellX : -1;
        int newY = hovered != null ? hovered.cellY : -1;

        // Clear previous highlight if the cursor moved to a different cell
        if (_hoverX >= 0 && (newX != _hoverX || newY != _hoverY))
        {
            GridUI.Instance?.ClearHoverHighlight(_hoverX, _hoverY);
            _hoverX = -1; _hoverY = -1;
        }

        // Apply highlight to the new cell
        if (newX >= 0 && (_hoverX != newX || _hoverY != newY))
        {
            bool valid = GridManager.Instance.CanPlaceTile(newX, newY, DraggedTile);
            GridUI.Instance?.SetHoverHighlight(newX, newY, valid ? ValidHover : InvalidFlash);
            _hoverX = newX;
            _hoverY = newY;
        }
    }

    private void ClearDragHover()
    {
        if (_hoverX >= 0)
        {
            GridUI.Instance?.ClearHoverHighlight(_hoverX, _hoverY);
            _hoverX = -1; _hoverY = -1;
        }
    }

    // ── Drop ──────────────────────────────────────────────────────────────────
    private void TryDropAtCursor()
    {
        ClearDragHover();

        var results   = new List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current)
                        { position = Input.mousePosition };
        EventSystem.current.RaycastAll(eventData, results);

        GridCellInteract target = null;
        foreach (var r in results)
        {
            target = r.gameObject.GetComponent<GridCellInteract>();
            if (target != null) break;
        }

        if (target != null)
        {
            DropOnCell(target.cellX, target.cellY);
        }
        else if (_fromGrid && IsOverHandArea())
        {
            DropOnHand();
        }
        else
        {
            CancelDrag();
        }
    }

    private bool IsOverHandArea()
    {
        // Auto-detect the hand area if not wired by SceneBuilder
        if (handAreaRT == null && TileHandUI.Instance != null)
            handAreaRT = TileHandUI.Instance.handParent as RectTransform;
        if (handAreaRT == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            handAreaRT, Input.mousePosition, null);
    }

    private void DropOnHand()
    {
        if (!IsDragging || !_fromGrid) return;

        var tile    = DraggedTile;
        DraggedTile = null;
        HideGhost();

        TileHandManager.Instance.ReturnTileToHand(tile);
        TileHandUI.Instance?.RefreshHand();
        GridUI.Instance?.RefreshGrid();
        GridUI.Instance?.RefreshPreview();
    }

    public void DropOnCell(int x, int y)
    {
        if (!IsDragging) return;

        if (!GridManager.Instance.CanPlaceTile(x, y, DraggedTile))
        {
            GridUI.Instance?.FlashCell(x, y, InvalidFlash);
            CancelDrag();
            return;
        }

        var tile    = DraggedTile;
        DraggedTile = null;
        HideGhost();

        if (!_fromGrid)
        {
            TileHandManager.Instance.RemoveTileFromHand(tile);
            _srcHandCard = null;
        }

        GridManager.Instance.PlaceTile(x, y, tile);
        SFXManager.Instance?.PlayTilePlace();
        TileHandUI.Instance?.RefreshHand();
        GridUI.Instance?.RefreshGrid();
        GridUI.Instance?.RefreshPreview();
    }

    // ── Cancel ────────────────────────────────────────────────────────────────
    public void CancelDrag()
    {
        if (!IsDragging) return;
        ClearDragHover();

        var tile    = DraggedTile;
        DraggedTile = null;
        HideGhost();

        if (_fromGrid)
        {
            if (GridManager.Instance.CanPlaceTile(_srcX, _srcY, tile))
                GridManager.Instance.PlaceTile(_srcX, _srcY, tile);
            else
                TileHandManager.Instance.ReturnTileToHand(tile);

            GridUI.Instance?.RefreshGrid();
            GridUI.Instance?.RefreshPreview();
            TileHandUI.Instance?.RefreshHand();
        }
        else
        {
            SetCardAlpha(_srcHandCard, 1f);
            _srcHandCard = null;
        }
    }

    // ── Ghost ─────────────────────────────────────────────────────────────────
    private void MoveGhostToCursor()
    {
        if (ghostImage == null || _canvasRT == null) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, Input.mousePosition, null, out local);
        ghostImage.GetComponent<RectTransform>().localPosition = local;
    }

    private void ShowGhost(TileInstance tile)
    {
        if (ghostImage == null) return;
        if (ghostText != null) ghostText.text = tile.Letter.ToString().ToUpper();
        ghostImage.gameObject.SetActive(true);
    }

    private void HideGhost()
    {
        if (ghostImage != null) ghostImage.gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void ClearPending()
    {
        _pendingTile     = null;
        _pendingCard     = null;
        _pendingFromGrid = false;
    }

    private static void SetCardAlpha(GameObject card, float alpha)
    {
        if (card == null) return;
        var img = card.GetComponent<Image>();
        if (img != null) { var c = img.color; c.a = alpha; img.color = c; }
    }
}
