// CreateRunConfigAssets.cs — Editor utility to generate the three RunConfig ScriptableObject assets.
// Run from Unity menu: Crossword > Create Run Config Assets
// Assets are saved to Assets/Resources/RunConfigs/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateRunConfigAssets
{
    [MenuItem("Crossword/Create Run Config Assets")]
    public static void CreateAll()
    {
        const string folder = "Assets/Resources/RunConfigs";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "RunConfigs");

        int created = 0;
        created += CreateConfig(folder, "RunConfig_Standard",   BuildStandard());
        created += CreateConfig(folder, "RunConfig_QuickRun",   BuildQuickRun());
        created += CreateConfig(folder, "RunConfig_Infinite",   BuildInfinite());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateRunConfigAssets] Done — created {created} new RunConfig assets.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog(
                "Run Config Assets",
                $"Created {created} new RunConfig assets in {folder}.\nExisting assets were not overwritten.",
                "OK");
    }

    private static int CreateConfig(string folder, string filename, RunConfig cfg)
    {
        string path = $"{folder}/{filename}.asset";
        if (AssetDatabase.LoadAssetAtPath<RunConfig>(path) != null)
        {
            Debug.Log($"[CreateRunConfigAssets] Skipping {filename} — already exists.");
            return 0;
        }
        AssetDatabase.CreateAsset(cfg, path);
        return 1;
    }

    // ── Mode Definitions ──────────────────────────────────────────────────────

    private static RunConfig BuildStandard()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName          = "Standard";
        cfg.modeDescription   = "The full roguelike experience. 8 chapters, full economy, all Lexicon effects.";
        cfg.modeAccentColor   = new Color(0.278f, 0.435f, 0.302f); // forest green
        cfg.startingLives     = 3;
        cfg.startingGold      = 0;
        cfg.startingHandSize  = 7;
        cfg.startingRadius    = 4;
        cfg.maxLexiconSlots   = 5;
        cfg.totalAntes        = 8;
        cfg.scoreMultiplier   = 1f;
        cfg.boardExpandAtExams = new[] { 3, 6 };
        cfg.noShop            = false;
        cfg.noUpgrades        = false;
        cfg.infiniteMode      = false;
        cfg.starterLexicon    = true;
        return cfg;
    }

    private static RunConfig BuildQuickRun()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName          = "Quick Run";
        cfg.modeDescription   = "4 chapters, tighter targets. No free Lexicon picks — buy everything from the Shop. Grid and hand reach max size by the end.";
        cfg.modeAccentColor   = new Color(0.420f, 0.439f, 0.278f); // olive
        cfg.startingLives     = 3;
        cfg.startingGold      = 4;
        cfg.startingHandSize  = 7;
        cfg.startingRadius    = 4;
        cfg.maxLexiconSlots   = 5;
        cfg.totalAntes        = 4;
        cfg.scoreMultiplier   = 1.5f;   // Divides targets — easier per blind, compensating for fewer antes
        cfg.boardExpandAtExams = new[] { 2, 3 }; // Accelerated: board maxes out by Exam 3
        cfg.noShop            = false;
        cfg.noUpgrades        = true;
        cfg.infiniteMode      = false;
        cfg.starterLexicon    = false;
        return cfg;
    }

    private static RunConfig BuildInfinite()
    {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        cfg.modeName          = "Infinite";
        cfg.modeDescription   = "There is no end. Blinds escalate forever — how far can you go?";
        cfg.modeAccentColor   = new Color(0.557f, 0.271f, 0.678f); // purple
        cfg.startingLives     = 3;
        cfg.startingGold      = 0;
        cfg.startingHandSize  = 7;
        cfg.startingRadius    = 4;
        cfg.maxLexiconSlots   = 5;
        cfg.totalAntes        = int.MaxValue;
        cfg.scoreMultiplier   = 1f;
        cfg.boardExpandAtExams = new[] { 3, 6 };
        cfg.noShop            = false;
        cfg.noUpgrades        = false;
        cfg.infiniteMode      = true;
        cfg.starterLexicon    = true;
        return cfg;
    }
}
#endif
