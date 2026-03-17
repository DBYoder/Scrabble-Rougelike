// ProgressionRewardUI.cs — Populates the post-Exam progression reward modal.
// Shown between the Upgrade screen and the next round, so the player sees
// exactly what expanded before they start playing.
// Attached to ProgressionRewardPanel by SceneBuilder.
using UnityEngine;
using UnityEngine.UI;

public class ProgressionRewardUI : MonoBehaviour
{
    [Header("References (wired by SceneBuilder)")]
    public Text headerText;
    public Text rewardText;

    public void Populate(ProgressionRewards r)
    {
        if (headerText != null)
            headerText.text = "CHAPTER CLEARED!";

        if (rewardText == null) return;

        var sb = new System.Text.StringBuilder();

        if (r.handGrew)
        {
            sb.AppendLine($"Hand size  {r.previousHandSize}  →  {r.newHandSize} tiles");
        }
        else
        {
            sb.AppendLine($"Hand size  {r.newHandSize} tiles  (max reached)");
        }

        if (r.boardExpanded)
        {
            sb.AppendLine($"Board  {r.previousBoardSize}×{r.previousBoardSize}  →  {r.newBoardSize}×{r.newBoardSize}");
            if (r.newBoardSize >= GridManager.GridSize)
                sb.AppendLine("  — full board now unlocked!");
        }

        rewardText.text = sb.ToString().TrimEnd();
    }
}
