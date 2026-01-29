using System;
using System.Collections.Generic;
using Infrastructure;
using Shared;
using UnityEngine;

namespace WS.Core.SDK.AppLovin
{
    public sealed class RewardedAdLimitService : MonoBehaviour
    {
        private const string ResetDateKey = "RewardedAdLimitService.ResetDate";
        private const string KeysPrefsKey = "RewardedAdLimitService.Keys";
        private const string CounterPrefix = "RewardedAdLimitService.Count.";

        private readonly List<string> _keys = new();

        private void Awake()
        {
            Services.Register(this);
            DontDestroyOnLoad(gameObject);
            LoadKeys();
            EnsureReset();
        }

        private void Update()
        {
            EnsureReset();
        }

        public static string BoosterKey(string boosterId)
        {
            return $"RewardedAdLimitService.Booster.{boosterId}";
        }

        public bool CanWatch(string key, int dailyLimit)
        {
            return GetCount(key) < dailyLimit;
        }

        public int GetCount(string key)
        {
            EnsureKey(key);
            EnsureReset();
            return PlayerPrefsProgressService.ReadInt(BuildKey(key), 0);
        }

        public void Increment(string key, int dailyLimit)
        {
            EnsureKey(key);
            EnsureReset();
            int current = PlayerPrefsProgressService.ReadInt(BuildKey(key), 0);
            int next = Mathf.Min(current + 1, dailyLimit);
            PlayerPrefsProgressService.WriteInt(BuildKey(key), next);
            PlayerPrefsProgressService.SaveNow();
        }

        public void SetCount(string key, int value)
        {
            EnsureKey(key);
            EnsureReset();
            int clamped = Mathf.Max(0, value);
            PlayerPrefsProgressService.WriteInt(BuildKey(key), clamped);
            PlayerPrefsProgressService.SaveNow();
        }

        public void ResetCount(string key)
        {
            SetCount(key, 0);
        }

        public void ResetAll()
        {
            EnsureReset();
            foreach (var key in _keys)
            {
                PlayerPrefsProgressService.WriteInt(BuildKey(key), 0);
            }

            PlayerPrefsProgressService.SaveNow();
        }

        public void ResetTimer()
        {
            PlayerPrefsProgressService.DeleteKey(ResetDateKey);
            EnsureReset();
        }

        private void LoadKeys()
        {
            _keys.Clear();
            string raw = PlayerPrefsProgressService.ReadString(KeysPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            foreach (string key in raw.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                _keys.Add(key);
            }
        }

        private void EnsureKey(string key)
        {
            if (_keys.Contains(key))
            {
                return;
            }

            _keys.Add(key);
            PlayerPrefsProgressService.WriteString(KeysPrefsKey, string.Join("|", _keys));
            PlayerPrefsProgressService.SaveNow();
        }

        private string BuildKey(string key)
        {
            return $"{CounterPrefix}{key}";
        }

        private void EnsureReset()
        {
            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            string stored = PlayerPrefsProgressService.ReadString(ResetDateKey, string.Empty);
            if (stored == today)
            {
                return;
            }

            foreach (string key in _keys)
            {
                PlayerPrefsProgressService.WriteInt(BuildKey(key), 0);
            }

            PlayerPrefsProgressService.WriteString(ResetDateKey, today);
            PlayerPrefsProgressService.SaveNow();
        }
    }
}
