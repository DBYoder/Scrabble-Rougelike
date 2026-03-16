// SceneBuilder.cs — Editor-only script.
// Menu: Crossword → Build Scene & Prefabs
// Creates all 6 prefabs and builds the full Main scene with wired references.
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SceneBuilder
{
    // ── Constants ──────────────────────────────────────────────────────────────
    private const string PrefabDir    = "Assets/Prefabs";
    private const string SceneDir     = "Assets/Scenes";
    private const string ScenePath    = "Assets/Scenes/Main.unity";
    private const string StyleCfgPath = "Assets/Data/UIStyleConfig.asset";

    // Loaded once at the start of BuildAll(). Null-safe: each usage checks _style != null.
    private static UIStyleConfig _style;

    // ── Entry Point ───────────────────────────────────────────────────────────
    [MenuItem("Crossword/Build Scene & Prefabs")]
    public static void BuildAll()
    {
        // Load central style config — if missing, colours fall back to the hardcoded defaults.
        _style = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(StyleCfgPath);
        if (_style == null)
            Debug.LogWarning($"[SceneBuilder] UIStyleConfig not found at {StyleCfgPath}. " +
                             "Create it via Assets > Create > Crossword > UI Style Config.");

        EnsureDirectories();

        // Part A — Prefabs
        var cellPrefab          = BuildGridCellPrefab();
        var tileCardPrefab      = BuildTileCardPrefab();
        var shopItemPrefab      = BuildShopItemPrefab();
        var lexiconCardPrefab   = BuildLexiconCardPrefab();
        var upgradeOptionPrefab = BuildUpgradeOptionPrefab();
        var wordTilePrefab      = BuildWordTilePrefab();
        var wordRowPrefab       = BuildWordRowPrefab();

        BuildScene(cellPrefab, tileCardPrefab, shopItemPrefab,
                   lexiconCardPrefab, upgradeOptionPrefab,
                   wordTilePrefab, wordRowPrefab);
    }

    // ── Wire Scene Only ───────────────────────────────────────────────────────
    // Rebuilds the scene and wires all references using the prefabs currently on
    // disk — your manual prefab edits are preserved.  Use this instead of
    // "Build Scene & Prefabs" whenever you only need to re-wire after adding a
    // new panel, manager, or UI element.
    [MenuItem("Crossword/Wire Scene Only")]
    public static void WireSceneOnly()
    {
        _style = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(StyleCfgPath);
        if (_style == null)
            Debug.LogWarning($"[SceneBuilder] UIStyleConfig not found at {StyleCfgPath}. " +
                             "Create it via Assets > Create > Crossword > UI Style Config.");

        EnsureDirectories();

        // Load each prefab from disk; fall back to building it if missing.
        var cellPrefab          = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/GridCell.prefab")      ?? BuildGridCellPrefab();
        var tileCardPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/TileCard.prefab")      ?? BuildTileCardPrefab();
        var shopItemPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/ShopItem.prefab")      ?? BuildShopItemPrefab();
        var lexiconCardPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/LexiconCard.prefab")   ?? BuildLexiconCardPrefab();
        var upgradeOptionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/UpgradeOption.prefab") ?? BuildUpgradeOptionPrefab();
        var wordTilePrefab      = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/WordTile.prefab")      ?? BuildWordTilePrefab();
        var wordRowPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/WordRow.prefab")       ?? BuildWordRowPrefab();

        BuildScene(cellPrefab, tileCardPrefab, shopItemPrefab,
                   lexiconCardPrefab, upgradeOptionPrefab,
                   wordTilePrefab, wordRowPrefab);
    }

    // ── Create Missing Prefabs ────────────────────────────────────────────────
    // Creates only the prefabs that do not already exist on disk.
    // Safe to run after editing prefabs in the Unity editor — skips any that are
    // already present so your changes are not overwritten.
    [MenuItem("Crossword/Create Missing Prefabs")]
    public static void CreateMissingPrefabs()
    {
        _style = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(StyleCfgPath);
        EnsureDirectories();
        if (!PrefabExists("GridCell"))      BuildGridCellPrefab();
        if (!PrefabExists("TileCard"))      BuildTileCardPrefab();
        if (!PrefabExists("ShopItem"))      BuildShopItemPrefab();
        if (!PrefabExists("LexiconCard"))   BuildLexiconCardPrefab();
        if (!PrefabExists("UpgradeOption")) BuildUpgradeOptionPrefab();
        if (!PrefabExists("WordTile"))      BuildWordTilePrefab();
        if (!PrefabExists("WordRow"))       BuildWordRowPrefab();
        AssetDatabase.Refresh();
        Debug.Log("[SceneBuilder] Create Missing Prefabs done.");
    }

    private static bool PrefabExists(string name) =>
        AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab") != null;

    // ── Build Scene ───────────────────────────────────────────────────────────
    // Shared by BuildAll() and WireSceneOnly(). Prefab assets are passed in as
    // parameters so WireSceneOnly() can supply the on-disk prefabs unchanged.
    private static void BuildScene(
        GameObject cellPrefab,          GameObject tileCardPrefab,
        GameObject shopItemPrefab,      GameObject lexiconCardPrefab,
        GameObject upgradeOptionPrefab, GameObject wordTilePrefab,
        GameObject wordRowPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Main Camera
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var cam = cameraGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.14f, 0.16f); // dark mauve
        cam.orthographic    = true;
        cameraGo.AddComponent<AudioListener>();

        // EventSystem
        CreateEventSystem();

        // GameManager object (holds all manager components)
        var gmGo = new GameObject("GameManager");
        var gm   = gmGo.AddComponent<GameManager>();
        gmGo.AddComponent<RunManager>();
        gmGo.AddComponent<GridManager>();
        gmGo.AddComponent<TileHandManager>();
        gmGo.AddComponent<WordValidator>();
        gmGo.AddComponent<ScoreManager>();
        gmGo.AddComponent<ShopManager>();
        var menuButtons = gmGo.AddComponent<MenuButtons>();

        // UI Root
        var canvasGo = CreateCanvas();

        // ── HUD ───────────────────────────────────────────────────────────────
        var hudGo  = CreatePanel(canvasGo.transform, "HUD", transparent: true);
        var runUI  = hudGo.AddComponent<RunUI>();
        StretchFull(hudGo.GetComponent<RectTransform>());

        // TopBar — 50px strip at top. Shows only: lives, ante progress, featured letter.
        var topBar = CreateStrip(hudGo.transform, "TopBar", isTop: true, thickness: 50f);
        AddHLG(topBar, 16, TextAnchor.MiddleLeft);
        var livesText         = AddLabel(topBar.transform, "LivesText",          "♥ 3", fontSize: 20, bold: true);
        livesText.color = new Color(0.95f, 0.55f, 0.60f); // rose
        var anteText          = AddLabel(topBar.transform, "AnteText",           "Ante 1/8  ·  Blind 1/3");
        var featuredLetterTxt = AddLabel(topBar.transform, "FeaturedLetterText", "", fontSize: 22, bold: true);
        featuredLetterTxt.color = new Color(1f, 0.88f, 0.25f); // gold — stands out from other labels

        // Wire RunUI (HUD labels only — lexicon parents wired after GamePanel/ScoringPanel are built)
        runUI.anteText           = anteText;
        runUI.livesText          = livesText;
        runUI.featuredLetterText = featuredLetterTxt;
        runUI.lexiconCardPrefab  = lexiconCardPrefab;

        // HUD must render on top of all full-screen panels (GamePanel, ShopPanel etc.)
        // Add a nested Canvas with overrideSorting so it always draws above the root Canvas
        var hudCanvas = hudGo.AddComponent<Canvas>();
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder    = 10;
        hudGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── MainMenuPanel ─────────────────────────────────────────────────────
        var mainMenuGo = CreatePanel(canvasGo.transform, "MainMenuPanel",
                                     transparent: false, color: new Color(0.26f, 0.21f, 0.23f));
        StretchFull(mainMenuGo.GetComponent<RectTransform>());
        AddVLG(mainMenuGo, 20, TextAnchor.MiddleCenter);

        AddLabel(mainMenuGo.transform, "TitleText",    "CROSSWORD ROGUELIKE",
                 fontSize: 52, bold: true);
        AddLabel(mainMenuGo.transform, "SubtitleText", "Build words. Build power.",
                 fontSize: 24);
        var startBtn  = AddButton(mainMenuGo.transform, "StartButton", "NEW RUN",
                                  new Color(0.28f, 0.50f, 0.30f), width: 220, height: 60);
        StyleButton(startBtn, ButtonType.Confirm);

        // ── GamePanel ─────────────────────────────────────────────────────────
        var gamePanelGo = CreatePanel(canvasGo.transform, "GamePanel",
                                      transparent: false, color: new Color(0.20f, 0.16f, 0.18f));
        StretchFull(gamePanelGo.GetComponent<RectTransform>());

        // GridContainer — centered
        var gridContainerGo = new GameObject("GridContainer", typeof(RectTransform));
        gridContainerGo.transform.SetParent(gamePanelGo.transform, false);
        var gcRt = gridContainerGo.GetComponent<RectTransform>();
        gcRt.anchorMin = new Vector2(0.5f, 0.5f);
        gcRt.anchorMax = new Vector2(0.5f, 0.5f);
        gcRt.pivot     = new Vector2(0.5f, 0.5f);
        gcRt.sizeDelta = new Vector2(460f, 460f); // 9 * (48+2) + wiggle

        // HandArea — 130px strip above bottom
        var handArea = CreateStrip(gamePanelGo.transform, "HandArea", isTop: false, thickness: 130f);
        AddHLG(handArea, 6, TextAnchor.MiddleCenter);
        var tileHandUI = handArea.AddComponent<TileHandUI>();

        // LexiconSidebar — right side vertical card list
        var lexSidebarGo = new GameObject("LexiconSidebar", typeof(RectTransform));
        lexSidebarGo.transform.SetParent(gamePanelGo.transform, false);
        var lsRt = lexSidebarGo.GetComponent<RectTransform>();
        lsRt.anchorMin        = new Vector2(1f, 0.5f);
        lsRt.anchorMax        = new Vector2(1f, 0.5f);
        lsRt.pivot            = new Vector2(1f, 0.5f);
        lsRt.anchoredPosition = new Vector2(-225f, 20f);
        lsRt.sizeDelta        = new Vector2(205f, 530f);
        lexSidebarGo.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.16f, 0.7f);

        // "LEXICONS" label at top
        var lexLabel = CreateText(lexSidebarGo.transform, "LexiconSidebarLabel", "LEXICONS",
                                  fontSize: 16, bold: true, alignment: TextAnchor.UpperCenter,
                                  anchorMin: new Vector2(0, 0.92f), anchorMax: new Vector2(1, 1));
        lexLabel.color = new Color(0.8f, 0.7f, 0.9f);

        // LexiconContent — VLG container for cards
        var lexContentGo = new GameObject("LexiconContent", typeof(RectTransform));
        lexContentGo.transform.SetParent(lexSidebarGo.transform, false);
        var lcRt = lexContentGo.GetComponent<RectTransform>();
        lcRt.anchorMin = new Vector2(0, 0);
        lcRt.anchorMax = new Vector2(1, 0.92f);
        lcRt.offsetMin = Vector2.zero;
        lcRt.offsetMax = Vector2.zero;
        var lcVlg = lexContentGo.AddComponent<VerticalLayoutGroup>();
        lcVlg.spacing              = 6;
        lcVlg.childAlignment       = TextAnchor.UpperCenter;
        lcVlg.childForceExpandWidth  = false;
        lcVlg.childForceExpandHeight = false;
        lcVlg.childControlWidth      = false;
        lcVlg.childControlHeight     = false;
        lcVlg.padding = new RectOffset(2, 2, 4, 4);

        // ActionButtons — VLG on right side
        var actionBtns = new GameObject("ActionButtons", typeof(RectTransform));
        actionBtns.transform.SetParent(gamePanelGo.transform, false);
        var abRt = actionBtns.GetComponent<RectTransform>();
        abRt.anchorMin = new Vector2(1f, 0.5f);
        abRt.anchorMax = new Vector2(1f, 0.5f);
        abRt.pivot     = new Vector2(1f, 0.5f);
        abRt.anchoredPosition = new Vector2(-10f, 0f);
        abRt.sizeDelta = new Vector2(150f, 380f);
        AddVLG(actionBtns, 8, TextAnchor.MiddleCenter);

        var submitBtn       = AddButton(actionBtns.transform, "SubmitButton",        "PLAY WORD",       new Color(0.28f, 0.50f, 0.30f), width: 140, height: 55);
        StyleButton(submitBtn, ButtonType.Confirm);
        var endRoundBtn     = AddButton(actionBtns.transform, "EndRoundButton",      "END ROUND",       new Color(0.58f, 0.25f, 0.32f), width: 140, height: 55);
        StyleButton(endRoundBtn, ButtonType.Danger);
        var redrawBtn       = AddButton(actionBtns.transform, "RedrawButton",        "Redraw (3)",      new Color(0.42f, 0.44f, 0.28f), width: 140, height: 55);
        StyleButton(redrawBtn, ButtonType.Neutral);
        var confirmRedrawBtn= AddButton(actionBtns.transform, "ConfirmRedrawButton", "Confirm Redraw",  new Color(0.55f, 0.40f, 0.24f), width: 140, height: 55);
        StyleButton(confirmRedrawBtn, ButtonType.Neutral);

        // Wire GridUI
        var gridUI = gamePanelGo.AddComponent<GridUI>();
        gridUI.cellPrefab  = cellPrefab;
        gridUI.gridParent  = gridContainerGo.GetComponent<RectTransform>();

        // Score preview panel — left side of game panel (ScrollRect so long breakdowns don't overflow)
        // GameInfoPanel removed — blind/ante/lives info is now shown in the TopBar.
        var previewGo = new GameObject("ScorePreview", typeof(RectTransform));
        previewGo.transform.SetParent(gamePanelGo.transform, false);
        var previewRt = previewGo.GetComponent<RectTransform>();
        previewRt.anchorMin        = new Vector2(0f, 0.5f);
        previewRt.anchorMax        = new Vector2(0f, 0.5f);
        previewRt.pivot            = new Vector2(0f, 0.5f);
        previewRt.anchoredPosition = new Vector2(10f, 0f);   // centred vertically
        previewRt.sizeDelta        = new Vector2(260f, 460f); // taller now that GameInfoPanel is gone
        previewGo.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.16f, 0.92f);

        var previewScroll = previewGo.AddComponent<ScrollRect>();
        previewScroll.horizontal        = false;
        previewScroll.scrollSensitivity = 30f;

        // Viewport (Mask clips the text to the panel bounds)
        var pvpGo = new GameObject("Viewport", typeof(RectTransform));
        pvpGo.transform.SetParent(previewGo.transform, false);
        var pvpRt = pvpGo.GetComponent<RectTransform>();
        pvpRt.anchorMin = Vector2.zero;
        pvpRt.anchorMax = Vector2.one;
        pvpRt.offsetMin = new Vector2(6f,  6f);
        pvpRt.offsetMax = new Vector2(-6f, -6f);
        pvpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // Mask needs a graphic
        pvpGo.AddComponent<Mask>().showMaskGraphic = false;

        // PreviewText — anchored top, grows downward via ContentSizeFitter
        var previewTxtGo = new GameObject("PreviewText", typeof(RectTransform));
        previewTxtGo.transform.SetParent(pvpGo.transform, false);
        var ptRt = previewTxtGo.GetComponent<RectTransform>();
        ptRt.anchorMin = new Vector2(0f, 1f);
        ptRt.anchorMax = new Vector2(1f, 1f);
        ptRt.pivot     = new Vector2(0.5f, 1f);
        ptRt.sizeDelta = Vector2.zero;
        var previewTxt = previewTxtGo.AddComponent<Text>();
        previewTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        previewTxt.fontSize           = 16;
        previewTxt.color              = Color.white;
        previewTxt.alignment          = TextAnchor.UpperLeft;
        previewTxt.supportRichText    = true;
        previewTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        previewTxt.verticalOverflow   = VerticalWrapMode.Overflow;
        var ptCsf = previewTxtGo.AddComponent<ContentSizeFitter>();
        ptCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        previewScroll.viewport = pvpRt;
        previewScroll.content  = ptRt;
        gridUI.scorePreviewText = previewTxt;

        // Wire TileHandUI
        tileHandUI.tileCardPrefab      = tileCardPrefab;
        tileHandUI.handParent          = handArea.GetComponent<RectTransform>();
        tileHandUI.redrawButton        = redrawBtn.GetComponent<Button>();
        tileHandUI.confirmRedrawButton = confirmRedrawBtn.GetComponent<Button>();
        tileHandUI.submitButton        = submitBtn.GetComponent<Button>();
        tileHandUI.endRoundButton      = endRoundBtn.GetComponent<Button>();

        // ── ScoringPanel ──────────────────────────────────────────────────────
        // Transparent overlay — GamePanel (grid) stays visible behind it so
        // chip-zoom animations can fly from tile positions to the score counter.
        var scoringPanelGo = CreatePanel(canvasGo.transform, "ScoringPanel", transparent: true);
        StretchFull(scoringPanelGo.GetComponent<RectTransform>());

        // WordNameBanner — centered above the grid, briefly shown per-word during animation
        var wordBannerGo = new GameObject("WordNameBanner", typeof(RectTransform));
        wordBannerGo.transform.SetParent(scoringPanelGo.transform, false);
        var wbRt = wordBannerGo.GetComponent<RectTransform>();
        wbRt.anchorMin        = new Vector2(0.5f, 0.5f);
        wbRt.anchorMax        = new Vector2(0.5f, 0.5f);
        wbRt.pivot            = new Vector2(0.5f, 0.5f);
        wbRt.anchoredPosition = new Vector2(0f, 210f); // above grid centre
        wbRt.sizeDelta        = new Vector2(520f, 90f);
        wordBannerGo.AddComponent<Image>().color = new Color(0.20f, 0.16f, 0.18f, 0.90f);
        // Text lives on a child GO — Image and Text must not share the same GameObject
        // because both require CanvasRenderer which causes AddComponent<Text>() to return null.
        var wordNameTxt = CreateText(wordBannerGo.transform, "WordNameText", "",
                                     fontSize: 54, bold: true, alignment: TextAnchor.MiddleCenter);
        wordNameTxt.color              = new Color(1f, 0.88f, 0.55f, 1f); // warm gold
        wordNameTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        wordNameTxt.verticalOverflow   = VerticalWrapMode.Overflow;
        wordBannerGo.SetActive(false);

        // ScoreOverlay — left side panel (same position as the placement ScorePreview)
        var overlayGo = new GameObject("ScoreOverlay", typeof(RectTransform));
        overlayGo.transform.SetParent(scoringPanelGo.transform, false);
        var overlayRt = overlayGo.GetComponent<RectTransform>();
        overlayRt.anchorMin        = new Vector2(0f, 0.5f);
        overlayRt.anchorMax        = new Vector2(0f, 0.5f);
        overlayRt.pivot            = new Vector2(0f, 0.5f);
        overlayRt.anchoredPosition = new Vector2(10f, 0f);
        overlayRt.sizeDelta        = new Vector2(260f, 520f);
        overlayGo.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.16f, 0.92f);
        AddVLG(overlayGo, 10, TextAnchor.UpperCenter);

        AddLabel(overlayGo.transform, "ScoringLabel", "ROUND SCORE", fontSize: 18, bold: true);
        var totalScoreText  = AddLabel(overlayGo.transform, "TotalScore",  "0",     fontSize: 60, bold: true);
        var targetScoreText = AddLabel(overlayGo.transform, "TargetScore", "/ 100", fontSize: 24);

        // Breakdown ScrollRect — shows per-word chip×mult=score details after animation
        var breakdownScrollGo = new GameObject("BreakdownScroll", typeof(RectTransform));
        breakdownScrollGo.transform.SetParent(overlayGo.transform, false);
        var bsLe = breakdownScrollGo.AddComponent<LayoutElement>();
        bsLe.preferredHeight = 180f;
        bsLe.flexibleWidth   = 1f;
        breakdownScrollGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // Mask needs graphic

        var bsScroll = breakdownScrollGo.AddComponent<ScrollRect>();
        bsScroll.horizontal        = false;
        bsScroll.scrollSensitivity = 30f;

        var bsViewport = new GameObject("Viewport", typeof(RectTransform));
        bsViewport.transform.SetParent(breakdownScrollGo.transform, false);
        var bsvRt = bsViewport.GetComponent<RectTransform>();
        bsvRt.anchorMin = Vector2.zero; bsvRt.anchorMax = Vector2.one;
        bsvRt.offsetMin = Vector2.zero; bsvRt.offsetMax = Vector2.zero;
        bsViewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        bsViewport.AddComponent<Mask>().showMaskGraphic = false;

        var bsContent = new GameObject("BreakdownText", typeof(RectTransform));
        bsContent.transform.SetParent(bsViewport.transform, false);
        var bscRt = bsContent.GetComponent<RectTransform>();
        bscRt.anchorMin = new Vector2(0f, 1f); bscRt.anchorMax = new Vector2(1f, 1f);
        bscRt.pivot     = new Vector2(0.5f, 1f);
        bscRt.sizeDelta = Vector2.zero;
        var bsTxt = bsContent.AddComponent<Text>();
        bsTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bsTxt.fontSize           = 15;
        bsTxt.color              = Color.white;
        bsTxt.alignment          = TextAnchor.UpperLeft;
        bsTxt.supportRichText    = true;
        bsTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        bsTxt.verticalOverflow   = VerticalWrapMode.Overflow;
        var bsCsf = bsContent.AddComponent<ContentSizeFitter>();
        bsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bsScroll.viewport = bsvRt;
        bsScroll.content  = bscRt;
        bsContent.SetActive(false); // hidden until animation completes

        var resultText  = AddLabel(overlayGo.transform, "ResultText",  "", fontSize: 38, bold: true);
        var continueBtn = AddButton(overlayGo.transform, "ContinueButton", "CONTINUE",
                                    new Color(0.28f, 0.50f, 0.30f), width: 200, height: 55);
        StyleButton(continueBtn, ButtonType.Confirm);

        // Wire ScoreUI
        var scoreUI = scoringPanelGo.AddComponent<ScoreUI>();
        scoreUI.totalScoreText    = totalScoreText;
        scoreUI.targetScoreText   = targetScoreText;
        scoreUI.resultText        = resultText;
        scoreUI.wordNameText      = wordNameTxt;
        scoreUI.wordBreakdownText = bsTxt;
        scoreUI.continueButton    = continueBtn.GetComponent<Button>();
        scoreUI.wordTilePrefab    = wordTilePrefab;
        scoreUI.wordRowPrefab     = wordRowPrefab;

        // Scoring Lexicon Sidebar — mirrors the GamePanel LexiconSidebar position so
        // lexicon cards remain visible (and flash) during the chip-zoom animation.
        var scoringLexSidebarGo = new GameObject("ScoringLexiconSidebar", typeof(RectTransform));
        scoringLexSidebarGo.transform.SetParent(scoringPanelGo.transform, false);
        var slsRt = scoringLexSidebarGo.GetComponent<RectTransform>();
        slsRt.anchorMin        = new Vector2(1f, 0.5f);
        slsRt.anchorMax        = new Vector2(1f, 0.5f);
        slsRt.pivot            = new Vector2(1f, 0.5f);
        slsRt.anchoredPosition = new Vector2(-225f, 20f);
        slsRt.sizeDelta        = new Vector2(205f, 530f);
        scoringLexSidebarGo.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.16f, 0.7f);

        // "LEXICONS" label
        var scoringLexLabel = CreateText(scoringLexSidebarGo.transform, "LexiconSidebarLabel", "LEXICONS",
                                         fontSize: 16, bold: true, alignment: TextAnchor.UpperCenter,
                                         anchorMin: new Vector2(0, 0.92f), anchorMax: new Vector2(1, 1));
        scoringLexLabel.color = new Color(0.8f, 0.7f, 0.9f);

        // VLG content container for the lexicon card instances
        var scoringLexContentGo = new GameObject("ScoringLexiconContent", typeof(RectTransform));
        scoringLexContentGo.transform.SetParent(scoringLexSidebarGo.transform, false);
        var slcRt = scoringLexContentGo.GetComponent<RectTransform>();
        slcRt.anchorMin = new Vector2(0, 0);
        slcRt.anchorMax = new Vector2(1, 0.92f);
        slcRt.offsetMin = Vector2.zero;
        slcRt.offsetMax = Vector2.zero;
        var slcVlg = scoringLexContentGo.AddComponent<VerticalLayoutGroup>();
        slcVlg.spacing               = 6;
        slcVlg.childAlignment        = TextAnchor.UpperCenter;
        slcVlg.childForceExpandWidth  = false;
        slcVlg.childForceExpandHeight = false;
        slcVlg.childControlWidth      = false;
        slcVlg.childControlHeight     = false;
        slcVlg.padding = new RectOffset(2, 2, 4, 4);

        // ── ShopPanel ─────────────────────────────────────────────────────────
        var shopPanelGo = CreatePanel(canvasGo.transform, "ShopPanel",
                                      transparent: false, color: new Color(0.26f, 0.21f, 0.23f));
        StretchFull(shopPanelGo.GetComponent<RectTransform>());
        AddVLG(shopPanelGo, 16, TextAnchor.UpperCenter);

        AddLabel(shopPanelGo.transform, "ShopTitle", "SHOP", fontSize: 48, bold: true);
        var shopGoldText = AddLabel(shopPanelGo.transform, "GoldText", "Gold: 0", fontSize: 28);

        var shopItemsArea = new GameObject("ShopItemsArea", typeof(RectTransform));
        shopItemsArea.transform.SetParent(shopPanelGo.transform, false);
        shopItemsArea.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 400f);
        AddHLG(shopItemsArea, 40, TextAnchor.MiddleCenter);
        shopItemsArea.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(20, 20, 10, 10);

        var leaveShopBtn = AddButton(shopPanelGo.transform, "LeaveShopButton", "LEAVE SHOP",
                                     new Color(0.58f, 0.25f, 0.32f), width: 220, height: 60);
        StyleButton(leaveShopBtn, ButtonType.Danger);

        // Wire ShopUI
        var shopUI = shopPanelGo.AddComponent<ShopUI>();
        shopUI.shopItemsParent = shopItemsArea.GetComponent<RectTransform>();
        shopUI.shopItemPrefab  = shopItemPrefab;
        shopUI.goldText        = shopGoldText;
        shopUI.leaveShopButton = leaveShopBtn.GetComponent<Button>();

        // ── UpgradePanel ──────────────────────────────────────────────────────
        var upgradePanelGo = CreatePanel(canvasGo.transform, "UpgradePanel",
                                         transparent: false, color: new Color(0.26f, 0.21f, 0.23f));
        StretchFull(upgradePanelGo.GetComponent<RectTransform>());
        AddVLG(upgradePanelGo, 16, TextAnchor.UpperCenter);

        AddLabel(upgradePanelGo.transform, "UpgradeTitle", "LEXICON UPGRADE", fontSize: 48, bold: true);
        var upgradePromptText = AddLabel(upgradePanelGo.transform, "PromptText", "Choose a Lexicon entry (free):", fontSize: 24);

        var upgradeOptionsArea = new GameObject("UpgradeOptionsArea", typeof(RectTransform));
        upgradeOptionsArea.transform.SetParent(upgradePanelGo.transform, false);
        upgradeOptionsArea.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 420f);
        AddHLG(upgradeOptionsArea, 40, TextAnchor.MiddleCenter);
        upgradeOptionsArea.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(20, 20, 10, 10);

        var skipBtn = AddButton(upgradePanelGo.transform, "SkipButton", "SKIP",
                                new Color(0.412f, 0.345f, 0.373f), width: 180, height: 55);
        StyleButton(skipBtn, ButtonType.Secondary);

        // Wire UpgradeUI
        var upgradeUI = upgradePanelGo.AddComponent<UpgradeUI>();
        upgradeUI.upgradeOptionsParent = upgradeOptionsArea.GetComponent<RectTransform>();
        upgradeUI.upgradeOptionPrefab  = upgradeOptionPrefab;
        upgradeUI.promptText           = upgradePromptText;
        upgradeUI.skipButton           = skipBtn.GetComponent<Button>();

        // ── GameOverPanel ─────────────────────────────────────────────────────
        var gameOverGo = CreatePanel(canvasGo.transform, "GameOverPanel",
                                     transparent: false, color: new Color(0.18f, 0.14f, 0.16f));
        StretchFull(gameOverGo.GetComponent<RectTransform>());
        AddVLG(gameOverGo, 16, TextAnchor.MiddleCenter);
        AddLabel(gameOverGo.transform, "GameOverTitle", "GAME OVER", fontSize: 72, bold: true);
        var gameOverStats = AddLabel(gameOverGo.transform, "StatsText", "", fontSize: 22);
        gameOverStats.alignment = TextAnchor.MiddleCenter;
        var retryBtn = AddButton(gameOverGo.transform, "RetryButton", "TRY AGAIN",
                                 new Color(0.58f, 0.25f, 0.32f), width: 220, height: 60);
        StyleButton(retryBtn, ButtonType.Confirm);

        // ── VictoryPanel ──────────────────────────────────────────────────────
        var victoryGo = CreatePanel(canvasGo.transform, "VictoryPanel",
                                    transparent: false, color: new Color(0.26f, 0.21f, 0.23f));
        StretchFull(victoryGo.GetComponent<RectTransform>());
        AddVLG(victoryGo, 16, TextAnchor.MiddleCenter);
        AddLabel(victoryGo.transform, "VictoryTitle", "VICTORY!", fontSize: 72, bold: true);
        var victoryStats = AddLabel(victoryGo.transform, "StatsText", "", fontSize: 22);
        victoryStats.color = new Color(0.722f, 0.847f, 0.729f); // sage green
        var playAgainBtn = AddButton(victoryGo.transform, "PlayAgainButton", "PLAY AGAIN",
                                     new Color(0.28f, 0.50f, 0.30f), width: 220, height: 60);
        StyleButton(playAgainBtn, ButtonType.Confirm);

        // Wire RunUI lexicon parents.
        // lexiconBarParent  — GamePanel sidebar, visible during Placement.
        // scoringLexiconParent — ScoringPanel sidebar, visible during Scoring animations.
        runUI.lexiconBarParent      = lexContentGo.GetComponent<RectTransform>();
        runUI.scoringLexiconParent  = scoringLexContentGo.GetComponent<RectTransform>();

        // ── BossPreviewPanel (modal overlay — shown over GamePanel before boss blinds) ───
        var bossPreviewGo = new GameObject("BossPreviewPanel", typeof(RectTransform));
        bossPreviewGo.transform.SetParent(canvasGo.transform, false);
        var bpRt = bossPreviewGo.GetComponent<RectTransform>();
        bpRt.anchorMin        = new Vector2(0.25f, 0.25f);
        bpRt.anchorMax        = new Vector2(0.75f, 0.75f);
        bpRt.offsetMin        = Vector2.zero;
        bpRt.offsetMax        = Vector2.zero;
        bossPreviewGo.AddComponent<Image>().color = _style != null
            ? _style.panelBg
            : new Color(0.18f, 0.14f, 0.16f); // dark mauve
        bossPreviewGo.AddComponent<CanvasGroup>();
        AddVLG(bossPreviewGo, 16, TextAnchor.MiddleCenter);

        var bpHeader = AddLabel(bossPreviewGo.transform, "BossHeader", "BOSS BLIND",
                                fontSize: 48, bold: true);
        bpHeader.color = _style != null ? _style.btnDanger : new Color(0.58f, 0.25f, 0.29f);

        var bpModName = AddLabel(bossPreviewGo.transform, "ModifierName", "", fontSize: 32, bold: true);
        var bpModDesc = AddLabel(bossPreviewGo.transform, "ModifierDesc", "", fontSize: 20);
        bpModDesc.color = new Color(0.85f, 0.80f, 0.80f);
        var bpAnteBlind = AddLabel(bossPreviewGo.transform, "AnteBlindText", "", fontSize: 18);
        bpAnteBlind.color = _style != null ? _style.lexiconLabel : new Color(0.784f, 0.698f, 0.902f);

        var faceItBtn = AddButton(bossPreviewGo.transform, "FaceItButton", "FACE IT",
                                  new Color(0.55f, 0.32f, 0.13f), width: 200, height: 60);
        StyleButton(faceItBtn, ButtonType.BossPreview);

        var bossPreviewUI = bossPreviewGo.AddComponent<BossPreviewUI>();
        bossPreviewUI.modifierNameText = bpModName;
        bossPreviewUI.modifierDescText = bpModDesc;
        bossPreviewUI.anteBlindText    = bpAnteBlind;

        bossPreviewGo.SetActive(false); // hidden until boss blind is entered

        // ── LexiconTooltip layer (last canvas child — renders above everything) ──
        var tooltipLayerGo = new GameObject("LexiconTooltipLayer", typeof(RectTransform));
        tooltipLayerGo.transform.SetParent(canvasGo.transform, false);
        StretchFull(tooltipLayerGo.GetComponent<RectTransform>());
        var lexiconTooltip = tooltipLayerGo.AddComponent<LexiconTooltip>();

        // Tooltip panel — dark background, auto-heights via ContentSizeFitter
        var tooltipPanelGo = new GameObject("TooltipPanel", typeof(RectTransform));
        tooltipPanelGo.transform.SetParent(tooltipLayerGo.transform, false);
        var tpRt = tooltipPanelGo.GetComponent<RectTransform>();
        tpRt.anchorMin = new Vector2(0f, 1f); // position driven by LexiconTooltip.Update
        tpRt.anchorMax = new Vector2(0f, 1f);
        tpRt.pivot     = new Vector2(0f, 1f); // top-left pivot → panel opens right+down from cursor
        tpRt.sizeDelta = new Vector2(260f, 60f);
        tooltipPanelGo.AddComponent<Image>().color = new Color(0.12f, 0.09f, 0.11f, 0.95f);

        // VLG + CSF so the panel resizes to fit the description text
        var tpVlg = tooltipPanelGo.AddComponent<VerticalLayoutGroup>();
        tpVlg.padding               = new RectOffset(8, 8, 6, 6);
        tpVlg.spacing               = 0;
        tpVlg.childAlignment        = TextAnchor.UpperLeft;
        tpVlg.childForceExpandWidth  = true;
        tpVlg.childForceExpandHeight = false;
        tpVlg.childControlWidth      = true;
        tpVlg.childControlHeight     = true;
        var tpCsf = tooltipPanelGo.AddComponent<ContentSizeFitter>();
        tpCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Tooltip text child
        var ttGo = new GameObject("TooltipText", typeof(RectTransform));
        ttGo.transform.SetParent(tooltipPanelGo.transform, false);
        var ttTxt = ttGo.AddComponent<Text>();
        ttTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ttTxt.fontSize           = 16;
        ttTxt.color              = Color.white;
        ttTxt.alignment          = TextAnchor.UpperLeft;
        ttTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        ttTxt.verticalOverflow   = VerticalWrapMode.Overflow;
        ttTxt.supportRichText    = false;

        tooltipPanelGo.SetActive(false); // hidden until hover

        // Wire LexiconTooltip
        lexiconTooltip.panel    = tooltipPanelGo;
        lexiconTooltip.bodyText = ttTxt;

        // ── Drag Layer (above tooltip layer — ghost tile follows cursor) ───────
        var dragLayerGo = new GameObject("DragLayer", typeof(RectTransform));
        dragLayerGo.transform.SetParent(canvasGo.transform, false);
        StretchFull(dragLayerGo.GetComponent<RectTransform>());
        var dragManager = dragLayerGo.AddComponent<DragManager>();

        // Ghost tile — size matches TileCard prefab; raycastTarget=false so drops still reach cells
        var ghostGo = new GameObject("GhostTile", typeof(RectTransform));
        ghostGo.transform.SetParent(dragLayerGo.transform, false);
        var ghostRt   = ghostGo.GetComponent<RectTransform>();
        ghostRt.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRt.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRt.pivot     = new Vector2(0.5f, 0.5f);
        ghostRt.sizeDelta = new Vector2(80f, 110f);

        var ghostImg          = ghostGo.AddComponent<Image>();
        ghostImg.color        = new Color(0.988f, 0.867f, 0.737f, 0.82f); // peach, semi-transparent
        ghostImg.raycastTarget = false;

        var ghostLetterGo = new GameObject("GhostLetter", typeof(RectTransform));
        ghostLetterGo.transform.SetParent(ghostGo.transform, false);
        StretchFull(ghostLetterGo.GetComponent<RectTransform>());
        var ghostTxt              = ghostLetterGo.AddComponent<Text>();
        ghostTxt.font             = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ghostTxt.fontSize         = 36;
        ghostTxt.fontStyle        = FontStyle.Bold;
        ghostTxt.color            = new Color(0.22f, 0.18f, 0.20f);
        ghostTxt.alignment        = TextAnchor.MiddleCenter;
        ghostTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        ghostTxt.verticalOverflow   = VerticalWrapMode.Overflow;
        ghostTxt.raycastTarget      = false;

        ghostGo.SetActive(false);

        dragManager.ghostImage  = ghostImg;
        dragManager.ghostText   = ghostTxt;
        dragManager.handAreaRT  = handArea.GetComponent<RectTransform>();

        // ── Part C — Wire GameManager panel refs ──────────────────────────────
        gm.mainMenuPanel      = mainMenuGo;
        gm.gamePanel          = gamePanelGo;
        gm.scoringPanel       = scoringPanelGo;
        gm.shopPanel          = shopPanelGo;
        gm.upgradePanel       = upgradePanelGo;
        gm.gameOverPanel      = gameOverGo;
        gm.victoryPanel       = victoryGo;
        gm.hudPanel           = hudGo;
        gm.bossPreviewPanel   = bossPreviewGo;
        gm.gameOverStatsText  = gameOverStats;
        gm.victoryStatsText   = victoryStats;

        // ── Part D — Persistent button listeners ─────────────────────────────
        WireButton(startBtn.GetComponent<Button>(),       menuButtons, "StartRun");
        WireButton(retryBtn.GetComponent<Button>(),       menuButtons, "StartRun");
        WireButton(playAgainBtn.GetComponent<Button>(),   menuButtons, "StartRun");
        WireButton(faceItBtn.GetComponent<Button>(),      menuButtons, "ProceedFromBossPreview");

        // ── SFX ───────────────────────────────────────────────────────────────
        var sfxManager  = gmGo.AddComponent<SFXManager>();
        var sfxGuids    = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" });
        var sfxClips    = new System.Collections.Generic.List<AudioClip>();
        foreach (var guid in sfxGuids)
        {
            var clipPath = AssetDatabase.GUIDToAssetPath(guid);
            var clip     = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip != null) sfxClips.Add(clip);
        }
        sfxManager.tilePlaceClips = sfxClips.ToArray();
        Debug.Log($"[SceneBuilder] Wired {sfxClips.Count} SFX clips to SFXManager.");

        // ── Music ─────────────────────────────────────────────────────────────
        // Added last so it doesn't interfere with UI component construction above.
        var musicManager = gmGo.AddComponent<MusicManager>();
        var audioGuids   = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" });
        var musicClips   = new System.Collections.Generic.List<AudioClip>();
        foreach (var guid in audioGuids)
        {
            var clipPath = AssetDatabase.GUIDToAssetPath(guid);
            var clip     = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip != null) musicClips.Add(clip);
        }
        musicManager.tracks = musicClips.ToArray();
        Debug.Log($"[SceneBuilder] Wired {musicClips.Count} music tracks to MusicManager.");

        // ── Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Refresh asset database so prefabs and scene show up
        AssetDatabase.Refresh();

        Debug.Log("[SceneBuilder] Done! Prefabs in Assets/Prefabs/, scene at Assets/Scenes/Main.unity");
    }

    // ── Directory helpers ─────────────────────────────────────────────────────
    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(SceneDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    // =========================================================================
    // PART A — PREFAB BUILDERS
    // =========================================================================

    // ── GridCell ──────────────────────────────────────────────────────────────
    private static GameObject BuildGridCellPrefab()
    {
        var go = new GameObject("GridCell", typeof(RectTransform));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(48f, 48f);

        // Background image
        var img = go.AddComponent<Image>();
        img.color = new Color(0.87f, 0.87f, 0.84f); // warm light grey

        // Button
        go.AddComponent<Button>();

        // Child: LetterText — dark text for light palette backgrounds
        var txt = CreateText(go.transform, "LetterText", "",
                             fontSize: 22, bold: true, alignment: TextAnchor.MiddleCenter);
        txt.color = new Color(0.22f, 0.18f, 0.20f); // dark mauve
        StretchFull(txt.GetComponent<RectTransform>());

        return SavePrefab(go, "GridCell");
    }

    // ── TileCard ──────────────────────────────────────────────────────────────
    private static GameObject BuildTileCardPrefab()
    {
        var go = new GameObject("TileCard", typeof(RectTransform));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 110f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.988f, 0.867f, 0.737f); // #FCDDBC soft peach

        go.AddComponent<Button>();

        // LetterText — large, top-centered
        var letterTxt = CreateText(go.transform, "LetterText", "A",
                                   fontSize: 36, bold: true, alignment: TextAnchor.UpperCenter);
        letterTxt.color = new Color(0.22f, 0.18f, 0.20f); // dark mauve
        var ltRt = letterTxt.GetComponent<RectTransform>();
        ltRt.anchorMin = new Vector2(0, 0.4f);
        ltRt.anchorMax = new Vector2(1, 1);
        ltRt.offsetMin = ltRt.offsetMax = Vector2.zero;

        // ChipsText — small, bottom-right
        var chipsTxt = CreateText(go.transform, "ChipsText", "1",
                                  fontSize: 18, bold: false, alignment: TextAnchor.LowerRight);
        chipsTxt.color = new Color(0.412f, 0.345f, 0.373f); // dark mauve
        var ctRt = chipsTxt.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0, 0);
        ctRt.anchorMax = new Vector2(1, 0.4f);
        ctRt.offsetMin = new Vector2(4, 4);
        ctRt.offsetMax = new Vector2(-4, 0);

        return SavePrefab(go, "TileCard");
    }

    // ── ShopItem ──────────────────────────────────────────────────────────────
    private static GameObject BuildShopItemPrefab()
    {
        var go = new GameObject("ShopItem", typeof(RectTransform));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 380f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.26f, 0.21f, 0.23f); // medium dark mauve

        go.AddComponent<Button>();

        // LayoutElement — explicit sizes so the HLG never squishes this card
        var le = go.AddComponent<LayoutElement>();
        le.minWidth        = 280f;
        le.preferredWidth  = 280f;
        le.minHeight       = 380f;
        le.preferredHeight = 380f;

        // Icon child
        var iconGo = new GameObject("ItemIcon", typeof(RectTransform));
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.6f);
        iconRt.anchorMax = new Vector2(0.5f, 0.95f);
        iconRt.sizeDelta = new Vector2(100f, 0f);
        iconGo.AddComponent<Image>().color = new Color(0.412f, 0.345f, 0.373f); // dark mauve

        var nameTxt = CreateText(go.transform, "ItemName", "Item",        fontSize: 28, bold: true,  alignment: TextAnchor.UpperCenter,
                   anchorMin: new Vector2(0,0.4f),  anchorMax: new Vector2(1,0.6f));
        nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        var descTxt = CreateText(go.transform, "ItemDesc", "Description", fontSize: 22, bold: false, alignment: TextAnchor.UpperCenter,
                   anchorMin: new Vector2(0,0.18f), anchorMax: new Vector2(1,0.4f));
        descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        var costTxt = CreateText(go.transform, "ItemCost", "$3",          fontSize: 26, bold: true,  alignment: TextAnchor.LowerCenter,
                   anchorMin: new Vector2(0,0),     anchorMax: new Vector2(1,0.18f));
        costTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        return SavePrefab(go, "ShopItem");
    }

    // ── LexiconCard ───────────────────────────────────────────────────────────
    private static GameObject BuildLexiconCardPrefab()
    {
        var go = new GameObject("LexiconCard", typeof(RectTransform));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 80f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.412f, 0.345f, 0.373f); // #69585F dark mauve

        // LayoutElement keeps card at 200×80 in both VLG (sidebar) and HLG (scoring row)
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 200f;
        le.preferredHeight = 80f;

        // Name fills the full card; effect description shown via hover tooltip
        var nameTxt = CreateText(go.transform, "LexiconName", "Name",
                                 fontSize: 18, bold: true, alignment: TextAnchor.MiddleCenter);
        nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        return SavePrefab(go, "LexiconCard");
    }

    // ── UpgradeOption ─────────────────────────────────────────────────────────
    private static GameObject BuildUpgradeOptionPrefab()
    {
        var go = new GameObject("UpgradeOption", typeof(RectTransform));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 400f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.26f, 0.21f, 0.23f); // medium dark mauve

        go.AddComponent<Button>();

        // LayoutElement — explicit sizes so the HLG never squishes this card
        var le = go.AddComponent<LayoutElement>();
        le.minWidth        = 400f;
        le.preferredWidth  = 400f;
        le.minHeight       = 400f;
        le.preferredHeight = 400f;

        var nameTxt = CreateText(go.transform, "UpgradeName",   "Name",   fontSize: 30, bold: true,  alignment: TextAnchor.UpperCenter,
                   anchorMin: new Vector2(0, 0.72f), anchorMax: new Vector2(1, 1));
        nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        var effectTxt = CreateText(go.transform, "UpgradeEffect", "Effect", fontSize: 22, bold: false, alignment: TextAnchor.UpperCenter,
                   anchorMin: new Vector2(0, 0.44f), anchorMax: new Vector2(1, 0.72f));
        effectTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        var flavorTxt = CreateText(go.transform, "UpgradeFlavor", "Flavor", fontSize: 20, bold: false, alignment: TextAnchor.UpperCenter,
                   anchorMin: new Vector2(0, 0.18f), anchorMax: new Vector2(1, 0.44f));
        flavorTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        var costTxt = CreateText(go.transform, "UpgradeCost",   "FREE",   fontSize: 26, bold: true,  alignment: TextAnchor.LowerCenter,
                   anchorMin: new Vector2(0, 0),     anchorMax: new Vector2(1, 0.18f));
        costTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        return SavePrefab(go, "UpgradeOption");
    }

    // ── WordTile ──────────────────────────────────────────────────────────────
    // Mirrors the inline tile structure used by ScoreUI.SpawnTile().
    // Saved as a prefab so you can edit the visual in the Unity editor.
    // Child GameObjects are named "L" (letter) and "C" (chips) — ScoreUI
    // finds them by name to populate their Text components at runtime.
    private static GameObject BuildWordTilePrefab()
    {
        var go = new GameObject("WordTile", typeof(RectTransform));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(62f, 82f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.988f, 0.867f, 0.737f); // #FCDDBC soft peach

        var ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(0.55f, 0.40f, 0.20f, 0.5f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        // "L" — letter text (upper 70% of tile)
        var letterTxt = CreateText(go.transform, "L", "A",
                                   fontSize: 30, bold: true, alignment: TextAnchor.MiddleCenter,
                                   anchorMin: new Vector2(0f, 0.30f), anchorMax: Vector2.one);
        letterTxt.color = new Color(0.22f, 0.18f, 0.20f); // dark mauve

        // "C" — chip value text (lower 34% of tile)
        var chipsTxt = CreateText(go.transform, "C", "1",
                                  fontSize: 14, bold: false, alignment: TextAnchor.MiddleCenter,
                                  anchorMin: Vector2.zero, anchorMax: new Vector2(1f, 0.34f));
        chipsTxt.color = new Color(0.45f, 0.28f, 0.10f); // dark amber

        return SavePrefab(go, "WordTile");
    }

    // ── WordRow ───────────────────────────────────────────────────────────────
    // Transparent container for one word's tiles in the ScoreUI animation.
    // Width is driven by anchor stretch at runtime; height is set via sizeDelta.
    private static GameObject BuildWordRowPrefab()
    {
        var go = new GameObject("WordRow", typeof(RectTransform));
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 112f);
        // Intentionally no Image — transparent container.
        return SavePrefab(go, "WordRow");
    }

    // =========================================================================
    // LAYOUT / WIDGET HELPERS
    // =========================================================================

    private static GameObject CreateCanvas()
    {
        var go = new GameObject("UI Root", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        canvas.pixelPerfect = true;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static void CreateEventSystem()
    {
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // Full-screen panel
    private static GameObject CreatePanel(Transform parent, string name,
                                           bool transparent, Color color = default)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (!transparent)
        {
            var img = go.AddComponent<Image>();
            img.color = color == default ? new Color(0, 0, 0, 0.5f) : color;
        }
        go.AddComponent<CanvasGroup>(); // Enables panel fade-in transitions via GameManager.FadeInPanel
        return go;
    }

    // Strip panel: anchored to top or bottom
    private static GameObject CreateStrip(Transform parent, string name, bool isTop, float thickness)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();

        if (isTop)
        {
            rt.anchorMin   = new Vector2(0, 1);
            rt.anchorMax   = new Vector2(1, 1);
            rt.pivot       = new Vector2(0.5f, 1);
            rt.offsetMin   = new Vector2(0, -thickness);
            rt.offsetMax   = Vector2.zero;
        }
        else
        {
            rt.anchorMin   = new Vector2(0, 0);
            rt.anchorMax   = new Vector2(1, 0);
            rt.pivot       = new Vector2(0.5f, 0);
            rt.offsetMin   = Vector2.zero;
            rt.offsetMax   = new Vector2(0, thickness);
        }

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.18f, 0.20f, 0.95f); // dark mauve strip

        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    // Label helper (anchored layout child, no sizeDelta override)
    private static Text AddLabel(Transform parent, string name, string content,
                                  int fontSize = 20, bool bold = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text               = content;
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize           = fontSize;
        t.fontStyle          = bold ? FontStyle.Bold : FontStyle.Normal;
        t.color              = Color.white;
        t.alignment          = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 400;
        le.preferredHeight = Mathf.CeilToInt(fontSize * 1.6f);

        return t;
    }

    // Button helper — returns the root GameObject
    private static GameObject AddButton(Transform parent, string name, string label,
                                         Color bgColor, float width = 160, float height = 50)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = Color.white;
        btn.colors = colors;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = height;

        // Label text child
        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        StretchFull(txtGo.GetComponent<RectTransform>());
        var t = txtGo.AddComponent<Text>();
        t.text               = label;
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize           = 22;
        t.fontStyle          = FontStyle.Bold;
        t.color              = Color.white;
        t.alignment          = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        return go;
    }

    // Applies a themed button colour from UIStyleConfig (no-op if _style is null).
    private static void StyleButton(GameObject btnGo, ButtonType type)
    {
        if (_style == null) return;
        var img = btnGo?.GetComponent<Image>();
        if (img == null) return;
        img.color = type switch {
            ButtonType.Confirm      => _style.btnConfirm,
            ButtonType.Danger       => _style.btnDanger,
            ButtonType.Neutral      => _style.btnNeutral,
            ButtonType.Secondary    => _style.btnSecondary,
            ButtonType.BossPreview  => _style.btnBossPreview,
            _                       => _style.btnNeutral
        };
    }

    // Add HorizontalLayoutGroup to an existing GameObject
    private static void AddHLG(GameObject go, int spacing, TextAnchor alignment)
    {
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing            = spacing;
        hlg.childAlignment     = alignment;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = false; // don't override child RectTransform sizes
        hlg.childControlHeight     = false;
        hlg.padding = new RectOffset(8, 8, 4, 4);
    }

    // Add VerticalLayoutGroup to an existing GameObject
    private static void AddVLG(GameObject go, int spacing, TextAnchor alignment)
    {
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing            = spacing;
        vlg.childAlignment     = alignment;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(12, 12, 12, 12);
    }

    // Create a Text child with optional anchor override
    private static Text CreateText(Transform parent, string name, string content,
                                    int fontSize = 18, bool bold = false,
                                    TextAnchor alignment = TextAnchor.MiddleCenter,
                                    Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        if (anchorMin.HasValue && anchorMax.HasValue)
        {
            rt.anchorMin = anchorMin.Value;
            rt.anchorMax = anchorMax.Value;
            rt.offsetMin = new Vector2(4, 2);
            rt.offsetMax = new Vector2(-4, -2);
        }
        else
        {
            StretchFull(rt);
        }

        var t = go.AddComponent<Text>();
        t.text               = content;
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize           = fontSize;
        t.fontStyle          = bold ? FontStyle.Bold : FontStyle.Normal;
        t.color              = Color.white;
        t.alignment          = alignment;
        t.resizeTextForBestFit = false;
        t.horizontalOverflow   = HorizontalWrapMode.Overflow;
        t.verticalOverflow     = VerticalWrapMode.Truncate;  // no bleed onto adjacent rows

        return t;
    }

    // Wire a persistent onClick listener by method name using reflection
    private static void WireButton(Button button, MenuButtons target, string methodName)
    {
        if (button == null || target == null) return;
        var method = typeof(MenuButtons).GetMethod(methodName);
        if (method == null)
        {
            Debug.LogWarning($"[SceneBuilder] Method '{methodName}' not found on MenuButtons");
            return;
        }
        var action = (UnityEngine.Events.UnityAction)
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, method);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    // Save a temporary GameObject as a prefab asset and destroy the temp object
    private static GameObject SavePrefab(GameObject go, string prefabName)
    {
        string path = $"{PrefabDir}/{prefabName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[SceneBuilder] Saved prefab: {path}");
        return prefab;
    }
}
#endif
