// CreateLetterDataAssets.cs — Editor utility to auto-generate A–Z LetterData assets.
// Run from Unity menu: Crossword > Create Letter Assets
// Assets are saved to Assets/Resources/Letters/Letter_X.asset
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateLetterDataAssets
{
    // letter → (chipValue, weight, isRare)
    // Chip values mirror Scrabble point values; weights approximate English letter frequency.
    private static readonly (char, int, float, bool)[] LetterConfig =
    {
        ('A',  1, 0.90f, false),
        ('B',  3, 0.20f, false),
        ('C',  3, 0.20f, false),
        ('D',  2, 0.40f, false),
        ('E',  1, 1.20f, false),
        ('F',  4, 0.20f, false),
        ('G',  2, 0.30f, false),
        ('H',  4, 0.20f, false),
        ('I',  1, 0.90f, false),
        ('J',  8, 0.10f, true ),  // Rare
        ('K',  5, 0.10f, false),
        ('L',  1, 0.40f, false),
        ('M',  3, 0.20f, false),
        ('N',  1, 0.60f, false),
        ('O',  1, 0.80f, false),
        ('P',  3, 0.20f, false),
        ('Q', 10, 0.10f, true ),  // Rare
        ('R',  1, 0.60f, false),
        ('S',  1, 0.40f, false),
        ('T',  1, 0.60f, false),
        ('U',  1, 0.40f, false),
        ('V',  4, 0.20f, false),
        ('W',  4, 0.20f, false),
        ('X',  8, 0.10f, true ),  // Rare
        ('Y',  4, 0.20f, false),
        ('Z', 10, 0.10f, true ),  // Rare
    };

    [MenuItem("Crossword/Create Letter Assets")]
    public static void CreateAll()
    {
        const string folder = "Assets/Resources/Letters";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Letters");

        int created = 0;
        foreach (var (letter, chips, weight, isRare) in LetterConfig)
        {
            string path = $"{folder}/Letter_{letter}.asset";

            // Skip if already exists
            var existing = AssetDatabase.LoadAssetAtPath<LetterData>(path);
            if (existing != null)
            {
                Debug.Log($"[CreateLetterDataAssets] Skipping {letter} — already exists.");
                continue;
            }

            var data        = ScriptableObject.CreateInstance<LetterData>();
            data.letter     = char.ToLower(letter);
            data.chipValue  = chips;
            data.weight     = weight;
            data.isRare     = isRare;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateLetterDataAssets] Done — created {created} new LetterData assets.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog(
                "Letter Assets",
                $"Created {created} new LetterData assets in {folder}.\nExisting assets were not overwritten.",
                "OK");
    }

    [MenuItem("Crossword/Create Lexicon Assets")]
    public static void CreateLexiconAssets()
    {
        const string folder = "Assets/Resources/Lexicon";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Lexicon");

        // (filename, displayName, flavorText, effectDescription, effectType, shopCost)
        var entries = new (string, string, string, string, LexiconEffectType, int)[]
        {
            ("Hapax_Legomenon",
             "Hapax Legomenon",
             "A word that appears only once in all of recorded literature.",
             "The rarest letter in your hand scores ×4 chips.",
             LexiconEffectType.HapaxLegomenon, 6),

            ("Portmanteau",
             "Portmanteau",
             "Two words crammed into one — like 'brunch' or 'smog'.",
             "Any word that shares tiles with 2+ other words scores ×2 mult.",
             LexiconEffectType.Portmanteau, 7),

            ("Loanword",
             "Loanword",
             "Borrowed from another language, Q and all.",
             "Words containing Q not followed by U give +3 Mult.",
             LexiconEffectType.Loanword, 5),

            ("Palindrome",
             "Palindrome",
             "The same forwards as backwards — 'racecar', 'level', 'noon'.",
             "Words that read the same forwards and backwards score ×5 Mult.",
             LexiconEffectType.Palindrome, 8),

            ("Pangram",
             "Pangram",
             "The quick brown fox jumps over the lazy dog.",
             "If all 7 hand tiles are placed this round: +50 flat chips.",
             LexiconEffectType.Pangram, 6),

            ("Neologism",
             "Neologism",
             "New words bubble up. Keep coining.",
             "Each unique word you've scored this run adds +0.1 Mult (stacks).",
             LexiconEffectType.Neologism, 7),

            ("Anagram",
             "Anagram",
             "The same letters, rearranged. SILENT = LISTEN.",
             "Placing a word that uses the exact same letters as a previous word this round: ×2 Mult.",
             LexiconEffectType.Anagram, 5),

            ("Oxymoron",
             "Oxymoron",
             "A self-contradicting pair — like 'deafening silence'.",
             "Words with Q and a common vowel in the same word: +2 Mult.",
             LexiconEffectType.Oxymoron, 4),

            ("Sesquipedalian",
             "Sesquipedalian",
             "Given to using long words. Ironically, also a long word.",
             "Words of 9+ letters score an additional +25 flat chips.",
             LexiconEffectType.Sesquipedalian, 5),

            ("The_Glossary",
             "The Glossary",
             "Every round, one letter gets its moment to shine.",
             "Each round a random letter is 'featured' — it scores double chips.",
             LexiconEffectType.TheGlossary, 6),
        };

        int created = 0;
        foreach (var (filename, display, flavor, effect, type, cost) in entries)
        {
            string path = $"{folder}/{filename}.asset";
            if (AssetDatabase.LoadAssetAtPath<LexiconWordData>(path) != null)
            {
                Debug.Log($"[CreateLexiconAssets] Skipping {display} — already exists.");
                continue;
            }

            var data                = ScriptableObject.CreateInstance<LexiconWordData>();
            data.displayName        = display;
            data.flavorText         = flavor;
            data.effectDescription  = effect;
            data.effectType         = type;
            data.shopCost           = cost;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateLexiconAssets] Done — created {created} Lexicon assets.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog(
                "Lexicon Assets",
                $"Created {created} new LexiconWordData assets in {folder}.",
                "OK");
    }

    [MenuItem("Crossword/Create All Blind Assets")]
    public static void CreateAllBlindAssets()
    {
        const string folder = "Assets/Resources/Blinds";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Blinds");

        // All 8 antes × 3 blinds = 24 assets.
        // Targets roughly double every 2 antes; boss modifiers cycle through all 5 types.
        // (filename, ante, blind, target, gold, modifier, modDescription, blindDescription)
        var blinds = new (string, int, int, int, int, BossModifier, string, string)[]
        {
            // Ante 1
            ("Ante1_Blind0", 1,0,   30, 3, BossModifier.None,             "", "Small Blind — learn the ropes."),
            ("Ante1_Blind1", 1,1,   60, 4, BossModifier.None,             "", "Big Blind — push harder."),
            ("Ante1_Blind2", 1,2,   90, 5, BossModifier.NoShortWords,     "Exam:Short words (3 letters) don't count.", "Exam."),
            // Ante 2
            ("Ante2_Blind0", 2,0,  180, 4, BossModifier.None,             "", "Small Blind."),
            ("Ante2_Blind1", 2,1,  300, 5, BossModifier.None,             "", "Big Blind."),
            ("Ante2_Blind2", 2,2,  450, 6, BossModifier.VowelsWorthZero,  "Exam:Vowels are worth 0 chips.", "Exam."),
            // Ante 3
            ("Ante3_Blind0", 3,0,  650, 5, BossModifier.None,             "", "Small Blind."),
            ("Ante3_Blind1", 3,1,  950, 6, BossModifier.None,             "", "Big Blind."),
            ("Ante3_Blind2", 3,2, 1300, 7, BossModifier.NoIntersections,  "Exam:Intersection bonuses disabled.", "Exam."),
            // Ante 4
            ("Ante4_Blind0", 4,0, 1800, 6, BossModifier.None,             "", "Small Blind."),
            ("Ante4_Blind1", 4,1, 2600, 7, BossModifier.None,             "", "Big Blind."),
            ("Ante4_Blind2", 4,2, 3600, 8, BossModifier.DoubleRedrawCost, "Exam:Each redraw costs 2 charges.", "Exam."),
            // Ante 5
            ("Ante5_Blind0", 5,0, 4800, 7, BossModifier.None,             "", "Small Blind."),
            ("Ante5_Blind1", 5,1, 7200, 8, BossModifier.None,             "", "Big Blind."),
            ("Ante5_Blind2", 5,2,10000, 9, BossModifier.RareTilesLocked,  "Exam:Rare letters (Q/Z/X/J) cannot be placed.", "Exam."),
            // Ante 6
            ("Ante6_Blind0", 6,0,13000, 8, BossModifier.None,             "", "Small Blind."),
            ("Ante6_Blind1", 6,1,19000, 9, BossModifier.None,             "", "Big Blind."),
            ("Ante6_Blind2", 6,2,26000,10, BossModifier.NoShortWords,     "Exam:Short words (3 letters) don't count.", "Exam."),
            // Ante 7
            ("Ante7_Blind0", 7,0,35000, 9, BossModifier.None,             "", "Small Blind."),
            ("Ante7_Blind1", 7,1,52000,10, BossModifier.None,             "", "Big Blind."),
            ("Ante7_Blind2", 7,2,70000,11, BossModifier.VowelsWorthZero,  "Exam:Vowels are worth 0 chips.", "Exam."),
            // Ante 8
            ("Ante8_Blind0", 8,0, 90000,10, BossModifier.None,            "", "Small Blind — the final stretch."),
            ("Ante8_Blind1", 8,1,135000,11, BossModifier.None,            "", "Big Blind — almost there."),
            ("Ante8_Blind2", 8,2,180000,12, BossModifier.NoIntersections, "Exam:Intersection bonuses disabled.", "Final Exam."),
        };

        int created = 0;
        foreach (var (fn, ante, blind, target, gold, mod, modDesc, blindDesc) in blinds)
        {
            string path = $"{folder}/{fn}.asset";
            if (AssetDatabase.LoadAssetAtPath<BlindData>(path) != null) continue;

            var data                 = ScriptableObject.CreateInstance<BlindData>();
            data.ante                = ante;
            data.blind               = blind;
            data.targetScore         = target;
            data.goldReward          = gold;
            data.bossModifier        = mod;
            data.modifierDescription = modDesc;
            data.blindDescription    = blindDesc;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateBlindAssets] Created {created} BlindData assets (all 8 antes).");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Blind Assets",
                $"Created {created} new BlindData assets in {folder}.\nExisting assets were not overwritten.", "OK");
    }

    [MenuItem("Crossword/Create Blind Assets (Ante 1)")]
    public static void CreateBlindAssetsAnte1()
    {
        const string folder = "Assets/Resources/Blinds";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Blinds");

        // (filename, ante, blind, target, gold, modifier, description)
        var blinds = new (string, int, int, int, int, BossModifier, string)[]
        {
            ("Ante1_Blind0", 1, 0,  30,  3, BossModifier.None,           "Small Blind — learn the ropes."),
            ("Ante1_Blind1", 1, 1,  60,  4, BossModifier.None,           "Big Blind — push harder."),
            ("Ante1_Blind2", 1, 2,  90,  5, BossModifier.NoShortWords,   "Exam:Short words (3 letters) don't count."),

            ("Ante2_Blind0", 2, 0, 180,  4, BossModifier.None,           "Small Blind."),
            ("Ante2_Blind1", 2, 1, 300,  5, BossModifier.None,           "Big Blind."),
            ("Ante2_Blind2", 2, 2, 450,  6, BossModifier.VowelsWorthZero,"Exam:Vowels are worth 0 chips."),

            ("Ante3_Blind0", 3, 0, 650,  5, BossModifier.None,           "Small Blind."),
            ("Ante3_Blind1", 3, 1, 950,  6, BossModifier.None,           "Big Blind."),
            ("Ante3_Blind2", 3, 2,1300,  7, BossModifier.NoIntersections,"Exam:Intersection bonuses disabled."),
        };

        int created = 0;
        foreach (var (fn, ante, blind, target, gold, mod, desc) in blinds)
        {
            string path = $"{folder}/{fn}.asset";
            if (AssetDatabase.LoadAssetAtPath<BlindData>(path) != null) continue;

            var data                  = ScriptableObject.CreateInstance<BlindData>();
            data.ante                 = ante;
            data.blind                = blind;
            data.targetScore          = target;
            data.goldReward           = gold;
            data.bossModifier         = mod;
            data.blindDescription     = desc;
            if (mod != BossModifier.None)
                data.modifierDescription = desc;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateBlindAssets] Created {created} BlindData assets.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Blind Assets", $"Created {created} BlindData assets.", "OK");
    }
}
#endif
