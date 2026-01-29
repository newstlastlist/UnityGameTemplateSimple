using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor.BuildTools
{
    public static class BuildGuiEntryPoint
    {
        private static bool _isBuildScheduled;
        private static bool _isBuildRunning;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.update -= TryRunPendingBuildOnEditorUpdate;
            EditorApplication.update += TryRunPendingBuildOnEditorUpdate;
        }

        public static void PerformGuiBuild()
        {
            if (_isBuildScheduled)
            {
                return;
            }

            _isBuildScheduled = true;
            EditorApplication.delayCall += PerformGuiBuildInternal;
        }

        private static void PerformGuiBuildInternal()
        {
            try
            {
                string[] commandLineArguments = Environment.GetCommandLineArgs();

                // При старте из PowerShell (после свитча Library) аргументы придут через CLI.
                // При старте из EditorWindow pending build уже сохранён в UserSettings.
                if (TryGetArgumentValue(commandLineArguments, "-artifact", out string artifactValue) &&
                    TryGetArgumentValue(commandLineArguments, "-profile", out string profileValue) &&
                    TryGetArgumentValue(commandLineArguments, "-outputPath", out string outputPath))
                {
                    string outputPathFull = Path.GetFullPath(outputPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPathFull) ?? ".");
                    BuildAutomationPendingBuildStore.SavePendingBuild(profileValue, artifactValue, outputPathFull);
                }

                TryRunPendingBuild();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BuildAutomation] GUI build failed with exception:\n{exception}");
                EditorUtility.DisplayDialog("Build Automation", $"Build failed:\n{exception.Message}\n\nSee Console for details.", "OK");
            }
        }

        public static void RequestBuildFromEditor(string buildProfile, string buildArtifact, string outputPathFull)
        {
            string outputFull = Path.GetFullPath(outputPathFull);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFull) ?? ".");
            BuildAutomationPendingBuildStore.SavePendingBuild(buildProfile, buildArtifact, outputFull);
            PerformGuiBuild();
        }

        private static void TryRunPendingBuildOnEditorUpdate()
        {
            TryRunPendingBuild();
        }

        private static void TryRunPendingBuild()
        {
            if (_isBuildRunning)
            {
                return;
            }

            if (!BuildAutomationPendingBuildStore.TryLoadPendingBuild(out string buildProfileValue, out string buildArtifactValue, out string outputPathFull))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[BuildAutomation] Pending build exists, but Play Mode is active. Stop Play Mode to continue.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[BuildAutomation] Switching active build target to Android (may take time on first switch)...");
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
                return;
            }

            _isBuildRunning = true;
            try
            {
                BuildArtifact buildArtifact = ParseEnum<BuildArtifact>(buildArtifactValue);
                BuildProfile buildProfile = ParseEnum<BuildProfile>(buildProfileValue);

                Directory.CreateDirectory(Path.GetDirectoryName(outputPathFull) ?? ".");

                ApplyBuildSettings(buildProfile, buildArtifact);

                string[] enabledScenePaths = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();

                if (enabledScenePaths.Length <= 0)
                {
                    throw new InvalidOperationException("No enabled scenes in Build Settings.");
                }

                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = enabledScenePaths,
                    locationPathName = outputPathFull,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                if (buildProfile == BuildProfile.Dev)
                {
                    buildPlayerOptions.options |= BuildOptions.Development;
                    buildPlayerOptions.options |= BuildOptions.AllowDebugging;
                }

                Debug.Log($"[BuildAutomation] Build started. Profile={buildProfile}, Artifact={buildArtifact}, Output={outputPathFull}");
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
                stopwatch.Stop();
                Debug.Log($"[BuildAutomation] Build result: {buildReport.summary.result}. Output={buildReport.summary.outputPath}");

                if (buildReport.summary.result != BuildResult.Succeeded)
                {
                    EditorUtility.DisplayDialog("Build Automation", $"Build failed: {buildReport.summary.result}\n\nSee Console for details.", "OK");
                    return;
                }

                BuildAutomationStateStore.SaveLastBuild(buildProfile.ToString(), buildArtifact.ToString());
                Debug.Log($"[BuildAutomation] Build succeeded. Time={buildReport.summary.totalTime} (stopwatch={stopwatch.Elapsed}). Output={outputPathFull}");
                EditorUtility.RevealInFinder(outputPathFull);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BuildAutomation] Build failed with exception:\n{exception}");
                EditorUtility.DisplayDialog("Build Automation", $"Build failed:\n{exception.Message}\n\nSee Console for details.", "OK");
            }
            finally
            {
                BuildAutomationPendingBuildStore.ClearPendingBuild();
                _isBuildRunning = false;
            }
        }

        private static void ApplyBuildSettings(BuildProfile buildProfile, BuildArtifact buildArtifact)
        {
            bool isDevelopmentBuild = buildProfile == BuildProfile.Dev;
            bool shouldBuildAppBundle = buildArtifact == BuildArtifact.Aab;

            EditorUserBuildSettings.development = isDevelopmentBuild;
            EditorUserBuildSettings.allowDebugging = isDevelopmentBuild;
            EditorUserBuildSettings.connectProfiler = isDevelopmentBuild;
            EditorUserBuildSettings.buildAppBundle = shouldBuildAppBundle;

            if (buildProfile == BuildProfile.Release)
            {
                if (!BuildSecretsStore.TryLoadReleaseSigningPasswords(out string keystorePassword, out string keyAliasPassword))
                {
                    throw new InvalidOperationException("Missing local encrypted secrets for Release signing. Open Tools → Build → Build Automation and save secrets.");
                }

                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystorePass = keystorePassword;
                PlayerSettings.Android.keyaliasPass = keyAliasPassword;
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.keystorePass = string.Empty;
                PlayerSettings.Android.keyaliasPass = string.Empty;
            }

            AssetDatabase.SaveAssets();
        }

        private static bool TryGetArgumentValue(string[] commandLineArguments, string argumentName, out string value)
        {
            value = string.Empty;

            for (int index = 0; index < commandLineArguments.Length; index++)
            {
                string argument = commandLineArguments[index];
                if (!argument.StartsWith(argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int equalsIndex = argument.IndexOf('=');
                if (equalsIndex >= 0 && equalsIndex + 1 < argument.Length)
                {
                    value = argument.Substring(equalsIndex + 1);
                    return true;
                }

                if (index + 1 < commandLineArguments.Length)
                {
                    value = commandLineArguments[index + 1];
                    return true;
                }
            }

            return false;
        }

        private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct
        {
            if (!Enum.TryParse(value, ignoreCase: true, out TEnum result))
            {
                throw new InvalidOperationException($"Invalid {typeof(TEnum).Name}: '{value}'");
            }

            return result;
        }

        private enum BuildProfile
        {
            Dev = 0,
            Release = 1
        }

        private enum BuildArtifact
        {
            Apk = 0,
            Aab = 1
        }
    }
}


