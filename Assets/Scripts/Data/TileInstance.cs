// TileInstance.cs — Runtime tile: wraps LetterData with per-instance modifiers.
[System.Serializable]
public class TileInstance
{
    public LetterData letterData;
    public int chipBonus;       // Added by shop upgrades
    public bool isFeatured;     // Set by The Glossary lexicon each round

    public char Letter    => letterData != null ? letterData.letter : ' ';
    public int  BaseChips => letterData != null ? letterData.chipValue : 0;
    public int  TotalChips => BaseChips + chipBonus;

    public TileInstance(LetterData data)
    {
        letterData = data;
        chipBonus  = 0;
        isFeatured = false;
    }

    public TileInstance Clone() => new TileInstance(letterData)
    {
        chipBonus  = this.chipBonus,
        isFeatured = this.isFeatured
    };
}
