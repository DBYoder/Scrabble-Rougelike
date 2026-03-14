// MenuButtons.cs — Serializable button-target wrappers for persistent onClick listeners.
// Attach to the GameManager GameObject alongside GameManager.
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public void StartRun()
    {
        GameManager.Instance?.StartRun();
    }

    public void ReturnToMenu()
    {
        GameManager.Instance?.ChangeState(GameState.MainMenu);
    }

    public void ProceedFromBossPreview()
    {
        GameManager.Instance?.ProceedFromBossPreview();
    }
}
