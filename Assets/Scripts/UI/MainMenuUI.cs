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

    [Header("Mode Cards")]
    public RectTransform modeCardsParent;
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
        if (landingScreen    != null) landingScreen.SetActive(true);
        if (modeSelectScreen != null) modeSelectScreen.SetActive(false);
    }

    public void ShowModeSelect()
    {
        if (landingScreen    != null) landingScreen.SetActive(false);
        if (modeSelectScreen != null) modeSelectScreen.SetActive(true);
        PopulateModeCards();
    }

    private void PopulateModeCards()
    {
        if (modeCardsParent == null || modeCardPrefab == null) return;

        // Clear existing cards
        foreach (Transform child in modeCardsParent)
            Destroy(child.gameObject);

        var configs = Resources.LoadAll<RunConfig>("RunConfigs");
        foreach (var cfg in configs)
        {
            var card = Instantiate(modeCardPrefab, modeCardsParent);
            card.GetComponent<ModeCardUI>()?.Populate(cfg);
        }
    }
}
