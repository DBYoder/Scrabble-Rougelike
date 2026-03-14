// UpgradeUI.cs — Shows 3 random Lexicon entries for the player to choose 1 for free.
// upgradeOptionPrefab should have children: Text "UpgradeName", Text "UpgradeEffect",
// Text "UpgradeFlavor", Image "UpgradeIcon", Button (root).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    public static UpgradeUI Instance { get; private set; }

    [Header("References")]
    public Transform  upgradeOptionsParent;
    public GameObject upgradeOptionPrefab;
    public Text       promptText;
    public Button     skipButton;

    private LexiconWordData[] options;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipUpgrade);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void ShowUpgradeOptions()
    {
        // Clear old entries
        foreach (Transform child in upgradeOptionsParent)
            Destroy(child.gameObject);

        // Build pool of unowned Lexicon entries
        var allLexicon = Resources.LoadAll<LexiconWordData>("Lexicon");
        var available  = new List<LexiconWordData>(allLexicon);
        if (RunManager.Instance != null)
            available.RemoveAll(l => RunManager.Instance.activeLexicon.Contains(l));

        // Pick up to 3
        int count = Mathf.Min(3, available.Count);
        options = new LexiconWordData[count];
        for (int i = 0; i < count; i++)
        {
            int idx  = Random.Range(0, available.Count);
            options[i] = available[idx];
            available.RemoveAt(idx);
        }

        if (count == 0)
        {
            // No options — auto-advance
            if (promptText != null) promptText.text = "No new Lexicon entries available.";
            GameManager.Instance.AdvanceBlind();
            return;
        }

        bool isStarter = RunManager.Instance != null && RunManager.Instance.isStarterPick;

        if (promptText != null)
        {
            if (isStarter)
                promptText.text = "Choose your starting Lexicon:";
            else
                promptText.text = RunManager.Instance != null && !RunManager.Instance.CanAddLexicon()
                    ? $"Lexicon full ({RunManager.MaxLexicon}/{RunManager.MaxLexicon}) — skip or sell in shop first."
                    : "Choose a Lexicon entry (free):";
        }

        // Skip is not allowed during the starter pick — player must choose one
        if (skipButton != null)
            skipButton.gameObject.SetActive(!isStarter);

        // Spawn option cards
        foreach (var opt in options)
        {
            var entry = Instantiate(upgradeOptionPrefab, upgradeOptionsParent);

            foreach (var t in entry.GetComponentsInChildren<Text>())
            {
                switch (t.name)
                {
                    case "UpgradeName":   t.text = opt.displayName;      break;
                    case "UpgradeEffect": t.text = opt.effectDescription; break;
                    case "UpgradeFlavor": t.text = opt.flavorText;        break;
                    case "UpgradeCost":   t.text = "FREE";                break;
                }
            }

            var iconImg = entry.transform.Find("UpgradeIcon")?.GetComponent<Image>();
            if (iconImg != null && opt.artwork != null)
                iconImg.sprite = opt.artwork;

            var btn = entry.GetComponent<Button>();
            if (btn != null)
            {
                var capturedOpt = opt;
                btn.interactable = RunManager.Instance != null && RunManager.Instance.CanAddLexicon();
                btn.onClick.AddListener(() => SelectUpgrade(capturedOpt));
            }
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────
    private void SelectUpgrade(LexiconWordData chosen)
    {
        if (RunManager.Instance != null && RunManager.Instance.CanAddLexicon())
        {
            RunManager.Instance.AddLexicon(chosen);
            RunUI.Instance?.RefreshLexiconBar();
        }
        GameManager.Instance.AdvanceBlind();
    }

    public void SkipUpgrade() => GameManager.Instance.AdvanceBlind();
}
