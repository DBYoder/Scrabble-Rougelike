// GridManager.cs — Owns the 13×13 grid, placement rules, and word detection.
// The board starts as a 9×9 area (outer cells locked) and expands via RunManager.unlockedRadius.
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public const int GridSize = 13;
    public const int CenterX  = 6;
    public const int CenterY  = 6;
    public const int MinWordLength = 2;

    private GridCell[,] grid;
    private bool firstWordPlaced;
    private System.Collections.Generic.HashSet<(int,int)> currentTurnCells
        = new System.Collections.Generic.HashSet<(int,int)>();

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Grid Initialisation ──────────────────────────────────────────────────
    public void InitGrid()
    {
        grid = new GridCell[GridSize, GridSize];
        for (int x = 0; x < GridSize; x++)
            for (int y = 0; y < GridSize; y++)
                grid[x, y] = new GridCell(x, y);
        firstWordPlaced = false;
        currentTurnCells.Clear();
        SetupModifiers();
    }

    private void SetupModifiers()
    {
        // ── Inner 9×9 area (unlocked from the start, unlockedRadius = 4) ─────
        // Triple Word Score — inner corners (the 9×9 corners when radius = 4)
        foreach (var (x, y) in new (int,int)[]
            { (2,2),(2,10),(10,2),(10,10) })
            grid[x, y].modifier = CellModifier.TripleWord;

        // Double Word Score — center star + inner diagonals
        foreach (var (x, y) in new (int,int)[]
            { (6,6),(4,4),(4,8),(8,4),(8,8) })
            grid[x, y].modifier = CellModifier.DoubleWord;

        // Triple Letter Score — cross midpoints of 9×9 area
        foreach (var (x, y) in new (int,int)[]
            { (2,6),(6,2),(10,6),(6,10) })
            grid[x, y].modifier = CellModifier.TripleLetter;

        // Double Letter Score — inner ring of 9×9 area
        foreach (var (x, y) in new (int,int)[]
            { (3,5),(3,7),(5,3),(7,3),(5,9),(7,9),(9,5),(9,7) })
            grid[x, y].modifier = CellModifier.DoubleLetter;

        // ── 11×11 ring (unlocked after Chapter 3 Exam, unlockedRadius = 5) ───
        // Triple Letter Score — 11×11 edge midpoints
        foreach (var (x, y) in new (int,int)[]
            { (1,6),(6,1),(11,6),(6,11) })
            grid[x, y].modifier = CellModifier.TripleLetter;

        // Double Letter Score — 11×11 ring diagonals
        foreach (var (x, y) in new (int,int)[]
            { (1,4),(1,8),(4,1),(8,1),(4,11),(8,11),(11,4),(11,8) })
            grid[x, y].modifier = CellModifier.DoubleLetter;

        // ── 13×13 outermost ring (unlocked after Chapter 6 Exam, radius = 6) ─
        // Triple Word Score — true corners (exciting unlock reward!)
        foreach (var (x, y) in new (int,int)[]
            { (0,0),(0,12),(12,0),(12,12) })
            grid[x, y].modifier = CellModifier.TripleWord;

        // Triple Letter Score — outer edge midpoints
        foreach (var (x, y) in new (int,int)[]
            { (0,6),(6,0),(12,6),(6,12) })
            grid[x, y].modifier = CellModifier.TripleLetter;

        // Double Letter Score — outer ring
        foreach (var (x, y) in new (int,int)[]
            { (0,3),(0,9),(3,0),(9,0),(3,12),(9,12),(12,3),(12,9) })
            grid[x, y].modifier = CellModifier.DoubleLetter;
    }

    // ── Queries ──────────────────────────────────────────────────────────────
    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return null;
        return grid[x, y];
    }

    public bool IsFirstWordPlaced => firstWordPlaced;

    public bool CanPlaceTile(int x, int y, TileInstance tile)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;
        if (grid[x, y].IsOccupied) return false;

        // ── Board lock: outer cells are locked until the board expands ────────
        if (RunManager.Instance != null && !RunManager.Instance.IsCellUnlocked(x, y))
            return false;

        // ── Boss modifier: RareTilesLocked ────────────────────────────────────
        // Q/Z/X/J cannot be placed on this boss blind.
        if (tile != null && tile.letterData != null && tile.letterData.isRare
            && RunManager.Instance != null
            && RunManager.Instance.IsBossBlind
            && RunManager.Instance.GetBossModifier() == BossModifier.RareTilesLocked)
            return false;

        // ── Scrabble direction rule ───────────────────────────────────────────
        // All tiles in a single turn must lie on the same row OR the same column.
        if (currentTurnCells.Count == 0) return true;

        if (currentTurnCells.Count == 1)
        {
            // One tile placed so far — either axis is still open.
            foreach (var (ox, oy) in currentTurnCells)
                return x == ox || y == oy;
        }

        // Two or more tiles already placed — the axis is locked.
        GetTurnDirection(out bool isHorizontal, out int fixedCoord);
        return isHorizontal ? (y == fixedCoord) : (x == fixedCoord);
    }

    /// <summary>
    /// True if the current-turn tiles are legally placed per Scrabble rules:
    ///   1. All tiles lie on the same row or the same column.
    ///   2. The run they define has no empty gaps (every cell between the
    ///      first and last placed tile must already be occupied).
    ///   3. First word: must include the center cell.
    ///      Subsequent words: at least one new tile touches a committed tile.
    /// </summary>
    public bool IsTurnPlacementConnected()
    {
        if (currentTurnCells.Count == 0) return false;

        // ── Rule 1 & 2: single line, no gaps ─────────────────────────────────
        GetTurnDirection(out bool isHorizontal, out int fixedCoord);

        // Collect the varying coordinates and find the span
        int minVar = int.MaxValue, maxVar = int.MinValue;
        foreach (var (cx, cy) in currentTurnCells)
        {
            int v = isHorizontal ? cx : cy;
            if (v < minVar) minVar = v;
            if (v > maxVar) maxVar = v;
        }

        // Every cell in the span must be occupied (by either a turn tile or a
        // committed tile — no empty gaps allowed)
        for (int v = minVar; v <= maxVar; v++)
        {
            int cx = isHorizontal ? v : fixedCoord;
            int cy = isHorizontal ? fixedCoord : v;
            if (!grid[cx, cy].IsOccupied) return false;
        }

        // ── Rule 3: connectivity ──────────────────────────────────────────────
        if (!HasCommittedTiles())
            return currentTurnCells.Contains((CenterX, CenterY));

        int[] dx = {  0,  0,  1, -1 };
        int[] dy = {  1, -1,  0,  0 };
        foreach (var (x, y) in currentTurnCells)
        {
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i], ny = y + dy[i];
                if (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize
                    && grid[nx, ny].IsOccupied
                    && !currentTurnCells.Contains((nx, ny)))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines the axis of the current turn's tile placements.
    /// isHorizontal = true  → all turn tiles share the same row (fixedCoord = Y).
    /// isHorizontal = false → all turn tiles share the same column (fixedCoord = X).
    /// With 0 or 1 turn cells, defaults to horizontal with the single cell's Y (or 0).
    /// </summary>
    private void GetTurnDirection(out bool isHorizontal, out int fixedCoord)
    {
        if (currentTurnCells.Count <= 1)
        {
            foreach (var (cx, cy) in currentTurnCells)
            { isHorizontal = true; fixedCoord = cy; return; }
            isHorizontal = true; fixedCoord = 0;
            return;
        }

        int firstX = -1, firstY = -1;
        bool sameRow = true, sameCol = true;
        bool first = true;
        foreach (var (cx, cy) in currentTurnCells)
        {
            if (first) { firstX = cx; firstY = cy; first = false; }
            else
            {
                if (cy != firstY) sameRow = false;
                if (cx != firstX) sameCol = false;
            }
        }

        if (sameRow)  { isHorizontal = true;  fixedCoord = firstY; return; }
        if (sameCol)  { isHorizontal = false; fixedCoord = firstX; return; }

        // Mixed placement (should never reach here after CanPlaceTile enforcement)
        isHorizontal = true; fixedCoord = firstY;
    }

    private bool HasCommittedTiles()
    {
        for (int x = 0; x < GridSize; x++)
            for (int y = 0; y < GridSize; y++)
                if (grid[x, y].IsOccupied && !currentTurnCells.Contains((x, y)))
                    return true;
        return false;
    }

    // ── Mutation ─────────────────────────────────────────────────────────────
    /// <summary>Returns true if the tile was placed successfully.</summary>
    public bool PlaceTile(int x, int y, TileInstance tile)
    {
        if (!CanPlaceTile(x, y, tile)) return false;
        grid[x, y].placedTile = tile;
        firstWordPlaced = true;
        currentTurnCells.Add((x, y));
        return true;
    }

    public bool HasCurrentTurnPlacements => currentTurnCells.Count > 0;
    public bool IsTurnCell(int x, int y) => currentTurnCells.Contains((x, y));

    /// <summary>Removes a single unsubmitted tile from the grid and returns it.</summary>
    public TileInstance RemoveTurnTile(int x, int y)
    {
        if (!currentTurnCells.Contains((x, y))) return null;
        var tile = grid[x, y].placedTile;
        grid[x, y].placedTile = null;
        currentTurnCells.Remove((x, y));
        if (!HasAnyTilePlaced()) firstWordPlaced = false;
        return tile;
    }

    /// <summary>Locks in the current turn — called after a valid play.</summary>
    public void CommitTurn() => currentTurnCells.Clear();

    /// <summary>Returns all tiles placed this turn to the caller and removes them from the grid.</summary>
    public List<TileInstance> RollbackTurn()
    {
        var returned = new List<TileInstance>();
        foreach (var (x, y) in currentTurnCells)
        {
            if (grid[x, y].placedTile != null)
            {
                returned.Add(grid[x, y].placedTile);
                grid[x, y].placedTile = null;
            }
        }
        currentTurnCells.Clear();
        if (!HasAnyTilePlaced()) firstWordPlaced = false;
        return returned;
    }

    public void RemoveTile(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return;
        var cell = grid[x, y];
        cell.placedTile = null;
    }

    public void ClearGrid() => InitGrid();

    // ── Word Detection ────────────────────────────────────────────────────────
    /// <summary>
    /// Scans all rows and columns for runs of 2+ placed letters.
    /// Returns all runs with length ≥ MinWordLength as Word objects (lowercase).
    /// Only considers unlocked cells.
    /// </summary>
    public List<Word> GetAllWords()
    {
        var words = new List<Word>();

        // Horizontal runs
        for (int y = 0; y < GridSize; y++)
        {
            int x = 0;
            while (x < GridSize)
            {
                if (grid[x, y].IsOccupied)
                {
                    int start = x;
                    var text = new System.Text.StringBuilder();
                    var positions = new List<(int, int)>();
                    while (x < GridSize && grid[x, y].IsOccupied)
                    {
                        text.Append(grid[x, y].placedTile.Letter);
                        positions.Add((x, y));
                        x++;
                    }
                    if (text.Length >= MinWordLength)
                        words.Add(new Word(text.ToString().ToLower(), positions, isHorizontal: true));
                }
                else x++;
            }
        }

        // Vertical runs — scan top-to-bottom (high y → low y, since y=0 is visual bottom)
        for (int x = 0; x < GridSize; x++)
        {
            int y = GridSize - 1;
            while (y >= 0)
            {
                if (grid[x, y].IsOccupied)
                {
                    var text = new System.Text.StringBuilder();
                    var positions = new List<(int, int)>();
                    while (y >= 0 && grid[x, y].IsOccupied)
                    {
                        text.Append(grid[x, y].placedTile.Letter);
                        positions.Add((x, y));
                        y--;
                    }
                    if (text.Length >= MinWordLength)
                        words.Add(new Word(text.ToString().ToLower(), positions, isHorizontal: false));
                }
                else y--;
            }
        }

        return words;
    }

    /// <summary>Returns true if the grid has at least one placed tile.</summary>
    public bool HasAnyTilePlaced()
    {
        for (int x = 0; x < GridSize; x++)
            for (int y = 0; y < GridSize; y++)
                if (grid[x, y].IsOccupied) return true;
        return false;
    }
}
