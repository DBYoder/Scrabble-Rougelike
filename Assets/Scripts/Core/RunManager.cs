// RunManager.cs — Tracks ante/blind progression, lives, gold, and active Lexicon.
using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    // ── Constants ─────────────────────────────────────────────────────────────
    public const int MaxAntes  = 8;
    public const int MaxLives  = 3;
    public const int MaxLexicon = 5;

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

    // ── Blind Assets ─────────────────────────────────────────────────────────
    private BlindData[] blindAssets;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Run Initialisation ───────────────────────────────────────────────────
    public void StartRun()
    {
        currentAnte   = 1;
        currentBlind  = 0;
        lives         = MaxLives;
        gold          = 0;
        score         = 0;
        totalWordsScored = 0;
        highestWordScore = 0;
        isStarterPick    = true;
        activeLexicon.Clear();
        scoredWordSet.Clear();
        blindAssets   = Resources.LoadAll<BlindData>("Blinds");
        RollFeaturedLetter();
    }

    // ── Score ─────────────────────────────────────────────────────────────────
    public void AddScore(int amount)   => score += amount;
    public void ResetBlindScore()      => score = 0;

    public bool CheckBlindPassed()     => score >= GetCurrentBlindTarget();

    public int GetCurrentBlindTarget()
    {
        var data = GetCurrentBlindData();
        if (data != null) return data.targetScore;
        // Fallback formula if no asset found
        int baseTarget = 100 * currentAnte * (currentBlind + 1);
        return baseTarget;
    }

    public int GetCurrentBlindGoldReward()
    {
        var data = GetCurrentBlindData();
        return data != null ? data.goldReward : 3 + currentAnte;
    }

    // ── Progression ──────────────────────────────────────────────────────────
    /// <summary>
    /// Advances to the next blind (or next ante). Returns false if the run is won.
    /// </summary>
    public bool AdvanceBlind()
    {
        ResetBlindScore();
        currentBlind++;
        if (currentBlind > 2)
        {
            currentBlind = 0;
            currentAnte++;
            if (currentAnte > MaxAntes) return false; // Victory!
        }
        RollFeaturedLetter();
        return true;
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
