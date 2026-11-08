using System;
using System.IO;
using Eclipse.Editor.Setup;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Eclipse.Editor.Build
{
    /// <summary>
    /// Builds the soak test player (M07 T9): Windows x64, Development Build (Profiler markers), the SoakTest scene only, into
    /// Builds/Soak/. The player starts the run by itself, writes Logs/soak.csv next to the executable and quits. Run:
    /// -executeMethod Eclipse.Editor.Build.SoakBuild.Build. Run the exe on the target hardware (11 section 3.14), not in CI.
    /// </summary>
    public static class SoakBuild
    {
        public const string OutputFolder = "Builds/Soak";
        public const string ExecutableName = "EclipseSoak.exe";

        [MenuItem("Eclipse/Build/Soak Test Player")]
        public static void Build()
        {
            SoakSceneBuilder.Run();
            Directory.CreateDirectory(OutputFolder);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SoakSceneBuilder.ScenePath },
                locationPathName = Path.Combine(OutputFolder, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log("[SoakBuild] " + s.result + " | " + s.totalSize / (1024 * 1024) + " MB | " + s.totalTime + " | " + s.outputPath);
            if (s.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw new Exception("Soak build failed: " + s.result);
            }
        }
    }
}
