// BossPreviewUI.cs — Populates the boss blind preview modal overlay.
// Attached to BossPreviewPanel by SceneBuilder.
using UnityEngine;
using UnityEngine.UI;

public class BossPreviewUI : MonoBehaviour
{
    [Header("References (wired by SceneBuilder)")]
    public Text modifierNameText;
    public Text modifierDescText;
    public Text anteBlindText;

    public void Populate(BossModifier modifier, string description)
    {
        if (modifierNameText != null)
            modifierNameText.text = FormatModifierName(modifier);

        if (modifierDescText != null)
            modifierDescText.text = description;

        if (anteBlindText != null && RunManager.Instance != null)
        {
            var rm = RunManager.Instance;
            anteBlindText.text = $"Ante {rm.currentAnte}  —  Boss Blind";
        }
    }

    private static string FormatModifierName(BossModifier modifier)
    {
        // Insert spaces before uppercase letters for display (e.g. VowelsWorthZero → Vowels Worth Zero)
        var name = modifier.ToString();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString().ToUpper();
    }
}
