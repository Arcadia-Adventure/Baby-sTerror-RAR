//Ommy
using UnityEngine;
using UnityEditor;
using System.IO;

public class ConvertPNGToSpriteContextMenu : Editor
{
    [MenuItem("Ommy/UI/Convert PNG to Sprite (2D and UI)", true)]
    private static bool ValidateConvertPNGToSprite()
    {
        // Enable the option only if a folder is selected
        return Selection.activeObject is DefaultAsset;
    }

    [MenuItem("Ommy/UI/Convert PNG to Sprite (2D and UI)")]
    private static void ConvertPNGToSprite()
    {
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning("Please select a valid folder in the Project window.");
            return;
        }

        ProcessFolder(folderPath);

        AssetDatabase.Refresh();
    }

    private static void ProcessFolder(string folderPath)
    {
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png");

        foreach (string pngFile in pngFiles)
        {
            ConvertPNG(pngFile);
        }

        // Recursively process subfolders
        string[] subfolders = Directory.GetDirectories(folderPath);
        foreach (string subfolder in subfolders)
        {
            ProcessFolder(subfolder);
        }
    }

    private static void ConvertPNG(string pngFile)
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(pngFile) as TextureImporter;

        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.mipmapEnabled = false; // Optional: Disable mipmaps

            AssetDatabase.ImportAsset(pngFile, ImportAssetOptions.ForceUpdate);

            Debug.Log($"Changed texture type of {pngFile} to Sprite (2D and UI)");
        }
        else
        {
            Debug.LogWarning($"Failed to load texture importer from {pngFile}");
        }
    }
}
