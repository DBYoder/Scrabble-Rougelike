// ScoreManager.cs — Calculates Chips × Multiplier for all words on the board.
// Applies: base chips, intersection doubling, length bonuses, rare-letter bonuses,
// Lexicon effects, and Boss Blind modifiers.
using System.Collections.Generic;
using UnityEngine;

// ── Result Types ─────────────────────────────────────────────────────────────
public class WordScoreResult
{
    public string word;
    public int    chips;
    public float  multiplier;
    public int    score;
    public List<string>            bonusLabels      = new List<string>();
    public List<LexiconEffectType> triggeredEffects = new List<LexiconEffectType>();
    public List<(int x, int y)>    cellPositions    = new List<(int x, int y)>();
}

public class ScoreResult
{
    public int totalScore;
    public List<WordScoreResult> wordResults = new List<WordScoreResult>();
}

// ── Manager ──────────────────────────────────────────────────────────────────
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Tracks words scored this round for Anagram detection
    private readonly HashSet<string> scoredWordsThisRound = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ResetRoundWords() => scoredWordsThisRound.Clear();

    // ── Main Entry Point ─────────────────────────────────────────────────────
    public ScoreResult CalculateScore(
        List<Word>           validWords,
        List<LexiconWordData> activeLexicon,
        BossModifier         bossModifier = BossModifier.None)
    {
        var result = new ScoreResult();
        if (validWords == null || validWords.Count == 0) return result;

        // Build intersection map: how many words use each grid cell
        var cellWordCount = BuildIntersectionMap(validWords);

        // Resolve lexicon references once
        bool hasHapax        = HasLexicon(activeLexicon, LexiconEffectType.HapaxLegomenon);
        bool hasPortmanteau  = HasLexicon(activeLexicon, LexiconEffectType.Portmanteau);
        bool hasLoanword     = HasLexicon(activeLexicon, LexiconEffectType.Loanword);
        bool hasPalindrome   = HasLexicon(activeLexicon, LexiconEffectType.Palindrome);
        bool hasPangram      = HasLexicon(activeLexicon, LexiconEffectType.Pangram);
        bool hasNeologism    = HasLexicon(activeLexicon, LexiconEffectType.Neologism);
        bool hasAnagram      = HasLexicon(activeLexicon, LexiconEffectType.Anagram);
        bool hasOxymoron     = HasLexicon(activeLexicon, LexiconEffectType.Oxymoron);
        bool hasSesqui       = HasLexicon(activeLexicon, LexiconEffectType.Sesquipedalian);
        bool hasGlossary     = HasLexicon(activeLexicon, LexiconEffectType.TheGlossary);
        bool hasPolysyllabic = HasLexicon(activeLexicon, LexiconEffectType.Polysyllabic);
        bool hasRareLetter   = HasLexicon(activeLexicon, LexiconEffectType.RareLetter);
        bool hasConfluence   = HasLexicon(activeLexicon, LexiconEffectType.Confluence);
        bool hasEpigram      = HasLexicon(activeLexicon, LexiconEffectType.Epigram);
        bool hasVerbosity    = HasLexicon(activeLexicon, LexiconEffectType.Verbosity);
        bool hasGemination   = HasLexicon(activeLexicon, LexiconEffectType.Gemination);
        bool hasSyllabary    = HasLexicon(activeLexicon, LexiconEffectType.Syllabary);
        bool hasAcrostic     = HasLexicon(activeLexicon, LexiconEffectType.Acrostic);
        bool hasLexeme       = HasLexicon(activeLexicon, LexiconEffectType.Lexeme);

        char rarestLetter  = hasHapax   ? GetRarestBoardLetter(validWords) : '\0';
        char featuredLetter = hasGlossary ? GetFeaturedLetter() : '\0';
        int  neologismStack = hasNeologism ? GetNeologismCount() : 0;

        // Pangram flat chips (+50 if all 7 tiles placed)
        bool pangramTriggered = hasPangram && TileHandManager.Instance != null
                                           && TileHandManager.Instance.AllTilesPlaced();

        // Lexeme: pre-compute board density bonus (all words share the same value)
        float lexemeBonus = hasLexeme ? Mathf.Min((validWords.Count - 1) * 0.3f, 2.0f) : 0f;

        // Score each word
        var anagramTracker = new HashSet<string>(); // sorted chars of words scored this round

        foreach (var word in validWords)
        {
            var wr = new WordScoreResult { word = word.text };
            wr.cellPositions.AddRange(word.cellPositions);

            // ── Step 1: Chips ────────────────────────────────────────────────
            int chips = 0;
            float cellWordMult = 1f;
            foreach (var pos in word.cellPositions)
            {
                var cell = GridManager.Instance.GetCell(pos.x, pos.y);
                if (cell?.placedTile == null) continue;
                var tile = cell.placedTile;

                int tc = tile.TotalChips;

                // Cell letter modifiers (DL / TL)
                if (cell.modifier == CellModifier.DoubleLetter)
                {
                    tc *= 2;
                    wr.bonusLabels.Add($"DL '{char.ToUpper(tile.Letter)}'×2");
                }
                else if (cell.modifier == CellModifier.TripleLetter)
                {
                    tc *= 3;
                    wr.bonusLabels.Add($"TL '{char.ToUpper(tile.Letter)}'×3");
                }

                // Accumulate cell word multipliers (DW / TW)
                if (cell.modifier == CellModifier.DoubleWord)   cellWordMult *= 2f;
                else if (cell.modifier == CellModifier.TripleWord) cellWordMult *= 3f;

                // Intersection: cell used by 2+ words → double that letter's chips (Confluence lexicon)
                if (hasConfluence
                    && !FlagSet(bossModifier, BossModifier.NoIntersections)
                    && cellWordCount.TryGetValue(pos, out int count) && count >= 2)
                {
                    tc *= 2;
                    wr.bonusLabels.Add($"Intersect '{char.ToUpper(tile.Letter)}'×2");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.Confluence))
                        wr.triggeredEffects.Add(LexiconEffectType.Confluence);
                }

                // Hapax: rarest hand letter → ×4 chips
                if (hasHapax && char.ToLower(tile.Letter) == char.ToLower(rarestLetter))
                {
                    tc *= 4;
                    wr.bonusLabels.Add($"Hapax '{char.ToUpper(tile.Letter)}'×4");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.HapaxLegomenon))
                        wr.triggeredEffects.Add(LexiconEffectType.HapaxLegomenon);
                }

                // Glossary: featured letter → ×2 chips
                if (hasGlossary && featuredLetter != '\0'
                    && char.ToLower(tile.Letter) == char.ToLower(featuredLetter))
                {
                    tc *= 2;
                    wr.bonusLabels.Add($"Glossary '{char.ToUpper(tile.Letter)}'×2");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.TheGlossary))
                        wr.triggeredEffects.Add(LexiconEffectType.TheGlossary);
                }

                // Boss: vowels worth zero chips
                if (bossModifier == BossModifier.VowelsWorthZero && IsVowel(tile.Letter))
                    tc = 0;

                chips += tc;
            }

            if (cellWordMult > 1f)
                wr.bonusLabels.Add($"Cell Word ×{cellWordMult:F0}");

            // Verbosity: +5 chips per letter
            if (hasVerbosity)
            {
                int vBonus = 5 * word.text.Length;
                chips += vBonus;
                wr.bonusLabels.Add($"Verbosity +{vBonus}");
                wr.triggeredEffects.Add(LexiconEffectType.Verbosity);
            }

            // Acrostic: +25 chips if word starts or ends on grid edge
            if (hasAcrostic && word.cellPositions.Count > 0)
            {
                var first = word.cellPositions[0];
                var last  = word.cellPositions[word.cellPositions.Count - 1];
                const int edgeMax = GridManager.GridSize - 1;
                if (first.x == 0 || first.x == edgeMax || first.y == 0 || first.y == edgeMax
                 || last.x  == 0 || last.x  == edgeMax || last.y  == 0 || last.y  == edgeMax)
                {
                    chips += 25;
                    wr.bonusLabels.Add("Acrostic +25");
                    wr.triggeredEffects.Add(LexiconEffectType.Acrostic);
                }
            }

            // Pangram flat bonus
            if (pangramTriggered)
            {
                chips += 50;
                wr.bonusLabels.Add("Pangram +50");
                if (!wr.triggeredEffects.Contains(LexiconEffectType.Pangram))
                    wr.triggeredEffects.Add(LexiconEffectType.Pangram);
            }

            // Sesquipedalian: 9+ letters → +25 chips
            if (hasSesqui && word.text.Length >= 9)
            {
                chips += 25;
                wr.bonusLabels.Add("Sesquipedalian +25");
                if (!wr.triggeredEffects.Contains(LexiconEffectType.Sesquipedalian))
                    wr.triggeredEffects.Add(LexiconEffectType.Sesquipedalian);
            }

            wr.chips = chips;

            // ── Step 2: Multiplier ───────────────────────────────────────────
            float mult = 1f;

            // Polysyllabic: length mult bonuses (lexicon-gated)
            if (hasPolysyllabic)
            {
                if (word.text.Length >= 5)  { mult += 0.5f; wr.bonusLabels.Add("5+ letters +0.5×"); }
                if (word.text.Length >= 7)  { mult += 1.0f; wr.bonusLabels.Add("7+ letters +1.0×"); }
                if (word.text.Length >= 10) { mult += 2.0f; wr.bonusLabels.Add("10+ letters +2.0×"); }
                if (word.text.Length >= 5 && !wr.triggeredEffects.Contains(LexiconEffectType.Polysyllabic))
                    wr.triggeredEffects.Add(LexiconEffectType.Polysyllabic);
            }

            // Rare Letter: Q/Z/X/J mult bonus (lexicon-gated)
            if (hasRareLetter)
            {
                foreach (char c in word.text)
                {
                    if (LetterData.IsRareLetter(c))
                    {
                        mult += 0.5f;
                        wr.bonusLabels.Add($"Rare '{char.ToUpper(c)}' +0.5×");
                        if (!wr.triggeredEffects.Contains(LexiconEffectType.RareLetter))
                            wr.triggeredEffects.Add(LexiconEffectType.RareLetter);
                    }
                }
            }

            // Neologism: +0.1× per unique word scored this run, capped at +3.0×
            if (hasNeologism && neologismStack > 0)
            {
                const float neologismCap = 3.0f;
                float neologismBonus = Mathf.Min(neologismStack * 0.1f, neologismCap);
                mult += neologismBonus;
                wr.bonusLabels.Add(neologismBonus >= neologismCap
                    ? $"Neologism +{neologismBonus:F1}× (max)"
                    : $"Neologism +{neologismBonus:F1}×");
                if (!wr.triggeredEffects.Contains(LexiconEffectType.Neologism))
                    wr.triggeredEffects.Add(LexiconEffectType.Neologism);
            }

            // Portmanteau: word shares a tile with 2+ other words → ×2 Mult
            if (hasPortmanteau)
            {
                int sharedWithCount = CountWordsSharedWith(word, validWords, cellWordCount);
                if (sharedWithCount >= 2)
                {
                    mult *= 2f;
                    wr.bonusLabels.Add("Portmanteau ×2");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.Portmanteau))
                        wr.triggeredEffects.Add(LexiconEffectType.Portmanteau);
                }
            }

            // Loanword: Q not followed by U → +3 Mult
            if (hasLoanword)
            {
                for (int i = 0; i < word.text.Length; i++)
                {
                    if (word.text[i] == 'q' &&
                        (i + 1 >= word.text.Length || word.text[i + 1] != 'u'))
                    {
                        mult += 3f;
                        wr.bonusLabels.Add("Loanword Q +3×");
                        if (!wr.triggeredEffects.Contains(LexiconEffectType.Loanword))
                            wr.triggeredEffects.Add(LexiconEffectType.Loanword);
                    }
                }
            }

            // Palindrome: same forwards and backwards → ×5 Mult
            if (hasPalindrome && IsPalindrome(word.text))
            {
                mult *= 5f;
                wr.bonusLabels.Add("Palindrome ×5");
                if (!wr.triggeredEffects.Contains(LexiconEffectType.Palindrome))
                    wr.triggeredEffects.Add(LexiconEffectType.Palindrome);
            }

            // Oxymoron: Q + common vowel in same word → +2 Mult
            if (hasOxymoron && word.text.Contains("q") && ContainsCommonVowel(word.text))
            {
                mult += 2f;
                wr.bonusLabels.Add("Oxymoron +2×");
                if (!wr.triggeredEffects.Contains(LexiconEffectType.Oxymoron))
                    wr.triggeredEffects.Add(LexiconEffectType.Oxymoron);
            }

            // Anagram: anagram of a word already scored this round → ×2 Mult
            if (hasAnagram)
            {
                string sortedChars = SortedChars(word.text);
                if (anagramTracker.Contains(sortedChars))
                {
                    mult *= 2f;
                    wr.bonusLabels.Add("Anagram ×2");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.Anagram))
                        wr.triggeredEffects.Add(LexiconEffectType.Anagram);
                }
                anagramTracker.Add(sortedChars);
            }

            // Epigram: short word mult bonus
            if (hasEpigram)
            {
                if (word.text.Length == 3)
                {
                    mult += 2.0f;
                    wr.bonusLabels.Add("Epigram 3L +2×");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.Epigram))
                        wr.triggeredEffects.Add(LexiconEffectType.Epigram);
                }
                else if (word.text.Length == 4)
                {
                    mult += 1.5f;
                    wr.bonusLabels.Add("Epigram 4L +1.5×");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.Epigram))
                        wr.triggeredEffects.Add(LexiconEffectType.Epigram);
                }
            }

            // Gemination: adjacent repeated letter pairs → +0.8× each
            if (hasGemination)
            {
                int pairs = 0;
                for (int i = 0; i < word.text.Length - 1; i++)
                    if (word.text[i] == word.text[i + 1]) pairs++;
                if (pairs > 0)
                {
                    mult += pairs * 0.8f;
                    wr.bonusLabels.Add($"Gemination ×{pairs} +{pairs * 0.8f:F1}×");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.Gemination))
                        wr.triggeredEffects.Add(LexiconEffectType.Gemination);
                }
            }

            // Syllabary: 3+ distinct vowels → +1.5× Mult
            if (hasSyllabary)
            {
                var distinctVowels = new HashSet<char>();
                foreach (char c in word.text)
                    if (IsVowel(c)) distinctVowels.Add(c);
                if (distinctVowels.Count >= 3)
                {
                    mult += 1.5f;
                    wr.bonusLabels.Add($"Syllabary {distinctVowels.Count}V +1.5×");
                    if (!wr.triggeredEffects.Contains(LexiconEffectType.Syllabary))
                        wr.triggeredEffects.Add(LexiconEffectType.Syllabary);
                }
            }

            // Lexeme: board density bonus (pre-computed above)
            if (hasLexeme && lexemeBonus > 0f)
            {
                mult += lexemeBonus;
                wr.bonusLabels.Add($"Lexeme +{lexemeBonus:F1}×");
                if (!wr.triggeredEffects.Contains(LexiconEffectType.Lexeme))
                    wr.triggeredEffects.Add(LexiconEffectType.Lexeme);
            }

            wr.multiplier = mult * cellWordMult;
            wr.score      = Mathf.RoundToInt(chips * mult * cellWordMult);
            result.totalScore += wr.score;
            result.wordResults.Add(wr);

            // Register word for Neologism tracking and high-score tracking
            RunManager.Instance?.RegisterScoredWord(word.text, wr.score);
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Dictionary<(int, int), int> BuildIntersectionMap(List<Word> words)
    {
        var map = new Dictionary<(int, int), int>();
        foreach (var w in words)
            foreach (var pos in w.cellPositions)
            {
                if (!map.ContainsKey(pos)) map[pos] = 0;
                map[pos]++;
            }
        return map;
    }

    private int CountWordsSharedWith(Word word, List<Word> allWords, Dictionary<(int,int),int> map)
    {
        int shared = 0;
        var posSet = new HashSet<(int,int)>(word.cellPositions);
        foreach (var other in allWords)
        {
            if (other == word) continue;
            foreach (var pos in other.cellPositions)
                if (posSet.Contains(pos)) { shared++; break; }
        }
        return shared;
    }

    private bool HasLexicon(List<LexiconWordData> list, LexiconEffectType type)
        => list != null && list.Exists(l => l.effectType == type);

    // Scan the tiles that are actually on the board in the words being scored.
    // Using hand tiles was wrong: DrawToFull() refills the hand before scoring runs,
    // so the hand no longer contains the tiles the player placed.
    private char GetRarestBoardLetter(List<Word> validWords)
    {
        char  rarest       = '\0';
        float lowestWeight = float.MaxValue;
        var   seen         = new HashSet<char>();

        foreach (var word in validWords)
        {
            foreach (var pos in word.cellPositions)
            {
                var cell = GridManager.Instance.GetCell(pos.x, pos.y);
                if (cell?.placedTile?.letterData == null) continue;
                char letter = char.ToLower(cell.placedTile.Letter);
                if (!seen.Add(letter)) continue;  // already evaluated this letter
                float weight = cell.placedTile.letterData.weight;
                if (weight < lowestWeight) { lowestWeight = weight; rarest = letter; }
            }
        }
        return rarest;
    }

    private char GetFeaturedLetter()
        => RunManager.Instance != null ? RunManager.Instance.featuredLetter : '\0';

    private int GetNeologismCount()
        => RunManager.Instance != null ? RunManager.Instance.totalWordsScored : 0;

    private bool FlagSet(BossModifier modifier, BossModifier flag) => modifier == flag;

    private static bool IsPalindrome(string s)
    {
        for (int i = 0; i < s.Length / 2; i++)
            if (s[i] != s[s.Length - 1 - i]) return false;
        return true;
    }

    private static bool ContainsCommonVowel(string s)
        => s.Contains("a") || s.Contains("e") || s.Contains("i")
        || s.Contains("o") || s.Contains("u");

    private static bool IsVowel(char c)
    {
        c = char.ToLower(c);
        return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u';
    }

    private static string SortedChars(string s)
    {
        char[] arr = s.ToCharArray();
        System.Array.Sort(arr);
        return new string(arr);
    }
}
