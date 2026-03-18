// RunConfig.cs — ScriptableObject defining all parameters for a game mode.
// Create assets via Crossword > Create Run Config Assets.
using UnityEngine;

[CreateAssetMenu(fileName = "RunConfig_New", menuName = "Crossword/Run Config")]
public class RunConfig : ScriptableObject
{
    [Header("Identity")]
    public string modeName;
    [TextArea] public string modeDescription;
    public Color modeAccentColor = Color.white;

    [Header("Starting State")]
    public int startingLives    = 3;
    public int startingGold     = 0;
    public int startingHandSize = 7;
    public int startingRadius   = 4;
    public int maxLexiconSlots  = 5;

    [Header("Progression")]
    public int   totalAntes      = 8;
    public float scoreMultiplier = 1f;   // Divides blind targets (values > 1 make targets easier)
    /// <summary>
    /// Which examsCleared counts trigger a board radius expansion (+1 each time).
    /// Standard = { 3, 6 }. Quick Run = { 2, 3 } for accelerated expansion.
    /// </summary>
    public int[] boardExpandAtExams = { 3, 6 };

    [Header("Mode Flags")]
    public bool noShop         = false;  // Skip Shop state; gold economy disabled
    public bool noUpgrades     = false;  // Skip Upgrade state; no free Lexicon picks
    public bool infiniteMode   = false;  // No Victory state; blind targets scale past Ante 8
    public bool starterLexicon = true;   // Show the pre-run starter Lexicon pick
}
