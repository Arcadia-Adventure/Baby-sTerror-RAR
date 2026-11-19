#if UNITY_ANDROID
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Gradle 9 cannot convert EDM's local Maven file:// URIs when the Unity project path
/// contains spaces (e.g. "Projects Git"). Re-applies the fix after EDM overwrites templates.
/// </summary>
public class FixFirebaseMavenRepoUri :
    IPreprocessBuildWithReport,
    IPostGenerateGradleAndroidProject
{
    const string TemplateRelativePath = "Assets/Plugins/Android/settingsTemplate.gradle";
    const string BrokenReplace = ".replace(\"\\\\\", \"/\")";
    const string FixedReplace = ".replace(\"\\\\\", \"/\").replace(\" \", \"%20\")";

    // Run after External Dependency Manager's AutoResolveOnBuild.
    public int callbackOrder => 10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        PatchFile(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, TemplateRelativePath));
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // path = .../Gradle/unityLibrary — settings.gradle lives next to it.
        var gradleRoot = Directory.GetParent(path)?.FullName;
        if (string.IsNullOrEmpty(gradleRoot))
            return;

        PatchFile(Path.Combine(gradleRoot, "settings.gradle"));
        PatchFile(Path.Combine(gradleRoot, "settings.gradle.kts"));
    }

    [InitializeOnLoadMethod]
    static void WatchTemplateInEditor()
    {
        EditorApplication.delayCall += () =>
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null)
                return;
            PatchFile(Path.Combine(projectRoot, TemplateRelativePath));
        };
    }

    internal static bool PatchFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;

        var text = File.ReadAllText(filePath);
        var original = text;

        // Keep EDM's unityProjectPath helper, but URL-encode spaces for Gradle 9.
        if (text.Contains("file://") && !text.Contains(".replace(\" \", \"%20\")"))
        {
            if (text.Contains(BrokenReplace))
                text = text.Replace(BrokenReplace, FixedReplace);
        }

        // Encode spaces inside file:// URIs (template or already-expanded Gradle settings).
        text = Regex.Replace(
            text,
            @"file:/{2,}[^""'\r\n]+",
            match => match.Value.Contains(" ") ? match.Value.Replace(" ", "%20") : match.Value);

        if (text == original)
            return false;

        File.WriteAllText(filePath, text);
        Debug.Log($"[FixFirebaseMavenRepoUri] Patched spaces in Maven file:// URI: {filePath}");
        return true;
    }
}

class FixFirebaseMavenRepoUriAssetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (var asset in importedAssets)
        {
            if (!asset.Replace('\\', '/').EndsWith("Plugins/Android/settingsTemplate.gradle"))
                continue;

            EditorApplication.delayCall += () =>
            {
                var fullPath = Path.Combine(
                    Directory.GetParent(Application.dataPath)!.FullName,
                    asset.Replace('/', Path.DirectorySeparatorChar));
                FixFirebaseMavenRepoUri.PatchFile(fullPath);
            };
        }
    }
}
#endif
