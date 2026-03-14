// BlindData.cs — ScriptableObject for one blind within an ante.
// Naming convention: Ante1_Blind0 (Small), Ante1_Blind1 (Big), Ante1_Blind2 (Boss).
// Create assets: Assets > Create > Crossword > Blind Data
using UnityEngine;

public enum BossModifier
{
    None,
    NoShortWords,       // 3-letter words don't count toward score
    VowelsWorthZero,    // Vowels contribute 0 chips
    NoIntersections,    // Intersection chip-doubling disabled
    DoubleRedrawCost,   // Each redraw costs 2 charges instead of 1
    RareTilesLocked     // Rare letters (Q/Z/X/J) cannot be placed
}

[CreateAssetMenu(fileName = "Ante1_Blind0", menuName = "Crossword/Blind Data")]
public class BlindData : ScriptableObject
{
    [Header("Position")]
    public int ante;
    public int blind; // 0 = Small, 1 = Big, 2 = Boss

    [Header("Scoring")]
    public int targetScore;
    public int goldReward;

    [Header("Boss")]
    public BossModifier bossModifier = BossModifier.None;
    [TextArea] public string modifierDescription;

    [Header("Flavour")]
    [TextArea] public string blindDescription;
}
