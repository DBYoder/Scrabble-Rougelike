#if UNITY_EDITOR
// CreateLexiconAssets.cs — Creates the three new lexicon card assets that were
// previously hard-coded base mechanics (length bonus, rare letter, intersection).
// Menu: Crossword → Create New Lexicon Assets
using UnityEngine;
using UnityEditor;

public static class CreateLexiconAssets
{
    [MenuItem("Crossword/Create New Lexicon Assets")]
    public static void CreateNewLexiconAssets()
    {
        Create("Assets/Resources/Lexicon/Polysyllabic.asset",
            displayName:   "Polysyllabic",
            effect:        "5+ letters: +0.5×\n7+ letters: +1.0×\n10+ letters: +2.0×",
            flavor:        "The longer the word, the more it commands.",
            effectType:    LexiconEffectType.Polysyllabic,
            shopCost:      6);

        Create("Assets/Resources/Lexicon/Rare_Letter.asset",
            displayName:   "Rare Letter",
            effect:        "Q, Z, X, and J each add +0.5× multiplier.",
            flavor:        "Uncommon letters carry uncommon power.",
            effectType:    LexiconEffectType.RareLetter,
            shopCost:      5);

        Create("Assets/Resources/Lexicon/Confluence.asset",
            displayName:   "Confluence",
            effect:        "Tiles shared between crossing words score double chips.",
            flavor:        "Where words meet, power compounds.",
            effectType:    LexiconEffectType.Confluence,
            shopCost:      7);

        Create("Assets/Resources/Lexicon/Epigram.asset",
            displayName:   "Epigram",
            effect:        "3-letter words: +2× Mult\n4-letter words: +1.5× Mult",
            flavor:        "Brevity amplified.",
            effectType:    LexiconEffectType.Epigram,
            shopCost:      5);

        Create("Assets/Resources/Lexicon/Verbosity.asset",
            displayName:   "Verbosity",
            effect:        "Each word scores +5 chips per letter.",
            flavor:        "Words multiply. So do words about words.",
            effectType:    LexiconEffectType.Verbosity,
            shopCost:      5);

        Create("Assets/Resources/Lexicon/Gemination.asset",
            displayName:   "Gemination",
            effect:        "Each adjacent repeated letter pair (LL, SS, TT, etc.) adds +0.8× Mult.",
            flavor:        "The stutter becomes strength.",
            effectType:    LexiconEffectType.Gemination,
            shopCost:      6);

        Create("Assets/Resources/Lexicon/Syllabary.asset",
            displayName:   "Syllabary",
            effect:        "Words with 3 or more distinct vowels score +1.5× Mult.",
            flavor:        "Vowels carry the voice.",
            effectType:    LexiconEffectType.Syllabary,
            shopCost:      6);

        Create("Assets/Resources/Lexicon/Acrostic.asset",
            displayName:   "Acrostic",
            effect:        "Words starting or ending at the grid edge score +25 chips.",
            flavor:        "The margins hold power.",
            effectType:    LexiconEffectType.Acrostic,
            shopCost:      6);

        Create("Assets/Resources/Lexicon/Lexeme.asset",
            displayName:   "Lexeme",
            effect:        "Each valid word beyond the first adds +0.3× Mult to all words this round (max +2×).",
            flavor:        "A denser board earns a denser reward.",
            effectType:    LexiconEffectType.Lexeme,
            shopCost:      7);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateLexiconAssets] Done — 3 new lexicon assets created in Assets/Resources/Lexicon/");
    }

    private static void Create(string path, string displayName, string effect,
                                string flavor, LexiconEffectType effectType, int shopCost)
    {
        // Overwrite so re-running always produces a clean asset
        var existing = AssetDatabase.LoadAssetAtPath<LexiconWordData>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        var asset = ScriptableObject.CreateInstance<LexiconWordData>();
        asset.displayName       = displayName;
        asset.effectDescription = effect;
        asset.flavorText        = flavor;
        asset.effectType        = effectType;
        asset.shopCost          = shopCost;

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[CreateLexiconAssets] Created: {path}");
    }
}
#endif
