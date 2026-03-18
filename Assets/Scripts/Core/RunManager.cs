// RunManager.cs — Tracks ante/blind progression, lives, gold, and active Lexicon.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Describes what the player earned after clearing an Exam.</summary>
public struct ProgressionRewards
{
    public bool handGrew;
    public int  previousHandSize;
    public int  newHandSize;
    public bool boardExpanded;
    public int  previousBoardSize; // board edge length in cells (5, 7, or 9)
    public int  newBoardSize;
    public int  previousRadius;    // for FlashNewCells
    public int  newRadius;
}

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    // ── Constants ─────────────────────────────────────────────────────────────
    public const int MaxLives = 3;

    // ── Active Config ─────────────────────────────────────────────────────────
    public RunConfig ActiveConfig { get; private set; }

    // MaxAntes and MaxLexicon are now per-run instance values (set from RunConfig).
    public int MaxAntes   { get; private set; } = 8;
    public int MaxLexicon { get; private set; } = 5;

    // ── Run State ─────────────────────────────────────────────────────────────
    public int currentAnte  = 1;
    public int currentBlind = 0;  // 0 = Small, 1 = Big, 2 = Boss
    public int lives;
    public int gold;
    public int score;             // Score accumulated this blind

    // ── Lexicon ───────────────────────────────────────────────────────────────
    public List<LexiconWordData> activeLexicon = new List<LexiconWordData>();

    // ── Neologism Tracking ────────────────────────────────────────────────────
    public int totalWordsScored  { get; private set; }
    public int highestWordScore  { get; private set; }
    private readonly HashSet<string> scoredWordSet = new HashSet<string>();

    // ── Starter Pick ─────────────────────────────────────────────────────────
    public bool isStarterPick { get; private set; } = false;
    public void ClearStarterPick() => isStarterPick = false;

    // ── Glossary (The Glossary lexicon effect) ────────────────────────────────
    public char featuredLetter { get; private set; }

    // ── Progression Rewards (awarded after each Exam) ─────────────────────────
    /// <summary>Current maximum hand size. Starts at 7, grows +1 per Exam (cap 10).</summary>
    public int currentHandSize { get; private set; } = 7;
    /// <summary>
    /// Half-side of the unlocked square board area (for the 13×13 grid, half = 6).
    ///   radius 4 → centre  9×9  (cells 2–10)  — starting area
    ///   radius 5 → centre 11×11 (cells 1–11)  — unlocks after Exam 3 (Standard)
    ///   radius 6 → full   13×13 (cells 0–12)  — unlocks after Exam 6 (Standard)
    /// </summary>
    public int unlockedRadius  { get; private set; } = 4;
    /// <summary>Total Exams cleared this run.</summary>
    public int examsCleared    { get; private set; } = 0;

    // ── Blind Assets ─────────────────────────────────────────────────────────
    private BlindData[] blindAssets;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Run Initialisation ───────────────────────────────────────────────────
    public void StartRun(RunConfig config = null)
    {
        ActiveConfig = config;

        // Apply config values, falling back to Standard defaults when config is null.
        MaxAntes          = config != null ? config.totalAntes      : 8;
        MaxLexicon        = config != null ? config.maxLexiconSlots : 5;
        lives             = config != null ? config.startingLives   : MaxLives;
        gold              = config != null ? config.startingGold    : 0;
        currentHandSize   = config != null ? config.startingHandSize: 7;
        unlockedRadius    = config != null ? config.startingRadius  : 4;
        isStarterPick     = config != null ? config.starterLexicon  : true;

        currentAnte      = 1;
        currentBlind     = 0;
        score            = 0;
        totalWordsScored = 0;
        highestWordScore = 0;
        examsCleared     = 0;
        activeLexicon.Clear();
        scoredWordSet.Clear();
        blindAssets = Resources.LoadAll<BlindData>("Blinds");
        RollFeaturedLetter();
    }

    // ── Score ─────────────────────────────────────────────────────────────────
    public void AddScore(int amount)   => score += amount;
    public void ResetBlindScore()      => score = 0;

    public bool CheckBlindPassed()     => score >= GetCurrentBlindTarget();

    public int GetCurrentBlindTarget()
    {
        var data = GetCurrentBlindData();
        int raw;
        if (data != null)
        {
            raw = data.targetScore;
        }
        else
        {
            // Procedural fallback for antes beyond what assets cover (used by Infinite mode).
            int baseTarget = 100 * currentAnte * (currentBlind + 1);
            int antesOver8 = Mathf.Max(0, currentAnte - 8);
            raw = Mathf.RoundToInt(baseTarget * Mathf.Pow(1.35f, antesOver8));
        }

        // scoreMultiplier > 1 means targets are divided (easier per blind) to compress run length.
        float mult = ActiveConfig != null && ActiveConfig.scoreMultiplier > 0f
            ? ActiveConfig.scoreMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(raw / mult));
    }

    public int GetCurrentBlindGoldReward()
    {
        var data = GetCurrentBlindData();
        return data != null ? data.goldReward : 3 + currentAnte;
    }

    // ── Progression ──────────────────────────────────────────────────────────
    /// <summary>
    /// Advances to the next blind (or next ante). Returns false if the run is won
    /// (never returns false when infiniteMode is active).
    /// </summary>
    public bool AdvanceBlind()
    {
        ResetBlindScore();
        currentBlind++;
        if (currentBlind > 2)
        {
            currentBlind = 0;
            currentAnte++;
            bool infinite = ActiveConfig != null && ActiveConfig.infiniteMode;
            if (!infinite && currentAnte > MaxAntes) return false; // Victory!
        }
        RollFeaturedLetter();
        return true;
    }

    /// <summary>
    /// Awards post-Exam progression rewards. Called by GameManager after an Exam is cleared.
    /// Hand grows +1 per Exam (cap 10). Board expands at the examsCleared thresholds defined
    /// by the active RunConfig (Standard: {3,6}; Quick Run: {2,3}).
    /// Returns a <see cref="ProgressionRewards"/> struct describing exactly what changed.
    /// </summary>
    public ProgressionRewards OnExamCleared()
    {
        examsCleared++;

        var r = new ProgressionRewards
        {
            previousHandSize  = currentHandSize,
            previousRadius    = unlockedRadius,
            previousBoardSize = unlockedRadius * 2 + 1,
        };

        // Hand: +1 per Exam, cap at 10
        if (currentHandSize < 10)
        {
            currentHandSize++;
            r.handGrew = true;
        }
        r.newHandSize = currentHandSize;

        // Board: expand at the thresholds defined by the active RunConfig.
        int[] expandAt = ActiveConfig?.boardExpandAtExams ?? new[] { 3, 6 };
        if (unlockedRadius < 6 && expandAt.Contains(examsCleared))
        {
            unlockedRadius++;
            r.boardExpanded = true;
        }
        r.newRadius    = unlockedRadius;
        r.newBoardSize = unlockedRadius * 2 + 1;

        return r;
    }

    /// <summary>
    /// Returns true if the grid coordinate (x, y) is within the currently unlocked board area.
    /// For the 9×9 grid (GridSize=9, half=4):
    ///   radius 2 → 5×5   radius 3 → 7×7   radius 4 → full 9×9
    /// </summary>
    public bool IsCellUnlocked(int x, int y)
    {
        int half = GridManager.GridSize / 2; // = 6 for 13×13
        return x >= half - unlockedRadius && x <= half + unlockedRadius
            && y >= half - unlockedRadius && y <= half + unlockedRadius;
    }

    // ── Lives & Gold ─────────────────────────────────────────────────────────
    public void LoseLife()              => lives = Mathf.Max(0, lives - 1);
    public void EarnGold(int amount)    => gold += amount;
    public bool SpendGold(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        return true;
    }

    // ── Lexicon ───────────────────────────────────────────────────────────────
    public bool CanAddLexicon()         => activeLexicon.Count < MaxLexicon;
    public void AddLexicon(LexiconWordData lex)
    {
        if (CanAddLexicon()) activeLexicon.Add(lex);
    }

    /// <summary>
    /// Removes a Lexicon card from the active list and refunds half its shop cost
    /// (minimum 1 gold). Safe to call any time, including mid-shop.
    /// </summary>
    public void SellLexicon(LexiconWordData lex)
    {
        if (lex == null || !activeLexicon.Contains(lex)) return;
        activeLexicon.Remove(lex);
        EarnGold(Mathf.Max(1, lex.shopCost / 2));
    }

    // ── Boss Blind ────────────────────────────────────────────────────────────
    public bool         IsBossBlind     => currentBlind == 2;
    public BossModifier GetBossModifier()
    {
        var data = GetCurrentBlindData();
        return data != null ? data.bossModifier : BossModifier.None;
    }
    public string GetBossModifierDescription()
    {
        var data = GetCurrentBlindData();
        return data != null ? data.modifierDescription : string.Empty;
    }

    // ── Neologism Tracking ────────────────────────────────────────────────────
    public void RegisterScoredWord(string word, int wordScore = 0)
    {
        if (scoredWordSet.Add(word.ToLower()))
            totalWordsScored++;
        if (wordScore > highestWordScore)
            highestWordScore = wordScore;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private BlindData GetCurrentBlindData()
    {
        if (blindAssets == null) return null;
        string expectedName = $"Ante{currentAnte}_Blind{currentBlind}";
        foreach (var b in blindAssets)
            if (b != null && b.name == expectedName) return b;
        return null;
    }

    private void RollFeaturedLetter()
    {
        const string alpha = "abcdefghijklmnopqrstuvwxyz";
        featuredLetter = alpha[Random.Range(0, alpha.Length)];
    }
}
