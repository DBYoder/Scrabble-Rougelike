# Sprite Integration v2 — Test Plan

## Asset inventory (after this change)
| Folder | Files | Usage |
|---|---|---|
| `Resources/Tiles/Letters/` | tile_A.png … tile_Z.png, tile_BLANK.png | Hand tiles, scoring tiles |
| `Resources/Tiles/GridCells/` | cell_empty, cell_tw, cell_dw, cell_tl, cell_dl | Grid background only |
| `Resources/Tiles/States/` | tile_state_default/valid/invalid/occupied | Available; not used on grid |

**NOT in GridCells/**: cell_valid, cell_invalid, cell_occupied (have letters baked in — would cause double-letter on grid)

## Rendering rules
| Element | Sprite | LetterText | ChipsText |
|---|---|---|---|
| Hand tile (with sprites) | tile_X.png (letter baked) | HIDDEN | SHOWN |
| Hand tile (no sprites) | none (peach color) | SHOWN | SHOWN |
| Scoring tile (with sprites) | tile_X.png | HIDDEN | SHOWN |
| Scoring tile (no sprites) | none (peach color) | SHOWN | SHOWN |
| Grid: empty | cell_empty.png | — | — |
| Grid: TW | cell_tw.png ("TW" baked) | "" always | — |
| Grid: DW | cell_dw.png ("DW" baked) | "" always | — |
| Grid: center ★ | no sprite (color only) | "★" always | — |
| Grid: TL | cell_tl.png ("TL" baked) | "" always | — |
| Grid: DL | cell_dl.png ("DL" baked) | "" always | — |
| Grid: occupied | no sprite (amber color) | letter text | — |
| Grid: turn valid | no sprite (blue color) | letter text | — |
| Grid: turn invalid | no sprite (rose color) | letter text | — |

---

## T1 — Hand tiles show sprite with chip text only
**Steps**: Enter Play mode (after Reimport Tile Sprites). Look at hand.
**Expected**: Each tile shows the letter sprite (serif letter on peach card). Chip value appears ONCE in the lower portion. No second letter text visible.
**Pass criteria**: Zero cases of double chip number. Letter sprite visible.

## T2 — Hand tiles fallback when sprites absent
**Steps**: Enter Play mode WITHOUT reimporting. Look at hand.
**Expected**: Each tile shows a peach (#FCDDBC) background. Letter text visible (upper center). Chip value visible (lower). No white/grey backgrounds.
**Pass criteria**: Peach color correct. Both letter and chip text readable. No doubling.

## T3 — Grid modifier cells: label suppressed when sprite loads
**Steps**: After Reimport, enter Play mode. Look at TW, DW, TL, DL, and empty cells.
**Expected**:
- TW: blue/purple square with "TW" from sprite. No second "TW" text overlay.
- DW: pink square with "DW" from sprite. No second "DW" text overlay.
- TL: green square with "TL" from sprite. No second "TL" text overlay.
- DL: olive square with "DL" from sprite. No second "DL" text overlay.
- Empty: plain light square. No text.
**Pass criteria**: Each modifier shows its label exactly once. No doubled labels.

## T4 — Grid modifier fallback labels when sprites absent
**Steps**: WITHOUT reimporting, enter Play mode.
**Expected**: Modifier cells show fallback colors with code-generated labels ("TW", "DW", "TL", "DL"). No doubling.
**Pass criteria**: Labels appear once per cell.

## T5 — Center cell shows ★ always
**Steps**: Enter Play mode. Find center cell (4,4).
**Expected**: Center cell shows DW color (pink/rose) with "★" label. Always uses color fallback (dw sprite says "DW" not "★").
**Pass criteria**: Star visible. No "DW" text on center cell.

## T6 — Grid occupied cells: correct color and letter, no double letter
**Steps**: Place a tile (e.g. 'A') on the grid.
**Expected**: The cell shows blue (turn valid), letter 'A' in text overlay. No image sprite on the cell. After submitting: amber color, 'A' text.
**Pass criteria**: Letter appears once. No "W"/"O"/"X" placeholder letters from baked-in sprites.

## T7 — Scoring animation: letter once, chip once
**Steps**: Submit a valid word. Watch scoring animation.
**Expected**: Each tile in the scoring sequence shows: letter sprite (or text if no sprite), chip value text once.
**Pass criteria**: Zero doubled numbers or doubled letters.

## T8 — Redraw mode tinting
**Steps**: Click RESHUFFLE. Click one tile.
**Expected**: Selected tile tints red. Other tiles stay white (sprite tinted white = unchanged) or peach (no sprite).
**Deselect**: Click same tile → reverts to white/peach.
**Cancel**: Click RESHUFFLE again → all tiles white/peach.
**Pass criteria**: No stuck red or white tiles after exiting redraw mode.

## T9 — No console errors
**Steps**: Play through placing + submitting a word.
**Expected**: Zero NullReferenceExceptions in TileHandUI, ScoreUI, or GridUI.
**Pass criteria**: Clean console.

---

## Analytical verification

- **T1**: `letterSpr != null` → `img.sprite = letterSpr`, `LetterText.SetActive(false)`, `ChipsText.text = chips`. One chip, no letter text. PASS.
- **T2**: `letterSpr == null` → `img.color = normalColor` (peach), `LetterText.SetActive(true)`, both texts shown. PASS.
- **T3**: `ApplyCellVisual("tw", …)` returns true → `txt.text = ""`. Label suppressed. PASS.
- **T4**: `ApplyCellVisual("tw", …)` returns false → `txt.text = "TW"`. Label shown. PASS.
- **T5**: Center cell bypasses sprite lookup entirely → `img.sprite = null; img.color = dwColor; txt.text = "★"`. PASS.
- **T6**: Occupied cells bypass sprite lookup entirely → `img.sprite = null; img.color = cellColor; txt.text = letter`. One letter. PASS.
- **T7**: ScoreUI `SpawnTile` — `letterSpr != null` → sprite background, "L" hidden, "C" shown. One chip. PASS.
- **T8**: Deselect uses `img.sprite != null ? Color.white : normalColor`. Correctly restores white for sprite tiles, peach for no-sprite. PASS.
- **T9**: No code path calls `GetLetterSprite` then ignores null. All null cases handled with else branch. PASS.
