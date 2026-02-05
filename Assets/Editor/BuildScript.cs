using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Tools/Warcraft Return/Build Android APK")]
    public static void BuildAndroid()
    {
        string buildPath = "Builds/Android";
        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        string apkPath = Path.Combine(buildPath, "WarcraftReturn.apk");

        string[] scenes = new string[]
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Gameplay.unity"
        };

        PlayerSettings.companyName = "AIGameStudio";
        PlayerSettings.productName = "MT Warcraft Return";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.aigamestudio.warcraftreturn");
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("BUILD SUCCESS: " + apkPath + " (" + (summary.totalSize / (1024 * 1024)) + " MB)");
        }
        else
        {
            Debug.LogError("BUILD FAILED: " + summary.result);
            Debug.LogError("Total errors: " + summary.totalErrors);
        }
    }

    public static void BuildFromCommandLine()
    {
        BuildAndroid();
    }
}
