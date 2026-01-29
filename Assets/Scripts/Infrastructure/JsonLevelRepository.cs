using System;
using Domain;
using Infrastructure.Resources;

namespace Infrastructure
{
    public sealed class JsonLevelRepository : ILevelRepository
    {
        private readonly LevelData[] _levels;

        public int Count => _levels.Length;

        public JsonLevelRepository(ResourcesRepository repo)
        {
            if (repo == null || repo.LevelsJsons == null || repo.LevelsJsons.Count == 0)
            {
                DebugLogger.LogWarning("[JsonLevelRepository] LevelsJsons не назначен или пуст в ResourcesRepository");
                _levels = Array.Empty<LevelData>();
                return;
            }

            int nonNullCount = 0;
            for (int i = 0; i < repo.LevelsJsons.Count; i++)
            {
                if (repo.LevelsJsons[i] != null)
                {
                    nonNullCount++;
                }
            }

            _levels = new LevelData[nonNullCount];
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i] = new LevelData();
            }
        }

        public LevelData[] LoadAll()
        {
            return _levels;
        }

        public LevelData LoadById(int id)
        {
            if (id < 0 || id >= _levels.Length)
            {
                return null;
            }

            return _levels[id];
        }
    }
}


