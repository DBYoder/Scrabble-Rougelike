// LexiconWordData.cs — ScriptableObject for a Lexicon entry (replaces Jokers).
// Create assets: Assets > Create > Crossword > Lexicon Entry
using UnityEngine;

public enum LexiconEffectType
{
    HapaxLegomenon,  // Rarest placed tile scores ×4 chips
    Portmanteau,     // Word sharing tiles with 2+ others scores ×2
    Loanword,        // Q not followed by U → +3 Mult
    Palindrome,      // Palindrome word → ×5 Mult
    Pangram,         // All 7 tiles placed → +50 flat chips
    Neologism,       // Each unique word scored this run adds +0.1 Mult
    Anagram,         // Anagram of a previously scored word → ×2 Mult
    Oxymoron,        // Q + common vowel in same word → +2 Mult
    Sesquipedalian,  // 9+ letter word → +25 flat chips
    TheGlossary,     // Featured random letter scores double chips each round
    Polysyllabic,    // 5+/7+/10+ letter words get +0.5/+1.0/+2.0× multiplier
    RareLetter,      // Q, Z, X, J each add +0.5× multiplier
    Confluence,      // Tiles shared between crossing words score double chips
    Epigram,         // 3-letter words: +2× Mult; 4-letter words: +1.5× Mult
    Verbosity,       // Each word scores +5 chips per letter
    Gemination,      // Each adjacent repeated letter pair (LL, SS, etc.): +0.8× Mult
    Syllabary,       // Words with 3+ distinct vowels score +1.5× Mult
    Acrostic,        // Words starting/ending at grid edge score +25 chips
    Lexeme,          // Each valid word beyond the first adds +0.3× Mult to all (cap +2×)
}

[CreateAssetMenu(fileName = "Lexicon_Hapax", menuName = "Crossword/Lexicon Entry")]
public class LexiconWordData : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    [TextArea] public string flavorText;
    [TextArea] public string effectDescription;
    public Sprite artwork;

    [Header("Effect")]
    public LexiconEffectType effectType;
    public float effectValue; // Generic numeric parameter if needed

    [Header("Shop")]
    public int shopCost = 5;
}
