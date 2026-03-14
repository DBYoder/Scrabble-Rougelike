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
        foreach (Transform child in shopItemsParent)
            Destroy(child.gameObject);

        UpdateGoldDisplay();

        if (ShopManager.Instance == null) return;

        foreach (var item in ShopManager.Instance.currentShopItems)
            SpawnShopEntry(item);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
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

        // Show "FULL" instead of the price when the lexicon has no room
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
