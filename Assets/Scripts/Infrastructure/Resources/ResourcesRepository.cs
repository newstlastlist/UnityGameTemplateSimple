using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure.Resources
{
    [CreateAssetMenu(fileName = "ResourcesRepository", menuName = "Project/Resources Repository")]
    public sealed class ResourcesRepository : ScriptableObject
    {
        [Header("Levels Data")]
        [SerializeField] private List<TextAsset> _levelsJsons = new();

        public IReadOnlyList<TextAsset> LevelsJsons => _levelsJsons;

        public void ClearAndAddLevelJsons(List<TextAsset> jsonAssets)
        {
            _levelsJsons.Clear();
            if (jsonAssets == null)
            {
                return;
            }

            for (int i = 0; i < jsonAssets.Count; i++)
            {
                var textAsset = jsonAssets[i];
                if (textAsset == null)
                {
                    continue;
                }

                if (!_levelsJsons.Contains(textAsset))
                {
                    _levelsJsons.Add(textAsset);
                }
            }
        }
    }
}