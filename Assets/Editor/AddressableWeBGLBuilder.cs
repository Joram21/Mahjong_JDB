using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;

public class AddressableWeBGLBuilder
{
    [MenuItem("Build/Build Addressables WebGL")]
    public static void BuildAddressablesWebGL()
    {
        // Build addressables first
        AddressableAssetSettings.BuildPlayerContent();

        // Get the build output path
        string buildPath = Path.Combine(Application.dataPath, "../ServerData");
        string webGLBuildPath = "Builds/WebGL";
        string targetPath = Path.Combine(webGLBuildPath, "AddressableAssets");

        // Create target directory if it doesn't exist
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        // Copy built addressables to WebGL build folder
        if (Directory.Exists(buildPath))
        {
            CopyDirectory(buildPath, targetPath, true);
            // Debug.Log($"Addressable assets copied to: {targetPath}");
            // Debug.Log("These files need to be uploaded to your web server alongside your WebGL build");
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);
        DirectoryInfo[] dirs = dir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}