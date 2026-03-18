// GridCell.cs — Plain class representing one cell in the 9×9 crossword grid.
public enum CellModifier { None, DoubleLetter, TripleLetter, DoubleWord, TripleWord }

[System.Serializable]
public class GridCell
{
    public int x;
    public int y;
    public TileInstance placedTile;
    public bool isCenter;
    public CellModifier modifier;

    public bool IsOccupied => placedTile != null;

    /// <summary>
    /// True once this cell's modifier has been consumed during scoring.
    /// Mirrors standard Scrabble: bonus squares only apply the first time
    /// a tile is played on them. Reset automatically via ClearGrid → InitGrid.
    /// </summary>
    public bool modifierUsed;

    public GridCell(int x, int y)
    {
        this.x = x;
        this.y = y;
        isCenter = (x == GridManager.CenterX && y == GridManager.CenterY);
    }
}
