using System.Collections;
using Infrastructure.Settings;
using Shared;
using UnityEngine;
using Io.AppMetrica;

namespace WS.Core.SDK.AppMetrica
{
    public class AppMetricaActivator : MonoBehaviour
    {
        private const int MaxFramesToWaitForProjectSettingsService = 30;

        private const string FirstLaunchKey = "is_first_launch";
        private static bool _activated;
        private static bool _sdkActivated;

        private void Awake()
        {
            if (_activated)
            {
                return;
            }

            _activated = true;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_sdkActivated)
            {
                return;
            }

            StartCoroutine(ActivateRoutine());
        }

        private IEnumerator ActivateRoutine()
        {
            for (int i = 0; i < MaxFramesToWaitForProjectSettingsService; i++)
            {
                if (TryActivateFromProjectSettingsServiceInternal())
                {
                    yield break;
                }

                if (i == 0)
                {
                    DebugLogger.LogWarning("[AppMetrica] IProjectSettingsService ещё не зарегистрирован. Ждём...");
                }

                yield return null;
            }

            DebugLogger.LogWarning("[AppMetrica] Не дождались IProjectSettingsService. Активация AppMetrica пропущена.");
            _sdkActivated = true;
        }

        private bool TryActivateFromProjectSettingsServiceInternal()
        {
            if (!Services.TryGet<IProjectSettingsService>(out var projectSettingsService) || projectSettingsService == null)
            {
                return false;
            }

            string appId = projectSettingsService.AppMetricaAppId;
            if (string.IsNullOrEmpty(appId))
            {
                DebugLogger.LogError("[AppMetrica] AppMetricaAppId в ProjectConfigs пустой. Активация AppMetrica пропущена.");
                _sdkActivated = true;
                return true;
            }

            DebugLogger.Log("[AppMetrica] AppMetrica App Id найден в ProjectConfigs.");
            ActivateSdkInternal(appId);
            return true;
        }

        private void ActivateSdkInternal(string appId)
        {
            if (_sdkActivated)
            {
                return;
            }

            _sdkActivated = true;

            bool isFirstLaunch = PlayerPrefs.GetInt(FirstLaunchKey, 1) == 1;

            var config = new AppMetricaConfig(appId)
            {
                Logs = false,
                SessionsAutoTrackingEnabled = true,
                LocationTracking = false,
                FirstActivationAsUpdate = !isFirstLaunch
            };

            Io.AppMetrica.AppMetrica.Activate(config);

            PlayerPrefs.SetInt(FirstLaunchKey, 0);
            PlayerPrefs.Save();
            EnsureAnalyticsExists();
        }

        // private methods (implementation)
        private void EnsureAnalyticsExists()
        {
            if (AppMetricaAnalytics.Instance != null)
            {
                return;
            }

            var go = new GameObject("AppMetricaAnalytics");
            go.AddComponent<AppMetricaAnalytics>();
        }
    }
}