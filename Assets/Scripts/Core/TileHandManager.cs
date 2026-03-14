// TileHandManager.cs — Manages the player's hand and the weighted tile bag.
using System.Collections.Generic;
using UnityEngine;

public class TileHandManager : MonoBehaviour
{
    public static TileHandManager Instance { get; private set; }

    public const int HandSize      = 7;
    public const int BaseRedraws   = 3;
    public const int MaxWordPlays  = 5;

    // Runtime state
    public List<TileInstance> hand    = new List<TileInstance>();
    public List<TileInstance> tileBag = new List<TileInstance>();
    public int redrawsRemaining;
    public int wordPlaysRemaining;
    public int bonusRedraws; // Persistent across the run (from shop purchases)

    private LetterData[] allLetterData;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        allLetterData = Resources.LoadAll<LetterData>("Letters");
        if (allLetterData == null || allLetterData.Length == 0)
            Debug.LogWarning("[TileHandManager] No LetterData assets found in Resources/Letters/.");
    }

    // ── Round Initialisation ─────────────────────────────────────────────────
    public void InitRound()
    {

        redrawsRemaining  = BaseRedraws + bonusRedraws;
        wordPlaysRemaining = MaxWordPlays;
        RebuildBag();
        hand.Clear();
        DrawToFull();
    }

    // ── Tile Bag ─────────────────────────────────────────────────────────────
    /// <summary>Rebuilds the bag from all LetterData assets using their weights.</summary>
    public void RebuildBag()
    {
        tileBag.Clear();
        if (allLetterData == null) return;

        foreach (var data in allLetterData)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(data.weight * 10));
            for (int i = 0; i < count; i++)
                tileBag.Add(new TileInstance(data));
        }
        ShuffleBag();
    }

    private void ShuffleBag()
    {
        for (int i = tileBag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (tileBag[i], tileBag[j]) = (tileBag[j], tileBag[i]);
        }
    }

    // ── Drawing ──────────────────────────────────────────────────────────────
    public void DrawToFull()
    {
        while (hand.Count < HandSize && tileBag.Count > 0)
        {
            hand.Add(tileBag[tileBag.Count - 1]);
            tileBag.RemoveAt(tileBag.Count - 1);
        }
    }

    // ── Redraw ───────────────────────────────────────────────────────────────
    /// <summary>Discards the given tiles and draws replacements.
    /// Costs 2 redraw charges on the DoubleRedrawCost boss blind, 1 otherwise.</summary>
    public bool Redraw(List<TileInstance> toDiscard)
    {
        int cost = (RunManager.Instance != null
                    && RunManager.Instance.IsBossBlind
                    && RunManager.Instance.GetBossModifier() == BossModifier.DoubleRedrawCost)
                   ? 2 : 1;
        if (redrawsRemaining < cost) return false;
        redrawsRemaining -= cost;
        foreach (var tile in toDiscard)
            hand.Remove(tile);
        DrawToFull();
        return true;
    }

    // ── Hand Manipulation ─────────────────────────────────────────────────────
    public void RemoveTileFromHand(TileInstance tile) => hand.Remove(tile);

    public void ReturnTileToHand(TileInstance tile)
    {
        if (!hand.Contains(tile) && hand.Count < HandSize)
            hand.Add(tile);
    }

    // ── Lexicon Helpers ───────────────────────────────────────────────────────
    /// <summary>Returns the letter in hand with the lowest bag weight (rarest).</summary>
    public char GetRarestHandLetter()
    {
        if (hand.Count == 0) return '\0';
        TileInstance rarest = hand[0];
        foreach (var t in hand)
            if (t.letterData.weight < rarest.letterData.weight)
                rarest = t;
        return rarest.Letter;
    }

    /// <summary>True when all hand tiles have been placed (for Pangram check).</summary>
    public bool AllTilesPlaced() => hand.Count == 0;
}
