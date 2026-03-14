// TileHandUI.cs — Renders the player's hand and manages tile selection / redraw mode.
// tileCardPrefab should have: Image (bg), Button, and children named "LetterText" (Text)
// and "ChipsText" (Text).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileHandUI : MonoBehaviour
{
    public static TileHandUI Instance { get; private set; }

    [Header("References")]
    public GameObject tileCardPrefab;
    public Transform  handParent;
    public Button     redrawButton;         // Toggle redraw mode
    public Button     confirmRedrawButton;  // Confirm selected tiles for redraw
    public Button     submitButton;         // Play word — lock tiles, draw new ones
    public Button     endRoundButton;       // End the round and go to scoring

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color redrawColor = new Color(1f, 0.5f, 0.5f);

    // Runtime
    private List<GameObject>   tileCards = new List<GameObject>();
    private List<TileInstance> tilesForRedraw = new List<TileInstance>();
    private bool               inRedrawMode;

    public bool IsInRedrawMode => inRedrawMode;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (redrawButton != null)
        {
            redrawButton.onClick.AddListener(ToggleRedrawMode);
        }
        if (confirmRedrawButton != null)
        {
            confirmRedrawButton.onClick.AddListener(ConfirmRedraw);
            confirmRedrawButton.gameObject.SetActive(false);
        }
        if (submitButton != null)
            submitButton.onClick.AddListener(() => GameManager.Instance?.SubmitWord());
        if (endRoundButton != null)
            endRoundButton.onClick.AddListener(() => GameManager.Instance?.EndRound());
    }

    // ── Hand Rendering ────────────────────────────────────────────────────────
    public void RefreshHand()
    {
        foreach (var card in tileCards)
            if (card != null) Destroy(card);
        tileCards.Clear();
        tilesForRedraw.Clear();

        if (TileHandManager.Instance == null) return;

        foreach (var tile in TileHandManager.Instance.hand)
        {
            var card = Instantiate(tileCardPrefab, handParent);

            // Set text fields by name
            foreach (var t in card.GetComponentsInChildren<Text>())
            {
                if (t.name == "LetterText") t.text = tile.Letter.ToString().ToUpper();
                if (t.name == "ChipsText")  t.text = tile.TotalChips.ToString();
            }

            var img = card.GetComponent<Image>();
            if (img != null)
            {
                img.color = normalColor;

                // The Glossary: tint tiles that match the featured letter amber gold
                bool hasGlossary = RunManager.Instance != null
                    && RunManager.Instance.activeLexicon.Exists(
                           l => l.effectType == LexiconEffectType.TheGlossary);
                if (hasGlossary
                    && RunManager.Instance.featuredLetter != '\0'
                    && char.ToLower(tile.Letter) == RunManager.Instance.featuredLetter)
                {
                    img.color = new Color(1f, 0.85f, 0.30f); // amber gold
                }
            }

            var capturedTile = tile;
            var capturedCard = card;

            // Click handler: redraw-mode toggle only
            var btn = card.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnTileClicked(capturedTile, capturedCard));

            // Drag handler: drag tile from hand to grid
            var drag = card.AddComponent<TileCardDrag>();
            drag.tile = capturedTile;

            tileCards.Add(card);
        }

        // Update redraw button label
        if (redrawButton != null)
        {
            var txt = redrawButton.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = $"Redraw ({TileHandManager.Instance.redrawsRemaining})";
        }

        // Update play-word button label and availability
        if (submitButton != null)
        {
            int playsLeft = TileHandManager.Instance.wordPlaysRemaining;
            var txt = submitButton.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = $"PLAY WORD ({playsLeft})";
            submitButton.interactable = playsLeft > 0;
        }
    }

    // ── Tile Click (redraw mode only) ─────────────────────────────────────────
    private void OnTileClicked(TileInstance tile, GameObject card)
    {
        if (!inRedrawMode) return;

        if (tilesForRedraw.Contains(tile))
        {
            tilesForRedraw.Remove(tile);
            card.GetComponent<Image>().color = normalColor;
        }
        else
        {
            tilesForRedraw.Add(tile);
            card.GetComponent<Image>().color = redrawColor;
        }
    }

    // ── Redraw Mode ───────────────────────────────────────────────────────────
    public void ToggleRedrawMode()
    {
        if (inRedrawMode)
            ExitRedrawMode();
        else
            EnterRedrawMode();
    }

    private void EnterRedrawMode()
    {
        if (TileHandManager.Instance.redrawsRemaining <= 0) return;
        inRedrawMode = true;
        tilesForRedraw.Clear();
        foreach (var c in tileCards)
            if (c != null) c.GetComponent<Image>().color = normalColor;
        if (confirmRedrawButton != null) confirmRedrawButton.gameObject.SetActive(true);
    }

    private void ExitRedrawMode()
    {
        inRedrawMode = false;
        tilesForRedraw.Clear();
        foreach (var c in tileCards)
            if (c != null) c.GetComponent<Image>().color = normalColor;
        if (confirmRedrawButton != null) confirmRedrawButton.gameObject.SetActive(false);
    }

    public void ConfirmRedraw()
    {
        if (!inRedrawMode || tilesForRedraw.Count == 0) return;
        bool success = TileHandManager.Instance.Redraw(new List<TileInstance>(tilesForRedraw));
        ExitRedrawMode();
        if (success) RefreshHand();
    }
}
