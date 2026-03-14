// WordValidator.cs — Loads wordlist.txt into a HashSet and validates words.
// Place a wordlist (one word per line) at Assets/Resources/wordlist.txt.
// Recommended: ENABLE word list or Collins Scrabble Words (both freely available).
using System.Collections.Generic;
using UnityEngine;

public class WordValidator : MonoBehaviour
{
    public static WordValidator Instance { get; private set; }

    private HashSet<string> wordSet;
    public int WordCount => wordSet?.Count ?? 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadWordList();
    }

    private void LoadWordList()
    {
        wordSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        TextAsset asset = Resources.Load<TextAsset>("wordlist");
        if (asset == null)
        {
            Debug.LogWarning("[WordValidator] wordlist.txt not found in Resources/. All words will fail validation.");
            return;
        }

        string[] lines = asset.text.Split('\n');
        foreach (string line in lines)
        {
            string word = line.Trim().ToLower();
            if (word.Length >= GridManager.MinWordLength)
                wordSet.Add(word);
        }
        Debug.Log($"[WordValidator] Loaded {wordSet.Count} words.");
    }

    /// <summary>Returns true if the word exists in the loaded dictionary.</summary>
    public bool IsValid(string word) => wordSet != null && wordSet.Contains(word.ToLower());
}
