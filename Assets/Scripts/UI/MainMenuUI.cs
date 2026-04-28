// MainMenuUI.cs — Manages the two sub-screens within the MainMenuPanel:
//   LandingScreen    (title + NEW GAME / SETTINGS buttons)
//   ModeSelectScreen (mode cards + BACK button)
// Mode cards are populated at runtime from Resources/RunConfigs/.
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public static MainMenuUI Instance { get; private set; }

    [Header("Sub-Screens")]
    public GameObject landingScreen;
    public GameObject modeSelectScreen;
    public GameObject profileSelectScreen;
    public GameObject indexScreen;

    [Header("The Index")]
    public IndexUI indexUI;

    [Header("Mode Cards")]
    public RectTransform modeCardsParent;
    public RectTransform profileCardsParent;
    public GameObject    modeCardPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ShowLanding();
    }

    public void ShowLanding()
    {
        SetScreens(landing: true);
    }

    public void ShowModeSelect()
    {
        SetScreens(modeSelect: true);
        PopulateModeCards();
    }

    public void ShowProfileSelect()
    {
        SetScreens(profileSelect: true);
        PopulateProfileCards();
    }

    public void ShowIndex()
    {
        SetScreens(index: true);
        indexUI?.Refresh();
    }

    private void SetScreens(bool landing = false, bool modeSelect = false,
                            bool profileSelect = false, bool index = false)
    {
        if (landingScreen        != null) landingScreen.SetActive(landing);
        if (modeSelectScreen     != null) modeSelectScreen.SetActive(modeSelect);
        if (profileSelectScreen  != null) profileSelectScreen.SetActive(profileSelect);
        if (indexScreen          != null) indexScreen.SetActive(index);
    }

    private void PopulateModeCards()
    {
        if (modeCardsParent == null || modeCardPrefab == null) return;
        foreach (Transform child in modeCardsParent) Destroy(child.gameObject);
        var configs = Resources.LoadAll<RunConfig>("RunConfigs");
        foreach (var cfg in configs)
        {
            if (!string.IsNullOrEmpty(cfg.unlockKey)) continue;
            Instantiate(modeCardPrefab, modeCardsParent).GetComponent<ModeCardUI>()?.Populate(cfg);
        }
    }

    private void PopulateProfileCards()
    {
        if (profileCardsParent == null || modeCardPrefab == null) return;
        foreach (Transform child in profileCardsParent) Destroy(child.gameObject);
        var configs = Resources.LoadAll<RunConfig>("RunConfigs");
        foreach (var cfg in configs)
        {
            if (string.IsNullOrEmpty(cfg.unlockKey)) continue;
            Instantiate(modeCardPrefab, profileCardsParent).GetComponent<ModeCardUI>()?.Populate(cfg);
        }
    }
}
