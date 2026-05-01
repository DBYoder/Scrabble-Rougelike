# Tile Artifact Fix — Test Plan

## What changed
1. **TileHandUI.cs** — Removed letter sprite loading from `RefreshHand()`. Tiles now always
   render with `normalColor` (peach) background + `LetterText` + `ChipsText`. No sprite overlays.
   Fixed `normalColor` default from `Color.white` → `new Color(0.988f, 0.867f, 0.737f)` (peach).
   Removed `img.sprite != null ? Color.white : normalColor` sprite checks from redraw mode.
2. **ScoreUI.cs** — Removed letter sprite loading from `SpawnTile()`. Scoring tiles always render
   peach background + "L" text + "C" text. No sprite overlays.

---

## T1 — Hand tiles show correct peach background
**Steps**: Enter Play mode. Inspect the 7 hand tiles at the bottom of the screen.
**Expected**: Each tile is a warm peach/cream color (`#FCDDBC`), with the letter centered in bold and the chip value in the lower portion.
**Pass criteria**: No white background tiles. No plain grey Unity-default tiles. Each tile is visually distinct from the grid cells above.

## T2 — No double chip number on hand tiles
**Steps**: Enter Play mode. Look at each tile card in the hand.
**Expected**: Each tile shows its chip value exactly once (small number in lower area of card).
**Pass criteria**: Zero cases of the chip number appearing twice on one tile.

## T3 — Letter text is correct and visible
**Steps**: Enter Play mode. Read all 7 tiles in hand.
**Expected**: Each tile shows a single capital letter (A–Z) that matches the actual tile.
**Pass criteria**: All letters correct, no missing or doubled letters.

## T4 — Redraw mode tints correctly
**Steps**:
1. Click the RESHUFFLE button to enter redraw mode.
2. Click one tile to mark it for redraw.
3. Click it again to un-mark it.
4. Click RESHUFFLE again to cancel redraw mode.
**Expected**:
- Un-selected tiles in redraw mode: peach background (unchanged).
- Selected tile: red/rose tint (`redrawColor`).
- After un-selecting: reverts to peach.
- After cancelling mode: all tiles revert to peach.
**Pass criteria**: Color tinting works correctly; no white or grey stuck backgrounds.

## T5 — Glossary lexicon tints matching tiles gold
**Steps**: Have a Glossary lexicon active (or test by temporarily adding one in RunManager).
**Expected**: Tiles matching the featured letter show amber gold tint instead of peach.
**Pass criteria**: Gold tint is applied on top of normalColor logic without conflict.

## T6 — Scoring animation tiles show no double number
**Steps**: Submit a valid word. Watch the scoring animation play.
**Expected**: Each tile in the word-scoring display shows its chip value once and its letter once. Background is peach.
**Pass criteria**: No double numbers. No white blank tiles.

## T7 — Scoring tiles use prefab correctly
**Steps**: Submit a valid word. Observe scoring tiles.
**Expected**: Tiles are the same peach color as hand tiles. "L" text (letter, bold, centered upper) and "C" text (chips, small, lower) both visible.
**Pass criteria**: Consistent appearance between hand tiles and scoring tiles.

## T8 — No console errors related to sprites
**Steps**: Play through placing tiles and submitting a word. Check Unity Console.
**Expected**: No errors about missing sprites, null references in TileHandUI or ScoreUI.
**Pass criteria**: Zero sprite-related errors in Console.

---

## Analytical verification (no sprites needed)

All 8 tests pass analytically with the code changes made:

- **T1**: `img.color = normalColor` is always set; `normalColor` is now peach. Pass.
- **T2**: `ChipsText` is set once; no sprite to double up with. Pass.
- **T3**: `LetterText` is always `SetActive(true)` and set once. Pass.
- **T4**: Redraw mode uses `img.color = normalColor` (peach) for un-selected, `img.color = redrawColor` for selected — no `img.sprite != null` checks remaining. Pass.
- **T5**: Glossary `img.color = new Color(1f, 0.85f, 0.30f)` runs after `img.color = normalColor`, correctly overrides. Pass.
- **T6/T7**: `SpawnTile()` always sets `img.sprite = null; img.color = peach`, both "L" and "C" texts active. Pass.
- **T8**: No `Resources.Load<Sprite>()` calls in hand/score tile paths. Pass.
