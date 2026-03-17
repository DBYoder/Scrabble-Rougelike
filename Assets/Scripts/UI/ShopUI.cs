// ShopUI.cs — Renders the shop panel and handles purchase clicks.
// shopItemPrefab should have children: Text "ItemName", Text "ItemDesc",
// Text "ItemCost", Image "ItemIcon", Button (root).
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("References")]
    public Transform  shopItemsParent;
    public GameObject shopItemPrefab;
    public Text       goldText;
    public Button     leaveShopButton;

    [Header("Your Lexicons (sell row)")]
    public Transform yourLexiconsParent;  // HLG row — populated at RefreshShop
    public Text      yourLexiconsLabel;   // Section header, hidden when no lexicons owned

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (leaveShopButton != null)
            leaveShopButton.onClick.AddListener(OnLeaveShop);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void RefreshShop()
    {
        // Rebuild shop items
        foreach (Transform child in shopItemsParent)
            Destroy(child.gameObject);

        UpdateGoldDisplay();

        if (ShopManager.Instance != null)
            foreach (var item in ShopManager.Instance.currentShopItems)
                SpawnShopEntry(item);

        // Rebuild "YOUR LEXICONS" sell row
        RefreshYourLexicons();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private void RefreshYourLexicons()
    {
        if (yourLexiconsParent == null) return;

        foreach (Transform child in yourLexiconsParent)
            Destroy(child.gameObject);

        bool hasLexicons = RunManager.Instance != null
                        && RunManager.Instance.activeLexicon.Count > 0;

        if (yourLexiconsLabel != null)
            yourLexiconsLabel.gameObject.SetActive(hasLexicons);

        if (!hasLexicons) return;

        foreach (var lex in RunManager.Instance.activeLexicon)
            SpawnYourLexiconEntry(lex);
    }

    private void SpawnYourLexiconEntry(LexiconWordData lex)
    {
        int refund = Mathf.Max(1, lex.shopCost / 2);

        // ── Root card ──────────────────────────────────────────────────────────
        var card = new GameObject("LexiconSellCard", typeof(RectTransform));
        card.transform.SetParent(yourLexiconsParent, false);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.26f, 0.21f, 0.23f); // dark mauve

        var cardLe = card.AddComponent<LayoutElement>();
        cardLe.preferredWidth  = 180f;
        cardLe.preferredHeight = 110f;

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 4;
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        // ── Name label ─────────────────────────────────────────────────────────
        var nameGo = new GameObject("LexName", typeof(RectTransform));
        nameGo.transform.SetParent(card.transform, false);
        var nameLe = nameGo.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 56f;
        var nameTxt = nameGo.AddComponent<Text>();
        nameTxt.text               = lex.displayName;
        nameTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameTxt.fontSize           = 15;
        nameTxt.fontStyle          = FontStyle.Bold;
        nameTxt.color              = Color.white;
        nameTxt.alignment          = TextAnchor.UpperCenter;
        nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameTxt.verticalOverflow   = VerticalWrapMode.Overflow;
        nameTxt.resizeTextForBestFit = true;
        nameTxt.resizeTextMinSize    = 10;
        nameTxt.resizeTextMaxSize    = 15;

        // ── Sell button ────────────────────────────────────────────────────────
        var sellGo = new GameObject("SellButton", typeof(RectTransform));
        sellGo.transform.SetParent(card.transform, false);
        sellGo.AddComponent<Image>().color = new Color(0.55f, 0.32f, 0.13f); // amber
        var sellBtn = sellGo.AddComponent<Button>();
        var sellLe  = sellGo.AddComponent<LayoutElement>();
        sellLe.preferredHeight = 36f;

        var btnTxtGo = new GameObject("Text", typeof(RectTransform));
        btnTxtGo.transform.SetParent(sellGo.transform, false);
        var btnRt = btnTxtGo.GetComponent<RectTransform>();
        btnRt.anchorMin = Vector2.zero;
        btnRt.anchorMax = Vector2.one;
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        var btnTxt = btnTxtGo.AddComponent<Text>();
        btnTxt.text               = $"SELL  +{refund}g";
        btnTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnTxt.fontSize           = 14;
        btnTxt.fontStyle          = FontStyle.Bold;
        btnTxt.color              = Color.white;
        btnTxt.alignment          = TextAnchor.MiddleCenter;
        btnTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        btnTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        // Wire click — sell the lexicon, then do a full refresh so blocked shop
        // items re-enable and the lexicon row shrinks by one card.
        var capturedLex = lex;
        sellBtn.onClick.AddListener(() => OnSellLexiconClicked(capturedLex));
    }

    private void OnSellLexiconClicked(LexiconWordData lex)
    {
        RunManager.Instance?.SellLexicon(lex);
        RunUI.Instance?.RefreshHUD();
        RunUI.Instance?.RefreshLexiconBar();
        // Full RefreshShop re-enables any previously-blocked lexicon buy buttons
        // and rebuilds the sell row with the now-shorter active lexicon list.
        // ShopManager.currentShopItems is unchanged — we keep unsold items.
        RefreshShop();
    }

    private void SpawnShopEntry(ShopItemData item)
    {
        var entry = Instantiate(shopItemPrefab, shopItemsParent);

        foreach (var t in entry.GetComponentsInChildren<Text>())
        {
            switch (t.name)
            {
                case "ItemName": t.text = item.itemName;    break;
                case "ItemDesc": t.text = item.description; break;
                case "ItemCost": t.text = $"${item.cost}";  break;
            }
        }

        // Icon
        var iconImg = entry.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (iconImg != null && item.icon != null)
            iconImg.sprite = item.icon;

        // Lexicon entries are also blocked when the player's lexicon is full
        bool lexiconFull = item.itemType == ShopItemType.LexiconEntry
                           && !RunManager.Instance.CanAddLexicon();

        bool canAfford = RunManager.Instance.gold >= item.cost;
        bool canBuy    = canAfford && !lexiconFull;

        var bg = entry.GetComponent<Image>();
        if (bg != null)
        {
            if (lexiconFull)
                bg.color = new Color(0.25f, 0.22f, 0.14f);       // dark ochre — capacity full
            else if (canAfford)
                bg.color = new Color(0.412f, 0.345f, 0.373f);    // dark mauve — can afford
            else
                bg.color = new Color(0.18f, 0.14f, 0.16f);       // very dark — can't afford
        }

        // Show "FULL — sell a Lexicon below" instead of the price when at capacity
        if (lexiconFull)
            foreach (var t in entry.GetComponentsInChildren<Text>())
                if (t.name == "ItemCost") { t.text = "FULL"; break; }

        var btn = entry.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = canBuy;
            var capturedItem  = item;
            var capturedEntry = entry;
            btn.onClick.AddListener(() => OnPurchaseClicked(capturedItem, capturedEntry));
        }
    }

    private void OnPurchaseClicked(ShopItemData item, GameObject entry)
    {
        if (ShopManager.Instance.Purchase(item))
        {
            Destroy(entry);
            UpdateGoldDisplay();
            RefreshYourLexicons(); // update sell row in case lexicon count changed
            RunUI.Instance?.RefreshHUD();
            RunUI.Instance?.RefreshLexiconBar();
        }
    }

    private void UpdateGoldDisplay()
    {
        if (goldText != null && RunManager.Instance != null)
            goldText.text = $"Gold: {RunManager.Instance.gold}";
    }

    private void OnLeaveShop()
    {
        GameManager.Instance.ChangeState(GameState.Upgrade);
    }
}
