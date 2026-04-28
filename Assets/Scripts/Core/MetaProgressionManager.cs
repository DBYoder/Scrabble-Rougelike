// MetaProgressionManager.cs — Persistent cross-run save state.
// Tracks Lexicon tiers, Scholar unlocks, Folios, and Index upgrades.
// Saved to Application.persistentDataPath/meta_progress.json.
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class MetaProgressionData
{
    public int  highestTierUnlocked = 1;    // 1 = only Tier 1; 2 = T1+T2; 3 = all
    public int  totalRunsCompleted  = 0;
    public int  totalWins           = 0;
    public int  totalUniqueWords    = 0;    // lifetime count across all runs

    // ── Folios ────────────────────────────────────────────────────────────────
    public int folios = 0;

    // ── Index upgrades (one-time purchases) ───────────────────────────────────
    public bool upgBibliography  = false;  // shop shows 3 lexicon slots instead of 2
    public bool upgMarginalia    = false;  // start each run with +1 gold
    public bool upgAddendum      = false;  // upgrade screen shows 4 choices instead of 3
    public bool upgAnnotatedHand = false;  // start each run with hand size +1
    public bool upgErrata        = false;  // shop shows 1 extra misc item

    // ── Scholar & Card unlocks ────────────────────────────────────────────────
    public List<string> unlockedScholars    = new List<string>();
    public List<string> unlockedCards       = new List<string>();
    public List<string> completedChallenges = new List<string>();

    // ── Edition system ────────────────────────────────────────────────────────
    // Stores which editions are unlocked per Scholar key (comma-separated int list)
    public List<string> editionUnlockKeys    = new List<string>(); // "archivist:1", "archivist:2", etc.
    // Stores the currently selected edition level per Scholar key
    public List<string> editionSelections    = new List<string>(); // "archivist=2"

    // ── Etymology — per-letter chip bonuses ──────────────────────────────────
    // Index 0='a' … 25='z'. +1 per research level (max +2 per letter).
    public int[] letterResearchLevels = new int[26];

    // ── Lifetime bests (for challenge progress display) ───────────────────────
    public int longestWordEver    = 0;
    public int maxLexiconsInARun  = 0;
}

public class MetaProgressionManager : MonoBehaviour
{
    public static MetaProgressionManager Instance { get; private set; }

    private MetaProgressionData data = new MetaProgressionData();
    private string SavePath => Path.Combine(Application.persistentDataPath, "meta_progress.json");

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── Save / Load ───────────────────────────────────────────────────────────
    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MetaProgression] Save failed: {e.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<MetaProgressionData>(json) ?? new MetaProgressionData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MetaProgression] Load failed, starting fresh: {e.Message}");
            data = new MetaProgressionData();
        }
    }

    // ── Tier Discovery ────────────────────────────────────────────────────────
    public bool IsDiscovered(LexiconEffectType effectType)
    {
        var allLexicon = Resources.LoadAll<LexiconWordData>("Lexicon");
        foreach (var lex in allLexicon)
        {
            if (lex.effectType == effectType)
                return lex.discoveryTier <= data.highestTierUnlocked;
        }
        return true;
    }

    public void UnlockTier(int tier)
    {
        if (tier > data.highestTierUnlocked)
        {
            data.highestTierUnlocked = tier;
            Save();
        }
    }

    // ── Folios ────────────────────────────────────────────────────────────────
    public int Folios => data.folios;

    public void EarnFolios(int amount)
    {
        if (amount <= 0) return;
        data.folios += amount;
        Save();
    }

    // Returns false if not enough Folios or already purchased
    public bool BuyIndexUpgrade(string key)
    {
        int cost = IndexUpgradeCost(key);
        if (cost < 0 || data.folios < cost) return false;
        if (HasIndexUpgrade(key)) return false;
        data.folios -= cost;
        SetIndexUpgrade(key, true);
        Save();
        return true;
    }

    public bool HasIndexUpgrade(string key)
    {
        switch (key)
        {
            case "bibliography":  return data.upgBibliography;
            case "marginalia":    return data.upgMarginalia;
            case "addendum":      return data.upgAddendum;
            case "annotatedhand": return data.upgAnnotatedHand;
            case "errata":        return data.upgErrata;
            default:              return false;
        }
    }

    private void SetIndexUpgrade(string key, bool value)
    {
        switch (key)
        {
            case "bibliography":  data.upgBibliography  = value; break;
            case "marginalia":    data.upgMarginalia    = value; break;
            case "addendum":      data.upgAddendum      = value; break;
            case "annotatedhand": data.upgAnnotatedHand = value; break;
            case "errata":        data.upgErrata        = value; break;
        }
    }

    public static int IndexUpgradeCost(string key)
    {
        switch (key)
        {
            case "bibliography":  return 8;
            case "marginalia":    return 5;
            case "addendum":      return 10;
            case "annotatedhand": return 12;
            case "errata":        return 15;
            default:              return -1;
        }
    }

    // Bonus values applied at run start
    public int  GoldBonus     => data.upgMarginalia    ? 1 : 0;
    public int  HandSizeBonus => data.upgAnnotatedHand ? 1 : 0;
    public int  LexiconSlotBonus => data.upgBibliography ? 1 : 0;
    public int  MiscSlotBonus    => data.upgErrata       ? 1 : 0;
    public int  UpgradeChoiceBonus => data.upgAddendum   ? 1 : 0;

    // ── Scholar Profiles ──────────────────────────────────────────────────────
    public bool IsScholarUnlocked(string unlockKey)
    {
        if (string.IsNullOrEmpty(unlockKey)) return true;
        return data.unlockedScholars.Contains(unlockKey);
    }

    public void UnlockScholar(string key)
    {
        if (!string.IsNullOrEmpty(key) && !data.unlockedScholars.Contains(key))
        {
            data.unlockedScholars.Add(key);
            Save();
        }
    }

    private void CheckScholarUnlocks(RunConfig config, bool won, int livesRemaining)
    {
        if (config == null) return;
        string mode = config.modeName;
        int startLives = config.startingLives;

        if (won && mode == "Standard")
            UnlockScholar("archivist");

        if (won && mode == "Quick Run" && livesRemaining >= startLives)
            UnlockScholar("cryptographer");

        if (config.infiniteMode && RunManager.Instance != null
            && RunManager.Instance.currentAnte >= 4)
            UnlockScholar("etymologist");

        if (data.totalUniqueWords >= 250)
            UnlockScholar("philologist");

        // Edition progression: winning a Scholar at edition N unlocks edition N+1 (max 4)
        if (won && !string.IsNullOrEmpty(config.unlockKey))
        {
            int currentEdition = GetSelectedEdition(config.unlockKey);
            int nextEdition    = currentEdition + 1;
            if (nextEdition <= 4)
                UnlockEdition(config.unlockKey, nextEdition);
        }
    }

    // ── Editions ──────────────────────────────────────────────────────────────
    // Edition 0 = Standard (always available). 1–4 are unlocked by winning
    // the Scholar in the previous edition.
    public static readonly string[] EditionNames =
        { "Standard", "Revised", "Annotated", "Critical", "Definitive" };

    public int MaxUnlockedEdition(string scholarKey)
    {
        int max = 0;
        foreach (var entry in data.editionUnlockKeys)
        {
            int colon = entry.IndexOf(':');
            if (colon < 0) continue;
            if (entry.Substring(0, colon) == scholarKey)
                max = Mathf.Max(max, int.Parse(entry.Substring(colon + 1)));
        }
        return max;
    }

    public void UnlockEdition(string scholarKey, int edition)
    {
        string entry = $"{scholarKey}:{edition}";
        if (!data.editionUnlockKeys.Contains(entry))
        {
            data.editionUnlockKeys.Add(entry);
            Save();
        }
    }

    public int GetSelectedEdition(string scholarKey)
    {
        foreach (var entry in data.editionSelections)
        {
            int eq = entry.IndexOf('=');
            if (eq < 0) continue;
            if (entry.Substring(0, eq) == scholarKey)
                return int.Parse(entry.Substring(eq + 1));
        }
        return 0; // default Standard
    }

    public void SetSelectedEdition(string scholarKey, int edition)
    {
        string prefix = scholarKey + "=";
        data.editionSelections.RemoveAll(e => e.StartsWith(prefix));
        data.editionSelections.Add($"{scholarKey}={edition}");
        Save();
    }

    // Applies edition difficulty modifiers to a runtime copy of the config
    public static RunConfig ApplyEdition(RunConfig source, int edition)
    {
        if (edition == 0) return source;
        var copy = UnityEngine.Object.Instantiate(source);
        if (edition >= 1) copy.scoreMultiplier *= 0.85f;     // Revised: harder targets
        if (edition >= 2) copy.startingLives    = Mathf.Min(copy.startingLives, 2); // Annotated: 2 lives
        if (edition >= 3) copy.startingGold     = Mathf.Max(copy.startingGold - 2, -2); // Critical: less gold
        // Definitive (4) is all three combined — already applied above
        return copy;
    }

    // ── Etymology — Letter Research ───────────────────────────────────────────
    public int GetLetterResearchLevel(char c)
    {
        int idx = char.ToLower(c) - 'a';
        if (idx < 0 || idx >= 26 || data.letterResearchLevels == null) return 0;
        if (data.letterResearchLevels.Length <= idx) return 0;
        return data.letterResearchLevels[idx];
    }

    // Folio cost scales with letter chip value: cheap for common letters, expensive for rare
    public int LetterResearchCost(char c, int currentLevel)
    {
        c = char.ToLower(c);
        // Find base chip value from assets
        var allLetters = Resources.LoadAll<LetterData>("Letters");
        int baseChips = 1;
        foreach (var ld in allLetters)
            if (ld.letter == c) { baseChips = ld.baseChipValue; break; }
        return (baseChips + currentLevel + 1) * 3; // scales with value and tier
    }

    public bool CanResearchLetter(char c)
    {
        int level = GetLetterResearchLevel(c);
        if (level >= 2) return false; // max 2 upgrades per letter
        int cost = LetterResearchCost(c, level);
        return data.folios >= cost;
    }

    public bool ResearchLetter(char c)
    {
        int idx = char.ToLower(c) - 'a';
        if (idx < 0 || idx >= 26) return false;
        if (data.letterResearchLevels == null || data.letterResearchLevels.Length < 26)
        {
            var tmp = new int[26];
            if (data.letterResearchLevels != null)
                System.Array.Copy(data.letterResearchLevels, tmp,
                    System.Math.Min(data.letterResearchLevels.Length, 26));
            data.letterResearchLevels = tmp;
        }
        int currentLevel = data.letterResearchLevels[idx];
        if (currentLevel >= 2) return false;
        int cost = LetterResearchCost(c, currentLevel);
        if (data.folios < cost) return false;
        data.folios -= cost;
        data.letterResearchLevels[idx]++;
        Save();
        return true;
    }

    // ── Challenges ────────────────────────────────────────────────────────────
    // (key, display name, description, Folio reward)
    public static readonly (string key, string name, string desc, int reward)[] ChallengeList =
    {
        ("logophile",    "Logophile",    "Score a word of 10 or more letters.",          5),
        ("polymath",     "Polymath",     "Hold 5 different Lexicons in one run.",         8),
        ("straightas",   "Straight A's", "Win a Standard run without losing a life.",    10),
        ("bibliophile",  "Bibliophile",  "Score 500 unique words across all runs.",       6),
    };

    public bool IsChallengeComplete(string key) => data.completedChallenges.Contains(key);

    private void CompleteChallenge(string key, int reward)
    {
        if (data.completedChallenges.Contains(key)) return;
        data.completedChallenges.Add(key);
        data.folios += reward;
    }

    private void CheckChallenges(RunConfig config, bool won, int livesRemaining)
    {
        var rm = RunManager.Instance;
        if (rm == null) return;

        // Update lifetime bests
        if (rm.longestWordLength > data.longestWordEver)
            data.longestWordEver = rm.longestWordLength;
        if (rm.maxLexiconsHeld > data.maxLexiconsInARun)
            data.maxLexiconsInARun = rm.maxLexiconsHeld;

        // Logophile: score a word of 10+ letters
        if (data.longestWordEver >= 10)
            CompleteChallenge("logophile", 5);

        // Polymath: hold 5 different Lexicons in one run
        if (data.maxLexiconsInARun >= 5)
            CompleteChallenge("polymath", 8);

        // Straight A's: win Standard without losing a life
        if (won && config != null && config.modeName == "Standard"
            && livesRemaining >= config.startingLives)
            CompleteChallenge("straightas", 10);

        // Bibliophile: 500 unique words lifetime
        if (data.totalUniqueWords >= 500)
            CompleteChallenge("bibliophile", 6);
    }

    // Challenge progress for display (0.0–1.0)
    public float ChallengeProgress(string key)
    {
        switch (key)
        {
            case "logophile":   return Mathf.Clamp01(data.longestWordEver   / 10f);
            case "polymath":    return Mathf.Clamp01(data.maxLexiconsInARun / 5f);
            case "straightas":  return IsChallengeComplete("straightas") ? 1f : 0f;
            case "bibliophile": return Mathf.Clamp01(data.totalUniqueWords / 500f);
            default:            return 0f;
        }
    }

    public string ChallengeProgressLabel(string key)
    {
        switch (key)
        {
            case "logophile":   return $"{data.longestWordEver}/10 letters";
            case "polymath":    return $"{data.maxLexiconsInARun}/5 Lexicons";
            case "straightas":  return IsChallengeComplete("straightas") ? "DONE" : "Win Standard with full lives";
            case "bibliophile": return $"{data.totalUniqueWords}/500 words";
            default:            return "";
        }
    }

    // ── Run-End Hooks ─────────────────────────────────────────────────────────
    public void OnRunCompleted(bool won, RunConfig config = null, int livesRemaining = 0)
    {
        data.totalRunsCompleted++;
        data.totalUniqueWords += RunManager.Instance?.totalWordsScored ?? 0;

        // Folio earnings
        int exams    = RunManager.Instance?.examsCleared ?? 0;
        int lexicons = RunManager.Instance?.activeLexicon.Count ?? 0;
        int earned   = exams + (won ? 2 : 0) + lexicons;
        if (earned > 0) data.folios += earned;

        if (won)
        {
            data.totalWins++;
            if (data.totalWins == 1 && data.highestTierUnlocked < 2) UnlockTier(2);
            if (data.totalWins >= 3 && data.highestTierUnlocked < 3) UnlockTier(3);
        }
        CheckScholarUnlocks(config, won, livesRemaining);
        CheckChallenges(config, won, livesRemaining);
        Save();
    }

    // ── Read-only accessors ───────────────────────────────────────────────────
    public int  HighestTierUnlocked => data.highestTierUnlocked;
    public int  TotalWins           => data.totalWins;
    public int  TotalRunsCompleted  => data.totalRunsCompleted;
    public int  TotalUniqueWords    => data.totalUniqueWords;
    public bool HasSaveData         => File.Exists(SavePath);
}
