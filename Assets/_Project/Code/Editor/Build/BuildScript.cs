using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Eclipse.Editor.Build
{
    /// <summary>Batchmode entry point for a Windows 64 test build of the CombatSandbox.</summary>
    public static class BuildScript
    {
        const string SandboxScene = "Assets/_Project/Scenes/Sandbox/CombatSandbox.unity";
        const string OutputExe = "Builds/Windows/ProjectEclipse.exe";

        public static void BuildWindows()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputExe));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SandboxScene },
                locationPathName = OutputExe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log($"Build {s.result}: {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalWarnings} warnings");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
