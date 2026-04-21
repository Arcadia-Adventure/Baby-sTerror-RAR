using UnityEditor;

public class BuildScript
{
    public static void PerformBuild()
    {
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = new[]
        {
            "Assets/Scenes/Loading.unity"
        };

        options.target = BuildTarget.Android;
        options.locationPathName = "Builds/iOS";
        options.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(options);
    }
}