// IndexUI.cs — The Index: between-run shop for spending Folios on permanent upgrades.
// Accessible from the main menu landing screen.
using UnityEngine;
using UnityEngine.UI;

public class IndexUI : MonoBehaviour
{
    public static IndexUI Instance { get; private set; }

    [Header("References")]
    public Text      folioCountText;
    public Transform upgradeListParent;
    public Button    closeButton;

    // ── Upgrade catalogue (key, display name, description, cost) ──────────────
    private static readonly (string key, string name, string desc, int cost)[] Upgrades =
    {
        ("marginalia",    "Marginalia",         "Start every run with +1 gold.",                          5),
        ("bibliography",  "Extended Bibliography", "The shop always shows 3 Lexicon slots instead of 2.", 8),
        ("addendum",      "Addendum",           "The Upgrade screen offers 4 choices instead of 3.",     10),
        ("annotatedhand", "Annotated Hand",     "Start every run with one extra tile in hand.",          12),
        ("errata",        "Errata",             "The shop always shows one extra miscellaneous item.",   15),
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Refresh()
    {
        UpdateFolioDisplay();
        BuildUpgradeRows();
    }

    // ── Private ───────────────────────────────────────────────────────────────
    private void UpdateFolioDisplay()
    {
        if (folioCountText != null && MetaProgressionManager.Instance != null)
            folioCountText.text = $"Folios: {MetaProgressionManager.Instance.Folios}";
    }

    private void BuildUpgradeRows()
    {
        if (upgradeListParent == null) return;
        foreach (Transform child in upgradeListParent)
            Destroy(child.gameObject);

        SpawnSectionHeader("PERMANENT UPGRADES");
        foreach (var (key, name, desc, cost) in Upgrades)
            SpawnRow(key, name, desc, cost);

        SpawnSectionHeader("CHALLENGES");
        foreach (var (key, name, desc, reward) in MetaProgressionManager.ChallengeList)
            SpawnChallengeRow(key, name, desc, reward);

        SpawnSectionHeader("LETTER RESEARCH");
        // Show all 26 letters — common letters are cheap, rare letters are expensive
        for (char c = 'a'; c <= 'z'; c++)
            SpawnLetterResearchRow(c);
    }

    private void SpawnSectionHeader(string title)
    {
        var go = new GameObject("SectionHeader", typeof(RectTransform));
        go.transform.SetParent(upgradeListParent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = 36f;
        le.preferredHeight = 36f;
        var t = go.AddComponent<Text>();
        t.text               = title;
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize           = 18;
        t.fontStyle          = FontStyle.Bold;
        t.color              = new Color(0.97f, 0.88f, 0.40f); // gold
        t.alignment          = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    private void SpawnChallengeRow(string key, string displayName, string desc, int reward)
    {
        var meta    = MetaProgressionManager.Instance;
        bool done   = meta != null && meta.IsChallengeComplete(key);
        string prog = meta?.ChallengeProgressLabel(key) ?? "";

        var card = new GameObject("ChallengeRow", typeof(RectTransform));
        card.transform.SetParent(upgradeListParent, false);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = done
            ? new Color(0.20f, 0.28f, 0.20f)
            : new Color(0.26f, 0.21f, 0.23f);

        var cardOl = card.AddComponent<Outline>();
        cardOl.effectColor    = done
            ? new Color(0.40f, 0.70f, 0.40f, 0.5f)
            : new Color(0.40f, 0.35f, 0.37f, 0.5f);
        cardOl.effectDistance = new Vector2(2f, -2f);

        var cardLe = card.AddComponent<LayoutElement>();
        cardLe.minHeight       = 80f;
        cardLe.preferredHeight = 80f;

        var hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 12f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(16, 16, 8, 8);

        var textCol = new GameObject("TextCol", typeof(RectTransform));
        textCol.transform.SetParent(card.transform, false);
        var tcLe = textCol.AddComponent<LayoutElement>();
        tcLe.preferredWidth = 680f;
        tcLe.minWidth       = 400f;
        tcLe.flexibleWidth  = 1f;
        var tcVlg = textCol.AddComponent<VerticalLayoutGroup>();
        tcVlg.childControlWidth     = true;
        tcVlg.childControlHeight    = false;
        tcVlg.childForceExpandWidth = true;
        tcVlg.spacing = 4f;
        AddText(textCol.transform, "ChalName",  displayName, 18, bold: true);
        AddText(textCol.transform, "ChalDesc",  desc,        15, bold: false,
                color: new Color(0.80f, 0.76f, 0.78f));
        if (!string.IsNullOrEmpty(prog))
            AddText(textCol.transform, "ChalProg",  prog, 14, bold: false,
                    color: done ? new Color(0.50f, 0.85f, 0.50f) : new Color(0.70f, 0.65f, 0.67f));

        // Reward badge
        var badgeGo = new GameObject("Reward", typeof(RectTransform));
        badgeGo.transform.SetParent(card.transform, false);
        var badgeLe = badgeGo.AddComponent<LayoutElement>();
        badgeLe.minWidth       = 80f;
        badgeLe.preferredWidth = 80f;
        badgeLe.minHeight      = 56f;
        var badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.color = done
            ? new Color(0.22f, 0.42f, 0.22f)
            : new Color(0.32f, 0.27f, 0.20f);

        var badgeTxtGo = new GameObject("Text", typeof(RectTransform));
        badgeTxtGo.transform.SetParent(badgeGo.transform, false);
        var bRt = badgeTxtGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
        var badgeTxt = badgeTxtGo.AddComponent<Text>();
        badgeTxt.text               = done ? "DONE" : $"+{reward} F";
        badgeTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        badgeTxt.fontSize           = 15;
        badgeTxt.fontStyle          = FontStyle.Bold;
        badgeTxt.color              = done ? new Color(0.50f, 0.90f, 0.50f) : new Color(0.97f, 0.88f, 0.40f);
        badgeTxt.alignment          = TextAnchor.MiddleCenter;
        badgeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        badgeTxt.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    private void SpawnRow(string key, string displayName, string desc, int cost)
    {
        var meta    = MetaProgressionManager.Instance;
        bool owned  = meta != null && meta.HasIndexUpgrade(key);
        bool afford = meta != null && meta.Folios >= cost;

        // ── Row card ──────────────────────────────────────────────────────────
        var card = new GameObject("IndexRow", typeof(RectTransform));
        card.transform.SetParent(upgradeListParent, false);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = owned
            ? new Color(0.20f, 0.28f, 0.20f)   // dark green — owned
            : afford
                ? new Color(0.40f, 0.32f, 0.36f) // mauve — can afford
                : new Color(0.18f, 0.14f, 0.16f); // very dark — can't afford

        var cardOl = card.AddComponent<Outline>();
        cardOl.effectColor    = owned
            ? new Color(0.40f, 0.70f, 0.40f, 0.6f)
            : new Color(0.65f, 0.52f, 0.58f, 0.6f);
        cardOl.effectDistance = new Vector2(2f, -2f);

        var cardLe = card.AddComponent<LayoutElement>();
        cardLe.minHeight       = 80f;
        cardLe.preferredHeight = 80f;

        var hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 12f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(16, 16, 8, 8);

        // ── Text column ───────────────────────────────────────────────────────
        var textCol = new GameObject("TextCol", typeof(RectTransform));
        textCol.transform.SetParent(card.transform, false);
        var tcLe = textCol.AddComponent<LayoutElement>();
        tcLe.preferredWidth  = 680f;
        tcLe.minWidth        = 400f;
        tcLe.flexibleWidth   = 1f;
        var tcVlg = textCol.AddComponent<VerticalLayoutGroup>();
        tcVlg.childControlWidth     = true;
        tcVlg.childControlHeight    = false;
        tcVlg.childForceExpandWidth = true;
        tcVlg.spacing = 4f;

        AddText(textCol.transform, "UpgName", displayName, 20, bold: true);
        AddText(textCol.transform, "UpgDesc", desc, 16, bold: false,
                color: new Color(0.80f, 0.76f, 0.78f));

        // ── Buy button / status ───────────────────────────────────────────────
        var btnGo = new GameObject("BuyBtn", typeof(RectTransform));
        btnGo.transform.SetParent(card.transform, false);
        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.minWidth       = 120f;
        btnLe.preferredWidth = 120f;
        btnLe.minHeight      = 56f;
        btnLe.preferredHeight = 56f;

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = owned
            ? new Color(0.22f, 0.42f, 0.22f)   // green — owned
            : afford
                ? new Color(0.35f, 0.55f, 0.25f) // olive — buy
                : new Color(0.25f, 0.20f, 0.22f); // dark — can't afford

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.interactable  = !owned && afford;

        var btnTxtGo = new GameObject("Text", typeof(RectTransform));
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero;
        btnTxtRt.offsetMax = Vector2.zero;
        var btnTxt = btnTxtGo.AddComponent<Text>();
        btnTxt.text               = owned ? "OWNED" : $"{cost} F";
        btnTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnTxt.fontSize           = 16;
        btnTxt.fontStyle          = FontStyle.Bold;
        btnTxt.color              = Color.white;
        btnTxt.alignment          = TextAnchor.MiddleCenter;
        btnTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        btnTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        if (!owned)
        {
            var capturedKey = key;
            btn.onClick.AddListener(() => OnBuyClicked(capturedKey));
        }
    }

    private void SpawnLetterResearchRow(char c)
    {
        var meta = MetaProgressionManager.Instance;
        int level    = meta != null ? meta.GetLetterResearchLevel(c) : 0;
        bool maxed   = level >= 2;
        int  cost    = meta != null ? meta.LetterResearchCost(c, level) : 0;
        bool afford  = meta != null && meta.Folios >= cost && !maxed;

        // Chip value display: find base from assets
        var allLetters = Resources.LoadAll<LetterData>("Letters");
        int baseChips = 1;
        foreach (var ld in allLetters)
            if (ld.letter == char.ToLower(c)) { baseChips = ld.baseChipValue; break; }
        int totalBonus = level;

        var card = new GameObject("LetterRow_" + c, typeof(RectTransform));
        card.transform.SetParent(upgradeListParent, false);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = maxed
            ? new Color(0.20f, 0.28f, 0.20f)
            : afford
                ? new Color(0.26f, 0.20f, 0.28f)
                : new Color(0.18f, 0.14f, 0.16f);

        var cardOl = card.AddComponent<Outline>();
        cardOl.effectColor    = maxed
            ? new Color(0.40f, 0.70f, 0.40f, 0.5f)
            : new Color(0.55f, 0.40f, 0.60f, 0.5f);
        cardOl.effectDistance = new Vector2(2f, -2f);

        var cardLe = card.AddComponent<LayoutElement>();
        cardLe.minHeight       = 64f;
        cardLe.preferredHeight = 64f;

        var hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 12f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(16, 16, 8, 8);

        // Letter badge (large letter)
        var letterBadge = new GameObject("LetterBadge", typeof(RectTransform));
        letterBadge.transform.SetParent(card.transform, false);
        var lbLe = letterBadge.AddComponent<LayoutElement>();
        lbLe.minWidth       = 48f;
        lbLe.preferredWidth = 48f;
        var lbTxt = letterBadge.AddComponent<Text>();
        lbTxt.text               = char.ToUpper(c).ToString();
        lbTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbTxt.fontSize           = 28;
        lbTxt.fontStyle          = FontStyle.Bold;
        lbTxt.color              = maxed ? new Color(0.50f, 0.85f, 0.50f) : Color.white;
        lbTxt.alignment          = TextAnchor.MiddleCenter;
        lbTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        lbTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        // Text column
        var textCol = new GameObject("TextCol", typeof(RectTransform));
        textCol.transform.SetParent(card.transform, false);
        var tcLe = textCol.AddComponent<LayoutElement>();
        tcLe.preferredWidth = 580f;
        tcLe.minWidth       = 300f;
        tcLe.flexibleWidth  = 1f;
        var tcVlg = textCol.AddComponent<VerticalLayoutGroup>();
        tcVlg.childControlWidth     = true;
        tcVlg.childControlHeight    = false;
        tcVlg.childForceExpandWidth = true;
        tcVlg.spacing = 2f;

        string chipLabel = totalBonus > 0
            ? $"Chip value: {baseChips} +{totalBonus} bonus"
            : $"Chip value: {baseChips}";
        AddText(textCol.transform, "ChipVal", chipLabel, 15, bold: false,
                color: new Color(0.80f, 0.76f, 0.88f));

        string levelLabel = maxed ? "Research: MAX" : $"Research: {level}/2";
        AddText(textCol.transform, "Level", levelLabel, 14, bold: false,
                color: maxed ? new Color(0.50f, 0.85f, 0.50f) : new Color(0.65f, 0.60f, 0.70f));

        // Buy / status button
        var btnGo = new GameObject("ResearchBtn", typeof(RectTransform));
        btnGo.transform.SetParent(card.transform, false);
        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.minWidth        = 100f;
        btnLe.preferredWidth  = 100f;
        btnLe.minHeight       = 48f;
        btnLe.preferredHeight = 48f;

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = maxed
            ? new Color(0.22f, 0.42f, 0.22f)
            : afford
                ? new Color(0.42f, 0.28f, 0.50f)
                : new Color(0.25f, 0.20f, 0.22f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.interactable  = afford;

        var btnTxtGo = new GameObject("Text", typeof(RectTransform));
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero;
        btnTxtRt.offsetMax = Vector2.zero;
        var btnTxt = btnTxtGo.AddComponent<Text>();
        btnTxt.text               = maxed ? "MAX" : $"{cost} F";
        btnTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnTxt.fontSize           = 15;
        btnTxt.fontStyle          = FontStyle.Bold;
        btnTxt.color              = maxed ? new Color(0.50f, 0.90f, 0.50f) : Color.white;
        btnTxt.alignment          = TextAnchor.MiddleCenter;
        btnTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        btnTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        if (!maxed)
        {
            var capturedChar = c;
            btn.onClick.AddListener(() => OnResearchClicked(capturedChar));
        }
    }

    private void OnResearchClicked(char c)
    {
        if (MetaProgressionManager.Instance?.ResearchLetter(c) == true)
            Refresh();
    }

    private void OnBuyClicked(string key)
    {
        if (MetaProgressionManager.Instance?.BuyIndexUpgrade(key) == true)
            Refresh();
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }

    private static void AddText(Transform parent, string goName, string text,
                                int fontSize, bool bold,
                                Color? color = null)
    {
        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = fontSize * 1.8f;
        le.preferredHeight = fontSize * 1.8f;
        var t = go.AddComponent<Text>();
        t.text               = text;
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize           = fontSize;
        t.fontStyle          = bold ? FontStyle.Bold : FontStyle.Normal;
        t.color              = color ?? Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }
}
