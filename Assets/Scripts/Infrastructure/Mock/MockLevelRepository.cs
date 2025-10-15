using System;
using Domain;

namespace Infrastructure
{
    public sealed class MockLevelRepository : ILevelRepository
    {
        private readonly LevelData[] _levels;

        public MockLevelRepository(int levelsCount = 3)
        {
            int count = Math.Max(1, levelsCount);
            _levels = new LevelData[count];
            for (int i = 0; i < count; i++)
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

        public int Count => _levels.Length;
    }
}


