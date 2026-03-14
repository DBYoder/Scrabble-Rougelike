// ScoreUI.cs — Scoring screen with tile-laying word animation.
// Sequence per word:
//   1. Letters scale in one at a time (inline — fully blocks before next step)
//   2. Chips fly from the grid to the score counter
//   3. Score counts up; word-score label pops in beside the row
// After all words: PASSED!/FAILED appears below the last row.
// The word display area is a child of this scoring panel so it disappears
// automatically when the panel is hidden (shop transition, etc.).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance { get; private set; }

    [Header("References — wired by SceneBuilder (all optional; auto-created if absent)")]
    public Text   totalScoreText;
    public Text   targetScoreText;
    public Text   resultText;        // left-panel result — hidden in favour of centre result
    public Text   wordNameText;      // unused in new flow; kept for SceneBuilder compat
    public Text   wordBreakdownText; // unused in new flow; kept for SceneBuilder compat
    public Button continueButton;

    [Header("Animation Timing")]
    public float tileScaleDur   = 0.13f;  // seconds each tile takes to scale 0 → 1
    public float tileLayDelay   = 0.06f;  // gap between consecutive tiles
    public float chipFlightTime = 0.45f;
    public float chipStagger    = 0.07f;
    public float countSpeed     = 120f;

    [Header("Colors")]
    public Color passColor = new Color(0.722f, 0.847f, 0.729f);
    public Color failColor = new Color(0.937f, 0.584f, 0.616f);

    // ── Runtime ────────────────────────────────────────────────────────────────
    private RectTransform             _canvasRT;
    private RectTransform             _wordDisplayArea;  // child of THIS panel
    private Text                      _centreResultText;
    private readonly List<GameObject> _wordRows = new List<GameObject>();

    private const float TileW   = 62f;
    private const float TileH   = 82f;
    private const float TileGap = 4f;
    private const float RowH    = 112f;
    private const float RowGap  = 12f;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() => GameManager.Instance.OnScoringComplete());

        var canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        if (canvas != null) _canvasRT = canvas.GetComponent<RectTransform>();

        EnsureElements();
    }

    // Creates the word display area and centre result text as children of this
    // scoring panel. Because they live inside the panel, hiding the panel (on
    // any state transition) automatically hides them — no manual cleanup needed.
    private void EnsureElements()
    {
        // Resolve canvas RT here as well, so it's ready even if Start() hasn't fired yet.
        if (_canvasRT == null)
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (canvas != null) _canvasRT = canvas.GetComponent<RectTransform>();
        }

        // Word display area: fills the space between the left score overlay
        // (≈280 px) and the right lexicon sidebar (≈230 px), using pixel offsets
        // so it adapts to any resolution without percentage-based overlaps.
        if (_wordDisplayArea == null)
        {
            var go = new GameObject("WordDisplayArea", typeof(RectTransform));
            go.transform.SetParent(transform, false); // child of the SCORING PANEL
            _wordDisplayArea           = go.GetComponent<RectTransform>();
            // Centre-right area — clear of the left score overlay (≈280 px wide).
            // Rows grow downward from the top of this region.
            _wordDisplayArea.anchorMin = new Vector2(0.15f, 0f);
            _wordDisplayArea.anchorMax = new Vector2(1f,    1f);
            _wordDisplayArea.offsetMin = new Vector2(10f,   60f);
            _wordDisplayArea.offsetMax = new Vector2(-20f, -160f);
        }

        // PASSED / FAILED text: anchored inside the word display area.
        // Its Y is set dynamically once we know how many word rows were drawn.
        if (_centreResultText == null)
        {
            var go = new GameObject("CentreResult", typeof(RectTransform));
            go.transform.SetParent(_wordDisplayArea, false);
            var rt            = go.GetComponent<RectTransform>();
            rt.anchorMin      = new Vector2(0f, 1f);
            rt.anchorMax      = new Vector2(1f, 1f);
            rt.pivot          = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta      = new Vector2(0f, 72f);

            _centreResultText                    = go.AddComponent<Text>();
            _centreResultText.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _centreResultText.fontSize           = 52;
            _centreResultText.fontStyle          = FontStyle.Bold;
            _centreResultText.alignment          = TextAnchor.MiddleCenter;
            _centreResultText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _centreResultText.verticalOverflow   = VerticalWrapMode.Overflow;
            _centreResultText.raycastTarget      = false;

            var sh = go.AddComponent<Shadow>();
            sh.effectColor    = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(2f, -2f);

            go.SetActive(false);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────
    public void ShowScore(ScoreResult result)
    {
        // EnsureElements creates _wordDisplayArea and resolves _canvasRT.
        // We call it here (not just in Start) because GameManager activates the
        // scoring panel and calls ShowScore in the same frame, before Unity has
        // had a chance to fire Start() on the newly-enabled ScoreUI component.
        EnsureElements();
        StopAllCoroutines();
        StartCoroutine(AnimateScore(result));
    }

    // ── Main Coroutine ─────────────────────────────────────────────────────────
    private IEnumerator AnimateScore(ScoreResult result)
    {
        // ── Reset ──────────────────────────────────────────────────────────────
        if (continueButton    != null) continueButton.gameObject.SetActive(false);
        if (resultText        != null) resultText.gameObject.SetActive(false);
        if (wordNameText      != null) wordNameText.gameObject.SetActive(false);
        if (wordBreakdownText != null) wordBreakdownText.gameObject.SetActive(false);
        if (_centreResultText != null) _centreResultText.gameObject.SetActive(false);
        ClearWordRows();

        int displayScore = 0;
        if (totalScoreText  != null) totalScoreText.text  = "0";
        int target = RunManager.Instance.GetCurrentBlindTarget();
        if (targetScoreText != null) targetScoreText.text = $"/ {target}";

        float nextRowTop = 0f;

        if (result.wordResults.Count == 0)
        {
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            foreach (var wr in result.wordResults)
            {
                // ── Step 1: Lay tiles one at a time (fully sequential) ─────────
                // Each tile completes its scale-in before the next begins.
                // No sub-coroutines — everything runs inline so nothing overlaps.
                int   letterCount = wr.word.Length;
                float totalW = letterCount * TileW + (letterCount - 1) * TileGap;
                float startX = -totalW * 0.5f + TileW * 0.5f;

                var row = SpawnWordRow(wr.word, nextRowTop, RowH);
                _wordRows.Add(row);

                for (int i = 0; i < letterCount; i++)
                {
                    int chipVal = 1;
                    if (i < wr.cellPositions.Count)
                    {
                        var (cx, cy) = wr.cellPositions[i];
                        var cell = GridManager.Instance?.GetCell(cx, cy);
                        if (cell?.placedTile != null) chipVal = cell.placedTile.TotalChips;
                    }

                    var tile = SpawnTile(row.transform, wr.word[i], chipVal,
                                         startX + i * (TileW + TileGap), TileW, TileH, 30, 14);
                    tile.transform.localScale = Vector3.zero;

                    // Inline scale-in — blocks until this tile is fully visible
                    float scaleElapsed = 0f;
                    while (scaleElapsed < tileScaleDur)
                    {
                        scaleElapsed += Time.deltaTime;
                        float t = Mathf.SmoothStep(0f, 1f,
                                      Mathf.Clamp01(scaleElapsed / tileScaleDur));
                        tile.transform.localScale = Vector3.one * t;
                        yield return null;
                    }
                    tile.transform.localScale = Vector3.one;

                    // Gap between tiles (skipped after the last one)
                    if (i < letterCount - 1)
                        yield return new WaitForSeconds(tileLayDelay);
                }

                // Full word is now visible — pause before scoring starts
                yield return new WaitForSeconds(0.35f);

                // Flash lexicon cards
                foreach (var effect in wr.triggeredEffects)
                    RunUI.Instance?.FlashLexiconCard(effect);

                // ── Step 2: Fly chips from the grid to the score counter ────────
                int chipsLaunched = 0, chipsArrived = 0;
                for (int i = 0; i < wr.cellPositions.Count; i++)
                {
                    var (cx, cy) = wr.cellPositions[i];
                    var cell = GridManager.Instance?.GetCell(cx, cy);
                    if (cell?.placedTile == null) continue;

                    chipsLaunched++;
                    StartCoroutine(FlyChip(
                        GridUI.Instance.GetCellWorldPosition(cx, cy),
                        cell.placedTile.TotalChips,
                        () => chipsArrived++));

                    yield return new WaitForSeconds(chipStagger);
                }

                // Wait for all chips to arrive
                float waited  = 0f;
                float timeout = chipFlightTime + chipStagger * wr.cellPositions.Count + 0.5f;
                while (chipsArrived < chipsLaunched && waited < timeout)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                // ── Step 3: Count up this word's score ─────────────────────────
                int   startScore = displayScore;
                int   endScore   = displayScore + wr.score;
                float duration   = Mathf.Clamp(wr.score / countSpeed, 0.15f, 1.5f);
                float elapsed    = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
                    if (totalScoreText != null)
                        totalScoreText.text = displayScore.ToString();
                    yield return null;
                }

                displayScore = endScore;
                if (totalScoreText != null) totalScoreText.text = displayScore.ToString();

                // Word-score label beside the row
                SpawnWordScoreLabel(row.transform, wr.score, totalW * 0.5f + 12f);

                // Bonus labels below the row (lexicon effects, cell modifiers, etc.)
                SpawnBonusLabels(row.transform, wr.bonusLabels);

                nextRowTop += RowH + RowGap;

                // Pause before next word's tiles begin
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ── Step 4: Show PASSED / FAILED below the last word row ──────────────
        bool passed = RunManager.Instance.CheckBlindPassed();

        if (_centreResultText != null)
        {
            var rt = _centreResultText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, -(nextRowTop + 8f));
            _centreResultText.text  = passed ? "PASSED!" : "FAILED";
            _centreResultText.color = passed ? passColor  : failColor;
            _centreResultText.gameObject.SetActive(true);
        }
        else if (resultText != null)
        {
            resultText.text  = passed ? "PASSED!" : "FAILED";
            resultText.color = passed ? passColor  : failColor;
            resultText.gameObject.SetActive(true);
        }

        if (continueButton != null) continueButton.gameObject.SetActive(true);
    }

    // ── Spawn helpers ──────────────────────────────────────────────────────────
    private GameObject SpawnWordRow(string word, float topOffset, float rowH)
    {
        var go = new GameObject($"Row_{word}", typeof(RectTransform));
        go.transform.SetParent(_wordDisplayArea ?? transform, false);
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -topOffset);
        rt.sizeDelta        = new Vector2(0f, rowH);
        return go;
    }

    private static GameObject SpawnTile(Transform parent, char letter, int chips, float posX,
                                        float tileW, float tileH, int lFont, int cFont)
    {
        var go = new GameObject($"T_{letter}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(posX, 0f);
        rt.sizeDelta        = new Vector2(tileW, tileH);

        go.AddComponent<Image>().color = new Color(0.988f, 0.867f, 0.737f);

        var ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(0.55f, 0.40f, 0.20f, 0.5f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        // Letter (upper portion)
        AddText(go.transform, "L", letter.ToString().ToUpper(),
                new Vector2(0f, 0.30f), Vector2.one, lFont, FontStyle.Bold,
                new Color(0.22f, 0.18f, 0.20f));

        // Chip value (bottom strip)
        AddText(go.transform, "C", chips.ToString(),
                Vector2.zero, new Vector2(1f, 0.34f), cFont, FontStyle.Normal,
                new Color(0.45f, 0.28f, 0.10f));

        return go;
    }

    private static void SpawnWordScoreLabel(Transform rowParent, int score, float rightEdgeX)
    {
        var go = new GameObject("ScoreLabel", typeof(RectTransform));
        go.transform.SetParent(rowParent, false);
        go.transform.position += new Vector3(0f, 10f, 0f);
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(rightEdgeX + 6f, 0f);
        rt.sizeDelta        = new Vector2(90f, 36f);

        var txt = go.AddComponent<Text>();
        txt.text               = $"= {score}";
        txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize           = 20;
        txt.fontStyle          = FontStyle.Bold;
        txt.color              = new Color(1f, 0.88f, 0.25f);
        txt.alignment          = TextAnchor.MiddleLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget      = false;

        var sh = go.AddComponent<Shadow>();
        sh.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        sh.effectDistance = new Vector2(1f, -1f);
    }

    private static void AddText(Transform parent, string name, string content,
                                Vector2 anchorMin, Vector2 anchorMax,
                                int fontSize, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.text               = content;
        txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize           = fontSize;
        txt.fontStyle          = style;
        txt.color              = color;
        txt.alignment          = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget      = false;
    }

    private static void SpawnBonusLabels(Transform rowParent, List<string> labels)
    {
        if (labels == null || labels.Count == 0) return;

        var go = new GameObject("BonusLabels", typeof(RectTransform));
        go.transform.SetParent(rowParent, false);
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 4f);
        rt.sizeDelta        = new Vector2(0f, 20f);

        var txt = go.AddComponent<Text>();
        txt.text               = string.Join("  ·  ", labels);
        txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize           = 14;
        txt.fontStyle          = FontStyle.Italic;
        txt.color              = new Color(1f, 0.88f, 0.45f);
        txt.alignment          = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget      = false;
    }

    private void ClearWordRows()
    {
        foreach (var r in _wordRows) if (r != null) Destroy(r);
        _wordRows.Clear();
    }

    // ── Flying chip ────────────────────────────────────────────────────────────
    private IEnumerator FlyChip(Vector3 worldStart, int value, System.Action onArrival)
    {
        if (_canvasRT == null || totalScoreText == null) { onArrival?.Invoke(); yield break; }

        var go = new GameObject("Chip", typeof(RectTransform));
        go.transform.SetParent(_canvasRT, false);
        var rt       = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(70f, 36f);
        rt.position  = worldStart;

        var txt = go.AddComponent<Text>();
        txt.text               = $"+{value}";
        txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize           = 26;
        txt.fontStyle          = FontStyle.Bold;
        txt.color              = new Color(1f, 0.88f, 0.25f, 1f);
        txt.alignment          = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.raycastTarget      = false;

        var ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(0f, 0f, 0f, 0.65f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Vector3 endPos  = totalScoreText.transform.position;
        float   elapsed = 0f;

        while (elapsed < chipFlightTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / chipFlightTime);
            rt.position   = Vector3.Lerp(worldStart, endPos, t);
            rt.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.4f);
            var c = txt.color;
            c.a       = t > 0.75f ? Mathf.Lerp(1f, 0f, (t - 0.75f) / 0.25f) : 1f;
            txt.color = c;
            yield return null;
        }

        Destroy(go);
        onArrival?.Invoke();
    }
}
