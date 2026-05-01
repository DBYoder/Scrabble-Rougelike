// UISpritePatcher.cs — Applies game-screen sprites to all UI elements at runtime.
// Attached to the Canvas by SceneBuilder; all references wired there.
// Pattern: if sprite loads → apply + suppress baked text; otherwise leave code color as fallback.
using UnityEngine;
using UnityEngine.UI;

public class UISpritePatcher : MonoBehaviour
{
    public static UISpritePatcher Instance { get; private set; }

    // ── Panel backgrounds ─────────────────────────────────────────────────────
    [Header("Panel Images")]
    public Image topBarImage;
    public Image mainMenuPanelImage;
    public Image shopPanelImage;
    public Image upgradePanelImage;
    public Image gameOverPanelImage;
    public Image victoryPanelImage;
    public Image bossPreviewPanelImage;
    public Image progressionRewardPanelImage;
    public Image scorePreviewImage;
    public Image scoreOverlayImage;
    public Image lexSidebarImage;
    public Image scoringLexSidebarImage;
    public Image tooltipPanelImage;

    // ── Title texts suppressed when result sprites load ───────────────────────
    [Header("Title Texts (suppressed when sprite baked)")]
    public Text gameOverTitleText;
    public Text victoryTitleText;
    public Text bossHeaderText;
    public Text rewardHeaderText;
    public Text lexSidebarLabel;
    public Text scoringLexSidebarLabel;

    // ── Buttons ───────────────────────────────────────────────────────────────
    [Header("Action Buttons")]
    public Button submitButton;
    public Button endRoundButton;
    public Button redrawButton;       // count is dynamic — keep text
    public Button confirmRedrawButton;

    [Header("Navigation Buttons")]
    public Button newGameButton;
    public Button continueButton;
    public Button onwardButton;
    public Button faceItButton;
    public Button leaveShopButton;
    public Button playAgainButton;
    public Button retryButton;
    public Button skipButton;

    // ── Scoring result ────────────────────────────────────────────────────────
    [Header("Scoring Result")]
    public Image scoreResultImage;   // child Image in ScoreOverlay for passed/retry sprite
    public Text  scoreResultText;    // resultText from ScoreUI — hidden when sprite shown

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => ApplyAll();

    // ── Public API ────────────────────────────────────────────────────────────
    /// <summary>Swaps the TopBar between normal and exam variant.</summary>
    public void SetTopBarExam(bool isExam)
    {
        var spr = isExam ? UIAssetLoader.GetHud("bar_exam") : UIAssetLoader.GetHud("bar");
        ApplySprite(topBarImage, spr);
    }

    /// <summary>Shows the PASSED or RETRY sprite in the ScoreOverlay.
    /// Called from ScoreUI.AnimateScore() after the result is known.</summary>
    public void ShowScoringResult(bool passed)
    {
        if (scoreResultImage == null) return;
        var spr = passed ? UIAssetLoader.GetResult("passed") : UIAssetLoader.GetResult("retry");
        if (spr != null)
        {
            scoreResultImage.sprite = spr;
            scoreResultImage.color  = Color.white;
            scoreResultImage.preserveAspect = true;
            scoreResultImage.gameObject.SetActive(true);
            if (scoreResultText != null) scoreResultText.gameObject.SetActive(false);
        }
        else
        {
            scoreResultImage.gameObject.SetActive(false);
            // resultText fallback is shown by ScoreUI directly
        }
    }

    /// <summary>Hides the scoring result sprites. Called when ScoringPanel is hidden.</summary>
    public void HideScoringResult()
    {
        if (scoreResultImage != null) scoreResultImage.gameObject.SetActive(false);
        if (scoreResultText  != null) scoreResultText.gameObject.SetActive(false);
    }

    // ── Apply all on start ────────────────────────────────────────────────────
    private void ApplyAll()
    {
        // Buttons — purpose-built button sprites; hide text when label is baked in
        ApplyButton(submitButton,        "play_word_action",         hideText: true);
        ApplyButton(endRoundButton,      "end_round_action",         hideText: true);
        ApplyButton(confirmRedrawButton, "confirm_reshuffle_action", hideText: true);
        ApplyButton(newGameButton,       "new_run_wide",             hideText: true);
        ApplyButton(continueButton,      "continue_std",             hideText: true);
        ApplyButton(onwardButton,        "onward_wide",              hideText: true);
        ApplyButton(faceItButton,        "face_it_wide",             hideText: true);
        ApplyButton(leaveShopButton,     "leave_shop_wide",          hideText: true);
        ApplyButton(playAgainButton,     "play_again_wide",          hideText: true);
        ApplyButton(retryButton,         "try_again_std",            hideText: true);
        ApplyButton(skipButton,          "skip_std",                 hideText: true);
        // redrawButton: sprite has label baked but count is dynamic — not applied as sprite

        // Scoring result image starts hidden; shown by ShowScoringResult()
        if (scoreResultImage != null) scoreResultImage.gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void ApplyPanel(Image img, string panelName, Sprite spr = null)
    {
        if (img == null) return;
        var sprite = spr ?? UIAssetLoader.GetPanel(panelName);
        ApplySprite(img, sprite);
    }

    // Returns true if sprite was applied (caller can use this to trigger additional logic).
    private static bool ApplyPanelSuppressText(Image img, Sprite spr, Text textToHide)
    {
        if (img == null) return false;
        if (spr != null)
        {
            img.sprite         = spr;
            img.color          = Color.white;
            img.preserveAspect = false;
            if (textToHide != null) textToHide.gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    private static void ApplyButton(Button btn, string spriteName, bool hideText)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        var spr = UIAssetLoader.GetButton(spriteName);
        if (img != null && spr != null)
        {
            img.sprite         = spr;
            img.color          = Color.white;
            img.preserveAspect = false;
            if (hideText)
            {
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null) txt.gameObject.SetActive(false);
            }
        }
    }

    private static void ApplySprite(Image img, Sprite spr)
    {
        if (img == null || spr == null) return;
        img.sprite         = spr;
        img.color          = Color.white;
        img.preserveAspect = false;
    }
}
