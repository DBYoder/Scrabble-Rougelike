// UIAssetLoader.cs — Cached Resources.Load helpers for game-screen sprites.
// All PNGs live under Assets/Resources/UI/{category}/.
// Run "Crossword → Reimport UI Sprites" once after adding new PNGs.
using System.Collections.Generic;
using UnityEngine;

public static class UIAssetLoader
{
    static readonly Dictionary<string, Sprite> _panels  = new();
    static readonly Dictionary<string, Sprite> _buttons = new();
    static readonly Dictionary<string, Sprite> _results = new();
    static readonly Dictionary<string, Sprite> _banners = new();
    static readonly Dictionary<string, Sprite> _hud     = new();
    static readonly Dictionary<string, Sprite> _lexicon = new();

    public static Sprite GetPanel(string name)   => Load(_panels,  $"UI/panels/panel_{name}");
    public static Sprite GetButton(string name)  => Load(_buttons, $"UI/buttons/btn_{name}");
    public static Sprite GetResult(string name)  => Load(_results, $"UI/results/result_{name}");
    public static Sprite GetBanner(string name)  => Load(_banners, $"UI/banners/{name}");
    public static Sprite GetHud(string name)     => Load(_hud,     $"UI/hud/hud_{name}");
    public static Sprite GetLexicon(string name) => Load(_lexicon, $"UI/lexicon/lex_{name}");

    public static void ClearCache()
    {
        _panels.Clear(); _buttons.Clear(); _results.Clear();
        _banners.Clear(); _hud.Clear(); _lexicon.Clear();
    }

    static Sprite Load(Dictionary<string, Sprite> cache, string path)
    {
        if (!cache.TryGetValue(path, out var spr))
            cache[path] = spr = Resources.Load<Sprite>(path);
        return spr;
    }
}
