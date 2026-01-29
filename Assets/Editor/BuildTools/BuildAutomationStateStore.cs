using System;
using System.IO;
using UnityEngine;

namespace Editor.BuildTools
{
    public static class BuildAutomationStateStore
    {
        private const string StateFileRelativePath = "UserSettings/BuildAutomationState.json";

        public static bool TryReadLastBuild(out string buildProfile, out string buildArtifact, out DateTime lastBuildUtc)
        {
            buildProfile = string.Empty;
            buildArtifact = string.Empty;
            lastBuildUtc = DateTime.MinValue;

            string filePath = GetStateFilePath();
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                BuildAutomationStateData data = JsonUtility.FromJson<BuildAutomationStateData>(json);
                if (data == null || string.IsNullOrWhiteSpace(data.lastBuildProfile) || string.IsNullOrWhiteSpace(data.lastBuildArtifact))
                {
                    return false;
                }

                buildProfile = data.lastBuildProfile.Trim();
                buildArtifact = data.lastBuildArtifact.Trim();

                if (!string.IsNullOrWhiteSpace(data.lastBuildUtc) && DateTime.TryParse(data.lastBuildUtc, out DateTime parsedUtc))
                {
                    lastBuildUtc = DateTime.SpecifyKind(parsedUtc, DateTimeKind.Utc);
                }
                else
                {
                    lastBuildUtc = DateTime.MinValue;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void SaveLastBuild(string buildProfile, string buildArtifact)
        {
            if (string.IsNullOrWhiteSpace(buildProfile))
            {
                throw new ArgumentException("Profile is empty.", nameof(buildProfile));
            }

            if (string.IsNullOrWhiteSpace(buildArtifact))
            {
                throw new ArgumentException("Artifact is empty.", nameof(buildArtifact));
            }

            BuildAutomationStateData data = new BuildAutomationStateData
            {
                lastBuildProfile = buildProfile.Trim(),
                lastBuildArtifact = buildArtifact.Trim(),
                lastBuildUtc = DateTime.UtcNow.ToString("O")
            };

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string filePath = GetStateFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            File.WriteAllText(filePath, json);
        }

        private static string GetStateFilePath()
        {
            string projectRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRootPath, StateFileRelativePath);
        }

        [Serializable]
        private sealed class BuildAutomationStateData
        {
            public string lastBuildProfile;
            public string lastBuildArtifact;
            public string lastBuildUtc;
        }
    }
}







