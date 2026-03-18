// MenuButtons.cs — Serializable button-target wrappers for persistent onClick listeners.
// Attach to the GameManager GameObject alongside GameManager.
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    /// <summary>Set by ModeCardUI when the player clicks a mode card.</summary>
    public RunConfig pendingConfig;

    // ── Start Menu Navigation ─────────────────────────────────────────────────
    public void ShowModeSelect()
    {
        MainMenuUI.Instance?.ShowModeSelect();
    }

    public void ShowLanding()
    {
        MainMenuUI.Instance?.ShowLanding();
    }

    // ── Run Start ─────────────────────────────────────────────────────────────
    /// <summary>Starts a new run with the config selected on the mode-select screen.</summary>
    public void StartRunWithConfig()
    {
        GameManager.Instance?.StartRun(pendingConfig);
    }

    /// <summary>Retries / plays again with the same mode config as the last run.</summary>
    public void StartRun()
    {
        GameManager.Instance?.StartRun(RunManager.Instance?.ActiveConfig);
    }

    // ── In-Game ───────────────────────────────────────────────────────────────
    public void ReturnToMenu()
    {
        GameManager.Instance?.ChangeState(GameState.MainMenu);
    }

    public void ProceedFromBossPreview()
    {
        GameManager.Instance?.ProceedFromBossPreview();
    }

    public void ProceedFromProgressionReward()
    {
        GameManager.Instance?.ProceedFromProgressionReward();
    }
}
