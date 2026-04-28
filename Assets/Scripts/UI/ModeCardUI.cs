// ModeCardUI.cs — Populates a single mode card and starts the run when clicked.
// For Scholar configs (unlockKey non-empty), shows an edition selector row.
using UnityEngine;
using UnityEngine.UI;

public class ModeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image  accentBar;
    public Text   modeNameText;
    public Text   modeDescText;
    public Text   statusText;
    public Button selectButton;

    private RunConfig _config;
    private Text      _editionLabel;   // spawned at runtime for scholars
    private int       _selectedEdition = 0;

    public void Populate(RunConfig cfg)
    {
        _config = cfg;
        _selectedEdition = string.IsNullOrEmpty(cfg.unlockKey)
            ? 0
            : (MetaProgressionManager.Instance?.GetSelectedEdition(cfg.unlockKey) ?? 0);

        bool locked = !string.IsNullOrEmpty(cfg.unlockKey)
                      && (MetaProgressionManager.Instance == null
                          || !MetaProgressionManager.Instance.IsScholarUnlocked(cfg.unlockKey));

        if (modeNameText != null)
        {
            modeNameText.text  = cfg.modeName;
            modeNameText.color = locked ? new Color(0.55f, 0.55f, 0.55f) : Color.white;
        }

        if (modeDescText != null)
        {
            modeDescText.text  = locked && !string.IsNullOrEmpty(cfg.unlockHint)
                ? cfg.unlockHint
                : cfg.modeDescription;
            modeDescText.color = locked ? new Color(0.50f, 0.50f, 0.50f) : new Color(0.85f, 0.85f, 0.85f);
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(locked);
            statusText.text  = "LOCKED";
            statusText.color = new Color(0.70f, 0.45f, 0.45f);
        }

        if (accentBar != null)
            accentBar.color = locked ? new Color(0.3f, 0.3f, 0.3f) : cfg.modeAccentColor;

        var cardBg = GetComponent<Image>();
        if (cardBg != null)
            cardBg.color = locked
                ? new Color(0.18f, 0.15f, 0.16f)
                : new Color(0.30f, 0.24f, 0.27f);

        if (selectButton != null)
        {
            selectButton.interactable = !locked;
            selectButton.onClick.RemoveAllListeners();
            if (!locked) selectButton.onClick.AddListener(OnSelect);
        }

        // Edition row — only shown for unlocked Scholar configs with editions available
        if (!locked && !string.IsNullOrEmpty(cfg.unlockKey))
            BuildEditionRow();
    }

    // ── Edition selector ──────────────────────────────────────────────────────
    private void BuildEditionRow()
    {
        // Remove any old edition row
        var old = transform.Find("EditionRow");
        if (old != null) Destroy(old.gameObject);

        int maxEd = MetaProgressionManager.Instance?.MaxUnlockedEdition(_config.unlockKey) ?? 0;
        if (maxEd == 0) return; // no editions unlocked yet

        var row = new GameObject("EditionRow", typeof(RectTransform));
        row.transform.SetParent(transform, false);

        // Position it after the desc text using a LayoutElement so VLG places it correctly
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight       = 32f;
        rowLe.preferredHeight = 32f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 6f;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childControlWidth     = false;
        hlg.padding               = new RectOffset(4, 4, 0, 0);

        // ◀ prev
        var prevBtn = MakeArrowButton(row.transform, "◀", () => CycleEdition(-1));

        // Edition name label
        var lblGo = new GameObject("EditionLabel", typeof(RectTransform));
        lblGo.transform.SetParent(row.transform, false);
        var lblLe = lblGo.AddComponent<LayoutElement>();
        lblLe.preferredWidth  = 160f;
        lblLe.preferredHeight = 28f;
        _editionLabel = lblGo.AddComponent<Text>();
        _editionLabel.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _editionLabel.fontSize           = 14;
        _editionLabel.fontStyle          = FontStyle.Bold;
        _editionLabel.color              = new Color(0.97f, 0.88f, 0.40f);
        _editionLabel.alignment          = TextAnchor.MiddleCenter;
        _editionLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        _editionLabel.verticalOverflow   = VerticalWrapMode.Overflow;
        RefreshEditionLabel();

        // ▶ next
        var nextBtn = MakeArrowButton(row.transform, "▶", () => CycleEdition(+1));
    }

    private Button MakeArrowButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Arrow", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 28f;
        le.preferredHeight = 28f;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.40f, 0.32f, 0.36f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("T", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var tRt = txtGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var t = txtGo.AddComponent<Text>();
        t.text               = label;
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize           = 16;
        t.fontStyle          = FontStyle.Bold;
        t.color              = Color.white;
        t.alignment          = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return btn;
    }

    private void CycleEdition(int delta)
    {
        if (_config == null || string.IsNullOrEmpty(_config.unlockKey)) return;
        int maxEd = MetaProgressionManager.Instance?.MaxUnlockedEdition(_config.unlockKey) ?? 0;
        _selectedEdition = (_selectedEdition + delta + maxEd + 1) % (maxEd + 1);
        MetaProgressionManager.Instance?.SetSelectedEdition(_config.unlockKey, _selectedEdition);
        RefreshEditionLabel();
    }

    private void RefreshEditionLabel()
    {
        if (_editionLabel == null) return;
        _editionLabel.text = MetaProgressionManager.EditionNames[
            Mathf.Clamp(_selectedEdition, 0, MetaProgressionManager.EditionNames.Length - 1)];
    }

    // ── Run start ─────────────────────────────────────────────────────────────
    private void OnSelect()
    {
        var mb = GameManager.Instance != null
            ? GameManager.Instance.GetComponent<MenuButtons>()
            : null;
        if (mb == null) return;

        // Apply edition modifiers to a copy of the config if needed
        RunConfig cfg = _selectedEdition > 0
            ? MetaProgressionManager.ApplyEdition(_config, _selectedEdition)
            : _config;

        mb.pendingConfig = cfg;
        mb.StartRunWithConfig();
    }
}
