// Word.cs — Represents a detected word on the grid (used by GridManager / ScoreManager).
using System.Collections.Generic;

public class Word
{
    public string text;                         // lowercase
    public List<(int x, int y)> cellPositions; // grid coordinates of each letter
    public bool isHorizontal;

    public Word(string text, List<(int x, int y)> positions, bool isHorizontal)
    {
        this.text          = text;
        this.cellPositions = positions;
        this.isHorizontal  = isHorizontal;
    }
}
