#if UNITY_EDITOR
// CreateLexiconAssets.cs — Creates the three new lexicon card assets that were
// previously hard-coded base mechanics (length bonus, rare letter, intersection).
// Menu: Crossword → Create New Lexicon Assets
using UnityEngine;
using UnityEditor;

public static class CreateLexiconAssets
{
    [MenuItem("Crossword/Create Lexicon Assets (Set 2 — Extended 9)")]
    public static void CreateNewLexiconAssets()
    {
        // Set 2 — original 9 cards
        Create("Assets/Resources/Lexicon/Polysyllabic.asset",
            displayName:    "Polysyllabic",
            effect:         "5+ letters: +0.5×\n7+ letters: +1.0×\n10+ letters: +2.0×",
            flavor:         "The longer the word, the more it commands.",
            effectType:     LexiconEffectType.Polysyllabic,
            shopCost:       6,
            discoveryTier:  3);

        Create("Assets/Resources/Lexicon/Rare_Letter.asset",
            displayName:    "Rare Letter",
            effect:         "Q, Z, X, and J each add +0.5× multiplier.",
            flavor:         "Uncommon letters carry uncommon power.",
            effectType:     LexiconEffectType.RareLetter,
            shopCost:       5,
            discoveryTier:  2);

        Create("Assets/Resources/Lexicon/Confluence.asset",
            displayName:    "Confluence",
            effect:         "Tiles shared between crossing words score double chips.",
            flavor:         "Where words meet, power compounds.",
            effectType:     LexiconEffectType.Confluence,
            shopCost:       7,
            discoveryTier:  3);

        Create("Assets/Resources/Lexicon/Epigram.asset",
            displayName:    "Epigram",
            effect:         "3-letter words: +2× Mult\n4-letter words: +1.5× Mult",
            flavor:         "Brevity amplified.",
            effectType:     LexiconEffectType.Epigram,
            shopCost:       5,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Verbosity.asset",
            displayName:    "Verbosity",
            effect:         "Each word scores +5 chips per letter.",
            flavor:         "Words multiply. So do words about words.",
            effectType:     LexiconEffectType.Verbosity,
            shopCost:       5,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Gemination.asset",
            displayName:    "Gemination",
            effect:         "Each adjacent repeated letter pair (LL, SS, TT, etc.) adds +0.8× Mult.",
            flavor:         "The stutter becomes strength.",
            effectType:     LexiconEffectType.Gemination,
            shopCost:       6,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Syllabary.asset",
            displayName:    "Syllabary",
            effect:         "Words with 3 or more distinct vowels score +1.5× Mult.",
            flavor:         "Vowels carry the voice.",
            effectType:     LexiconEffectType.Syllabary,
            shopCost:       6,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Acrostic.asset",
            displayName:    "Acrostic",
            effect:         "Words starting or ending at the grid edge score +25 chips.",
            flavor:         "The margins hold power.",
            effectType:     LexiconEffectType.Acrostic,
            shopCost:       6,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Lexeme.asset",
            displayName:    "Lexeme",
            effect:         "Each valid word beyond the first adds +0.3× Mult to all words this round (max +2×).",
            flavor:         "A denser board earns a denser reward.",
            effectType:     LexiconEffectType.Lexeme,
            shopCost:       7,
            discoveryTier:  3);

        // Expansion set — 8 new cards
        Create("Assets/Resources/Lexicon/Isogram.asset",
            displayName:    "Isogram",
            effect:         "Words with no repeated letters score +2× Mult.",
            flavor:         "Every letter earns its place.",
            effectType:     LexiconEffectType.Isogram,
            shopCost:       5,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Epanalepsis.asset",
            displayName:    "Epanalepsis",
            effect:         "Words that start and end with the same letter score +1.5× Mult.",
            flavor:         "The end echoes the beginning.",
            effectType:     LexiconEffectType.Epanalepsis,
            shopCost:       5,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Chiasmus.asset",
            displayName:    "Chiasmus",
            effect:         "When a turn contains at least one horizontal AND one vertical word, each word scores +20 chips.",
            flavor:         "The crossing is the point.",
            effectType:     LexiconEffectType.Chiasmus,
            shopCost:       6,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Appendage.asset",
            displayName:    "Appendage",
            effect:         "Short words (3 letters or fewer) that cross another word score +30 chips.",
            flavor:         "A small word in a busy grid is worth twice its letters.",
            effectType:     LexiconEffectType.Appendage,
            shopCost:       5,
            discoveryTier:  1);

        Create("Assets/Resources/Lexicon/Litotes.asset",
            displayName:    "Litotes",
            effect:         "Words with at most 1 distinct vowel score +2× Mult.",
            flavor:         "Not a bad word.",
            effectType:     LexiconEffectType.Litotes,
            shopCost:       6,
            discoveryTier:  2);

        Create("Assets/Resources/Lexicon/Polyphony.asset",
            displayName:    "Polyphony",
            effect:         "If any single letter appears 3+ times in a word, score +(occurrences−2)×0.8 Mult.",
            flavor:         "Repetition earns resonance.",
            effectType:     LexiconEffectType.Polyphony,
            shopCost:       6,
            discoveryTier:  2);

        Create("Assets/Resources/Lexicon/Couplet.asset",
            displayName:    "Couplet",
            effect:         "When 2 or more words of the same length are scored in one turn, each scores +25 chips.",
            flavor:         "Two words of one measure.",
            effectType:     LexiconEffectType.Couplet,
            shopCost:       7,
            discoveryTier:  3);

        Create("Assets/Resources/Lexicon/Compendium.asset",
            displayName:    "Compendium",
            effect:         "Each word scores +10 chips per active Lexicon you hold.",
            flavor:         "The more you know, the more every word is worth.",
            effectType:     LexiconEffectType.Compendium,
            shopCost:       8,
            discoveryTier:  3);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateLexiconAssets] Done — 17 lexicon assets created/updated in Assets/Resources/Lexicon/");
    }

    private static void Create(string path, string displayName, string effect,
                                string flavor, LexiconEffectType effectType, int shopCost,
                                int discoveryTier = 1)
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
        asset.discoveryTier     = discoveryTier;

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[CreateLexiconAssets] Created: {path}");
    }
}
#endif
