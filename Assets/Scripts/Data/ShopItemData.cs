// ShopItemData.cs — ScriptableObject for a shop item (non-Lexicon items).
// Lexicon entries are wrapped dynamically by ShopManager; use this for consumables.
// Create assets: Assets > Create > Crossword > Shop Item
using UnityEngine;

public enum ShopItemType
{
    LexiconEntry,    // Grants a Lexicon card (set lexiconRef)
    LetterUpgrade,   // Permanently raises chipValue of a letter (set letterRef + upgradeValue)
    ExtraRedraw,     // Grants +upgradeValue extra redraws for the rest of the run
    ScoreBoost,      // Adds upgradeValue flat points to current blind score
    LifeRestore      // Restores 1 life (up to max)
}

[CreateAssetMenu(fileName = "ShopItem_", menuName = "Crossword/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Display")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Economy")]
    public ShopItemType itemType;
    public int cost = 3;

    [Header("References")]
    public LexiconWordData lexiconRef;   // ShopItemType.LexiconEntry
    public LetterData      letterRef;    // ShopItemType.LetterUpgrade
    public int             upgradeValue; // Multi-purpose numeric field
}
