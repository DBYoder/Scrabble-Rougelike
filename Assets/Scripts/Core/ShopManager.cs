// ShopManager.cs — Generates a shop inventory and handles purchases.
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Config")]
    public int lexiconSlots = 2;  // How many Lexicon entries appear in shop
    public int miscSlots    = 3;  // How many misc items appear in shop

    public List<ShopItemData> currentShopItems = new List<ShopItemData>();

    private ShopItemData[]   allShopItems;
    private LexiconWordData[] allLexiconItems;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        allShopItems    = Resources.LoadAll<ShopItemData>("Shop");
        allLexiconItems = Resources.LoadAll<LexiconWordData>("Lexicon");
    }

    // ── Shop Generation ───────────────────────────────────────────────────────
    public void GenerateShop()
    {

        currentShopItems.Clear();

        // --- Lexicon entries ---
        var availableLexicon = new List<LexiconWordData>(allLexiconItems);
        availableLexicon.RemoveAll(l => RunManager.Instance.activeLexicon.Contains(l));

        int lexCount = Mathf.Min(lexiconSlots, availableLexicon.Count);
        for (int i = 0; i < lexCount; i++)
        {
            int idx = Random.Range(0, availableLexicon.Count);
            currentShopItems.Add(WrapLexiconAsShopItem(availableLexicon[idx]));
            availableLexicon.RemoveAt(idx);
        }

        // --- Misc items ---
        var miscPool = new List<ShopItemData>(allShopItems ?? new ShopItemData[0]);
        int miscCount = Mathf.Min(miscSlots, miscPool.Count);
        for (int i = 0; i < miscCount; i++)
        {
            int idx = Random.Range(0, miscPool.Count);
            currentShopItems.Add(miscPool[idx]);
            miscPool.RemoveAt(idx);
        }
    }

    // ── Purchase ─────────────────────────────────────────────────────────────
    /// <summary>Attempts to buy the item. Returns true on success.</summary>
    public bool Purchase(ShopItemData item)
    {
        // Validate lexicon capacity before spending gold so the player isn't charged
        // for an entry they can't receive.
        if (item.itemType == ShopItemType.LexiconEntry
            && (item.lexiconRef == null || !RunManager.Instance.CanAddLexicon()))
            return false;

        if (!RunManager.Instance.SpendGold(item.cost)) return false;

        switch (item.itemType)
        {
            case ShopItemType.LexiconEntry:
                if (item.lexiconRef != null && RunManager.Instance.CanAddLexicon())
                    RunManager.Instance.AddLexicon(item.lexiconRef);
                break;

            case ShopItemType.LetterUpgrade:
                if (item.letterRef != null)
                    item.letterRef.chipValue += item.upgradeValue;
                break;

            case ShopItemType.ExtraRedraw:
                if (TileHandManager.Instance != null)
                    TileHandManager.Instance.bonusRedraws += item.upgradeValue;
                break;

            case ShopItemType.ScoreBoost:
                RunManager.Instance.AddScore(item.upgradeValue);
                break;

            case ShopItemType.LifeRestore:
                if (RunManager.Instance.lives < RunManager.MaxLives)
                    RunManager.Instance.lives++;
                break;
        }

        currentShopItems.Remove(item);
        return true;
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private ShopItemData WrapLexiconAsShopItem(LexiconWordData lex)
    {
        var item = ScriptableObject.CreateInstance<ShopItemData>();
        item.itemName    = lex.displayName;
        item.description = lex.effectDescription;
        item.itemType    = ShopItemType.LexiconEntry;
        item.cost        = lex.shopCost;
        item.lexiconRef  = lex;
        item.icon        = lex.artwork;
        return item;
    }
}
