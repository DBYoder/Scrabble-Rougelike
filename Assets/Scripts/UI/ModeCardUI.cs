// ModeCardUI.cs — Populates a single mode card and starts the run when clicked.
using UnityEngine;
using UnityEngine.UI;

public class ModeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image  accentBar;
    public Text   modeNameText;
    public Text   modeDescText;
    public Button selectButton;

    private RunConfig _config;

    public void Populate(RunConfig cfg)
    {
        _config = cfg;
        if (modeNameText != null) modeNameText.text  = cfg.modeName;
        if (modeDescText != null) modeDescText.text  = cfg.modeDescription;
        if (accentBar    != null) accentBar.color     = cfg.modeAccentColor;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelect);
        }
    }

    private void OnSelect()
    {
        var mb = GameManager.Instance != null
            ? GameManager.Instance.GetComponent<MenuButtons>()
            : null;
        if (mb == null) return;
        mb.pendingConfig = _config;
        mb.StartRunWithConfig();
    }
}
