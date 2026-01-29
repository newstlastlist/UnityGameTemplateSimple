using System.Collections;
using UnityEngine;

namespace WS.Core.SDK.AppMetrica
{
    public class AppMetricaAnalytics : MonoBehaviour
    {
        private const string TotalMinutesKey = "analytics_total_minutes";
        private static AppMetricaAnalytics _instance;
        private Coroutine _minuteTickerCoroutine;

        public static AppMetricaAnalytics Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_minuteTickerCoroutine == null)
            {
                _minuteTickerCoroutine = StartCoroutine(MinuteTickerRoutine());
            }
        }

        // public methods (API)
        public void ReportLevelStarted(int levelId, int levelIndexZeroBased)
        {
            int cumulativeMinutes = GetTotalPlayMinutes();
            string json = BuildLevelEventJson(levelId, levelIndexZeroBased, cumulativeMinutes);
            Io.AppMetrica.AppMetrica.ReportEvent("level_started", json);
        }

        public void ReportLevelCompleted(int levelId, int levelIndexZeroBased)
        {
            int cumulativeMinutes = GetTotalPlayMinutes();
            string json = BuildLevelEventJson(levelId, levelIndexZeroBased, cumulativeMinutes);
            Io.AppMetrica.AppMetrica.ReportEvent("level_completed", json);
        }

        public int GetTotalPlayMinutes()
        {
            return PlayerPrefs.GetInt(TotalMinutesKey, 0);
        }

        // private methods (implementation)
        private IEnumerator MinuteTickerRoutine()
        {
            var wait = new WaitForSecondsRealtime(60f);
            while (true)
            {
                yield return wait;
                int total = PlayerPrefs.GetInt(TotalMinutesKey, 0) + 1;
                PlayerPrefs.SetInt(TotalMinutesKey, total);
                PlayerPrefs.Save();

                string json = BuildPlayTimeMinutesJson(total);
                Io.AppMetrica.AppMetrica.ReportEvent("play_time_minutes", json);
            }
        }

        // private methods (implementation)
        private string BuildLevelEventJson(int levelId, int levelIndexZeroBased, int playTimeMinutes)
        {
            return "{\"level_id\":" + levelId
                + ",\"level_index\":" + levelIndexZeroBased
                + ",\"play_time_minutes\":" + playTimeMinutes + "}";
        }

        private string BuildPlayTimeMinutesJson(int minutes)
        {
            return "{\"minutes\":" + minutes + "}";
        }
    }
}


