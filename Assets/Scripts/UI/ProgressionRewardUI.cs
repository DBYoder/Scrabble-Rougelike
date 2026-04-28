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

        if (r.boardExpanded)
        {
            sb.AppendLine($"Board  {r.previousBoardSize}×{r.previousBoardSize}  →  {r.newBoardSize}×{r.newBoardSize}");
            if (r.newBoardSize >= GridManager.GridSize)
                sb.AppendLine("  Full board now unlocked!");
        }

        // Hand size grows every blind — show as current state
        string handCap = r.newHandSize >= 12 ? " (max)" : "";
        sb.AppendLine($"Hand size: {r.newHandSize} tiles{handCap}");

        rewardText.text = sb.ToString().TrimEnd();

        // Resize the LayoutElement so the VLG allocates enough vertical space.
        // AddLabel() only reserves one line worth of height; this content can be 2-3 lines.
        var le = rewardText.GetComponent<LayoutElement>();
        if (le != null)
        {
            int lineCount = rewardText.text.Split('\n').Length;
            le.preferredHeight = Mathf.Max(lineCount * rewardText.fontSize * 1.6f, 60f);
        }

        // Force the layout to recalculate immediately so the button repositions.
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            GetComponent<RectTransform>() ?? transform.parent.GetComponent<RectTransform>());
    }
}
