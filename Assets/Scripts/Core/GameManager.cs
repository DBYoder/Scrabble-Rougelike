// GameManager.cs — Singleton state machine driving the entire game loop.
// States: MainMenu → Drawing → Placement → Scoring → Shop → Upgrade → (loop)
//         GameOver / Victory are terminal states.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    MainMenu,
    Drawing,     // Brief state: init round, then auto-advance to Placement
    Placement,   // Player places tiles on the grid
    Scoring,     // Validate words, calculate score, show results
    Shop,        // Spend gold
    Upgrade,     // Choose 1 of 3 free Lexicon entries
    GameOver,
    Victory
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gamePanel;        // Grid + hand + action buttons
    public GameObject scoringPanel;
    public GameObject shopPanel;
    public GameObject upgradePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Always-Visible HUD")]
    public GameObject hudPanel;         // RunUI + LexiconBar

    [Header("Boss Preview")]
    public GameObject bossPreviewPanel;  // Modal overlay — managed independently, not in hide-all block

    [Header("Progression Reward")]
    public GameObject progressionRewardPanel; // Modal overlay — shown after each Exam is cleared

    [Header("End-Screen Stats")]
    public UnityEngine.UI.Text gameOverStatsText;
    public UnityEngine.UI.Text victoryStatsText;

    // Prevents boss preview from re-showing during a lives-retry of the same boss blind.
    private bool _bossPreviewShown;

    // Previous board radius stored before OnExamCleared() so we can flash newly unlocked cells.
    private int _prevBoardRadius = -1;

    // Words validated at each play submission this round.
    // Used to re-score words that were later extended (e.g. CAT → CATS).
    private readonly List<Word> _roundPlayedWords = new List<Word>();
    public IReadOnlyList<Word> RoundPlayedWords => _roundPlayedWords;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => ChangeState(GameState.MainMenu);

    // ── State Machine ─────────────────────────────────────────────────────────
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        // Hide all gameplay panels
        SetActive(mainMenuPanel, false);
        SetActive(gamePanel,     false);
        SetActive(scoringPanel,  false);
        SetActive(shopPanel,     false);
        SetActive(upgradePanel,  false);
        SetActive(gameOverPanel, false);
        SetActive(victoryPanel,  false);

        // HUD is visible during all run states
        bool runActive = newState != GameState.MainMenu
                      && newState != GameState.GameOver
                      && newState != GameState.Victory;
        SetActive(hudPanel, runActive);

        switch (newState)
        {
            case GameState.MainMenu:
                SetActive(mainMenuPanel, true);
                break;

            case GameState.Drawing:
                SetActive(gamePanel, true);
                OnEnterDrawing();
                break;

            case GameState.Placement:
                SetActive(gamePanel, true);
                FadeInPanel(gamePanel);
                OnEnterPlacement();
                break;

            case GameState.Scoring:
                SetActive(scoringPanel, true);
                FadeInPanel(scoringPanel);
                OnEnterScoring();
                break;

            case GameState.Shop:
                SetActive(shopPanel, true);
                FadeInPanel(shopPanel);
                OnEnterShop();
                break;

            case GameState.Upgrade:
                SetActive(upgradePanel, true);
                FadeInPanel(upgradePanel);
                OnEnterUpgrade();
                break;

            case GameState.GameOver:
                SetActive(gameOverPanel, true);
                FadeInPanel(gameOverPanel);
                SetEndScreenStats(gameOverStatsText);
                break;

            case GameState.Victory:
                SetActive(victoryPanel, true);
                FadeInPanel(victoryPanel);
                SetEndScreenStats(victoryStatsText);
                break;
        }

        // Keep HUD fresh
        RunUI.Instance?.RefreshHUD();
        if (runActive)
            RunUI.Instance?.RefreshLexiconBar();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void StartRun()
    {
        RunManager.Instance.StartRun();
        ChangeState(GameState.Upgrade); // Starter Lexicon pick before the first blind
    }

    /// <summary>Called by the Play Word button — validates newly placed tiles.
    /// On valid play: locks them in and draws replacements.
    /// On invalid play: returns the tiles to hand.</summary>
    public void SubmitWord()
    {
        if (CurrentState != GameState.Placement) return;
        if (!GridManager.Instance.HasCurrentTurnPlacements) return;
        if (TileHandManager.Instance.wordPlaysRemaining <= 0) return;

        // Find all words that touch at least one newly placed cell
        var allWords    = GridManager.Instance.GetAllWords();
        bool hasNewWord = false;
        bool allValid   = true;

        foreach (var word in allWords)
        {
            bool touchesTurn = false;
            foreach (var pos in word.cellPositions)
            {
                // Check via RollbackTurn preview — we check using a fresh set lookup
                // by re-examining currentTurnCells indirectly through PlaceTile bookkeeping.
                // Simpler: ask GridManager if the cell was placed this turn.
                if (GridManager.Instance.IsTurnCell(pos.x, pos.y))
                { touchesTurn = true; break; }
            }
            if (!touchesTurn) continue;

            hasNewWord = true;
            if (!WordValidator.Instance.IsValid(word.text))
            { allValid = false; break; }
        }

        bool connected = GridManager.Instance.IsTurnPlacementConnected();

        if (!hasNewWord || !allValid || !connected)
        {
            // Return newly placed tiles to hand
            var returned = GridManager.Instance.RollbackTurn();
            foreach (var tile in returned)
                TileHandManager.Instance.ReturnTileToHand(tile);
            TileHandUI.Instance?.RefreshHand();
            GridUI.Instance?.RefreshGrid();
            GridUI.Instance?.RefreshPreview();
            return;
        }

        // Record valid words formed by this play so they can be re-scored at
        // the end of the round even if they are later extended by another play.
        foreach (var word in allWords)
        {
            foreach (var pos in word.cellPositions)
            {
                if (GridManager.Instance.IsTurnCell(pos.x, pos.y))
                {
                    if (WordValidator.Instance.IsValid(word.text))
                        _roundPlayedWords.Add(word);
                    break;
                }
            }
        }

        // Valid — commit, decrement play count, and draw replacements
        GridManager.Instance.CommitTurn();
        TileHandManager.Instance.wordPlaysRemaining--;
        TileHandManager.Instance.DrawToFull();
        TileHandUI.Instance?.RefreshHand();
        GridUI.Instance?.RefreshGrid();
        GridUI.Instance?.RefreshPreview();

        // Auto-finish the round when all play actions are exhausted
        if (TileHandManager.Instance.wordPlaysRemaining <= 0)
            EndRound();
    }

    /// <summary>Called by the End Round button — scores all words on the board and advances.</summary>
    public void EndRound()
    {
        if (CurrentState != GameState.Placement) return;
        if (!GridManager.Instance.HasAnyTilePlaced()) return;
        ChangeState(GameState.Scoring);
    }

    /// <summary>Called by ScoreUI continue button after animations finish.</summary>
    public void OnScoringComplete()
    {
        bool passed = RunManager.Instance.CheckBlindPassed();

        if (passed)
        {
            RunManager.Instance.EarnGold(RunManager.Instance.GetCurrentBlindGoldReward());
            ChangeState(GameState.Shop);
        }
        else
        {
            RunManager.Instance.LoseLife();
            RunUI.Instance?.ShowLifeLostFeedback(RunManager.Instance.lives);
            ChangeState(RunManager.Instance.lives <= 0 ? GameState.GameOver : GameState.Drawing);
        }
    }

    /// <summary>Called after the Upgrade screen (or Skip). Moves to the next blind.</summary>
    public void AdvanceBlind()
    {
        // Starter pick fires before the first blind — don't increment the blind counter.
        if (RunManager.Instance.isStarterPick)
        {
            RunManager.Instance.ClearStarterPick();
            ChangeState(GameState.Drawing);
            return;
        }

        bool wasExam = RunManager.Instance.IsBossBlind; // capture before advancing

        _bossPreviewShown = false; // Allow the next boss blind to show its preview
        bool hasMore = RunManager.Instance.AdvanceBlind();
        if (!hasMore)
        {
            ChangeState(GameState.Victory);
            return;
        }

        // Award post-Exam progression (hand growth and/or board expansion).
        // Board expansion takes effect when BuildGrid() is called on entering Placement.
        if (wasExam)
        {
            _prevBoardRadius = RunManager.Instance.unlockedRadius; // capture before expanding
            var rewards = RunManager.Instance.OnExamCleared();

            // Show the reward modal so the player sees what they earned before the next round.
            if (rewards.handGrew || rewards.boardExpanded)
            {
                ShowProgressionReward(rewards);
                return; // ProceedFromProgressionReward() will call ChangeState(Drawing)
            }
        }

        ChangeState(GameState.Drawing);
    }

    /// <summary>Called by the FACE IT button on the boss preview modal.</summary>
    public void ProceedFromBossPreview()
    {
        SetActive(bossPreviewPanel, false);
        ChangeState(GameState.Placement);
    }

    /// <summary>Called by the ONWARD button on the progression reward modal.</summary>
    public void ProceedFromProgressionReward()
    {
        SetActive(progressionRewardPanel, false);
        ChangeState(GameState.Drawing);
    }

    private void ShowProgressionReward(ProgressionRewards rewards)
    {
        SetActive(progressionRewardPanel, true);
        progressionRewardPanel.GetComponent<ProgressionRewardUI>()?.Populate(rewards);
    }

    // ── State Handlers ────────────────────────────────────────────────────────
    private void OnEnterDrawing()
    {
        _roundPlayedWords.Clear();
        RunManager.Instance.ResetBlindScore(); // Reset score for retries (AdvanceBlind also calls this normally)
        GridManager.Instance.InitGrid();
        TileHandManager.Instance.InitRound();
        ScoreManager.Instance.ResetRoundWords();

        // Show boss preview modal on first entry to a boss blind
        if (RunManager.Instance.IsBossBlind && !_bossPreviewShown && bossPreviewPanel != null)
        {
            ShowBossPreview();
            return; // ProceedFromBossPreview() will call ChangeState(Placement)
        }

        ChangeState(GameState.Placement);
    }

    private void ShowBossPreview()
    {
        _bossPreviewShown = true;
        SetActive(bossPreviewPanel, true);
        bossPreviewPanel.GetComponent<BossPreviewUI>()?.Populate(
            RunManager.Instance.GetBossModifier(),
            RunManager.Instance.GetBossModifierDescription());
        RunUI.Instance?.RefreshHUD();
    }

    private void OnEnterPlacement()
    {
        GridUI.Instance?.BuildGrid();
        TileHandUI.Instance?.RefreshHand();

        // Flash newly unlocked cells when the board expanded after the last Exam.
        if (_prevBoardRadius >= 0
            && RunManager.Instance != null
            && _prevBoardRadius < RunManager.Instance.unlockedRadius)
        {
            GridUI.Instance?.FlashNewCells(_prevBoardRadius);
            _prevBoardRadius = -1; // consume — don't flash again on retry
        }
    }

    private void OnEnterScoring()
    {
        var allWords = GridManager.Instance.GetAllWords();
        var validWords = new List<Word>();

        BossModifier boss = RunManager.Instance.IsBossBlind
            ? RunManager.Instance.GetBossModifier()
            : BossModifier.None;

        // Set of word texts currently on the final board (used for extension detection below)
        var finalBoardTexts = new HashSet<string>();

        foreach (var w in allWords)
        {
            if (!WordValidator.Instance.IsValid(w.text)) continue;
            if (boss == BossModifier.NoShortWords && w.text.Length == 3) continue;
            if (boss == BossModifier.RareTilesLocked && ContainsRareLetter(w.text)) continue;
            validWords.Add(w);
            finalBoardTexts.Add(w.text);
        }

        // Re-score any word from a previous play that no longer appears on the final board
        // because it was extended (e.g. CAT played in play 1, then S added to make CATS).
        foreach (var w in _roundPlayedWords)
        {
            if (finalBoardTexts.Contains(w.text)) continue;   // still on board unchanged — already scored above
            if (!WordValidator.Instance.IsValid(w.text)) continue;
            if (boss == BossModifier.NoShortWords && w.text.Length == 3) continue;
            if (boss == BossModifier.RareTilesLocked && ContainsRareLetter(w.text)) continue;
            validWords.Add(w);
        }

        var scoreResult = ScoreManager.Instance.CalculateScore(
            validWords, RunManager.Instance.activeLexicon, boss);

        RunManager.Instance.AddScore(scoreResult.totalScore);

        if (ScoreUI.Instance != null)
            ScoreUI.Instance.ShowScore(scoreResult);
        else
            OnScoringComplete(); // Auto-advance if ScoreUI is missing
    }

    private void OnEnterShop()
    {
        ShopManager.Instance?.GenerateShop();
        ShopUI.Instance?.RefreshShop();
    }

    private void OnEnterUpgrade()
    {
        UpgradeUI.Instance?.ShowUpgradeOptions();
    }

    // ── Utility ───────────────────────────────────────────────────────────────
    private static void SetEndScreenStats(UnityEngine.UI.Text label)
    {
        if (label == null || RunManager.Instance == null) return;
        var rm = RunManager.Instance;
        string stage = rm.currentBlind == 0 ? "Exercise" : rm.currentBlind == 1 ? "Test" : "Exam";
        label.text =
            $"Chapter {rm.currentAnte} / {RunManager.MaxAntes}  ·  {stage}\n" +
            $"Words scored: {rm.totalWordsScored}\n" +
            $"Best word: {rm.highestWordScore} pts\n" +
            $"Lexicons: {rm.activeLexicon.Count}";
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private static bool ContainsRareLetter(string word)
    {
        foreach (char c in word)
            if (LetterData.IsRareLetter(c)) return true;
        return false;
    }

    // ── Panel Fade ────────────────────────────────────────────────────────────
    private void FadeInPanel(GameObject panel)
    {
        if (panel == null) return;
        var cg = panel.GetComponent<UnityEngine.CanvasGroup>();
        if (cg == null) return;
        StartCoroutine(FadeIn(cg, 0.15f));
    }

    private static IEnumerator FadeIn(UnityEngine.CanvasGroup cg, float duration)
    {
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }
}
