using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-click builds (menu "Build"), also usable from the command line with -executeMethod:
///   Build > Windows (zip)          -> Builds/SpaceShooter_Windows.zip  (submission)
///   Build > WebGL for itch.io (zip) -> Builds/SpaceShooter_WebGL_itch.zip (upload to itch.io as HTML game)
/// Builds go to the Builds/ folder, which git ignores.
/// </summary>
public static class BuildTools
{
    private const string ProductName = "Space Shooter";
    private const string BuildRoot = "Builds";
    private static readonly string[] Videos =
        { "Sfxstart2-1.mp4", "anim.webm", "GAMEOVERSCREEN.webm", "Win-1.mp4" };

    [MenuItem("Build/Windows (zip)")]
    public static void BuildWindows()
    {
        PlayerSettings.productName = ProductName;
        string folder = Path.Combine(BuildRoot, "Windows");
        string exe = Path.Combine(folder, ProductName + ".exe");
        if (!Build(BuildTarget.StandaloneWindows64, exe, BuildTargetGroup.Standalone)) return;

        // Unity's debug-symbol folders are not meant to be shipped.
        foreach (var dir in Directory.GetDirectories(folder).Where(d =>
                     d.Contains("DoNotShip") || d.Contains("DontShip")))
            Directory.Delete(dir, true);

        Zip(folder, Path.Combine(BuildRoot, "SpaceShooter_Windows.zip"));
    }

    [MenuItem("Build/WebGL for itch.io (zip)")]
    public static void BuildWebGL()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Fail("WebGL Build Support is not installed. Add it in Unity Hub: Installs > this version > Add modules.");
            return;
        }

        PlayerSettings.productName = ProductName;
        // itch.io: gzip with decompression fallback works without special server headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        string folder = Path.Combine(BuildRoot, "WebGL");
        CopyVideosToStreamingAssets();
        try
        {
            if (!Build(BuildTarget.WebGL, folder, BuildTargetGroup.WebGL)) return;
            Zip(folder, Path.Combine(BuildRoot, "SpaceShooter_WebGL_itch.zip")); // index.html at the zip root
        }
        finally
        {
            RemoveStreamingAssetsVideos();
        }
    }

    // ------------------------------------------------------------------ helpers

    private static bool Build(BuildTarget target, string location, BuildTargetGroup group)
    {
        if (Directory.Exists(location)) Directory.Delete(location, true);
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = location,
            target = target,
            targetGroup = group,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;
        Debug.Log($"[BuildTools] {target}: {s.result}, {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB, {s.totalTime:mm\\:ss}");
        if (s.result != BuildResult.Succeeded)
        {
            Fail($"{target} build failed ({s.totalErrors} errors). See the Console.");
            return false;
        }
        return true;
    }

    private static void Zip(string folder, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(folder, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
        Debug.Log($"[BuildTools] Created {Path.GetFullPath(zipPath)} ({new FileInfo(zipPath).Length / (1024 * 1024)} MB)");
        EditorUtility.RevealInFinder(zipPath);
    }

    private static string StreamingVideoFolder => Path.Combine("Assets", "StreamingAssets", "Video");

    private static void CopyVideosToStreamingAssets()
    {
        Directory.CreateDirectory(StreamingVideoFolder);
        foreach (string v in Videos)
        {
            string src = Path.Combine("Assets", "Materials", "Video", v);
            if (File.Exists(src)) File.Copy(src, Path.Combine(StreamingVideoFolder, v), true);
        }
        AssetDatabase.Refresh();
    }

    private static void RemoveStreamingAssetsVideos()
    {
        string streaming = Path.Combine("Assets", "StreamingAssets");
        if (Directory.Exists(streaming)) Directory.Delete(streaming, true);
        if (File.Exists(streaming + ".meta")) File.Delete(streaming + ".meta");
        AssetDatabase.Refresh();
    }

    private static void Fail(string message)
    {
        Debug.LogError("[BuildTools] " + message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
        else EditorUtility.DisplayDialog("Build", message, "OK");
    }
}
