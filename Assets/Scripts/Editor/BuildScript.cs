using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Crossword/Build for Windows")]
    public static void BuildWindows()
    {
        string folder = EditorUtility.SaveFolderPanel("Choose Build Output Folder", "", "");
        if (string.IsNullOrEmpty(folder)) return;

        string path = Path.Combine(folder, "CrossWordRoguelike.exe");

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"Build succeeded: {path}");
        else
            Debug.LogError($"Build failed with {report.summary.totalErrors} error(s)");
    }
}
