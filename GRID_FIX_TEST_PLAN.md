# Grid Fix — Test Plan

## What changed
1. **GridUI.cs** — `UpdateCellVisual` now uses `ApplyCellVisual(img, spriteName, fallbackColor)`.
   When the PNG sprite hasn't been imported as Sprite type, `img.sprite` is cleared and the
   original code-defined color is applied. No visual regression when sprites are absent.
2. **TileAssetPostprocessor.cs** — new Editor script:
   - `OnPreprocessTexture` sets Sprite import type for any future PNG under `Assets/Resources/Tiles/`.
   - `Crossword/Reimport Tile Sprites` menu item fixes already-imported Texture2D assets.
3. **SceneBuilder.cs** — grid y-position corrected (+72 → +66 px) to match the new TopBar
   height (42 px) and combined hand/button zone (173 px).

---

## T1 — Empty cells show correct modifier colors (no sprites)
**Steps**: Enter Play mode. Do NOT run Reimport yet. Inspect the 9×9 grid.
**Expected**:
- Center cell: ★ (double-word, blush/rose background)
- TW cells: warm orange background, "TW" label
- DW cells: blush background, "DW" label
- TL cells: sage green background, "TL" label
- DL cells: light olive background, "DL" label
- Plain empty cells: warm light grey background
**Pass criteria**: Every modifier cell is a distinct visible color. No cell is pure white or Unity-default grey.

## T2 — Placing a tile changes the cell color
**Steps**: Drag a tile card from the hand onto an empty cell.
**Expected**: Cell background changes to soft blue (turnTileColor). Letter appears in cell.
**Pass criteria**: Cell is visually distinct from its neighbors.

## T3 — Invalid-word turn cells show rose
**Steps**: Place tiles that form a non-dictionary word.
**Expected**: The turn cells forming the invalid word turn rose/pink (invalidTurnTileColor).
**Pass criteria**: Color updates in real time without submit; reverts to blue when letters are rearranged into a valid word.

## T4 — Submitted word cells show amber
**Steps**: Form a valid word, press PLAY WORD.
**Expected**: Cells used for the word change from blue to amber (occupiedColor).
**Pass criteria**: Committed tiles are amber; newly placed tiles for the next word are blue.

## T5 — Hover highlight clears correctly
**Steps**: Drag a tile slowly across multiple cells.
**Expected**: Hovered cell shows hover color; cells it left revert to their correct state color (not stuck).
**Pass criteria**: No "color ghosts" left behind after dragging away.

## T6 — Locked cells are dark
**Steps**: Start Ante 1 (only 5×5 unlocked). Observe outer cells.
**Expected**: Outer ring of cells is very dark (near-black).
**Pass criteria**: Locked cells are visually blocked; button is non-interactable.

## T7 — Sprites load after Reimport
**Steps**:
1. In Unity menu: Crossword → Reimport Tile Sprites. Wait for import to finish.
2. Enter Play mode.
**Expected**: Grid cells show the PNG sprite art (cell_tw.png orange tile, cell_empty.png grey tile, etc.).
**Pass criteria**: Console shows "[TileImport] Reimported N tile sprites." No null-ref errors.

## T8 — Hand tiles show correct letter and chip text (no sprites)
**Steps**: Enter Play mode. Inspect hand tiles.
**Expected**: Each tile shows the letter (A–Z) and its chip value. Background is peach.
**Pass criteria**: No blank tiles; ChipsText value matches the tile's TotalChips.

## T9 — Hand tiles show sprite art (with sprites)
**Steps**: After Reimport, enter Play mode.
**Expected**: Each hand tile shows the portrait card sprite image. LetterText is hidden (baked into sprite).
ChipsText still shows if value differs from sprite's baked value.
**Pass criteria**: Sprites visible on tiles; no LetterText rendered on top.

## T10 — Redraw mode tinting works on sprite tiles
**Steps**: Press RESHUFFLE button. Click a tile.
**Expected**: Clicked tile tints red (redrawColor). Un-clicked tiles stay white/normal.
Click same tile again: reverts. Press RESHUFFLE again to cancel: all tiles revert.
**Pass criteria**: Color tinting works whether sprites are loaded or not.

## T11 — Scoring tiles use sprites
**Steps**: Submit a word. Let Scoring animation run.
**Expected**: Each scoring tile shows the portrait card sprite. Chip value (ChipsText) is visible.
**Pass criteria**: No plain peach rectangles (unless sprites not yet imported).

## T12 — SceneBuilder grid position (after Build Scene & Prefabs)
**Steps**: Run Crossword → Build Scene & Prefabs. Enter Play mode.
**Expected**:
- Grid is not hidden behind TopBar or HandArea.
- Grid is horizontally centered between the ScorePreview panel and the LexiconSidebar.
- Grid is vertically centered in the open play area (not too high or too low).
**Pass criteria**: Grid fully visible, not clipped, not offset noticeably to left or right.
