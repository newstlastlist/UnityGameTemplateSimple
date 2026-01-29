using System;
using UnityEngine;

namespace Infrastructure
{
    public sealed class PlayerPrefsProgressService : IProgressService
    {
        private const string Key = "PG_LastCompletedLevelIndex";
        private const string LoopLevelIndexKey = "PG_LoopLevelIndex";
        private const string WinStreakKey = "PG_WinStreak";
        private const string AdsDisabledKey = "DEV_AdsDisabled";

        public int LastCompletedLevelIndex { get; set; } = -1;

        public int LoopLevelIndex { get; set; }

        public void Save()
        {
            PlayerPrefs.SetInt(Key, LastCompletedLevelIndex);
            PlayerPrefs.SetInt(LoopLevelIndexKey, LoopLevelIndex);
        }

        public void Load()
        {
            LastCompletedLevelIndex = PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetInt(Key) : -1;
            LoopLevelIndex = PlayerPrefs.GetInt(LoopLevelIndexKey, 0);
        }

        public void Reset()
        {
            LastCompletedLevelIndex = -1;
            PlayerPrefs.DeleteKey(Key);
            LoopLevelIndex = 0;
            PlayerPrefs.DeleteKey(LoopLevelIndexKey);
        }

        public int GetWinStreak()
        {
            return PlayerPrefs.GetInt(WinStreakKey, 0);
        }

        public void SetWinStreak(int value)
        {
            if (value < 0)
            {
                value = 0;
            }
            PlayerPrefs.SetInt(WinStreakKey, value);
        }

        public void IncrementWinStreak()
        {
            int current = GetWinStreak();
            PlayerPrefs.SetInt(WinStreakKey, current + 1);
        }

        public void ResetWinStreak()
        {
            PlayerPrefs.SetInt(WinStreakKey, 0);
        }

        public void ClearAllPersistedData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
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
        
        public static int ReadInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public static void WriteInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public static string ReadString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public static void WriteString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public static void SaveNow()
        {
            PlayerPrefs.Save();
        }

        public static bool IsAdsDisabled()
        {
            return ReadInt(AdsDisabledKey, 0) == 1;
        }

        public static void SetAdsDisabled(bool isDisabled)
        {
            WriteInt(AdsDisabledKey, isDisabled ? 1 : 0);
            SaveNow();
        }
    }
}