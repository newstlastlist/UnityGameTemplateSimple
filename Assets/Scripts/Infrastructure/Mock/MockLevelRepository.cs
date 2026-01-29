using System;
using Domain;

namespace Infrastructure
{
    public sealed class MockLevelRepository : ILevelRepository
    {
        public int Count => 0;

        public MockLevelRepository()
        {
        }

        public LevelData[] LoadAll()
        {
            return Array.Empty<LevelData>();
        }

        public LevelData LoadById(int id)
        {
            return null;
        }
    }
}


