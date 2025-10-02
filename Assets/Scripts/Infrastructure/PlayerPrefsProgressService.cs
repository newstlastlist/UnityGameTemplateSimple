using System;
using UnityEngine;

namespace Infrastructure
{
    public sealed class PlayerPrefsProgressService : IProgressService
    {
        private const string Key = "PG_LastCompletedLevelIndex";

        public int LastCompletedLevelIndex { get; set; } = -1;

        public int LoopLevelIndex { get; set; }

        public void Save()
        {
            PlayerPrefs.SetInt(Key, LastCompletedLevelIndex);
        }

        public void Load()
        {
            LastCompletedLevelIndex = PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetInt(Key) : -1;
        }

        public void Reset()
        {
            LastCompletedLevelIndex = -1;
            PlayerPrefs.DeleteKey(Key);
        }
        
        public int ResolveCurrentLevelIndex(int levelsCount)
        {
            if (levelsCount <= 0)
            {
                return 0;
            }

            if (LastCompletedLevelIndex < levelsCount - 1)
            {
                int next = LastCompletedLevelIndex + 1;
                return Math.Clamp(next, 0, levelsCount - 1);
            }

            int loop = Math.Clamp(LoopLevelIndex, 0, levelsCount - 1);
            return loop;
        }

        public void OnLevelCompleted(int levelsCount, int completedIndex)
        {
            if (levelsCount <= 0)
            {
                return;
            }

            if (LastCompletedLevelIndex < levelsCount - 1)
            {
                LastCompletedLevelIndex = Math.Max(LastCompletedLevelIndex, completedIndex);
                return;
            }

            int next = completedIndex + 1;
            if (next >= levelsCount)
            {
                LoopLevelIndex = 0;
            }
            else
            {
                LoopLevelIndex = Math.Max(LoopLevelIndex, next);
            }
        }
    }
}