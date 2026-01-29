using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Infrastructure.Resources;

namespace Infrastructure.Resources.Editor
{
    [CustomEditor(typeof(ResourcesRepository))]
    public sealed class ResourcesRepositoryEditor : UnityEditor.Editor
    {
        private const string LevelsDataFolderPath = "Assets/Resources/LevelsData";
        private static readonly Regex FirstNumberRegex = new Regex(@"\d+", RegexOptions.Compiled);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Pull Levels Data заменит список Levels Data значениями из папки:\n{LevelsDataFolderPath}\n\n" +
                "Сортировка: сначала по номеру в имени файла (если есть), затем по имени.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!AssetDatabase.IsValidFolder(LevelsDataFolderPath)))
            {
                if (GUILayout.Button("Pull Levels Data"))
                {
                    PullLevelsDataIntoRepository((ResourcesRepository)target);
                }
            }

            if (!AssetDatabase.IsValidFolder(LevelsDataFolderPath))
            {
                EditorGUILayout.HelpBox($"Папка не найдена: {LevelsDataFolderPath}", MessageType.Warning);
            }
        }

        private static void PullLevelsDataIntoRepository(ResourcesRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            var jsonAssets = FindLevelJsonAssetsInFolder(LevelsDataFolderPath);

            Undo.RecordObject(repository, "Pull Levels Data");
            repository.ClearAndAddLevelJsons(jsonAssets);
            EditorUtility.SetDirty(repository);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ResourcesRepository] Pull Levels Data: загружено {jsonAssets.Count} json из '{LevelsDataFolderPath}'.");
        }

        private static List<TextAsset> FindLevelJsonAssetsInFolder(string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { folderPath });
            var assets = new List<(TextAsset asset, int number, string name)>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (ta == null)
                {
                    continue;
                }

                int number = TryExtractFirstNumber(ta.name, out int n) ? n : int.MaxValue;
                assets.Add((ta, number, ta.name));
            }

            return assets
                .OrderBy(a => a.number)
                .ThenBy(a => a.name, StringComparer.Ordinal)
                .Select(a => a.asset)
                .ToList();
        }

        private static bool TryExtractFirstNumber(string text, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var match = FirstNumberRegex.Match(text);
            if (!match.Success)
            {
                return false;
            }

            return int.TryParse(match.Value, out number);
        }
    }
}



