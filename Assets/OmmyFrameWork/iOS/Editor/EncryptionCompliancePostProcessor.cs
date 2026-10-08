#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class EncryptionCompliancePostProcessor
{
    // Runs late so other SDK post-processors can't overwrite the value.
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");

        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // false = "None of the algorithms mentioned above" in App Store Connect.
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif
