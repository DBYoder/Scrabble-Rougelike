// CreateRunConfigAssets.cs — Editor utility to generate the three RunConfig ScriptableObject assets.
// Run from Unity menu: Crossword > Create Run Config Assets (skip existing)
//                      Crossword > Force Regenerate Run Config Assets (overwrite)
// Assets are saved to Assets/Resources/RunConfigs/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateRunConfigAssets
{
    [MenuItem("Crossword/Create Run Config Assets")]
    public static void CreateAll() => CreateOrUpdate(overwrite: false);

    [MenuItem("Crossword/Force Regenerate Run Config Assets")]
    public static void ForceRegenerateAll() => CreateOrUpdate(overwrite: true);

    private static void CreateOrUpdate(bool overwrite)
    {
        const string folder = "Assets/Resources/RunConfigs";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "RunConfigs");

        int saved = 0;
        // Base modes — always available
        saved += Upsert(folder, "RunConfig_Standard",    BuildStandard(),   overwrite);
        saved += Upsert(folder, "RunConfig_QuickRun",    BuildQuickRun(),   overwrite);
        saved += Upsert(folder, "RunConfig_Infinite",    BuildInfinite(),   overwrite);
        // Scholar profiles — unlocked via MetaProgressionManager
        saved += Upsert(folder, "RunConfig_Archivist",   BuildArchivist(),  overwrite);
        saved += Upsert(folder, "RunConfig_Philologist", BuildPhilologist(), overwrite);
        saved += Upsert(folder, "RunConfig_Cryptographer", BuildCryptographer(), overwrite);
        saved += Upsert(folder, "RunConfig_Etymologist", BuildEtymologist(), overwrite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string verb = overwrite ? "updated" : "created";
        Debug.Log($"[CreateRunConfigAssets] Done — {verb} {saved} RunConfig assets.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog(
                "Run Config Assets",
                $"{verb.Substring(0,1).ToUpper()}{verb.Substring(1)} {saved} RunConfig assets in {folder}.",
                "OK");
    }

    private static int Upsert(string folder, string filename, RunConfig cfg, bool overwrite)
    {
        string path = $"{folder}/{filename}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<RunConfig>(path);
        if (existing != null)
        {
            if (!overwrite)
            {
                Debug.Log($"[CreateRunConfigAssets] Skipping {filename} — already exists.");
                return 0;
            }
            // Copy values onto the existing asset so GUIDs/references are preserved
            existing.modeName          = cfg.modeName;
            existing.modeDescription   = cfg.modeDescription;
            existing.modeAccentColor   = cfg.modeAccentColor;
            existing.startingLives     = cfg.startingLives;
            existing.startingGold      = cfg.startingGold;
            existing.startingHandSize  = cfg.startingHandSize;
            existing.startingRadius    = cfg.startingRadius;
            existing.maxLexiconSlots   = cfg.maxLexiconSlots;
            existing.totalAntes        = cfg.totalAntes;
            existing.scoreMultiplier   = cfg.scoreMultiplier;
            existing.boardExpandAtExams = cfg.boardExpandAtExams;
            existing.noShop                  = cfg.noShop;
            existing.noUpgrades              = cfg.noUpgrades;
            existing.infiniteMode            = cfg.infiniteMode;
            existing.starterLexicon          = cfg.starterLexicon;
            existing.hasStartingLexicon      = cfg.hasStartingLexicon;
            existing.startingLexiconEffect   = cfg.startingLexiconEffect;
            existing.unlockKey               = cfg.unlockKey;
            existing.unlockHint              = cfg.unlockHint;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(cfg); // discard the temp instance
            Debug.Log($"[CreateRunConfigAssets] Updated {filename}.");
            return 1;
        }
        AssetDatabase.CreateAsset(cfg, path);
        Debug.Log($"[CreateRunConfigAssets] Created {filename}.");
        return 1;
    }

    // ── Mode Definitions ──────────────────────────────────────────────────────
    // Standard: 6 chapters (~25 min). Board expands fast (Exams 1 & 2).
    // Quick Run: 3 chapters (~15 min). Board expands after Exam 1 only, easier targets.
    // Infinite: endless run, same fast expansion as Standard.

    private static RunConfig BuildStandard()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName           = "Standard";
        cfg.modeDescription    = "The full roguelike experience. 6 chapters, full economy, all Lexicon effects.";
        cfg.modeAccentColor    = new Color(0.278f, 0.435f, 0.302f); // forest green
        cfg.startingLives      = 3;
        cfg.startingGold       = 0;
        cfg.startingHandSize   = 7;
        cfg.startingRadius     = 4;   // 9×9 start
        cfg.maxLexiconSlots    = 5;
        cfg.totalAntes         = 6;
        cfg.scoreMultiplier    = 1f;
        cfg.boardExpandAtExams = new[] { 1, 2 }; // 11×11 after Exam 1, 13×13 after Exam 2
        cfg.noShop             = false;
        cfg.noUpgrades         = false;
        cfg.infiniteMode       = false;
        cfg.starterLexicon     = true;
        return cfg;
    }

    private static RunConfig BuildQuickRun()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName           = "Quick Run";
        cfg.modeDescription    = "3 chapters, ~15 minutes. Easier targets, no free Lexicon picks — buy everything from the Shop. Board expands to full 13×13.";
        cfg.modeAccentColor    = new Color(0.420f, 0.439f, 0.278f); // olive
        cfg.startingLives      = 3;
        cfg.startingGold       = 4;
        cfg.startingHandSize   = 7;
        cfg.startingRadius     = 4;
        cfg.maxLexiconSlots    = 5;
        cfg.totalAntes         = 3;
        cfg.scoreMultiplier    = 2f;  // halves all blind targets
        cfg.boardExpandAtExams = new[] { 1, 2 }; // 11×11 after Exam 1, 13×13 after Exam 2
        cfg.noShop             = false;
        cfg.noUpgrades         = true;
        cfg.infiniteMode       = false;
        cfg.starterLexicon     = false;
        return cfg;
    }

    private static RunConfig BuildInfinite()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName           = "Infinite";
        cfg.modeDescription    = "There is no end. Blinds escalate forever — how far can you go?";
        cfg.modeAccentColor    = new Color(0.557f, 0.271f, 0.678f); // purple
        cfg.startingLives      = 3;
        cfg.startingGold       = 0;
        cfg.startingHandSize   = 7;
        cfg.startingRadius     = 4;
        cfg.maxLexiconSlots    = 5;
        cfg.totalAntes         = int.MaxValue;
        cfg.scoreMultiplier    = 1f;
        cfg.boardExpandAtExams = new[] { 1, 2 }; // same fast expansion as Standard
        cfg.noShop             = false;
        cfg.noUpgrades         = false;
        cfg.infiniteMode       = true;
        cfg.starterLexicon     = true;
        return cfg;
    }

    // ── Scholar Profiles (locked by default) ──────────────────────────────────

    private static RunConfig BuildArchivist()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName           = "The Archivist";
        cfg.modeDescription    = "Begins with extra gold but can hold fewer Lexicons. Master the economy or drown in it.";
        cfg.modeAccentColor    = new Color(0.502f, 0.400f, 0.251f); // parchment brown
        cfg.startingLives      = 3;
        cfg.startingGold       = 2;
        cfg.startingHandSize   = 7;
        cfg.startingRadius     = 4;
        cfg.maxLexiconSlots    = 4;    // one fewer slot
        cfg.totalAntes         = 6;
        cfg.scoreMultiplier    = 1f;
        cfg.boardExpandAtExams = new[] { 1, 2 };
        cfg.noShop             = false;
        cfg.noUpgrades         = false;
        cfg.infiniteMode       = false;
        cfg.starterLexicon     = true;
        cfg.unlockKey          = "archivist";
        cfg.unlockHint         = "Win a Standard run to unlock.";
        return cfg;
    }

    private static RunConfig BuildPhilologist()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName           = "The Philologist";
        cfg.modeDescription    = "Starts with the Neologism Lexicon already active and a hand of 6 — breadth over depth.";
        cfg.modeAccentColor    = new Color(0.278f, 0.388f, 0.502f); // steel blue
        cfg.startingLives      = 3;
        cfg.startingGold       = 0;
        cfg.startingHandSize   = 6;    // one fewer tile per hand
        cfg.startingRadius     = 4;
        cfg.maxLexiconSlots    = 5;
        cfg.totalAntes         = 6;
        cfg.scoreMultiplier    = 1f;
        cfg.boardExpandAtExams = new[] { 1, 2 };
        cfg.noShop             = false;
        cfg.noUpgrades         = false;
        cfg.infiniteMode       = false;
        cfg.starterLexicon     = false;   // starting lexicon set below, no pick screen
        cfg.hasStartingLexicon      = true;
        cfg.startingLexiconEffect   = LexiconEffectType.Neologism;
        cfg.unlockKey          = "philologist";
        cfg.unlockHint         = "Score 250 unique words across all runs to unlock.";
        return cfg;
    }

    private static RunConfig BuildCryptographer()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName           = "The Cryptographer";
        cfg.modeDescription    = "Wealthy but inflexible — no free Lexicon picks, ever. Starts with 8 gold; buy everything.";
        cfg.modeAccentColor    = new Color(0.251f, 0.400f, 0.427f); // teal
        cfg.startingLives      = 3;
        cfg.startingGold       = 8;
        cfg.startingHandSize   = 7;
        cfg.startingRadius     = 4;
        cfg.maxLexiconSlots    = 5;
        cfg.totalAntes         = 6;
        cfg.scoreMultiplier    = 1f;
        cfg.boardExpandAtExams = new[] { 1, 2 };
        cfg.noShop             = false;
        cfg.noUpgrades         = true;   // no Upgrade screen ever
        cfg.infiniteMode       = false;
        cfg.starterLexicon     = false;
        cfg.unlockKey          = "cryptographer";
        cfg.unlockHint         = "Win a Quick Run without losing a life to unlock.";
        return cfg;
    }

    private static RunConfig BuildEtymologist()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName           = "The Etymologist";
        cfg.modeDescription    = "Starts on a cramped 7×7 grid with a hand of 9. Density is the only strategy.";
        cfg.modeAccentColor    = new Color(0.435f, 0.251f, 0.388f); // plum
        cfg.startingLives      = 3;
        cfg.startingGold       = 0;
        cfg.startingHandSize   = 9;    // two extra tiles
        cfg.startingRadius     = 3;   // 7×7 start (radius 3 = 7×7)
        cfg.maxLexiconSlots    = 5;
        cfg.totalAntes         = 6;
        cfg.scoreMultiplier    = 1f;
        cfg.boardExpandAtExams = new[] { 1, 2 };
        cfg.noShop             = false;
        cfg.noUpgrades         = false;
        cfg.infiniteMode       = false;
        cfg.starterLexicon     = true;
        cfg.unlockKey          = "etymologist";
        cfg.unlockHint         = "Survive to Ante 4 in Infinite mode to unlock.";
        return cfg;
    }
}
#endif
