// UIStyleConfig.cs — Central colour palette and button-style constants for the UI.
// Create one asset at Assets/Data/UIStyleConfig.asset before running SceneBuilder.
using UnityEngine;

public enum ButtonType { Confirm, Danger, Neutral, Secondary, BossPreview }

[CreateAssetMenu(menuName = "Crossword/UI Style Config", fileName = "UIStyleConfig")]
public class UIStyleConfig : ScriptableObject
{
    // ── Backgrounds ──────────────────────────────────────────────────────────
    [Header("Backgrounds")]
    public Color panelBg      = new Color(0.180f, 0.141f, 0.157f);  // #2E2428 darkest mauve
    public Color panelBgLight = new Color(0.259f, 0.208f, 0.220f);  // #423538
    public Color panelBgMid   = new Color(0.412f, 0.345f, 0.373f);  // #69585F

    // ── Buttons — face colour ─────────────────────────────────────────────
    [Header("Buttons")]
    public Color btnConfirm     = new Color(0.278f, 0.435f, 0.302f);  // #476F4D muted forest green
    public Color btnDanger      = new Color(0.580f, 0.251f, 0.290f);  // #94404A dusty rose-red
    public Color btnNeutral     = new Color(0.420f, 0.439f, 0.278f);  // #6B7047 olive
    public Color btnSecondary   = new Color(0.412f, 0.345f, 0.373f);  // #69585F dark mauve
    public Color btnBossPreview = new Color(0.549f, 0.318f, 0.125f);  // #8C5120 amber-brown

    // ── Button dimensions ─────────────────────────────────────────────────
    [Header("Button Dimensions")]
    public float btnWidthWide   = 220f;
    public float btnHeightTall  = 60f;
    public float btnWidthStd    = 160f;
    public float btnHeightStd   = 50f;
    public float btnWidthAction = 140f;
    public float btnHeightAction = 55f;

    // ── Cell modifiers ────────────────────────────────────────────────────
    [Header("Cell Modifiers")]
    public Color cellTW = new Color(0.937f, 0.627f, 0.314f);  // #EF9F50 warm orange — DISTINCT from invalid
    public Color cellDW = new Color(0.970f, 0.760f, 0.780f);  // #F7C2C7 light rose
    public Color cellTL = new Color(0.600f, 0.780f, 0.620f);  // #9AC79E sage green
    public Color cellDL = new Color(0.851f, 0.859f, 0.737f);  // #D9DBBC light olive

    // ── Tile states ───────────────────────────────────────────────────────
    [Header("Tile States")]
    public Color tileEmpty    = new Color(0.870f, 0.867f, 0.839f);  // #DEDDD6
    public Color tileOccupied = new Color(0.851f, 0.604f, 0.278f);  // #D99A47 warm amber
    public Color tileTurnValid  = new Color(0.580f, 0.761f, 0.878f);  // #94C2E0 slate blue
    public Color tileInvalid    = new Color(0.937f, 0.584f, 0.616f);  // #EF959D rose pink

    // ── HUD ───────────────────────────────────────────────────────────────
    [Header("HUD")]
    public Color hudGold        = new Color(1.000f, 0.882f, 0.251f);  // #FFE140 gold
    public Color hudLives       = new Color(0.937f, 0.400f, 0.416f);  // #EF6670 rose-red
    public Color hudScore       = new Color(0.929f, 0.918f, 0.835f);  // #EDEAD5 near-white warm
    public Color hudBossWarning = new Color(0.937f, 0.627f, 0.314f);  // same as cellTW orange
    public Color lexiconLabel   = new Color(0.784f, 0.698f, 0.902f);  // #C8B2E6 soft lavender
    public Color textPrimary    = new Color(0.929f, 0.918f, 0.835f);  // #EDEAD5 near-white warm
    public Color textSecondary  = new Color(0.722f, 0.663f, 0.686f);  // #B8A9AF muted mauve-grey
    public Color textMuted      = new Color(0.478f, 0.408f, 0.439f);  // #7A6870 dark muted

    // ── Shop item backgrounds ─────────────────────────────────────────────
    [Header("Shop Items")]
    public Color shopAffordable  = new Color(0.412f, 0.345f, 0.373f);  // #69585F dark mauve
    public Color shopCantAfford  = new Color(0.180f, 0.141f, 0.165f);  // #2E2429 very dark
    public Color shopLexiconFull = new Color(0.294f, 0.243f, 0.165f);  // #4B3E2A dark ochre

    // ── Sprites ───────────────────────────────────────────────────────────────
    // Assign sprites here to replace solid-colour rectangles with real artwork.
    // Leave any field empty to keep the existing solid-colour fallback.

    [Header("Panel Sprites (9-slice)")]
    public Sprite panelBgSprite;        // MainMenuPanel, ShopPanel, UpgradePanel, GameOver, Victory
    public Sprite panelDarkSprite;      // GamePanel, ScoreOverlay, BossPreviewPanel
    public Sprite sidebarSprite;        // LexiconSidebar, ScoringLexiconSidebar
    public Sprite stripSprite;          // TopBar, HandArea

    [Header("Button Sprites (9-slice)")]
    public Sprite btnConfirmSprite;
    public Sprite btnDangerSprite;
    public Sprite btnNeutralSprite;
    public Sprite btnSecondarySprite;

    [Header("Tile Sprites")]
    public Sprite tileCardSprite;       // TileCard prefab + ScoreUI WordTile
    public Sprite gridCellSprite;       // GridCell prefab (empty cell background)
    public Sprite gridCellModTWSprite;  // Triple Word cell (optional distinct sprite)
    public Sprite gridCellModDWSprite;  // Double Word cell
    public Sprite gridCellModTLSprite;  // Triple Letter cell
    public Sprite gridCellModDLSprite;  // Double Letter cell

    [Header("Card Sprites")]
    public Sprite lexiconCardSprite;
    public Sprite shopItemSprite;
    public Sprite upgradeOptionSprite;
    public Sprite wordRowTileSprite;    // WordTile prefab used in ScoreUI scoring animation
}
