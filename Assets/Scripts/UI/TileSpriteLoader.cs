// TileSpriteLoader.cs — Cached Resources.Load helpers for tile/cell sprites.
// All PNGs must live under Assets/Resources/Tiles/{Letters,States,GridCells}/.
using System.Collections.Generic;
using UnityEngine;

public static class TileSpriteLoader
{
    static readonly Dictionary<char, Sprite>   _letters    = new();
    static readonly Dictionary<string, Sprite> _states     = new();
    static readonly Dictionary<string, Sprite> _cells      = new();

    // ── Letter tile sprites (portrait card per letter) ────────────────────────
    public static Sprite GetLetterSprite(char letter)
    {
        char key = char.ToUpper(letter);
        if (!_letters.TryGetValue(key, out var spr))
        {
            spr = Resources.Load<Sprite>($"Tiles/Letters/tile_{key}");
            _letters[key] = spr;
        }
        return spr;
    }

    public static Sprite GetBlankSprite()
    {
        if (!_letters.TryGetValue('_', out var spr))
        {
            spr = Resources.Load<Sprite>("Tiles/Letters/tile_BLANK");
            _letters['_'] = spr;
        }
        return spr;
    }

    // ── State sprites (default / valid / invalid / occupied) ─────────────────
    // name: "default" | "valid" | "invalid" | "occupied"
    public static Sprite GetStateSprite(string name)
    {
        if (!_states.TryGetValue(name, out var spr))
        {
            spr = Resources.Load<Sprite>($"Tiles/States/tile_state_{name}");
            _states[name] = spr;
        }
        return spr;
    }

    // Clears all cached lookups — call after reimporting sprites so stale nulls are evicted.
    public static void ClearCache()
    {
        _letters.Clear();
        _states.Clear();
        _cells.Clear();
    }

    // ── Grid cell sprites ─────────────────────────────────────────────────────
    // name: "empty" | "tw" | "dw" | "tl" | "dl"  (occupied/valid/invalid use color fallback)
    public static Sprite GetCellSprite(string name)
    {
        if (!_cells.TryGetValue(name, out var spr))
        {
            spr = Resources.Load<Sprite>($"Tiles/GridCells/cell_{name}");
            _cells[name] = spr;
        }
        return spr;
    }
}
