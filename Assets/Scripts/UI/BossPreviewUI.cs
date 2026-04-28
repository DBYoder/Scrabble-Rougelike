// BossPreviewUI.cs — Populates the Exam preview modal overlay.
// Attached to BossPreviewPanel by SceneBuilder.
using UnityEngine;
using UnityEngine.UI;

public class BossPreviewUI : MonoBehaviour
{
    [Header("References (wired by SceneBuilder)")]
    public Text modifierNameText;
    public Text modifierDescText;
    public Text anteBlindText;

    private void Start()
    {
        // Widen modifier labels to fill the modal (default 400px wraps in a 960px panel)
        foreach (var t in new[] { modifierNameText, modifierDescText, anteBlindText })
        {
            if (t == null) continue;
            var le = t.GetComponent<LayoutElement>();
            if (le != null) le.preferredWidth = 900f;
        }
    }

    public void Populate(BossModifier modifier, string description)
    {
        if (modifierNameText != null)
            modifierNameText.text = FormatModifierName(modifier);

        if (modifierDescText != null)
        {
            // Normalise legacy "Boss: " prefix → "Exam: " for consistent academic terminology.
            string desc = description;
            if (desc.StartsWith("Boss: ", System.StringComparison.OrdinalIgnoreCase))
                desc = "Exam: " + desc.Substring("Boss: ".Length);
            modifierDescText.text = desc;
        }

        if (anteBlindText != null && RunManager.Instance != null)
        {
            var rm = RunManager.Instance;
            anteBlindText.text = $"Chapter {rm.currentAnte}  —  Exam";
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
