// GridUI.cs — Renders the 13×13 grid and handles click-to-place interaction.
// Attach to a GameObject in the Game Panel. Assign cellPrefab and gridParent.
// cellPrefab should have: Image (background), Button, and a child Text named "LetterText".
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridUI : MonoBehaviour
{
    public static GridUI Instance { get; private set; }

    [Header("References")]
    public GameObject cellPrefab;   // Prefab: Image + Button + child Text "LetterText"
    public Transform  gridParent;   // Parent RectTransform for grid cells

    [Header("Layout")]
    public float cellSize    = 48f;
    public float cellSpacing = 2f;

    [Header("Score Preview")]
    public Text scorePreviewText;

    [Header("Colors")]
    public Color emptyColor    = new Color(0.87f, 0.87f, 0.84f);
    public Color occupiedColor = new Color(0.85f, 0.60f, 0.28f);   // warm amber — committed (high contrast)
    public Color turnTileColor = new Color(0.58f, 0.76f, 0.88f);   // soft slate blue — placed this turn
    public Color validHover    = new Color(0.58f, 0.76f, 0.88f, 0.7f);
    public Color invalidColor  = new Color(0.937f, 0.584f, 0.616f, 0.6f);
    // Modifier cell colors — palette-matched
    public Color twColor = new Color(0.937f, 0.627f, 0.314f); // #EF9F50 warm orange — distinct from invalid rose
    public Color dwColor = new Color(0.97f,  0.76f,  0.78f);  // lighter rose
    public Color tlColor = new Color(0.60f,  0.78f,  0.62f);  // medium sage
    public Color dlColor              = new Color(0.851f, 0.859f, 0.737f); // #D9DBBC light olive
    public Color invalidTurnTileColor = new Color(0.937f, 0.584f, 0.616f); // rose — invalid word

    private GameObject[,] cellObjects;
    // Cells that belong to a word formed this turn that fails dictionary validation.
    // Recomputed on every RefreshGrid so the player gets immediate visual feedback.
    private readonly HashSet<(int, int)> _invalidTurnCells = new HashSet<(int, int)>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Build / Rebuild ───────────────────────────────────────────────────────
    public void BuildGrid()
    {
        // Destroy old cells
        if (cellObjects != null)
            foreach (var obj in cellObjects)
                if (obj != null) Destroy(obj);

        cellObjects = new GameObject[GridManager.GridSize, GridManager.GridSize];

        float step   = cellSize + cellSpacing;
        float offset = (GridManager.GridSize - 1) * step * 0.5f;

        for (int x = 0; x < GridManager.GridSize; x++)
        {
            for (int y = 0; y < GridManager.GridSize; y++)
            {
                var obj = Instantiate(cellPrefab, gridParent);
                var rt  = obj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(x * step - offset, y * step - offset);
                    rt.sizeDelta        = new Vector2(cellSize, cellSize);
                }

                int cx = x, cy = y; // capture for closure
                var btn = obj.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnCellClicked(cx, cy));

                // Drag-and-drop: drop target + drag source for unsubmitted tiles
                var interact = obj.AddComponent<GridCellInteract>();
                interact.cellX = cx;
                interact.cellY = cy;

                cellObjects[x, y] = obj;
                UpdateCellVisual(x, y);
            }
        }

        RefreshPreview();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────
    public void RefreshGrid()
    {
        if (cellObjects == null) { BuildGrid(); return; }
        RecomputeInvalidTurnCells();
        for (int x = 0; x < GridManager.GridSize; x++)
            for (int y = 0; y < GridManager.GridSize; y++)
                UpdateCellVisual(x, y);
    }

    /// <summary>
    /// Identifies which current-turn cells belong to a word that fails dictionary validation.
    /// Called every RefreshGrid so the player sees instant feedback as they place tiles.
    /// </summary>
    private void RecomputeInvalidTurnCells()
    {
        _invalidTurnCells.Clear();
        if (GridManager.Instance == null || WordValidator.Instance == null) return;
        if (GameManager.Instance?.CurrentState != GameState.Placement) return;

        foreach (var word in GridManager.Instance.GetAllWords())
        {
            // Only care about words that touch the current turn
            bool touchesTurn = false;
            foreach (var pos in word.cellPositions)
                if (GridManager.Instance.IsTurnCell(pos.x, pos.y)) { touchesTurn = true; break; }
            if (!touchesTurn) continue;

            if (!WordValidator.Instance.IsValid(word.text))
                foreach (var pos in word.cellPositions)
                    if (GridManager.Instance.IsTurnCell(pos.x, pos.y))
                        _invalidTurnCells.Add(pos);
        }
    }

    // ── Drag hover highlight ──────────────────────────────────────────────────
    public void SetHoverHighlight(int x, int y, Color color)
    {
        var obj = cellObjects?[x, y];
        if (obj == null) return;
        var img = obj.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    public void ClearHoverHighlight(int x, int y) => UpdateCellVisual(x, y);

    private void UpdateCellVisual(int x, int y)
    {
        var obj = cellObjects?[x, y];
        if (obj == null) return;

        var cell = GridManager.Instance.GetCell(x, y);
        var img  = obj.GetComponent<Image>();
        var txt  = obj.GetComponentInChildren<Text>();

        if (cell.IsOccupied)
        {
            bool isTurn = GridManager.Instance.IsTurnCell(x, y);
            Color tileColor = isTurn
                ? (_invalidTurnCells.Contains((x, y)) ? invalidTurnTileColor : turnTileColor)
                : occupiedColor;
            if (img != null) img.color = tileColor;
            if (txt != null) txt.text  = cell.placedTile.Letter.ToString().ToUpper();
        }
        else
        {
            switch (cell.modifier)
            {
                case CellModifier.TripleWord:
                    if (img != null) img.color = twColor;
                    if (txt != null) txt.text  = "TW";
                    break;
                case CellModifier.DoubleWord:
                    if (img != null) img.color = dwColor;
                    if (txt != null) txt.text  = cell.isCenter ? "★" : "DW";
                    break;
                case CellModifier.TripleLetter:
                    if (img != null) img.color = tlColor;
                    if (txt != null) txt.text  = "TL";
                    break;
                case CellModifier.DoubleLetter:
                    if (img != null) img.color = dlColor;
                    if (txt != null) txt.text  = "DL";
                    break;
                default:
                    if (img != null) img.color = emptyColor;
                    if (txt != null) txt.text  = "";
                    break;
            }
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    private void OnCellClicked(int x, int y)
    {
        if (GameManager.Instance.CurrentState != GameState.Placement) return;

        // Clicking an unsubmitted turn tile returns it to hand.
        // (Drag is handled by GridCellInteract — this click path is a convenience fallback.)
        if (GridManager.Instance.IsTurnCell(x, y))
        {
            var removed = GridManager.Instance.RemoveTurnTile(x, y);
            if (removed != null)
            {
                TileHandManager.Instance.ReturnTileToHand(removed);
                TileHandUI.Instance.RefreshHand();
                UpdateCellVisual(x, y);
                RefreshPreview();
            }
        }
    }

    public void RefreshPreview()
    {
        if (scorePreviewText == null) return;

        var words        = GridManager.Instance?.GetAllWords();
        int currentScore = RunManager.Instance?.score ?? 0;
        int target       = RunManager.Instance?.GetCurrentBlindTarget() ?? 0;

        if (words == null || words.Count == 0)
        {
            scorePreviewText.text =
                "<color=#888888>Place tiles\nto see scoring</color>\n\n" +
                $"Score: {currentScore} / {target}";
            return;
        }

        BossModifier boss = RunManager.Instance != null && RunManager.Instance.IsBossBlind
            ? RunManager.Instance.GetBossModifier()
            : BossModifier.None;

        var validWords   = new List<Word>();
        var invalidNames = new List<string>();
        var boardTexts   = new HashSet<string>();

        foreach (var word in words)
        {
            bool valid = WordValidator.Instance != null && WordValidator.Instance.IsValid(word.text);
            if (boss == BossModifier.NoShortWords    && word.text.Length == 3)         valid = false;
            if (boss == BossModifier.RareTilesLocked && ContainsRareLetter(word.text)) valid = false;

            if (valid) { validWords.Add(word); boardTexts.Add(word.text); }
            else         invalidNames.Add(word.text.ToUpper());
        }

        // Words committed in earlier plays this round that are no longer on the board
        // because they were extended (e.g. CAT → CATS). These will be re-scored at
        // end-of-round, so show them here too.
        var prevWords  = new List<Word>();
        var prevAdded  = new HashSet<string>();
        var roundPlayed = GameManager.Instance?.RoundPlayedWords;
        if (roundPlayed != null)
        {
            foreach (var w in roundPlayed)
            {
                if (boardTexts.Contains(w.text)) continue;               // still on board — shown above
                if (WordValidator.Instance == null || !WordValidator.Instance.IsValid(w.text)) continue;
                if (boss == BossModifier.NoShortWords    && w.text.Length == 3)         continue;
                if (boss == BossModifier.RareTilesLocked && ContainsRareLetter(w.text)) continue;
                if (prevAdded.Add(w.text)) prevWords.Add(w);             // deduplicate by text
            }
        }

        // Score previously-extended words first, then current board words
        var allToScore = new List<Word>(prevWords);
        allToScore.AddRange(validWords);

        var sb           = new System.Text.StringBuilder();
        int previewTotal = 0;

        if (allToScore.Count > 0 && ScoreManager.Instance != null && RunManager.Instance != null)
        {
            var result = ScoreManager.Instance.CalculateScore(
                allToScore, RunManager.Instance.activeLexicon, boss);
            previewTotal = result.totalScore;

            foreach (var wr in result.wordResults)
            {
                bool isPrev = prevAdded.Contains(wr.word);
                string col  = isPrev ? "#88ccff" : "#44ff44";   // blue = prev play, green = this play
                string tag  = isPrev ? " <color=#666666>(prev)</color>" : "";
                sb.AppendLine($"<color={col}>✓ {wr.word.ToUpper()}</color>{tag}");
                sb.AppendLine($"  <color=#ffcc44>{wr.chips} × {wr.multiplier:F1} = {wr.score}</color>");
                foreach (var bonus in wr.bonusLabels)
                    sb.AppendLine($"  <color=#aaaaaa>{bonus}</color>");
            }
        }

        // Invalid words
        foreach (var name in invalidNames)
            sb.AppendLine($"<color=#ff5555>✗ {name}</color>");

        // Summary line
        int    projected = currentScore + previewTotal;
        string progColor = projected >= target ? "#44ff44" : "#ffcc44";
        sb.AppendLine();
        sb.AppendLine($"<b>Preview: +{previewTotal}</b>");
        sb.Append($"<color={progColor}><b>{projected} / {target}</b></color>");

        scorePreviewText.text = sb.ToString();

        // Force layout rebuild so ContentSizeFitter expands the scroll content immediately
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            scorePreviewText.GetComponent<RectTransform>());
    }

    private static bool ContainsRareLetter(string word)
    {
        foreach (char c in word)
            if (LetterData.IsRareLetter(c)) return true;
        return false;
    }

    public void FlashCell(int x, int y, Color flashColor)
    {
        var obj = cellObjects?[x, y];
        if (obj == null) return;
        var img = obj.GetComponent<Image>();
        if (img != null)
            StartCoroutine(FlashCoroutine(img, flashColor));
    }

    private System.Collections.IEnumerator FlashCoroutine(Image img, Color flash)
    {
        Color original = img.color;
        img.color = flash;
        yield return new WaitForSeconds(0.25f);
        img.color = original;
    }

    /// <summary>Returns the world/screen position of the centre of a grid cell.
    /// For a ScreenSpaceOverlay canvas, world position == screen position.</summary>
    public Vector3 GetCellWorldPosition(int x, int y)
    {
        if (cellObjects == null || x < 0 || x >= GridManager.GridSize
                                || y < 0 || y >= GridManager.GridSize) return Vector3.zero;
        var obj = cellObjects[x, y];
        return obj != null ? obj.transform.position : Vector3.zero;
    }
}
