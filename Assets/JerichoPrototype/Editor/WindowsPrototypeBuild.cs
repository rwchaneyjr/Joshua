using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class WindowsPrototypeBuild
    {
        public const string Scene = "Assets/Scenes/JerichoPrototype.unity";
        [MenuItem("Tools/Jericho Prototype/Build Windows Desktop")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before building.");
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene))
                throw new FileNotFoundException("Existing prototype scene is required. No assets will be generated.", Scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = "Builds/Windows/Joshua.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            var summary = report.summary;
            string result = $"{summary.result}: Windows x64, {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalSize} bytes, {summary.totalTime}";
            File.WriteAllText("Builds/Windows/build-result.txt", result);
            foreach (var step in report.steps)
                foreach (var message in step.messages)
                    if (message.type == LogType.Warning || message.type == LogType.Error)
                        File.AppendAllText("Builds/Windows/build-result.txt", "\n" + message.type + ": " + message.content);
            if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException(result);
            Debug.Log("JERICHO WINDOWS BUILD PASS: " + result);
        }
    }
}
