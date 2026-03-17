// RunUI.cs — Always-visible HUD showing Chapter, Drafts, and Lexicon bars.
// lexiconCardPrefab should have children: Text "LexiconName", Text "LexiconEffect".
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RunUI : MonoBehaviour
{
    public static RunUI Instance { get; private set; }

    [Header("HUD Labels")]
    public Text livesText;
    public Text anteText;           // "Chapter 2/8  ·  Test"
    public Text featuredLetterText; // Shows featured letter when The Glossary is active

    [Header("Lexicon Display")]
    public Transform  lexiconBarParent;      // GamePanel sidebar (VLG, right side)
    public Transform  scoringLexiconParent;  // ScoringPanel row (HLG, above scroll)
    public GameObject lexiconCardPrefab;

    private readonly Dictionary<LexiconEffectType, List<Image>> _lexiconImages = new Dictionary<LexiconEffectType, List<Image>>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void RefreshHUD()
    {
        if (RunManager.Instance == null) return;
        var rm = RunManager.Instance;

        // Chapter + stage-within-chapter progress ("Chapter 2/8  ·  Test")
        string stageName = rm.currentBlind == 0 ? "Exercise" : rm.currentBlind == 1 ? "Test" : "Exam";
        Set(livesText, $"Drafts: {rm.lives}");
        Set(anteText,  $"Chapter {rm.currentAnte} / {RunManager.MaxAntes}  ·  {stageName}");

        if (featuredLetterText != null)
        {
            bool hasGlossary = rm.activeLexicon.Exists(l => l.effectType == LexiconEffectType.TheGlossary);
            featuredLetterText.gameObject.SetActive(hasGlossary);
            if (hasGlossary)
                featuredLetterText.text = $"★ {char.ToUpper(rm.featuredLetter)}";
        }

    }

    public void RefreshLexiconBar()
    {
        _lexiconImages.Clear();
        PopulateLexiconParent(lexiconBarParent);
        PopulateLexiconParent(scoringLexiconParent);
    }

    private void PopulateLexiconParent(Transform parent)
    {
        if (parent == null || lexiconCardPrefab == null || RunManager.Instance == null) return;

        foreach (Transform child in parent)
            Destroy(child.gameObject);

        foreach (var lex in RunManager.Instance.activeLexicon)
        {
            var card = Instantiate(lexiconCardPrefab, parent);

            // Set card name — effect description shown via hover tooltip
            // Neologism also shows its live stack bonus so players can see it growing
            foreach (var t in card.GetComponentsInChildren<Text>())
            {
                if (t.name != "LexiconName") continue;
                if (lex.effectType == LexiconEffectType.Neologism && RunManager.Instance != null)
                {
                    float bonus = Mathf.Min(RunManager.Instance.totalWordsScored * 0.1f, 3.0f);
                    t.text = $"{lex.displayName}\n+{bonus:F1}×";
                }
                else
                {
                    t.text = lex.displayName;
                }
            }

            // Show artwork if available
            var iconImg = card.transform.Find("LexiconIcon")?.GetComponent<Image>();
            if (iconImg != null && lex.artwork != null)
                iconImg.sprite = lex.artwork;

            // Hover tooltip — shows effect description when the player mouses over the card
            var hover = card.AddComponent<LexiconCardHover>();
            hover.effectText = lex.effectDescription;

            // Register the card's root Image for flash
            var img = card.GetComponent<Image>();
            if (img != null)
            {
                if (!_lexiconImages.ContainsKey(lex.effectType))
                    _lexiconImages[lex.effectType] = new List<Image>();
                _lexiconImages[lex.effectType].Add(img);
            }
        }
    }

    /// <summary>Briefly flashes the lives counter and shows RETRY! or GAME OVER feedback.</summary>
    public void ShowLifeLostFeedback(int livesRemaining)
    {
        StartCoroutine(LifeLostCoroutine(livesRemaining));
    }

    private IEnumerator LifeLostCoroutine(int livesRemaining)
    {
        if (livesText == null) yield break;
        Color originalColor = livesText.color;
        livesText.color = new Color(0.937f, 0.584f, 0.616f); // rose
        livesText.text  = livesRemaining > 0
            ? $"Drafts: {livesRemaining}  — RETRY!"
            : $"Drafts: 0  — GAME OVER";
        yield return new WaitForSeconds(1.2f);
        livesText.color = originalColor;
        // HUD will be refreshed by the next state transition
    }

    public void FlashLexiconCard(LexiconEffectType effectType)
    {
        if (_lexiconImages.TryGetValue(effectType, out var images))
            foreach (var img in images)
                if (img != null) StartCoroutine(FlashCard(img));
    }

    private IEnumerator FlashCard(Image img)
    {
        Color orig      = img.color;
        Vector3 origScale = img.transform.localScale;
        img.color = new Color(1f, 0.88f, 0.2f); // bright gold
        float dur     = 0.7f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            img.transform.localScale = origScale * scale;
            yield return null;
        }
        img.color = orig;
        img.transform.localScale = origScale;
    }

    private static void Set(Text label, string value)
    {
        if (label != null) label.text = value;
    }
}
