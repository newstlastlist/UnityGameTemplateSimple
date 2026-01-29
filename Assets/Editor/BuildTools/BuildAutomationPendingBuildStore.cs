using System;
using System.IO;
using UnityEngine;

namespace Editor.BuildTools
{
    public static class BuildAutomationPendingBuildStore
    {
        private const string FileRelativePath = "UserSettings/BuildAutomationPendingBuild.json";

        public static void SavePendingBuild(string buildProfile, string buildArtifact, string outputPathFull)
        {
            if (string.IsNullOrWhiteSpace(buildProfile))
            {
                throw new ArgumentException("buildProfile is empty.", nameof(buildProfile));
            }

            if (string.IsNullOrWhiteSpace(buildArtifact))
            {
                throw new ArgumentException("buildArtifact is empty.", nameof(buildArtifact));
            }

            if (string.IsNullOrWhiteSpace(outputPathFull))
            {
                throw new ArgumentException("outputPathFull is empty.", nameof(outputPathFull));
            }

            PendingBuildData pendingBuildData = new PendingBuildData
            {
                BuildProfile = buildProfile,
                BuildArtifact = buildArtifact,
                OutputPathFull = outputPathFull,
                CreatedUtc = DateTime.UtcNow.ToString("O")
            };

            string filePath = GetFileFullPath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            File.WriteAllText(filePath, JsonUtility.ToJson(pendingBuildData, prettyPrint: true));
        }

        public static bool TryLoadPendingBuild(out string buildProfile, out string buildArtifact, out string outputPathFull)
        {
            buildProfile = string.Empty;
            buildArtifact = string.Empty;
            outputPathFull = string.Empty;

            try
            {
                string filePath = GetFileFullPath();
                if (!File.Exists(filePath))
                {
                    return false;
                }

                string json = File.ReadAllText(filePath);
                PendingBuildData pendingBuildData = JsonUtility.FromJson<PendingBuildData>(json);
                if (pendingBuildData == null)
                {
                    return false;
                }

                buildProfile = pendingBuildData.BuildProfile ?? string.Empty;
                buildArtifact = pendingBuildData.BuildArtifact ?? string.Empty;
                outputPathFull = pendingBuildData.OutputPathFull ?? string.Empty;
                return
                    !string.IsNullOrWhiteSpace(buildProfile) &&
                    !string.IsNullOrWhiteSpace(buildArtifact) &&
                    !string.IsNullOrWhiteSpace(outputPathFull);
            }
            catch
            {
                return false;
            }
        }

        public static void ClearPendingBuild()
        {
            try
            {
                string filePath = GetFileFullPath();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Best-effort.
            }
        }

        private static string GetFileFullPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileRelativePath));
        }

        [Serializable]
        private sealed class PendingBuildData
        {
            public string BuildProfile;
            public string BuildArtifact;
            public string OutputPathFull;
            public string CreatedUtc;
        }
    }
}






