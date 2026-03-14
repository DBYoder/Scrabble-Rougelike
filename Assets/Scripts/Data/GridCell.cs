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

    public GridCell(int x, int y)
    {
        this.x = x;
        this.y = y;
        isCenter = (x == GridManager.CenterX && y == GridManager.CenterY);
    }
}
