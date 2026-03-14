# Crossword Roguelike — Unity Setup Guide

## Prerequisites
- Unity 2022 LTS or newer (2D project template)
- `.NET Standard 2.1` or higher (set in Player Settings → Other Settings)

---

## Step 1 — Create the Unity Project

1. Open **Unity Hub**
2. Click **New Project** → select **2D (Core)** template
3. Set the **Location** to `C:\Users\Milo\` and the **Project Name** to `Cross Word Rougelike`
   *(Unity will place everything inside the folder that already contains this SETUP.md)*
4. Click **Create Project** — Unity will generate the project files inside the existing folder

> All C# scripts and resources are already in `Assets/`. Unity will detect them automatically.

---

## Step 2 — Generate ScriptableObject Assets

Once the project opens, run these three menu commands **in order**:

| Menu | What it creates |
|------|----------------|
| **Crossword → Create Letter Assets** | `Assets/Resources/Letters/Letter_A.asset` … `Letter_Z.asset` |
| **Crossword → Create Lexicon Assets** | All 10 Lexicon entries in `Assets/Resources/Lexicon/` |
| **Crossword → Create Blind Assets (Ante 1)** | Antes 1–3 blind data in `Assets/Resources/Blinds/` |

To add Antes 4–8, duplicate the existing `BlindData` assets in the Blinds folder and adjust `targetScore`, `goldReward`, and `bossModifier`.

---

## Step 3 — Add a Full Word List *(Recommended)*

The starter `wordlist.txt` (~800 words) is enough to test the game. For real play, replace it with a full English wordlist:

- **ENABLE list** (172k words, public domain):
  Search for `enable1.txt` — widely mirrored
- **Collins Scrabble Words** (279k words):
  Search for `collins-scrabble-words-2019.txt`

Place the file at `Assets/Resources/wordlist.txt` — one lowercase word per line.

---

## Step 4 — Build the Scene

Create a **Single Scene** (`Assets/Scenes/Main.unity`) with this hierarchy:

```
GameManager          ← Add: GameManager.cs, RunManager.cs, GridManager.cs,
                            TileHandManager.cs, WordValidator.cs, ScoreManager.cs,
                            ShopManager.cs

UI Root (Canvas)
  ├── HUD                  ← RunUI.cs | always visible during run
  │    ├── RunInfoBar      ← Text labels: AnteText, BlindText, LivesText, GoldText,
  │    │                     ScoreText, TargetText, RedrawsText, BossModifierText
  │    └── LexiconBar      ← Horizontal layout, holds up to 5 LexiconCard prefabs
  │
  ├── MainMenuPanel        ← Button calling GameManager.StartRun()
  │
  ├── GamePanel            ← GridUI.cs + TileHandUI.cs
  │    ├── GridUI           ← Assign cellPrefab + gridParent
  │    └── HandArea
  │         ├── TileHandUI  ← Assign tileCardPrefab + handParent
  │         ├── RedrawBtn   ← Calls TileHandUI.ToggleRedrawMode()
  │         ├── ConfirmBtn  ← Calls TileHandUI.ConfirmRedraw()
  │         └── SubmitBtn   ← Calls GameManager.SubmitBoard()
  │
  ├── ScoringPanel         ← ScoreUI.cs
  │    ├── TotalScoreText
  │    ├── TargetScoreText
  │    ├── ResultText       ← "PASSED!" / "FAILED"
  │    ├── WordResultsList  ← Scroll view content transform
  │    └── ContinueButton   ← ScoreUI.OnContinueClicked()
  │
  ├── ShopPanel            ← ShopUI.cs
  │    ├── GoldText
  │    ├── ShopItemsList
  │    └── LeaveShopButton  ← ShopUI.OnLeaveShopClicked()
  │
  ├── UpgradePanel         ← UpgradeUI.cs
  │    ├── PromptText
  │    ├── UpgradeOptionsList
  │    └── SkipButton       ← UpgradeUI.SkipUpgrade()
  │
  ├── GameOverPanel        ← Button calling GameManager.StartRun()
  └── VictoryPanel         ← Button calling GameManager.StartRun()
```

Wire all panel references on **GameManager**.

---

## Step 5 — Create Prefabs

### GridCell.prefab
- Root: `Image` (set `Source Image` to a white square sprite)
- Root: `Button`
- Child `LetterText`: `Text`, centered, font size 20, bold

### TileCard.prefab
- Root: `Image`, `Button` (200×120 recommended)
- Child `LetterText`: large centered `Text`
- Child `ChipsText`: smaller `Text` in corner showing chip value

### WordResultEntry.prefab (for ScoringPanel)
- Root: `HorizontalLayoutGroup` or just stacked `Text` children
- Children: `WordText`, `FormulaText`, `BonusText`

### ShopItem.prefab
- Root: `Image`, `Button`
- Children: `ItemName` (Text), `ItemDesc` (Text), `ItemCost` (Text), `ItemIcon` (Image)

### LexiconCard.prefab (for LexiconBar)
- Root: `Image` (card background)
- Children: `LexiconName` (Text), `LexiconEffect` (Text), `LexiconIcon` (Image, optional)

### UpgradeOption.prefab
- Root: `Image`, `Button`
- Children: `UpgradeName` (Text), `UpgradeEffect` (Text), `UpgradeFlavor` (Text), `UpgradeIcon` (Image, optional)

---

## Step 6 — Assign References in Inspector

On `GameManager`:
- Drag all panel GameObjects into their respective slots

On `GridUI`:
- `cellPrefab` → GridCell.prefab
- `gridParent` → the RectTransform that will hold all cells

On `TileHandUI`:
- `tileCardPrefab` → TileCard.prefab
- `handParent` → the hand area transform

On `ScoreUI`, `ShopUI`, `RunUI`, `UpgradeUI`:
- Wire all `Text` and `Button` references in the Inspector

---

## Implementation Phases

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | ✅ Done | Folder structure + Data classes + ScriptableObjects |
| 2 | ✅ Done | Core managers (Grid, Hand, Score, Game, Run, Shop) + WordValidator |
| 3 | ✅ Done | All UI scripts (Grid, Hand, Score, Shop, Run, Upgrade) |
| 4 | 🔲 Todo | Create scene hierarchy + prefabs in Unity Editor |
| 5 | 🔲 Todo | Replace starter wordlist with full dictionary |
| 6 | 🔲 Todo | Sound effects, visual polish, particle effects |
| 7 | 🔲 Todo | BlindData assets for Antes 4–8 |

---

## Scoring Formula Reference

```
Word Score = (Sum of letter Chips in word) × Word Multiplier

Multiplier bonuses (additive):
  Base:           ×1.0
  5+ letters:     +0.5
  7+ letters:     +1.0  (cumulative)
  10+ letters:    +2.0  (cumulative)
  Each Q/Z/X/J:   +0.5 per rare letter

Intersection: that letter's chip value ×2
Lexicon cards: applied last (can be multiplicative)
```

---

## File Map

```
Assets/
  Scripts/
    Core/
      GameManager.cs       State machine + coordinator
      RunManager.cs        Ante/blind/lives/gold tracking
      GridManager.cs       13×13 grid, PlaceTile, GetAllWords
      TileHandManager.cs   Hand of 7, weighted tile bag, redraws
      WordValidator.cs     HashSet dictionary lookup
      ScoreManager.cs      Chips × Mult, all Lexicon effects
      ShopManager.cs       Shop generation + purchases
    Data/
      LetterData.cs        ScriptableObject: letter, chips, weight
      LexiconWordData.cs   ScriptableObject: Lexicon entry
      ShopItemData.cs      ScriptableObject: shop item
      BlindData.cs         ScriptableObject: target, gold, boss mod
      GridCell.cs          Plain class: cell state
      TileInstance.cs      Plain class: runtime tile
      Word.cs              Plain class: detected word
    UI/
      GridUI.cs            Grid rendering + click-to-place
      TileHandUI.cs        Hand display + selection + redraw
      ScoreUI.cs           Animated score reveal
      ShopUI.cs            Shop panel
      RunUI.cs             HUD (ante/blind/lives/gold/lexicon bar)
      UpgradeUI.cs         1-of-3 free Lexicon picker
    Editor/
      CreateLetterDataAssets.cs   Auto-generates A–Z + Lexicon + Blind assets
  Resources/
    wordlist.txt           Starter word list (replace with full dictionary)
    Letters/               LetterData assets (A–Z) — generated by Editor menu
    Lexicon/               LexiconWordData assets — generated by Editor menu
    Blinds/                BlindData assets — generated by Editor menu
    Shop/                  ShopItemData assets (create manually as needed)
```
