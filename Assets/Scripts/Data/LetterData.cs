// LetterData.cs — ScriptableObject asset for a single letter (A–Z).
// Create assets: Assets > Create > Crossword > Letter Data
// Or use the Editor menu: Crossword > Create Letter Assets (auto-generates A–Z).
using UnityEngine;

[CreateAssetMenu(fileName = "Letter_A", menuName = "Crossword/Letter Data")]
public class LetterData : ScriptableObject
{
    [Header("Letter")]
    public char letter;

    [Header("Economy")]
    [Tooltip("Base chip value when this letter appears in a word.")]
    public int chipValue = 1;

    [Tooltip("Relative draw weight. Higher = more copies in the tile bag (≈ Scrabble frequency).")]
    public float weight = 1f;

    [Header("Flags")]
    [Tooltip("True for Q, Z, X, J — triggers rare-letter mult bonus.")]
    public bool isRare;

    /// <summary>Single authoritative check for whether a character is a rare letter.</summary>
    public static bool IsRareLetter(char c)
    {
        c = char.ToLower(c);
        return c == 'q' || c == 'z' || c == 'x' || c == 'j';
    }
}
