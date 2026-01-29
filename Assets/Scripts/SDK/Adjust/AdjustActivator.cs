using System.Collections;
using AdjustSdk;
using Infrastructure.Settings;
using Shared;
using UnityEngine;

namespace WS.Core.SDK.Adjust
{
    public class AdjustActivator : MonoBehaviour
    {
        private const int MaxFramesToWaitForProjectSettingsService = 30;

        [SerializeField] private bool _sendInBackground = true;

        private static bool _initialized;
        private static bool _sdkInitialized;

        private void Awake()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_sdkInitialized)
            {
                return;
            }

            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            for (int i = 0; i < MaxFramesToWaitForProjectSettingsService; i++)
            {
                if (TryInitializeFromProjectSettingsServiceInternal())
                {
                    yield break;
                }

                if (i == 0)
                {
                    DebugLogger.LogWarning("[Adjust] IProjectSettingsService ещё не зарегистрирован. Ждём...");
                }

                yield return null;
            }

            DebugLogger.LogWarning("[Adjust] Не дождались IProjectSettingsService. Инициализация Adjust пропущена.");
            _sdkInitialized = true;
        }

        private bool TryInitializeFromProjectSettingsServiceInternal()
        {
            if (!Services.TryGet<IProjectSettingsService>(out var projectSettingsService) || projectSettingsService == null)
            {
                return false;
            }

            string appToken = projectSettingsService.AdjustAppToken;
            if (string.IsNullOrEmpty(appToken))
            {
                DebugLogger.LogError("[Adjust] AdjustAppToken в ProjectConfigs пустой. Инициализация Adjust пропущена.");
                _sdkInitialized = true;
                return true;
            }

            DebugLogger.Log("[Adjust] Adjust App Token найден в ProjectConfigs.");
            InitializeSdkInternal(appToken);
            return true;
        }

        private void InitializeSdkInternal(string appToken)
        {
            if (_sdkInitialized)
            {
                return;
            }

            _sdkInitialized = true;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var env = AdjustEnvironment.Sandbox;
            var logLevel = AdjustLogLevel.Verbose;
#else
            var env = AdjustEnvironment.Production;
            var logLevel = AdjustLogLevel.Info;
#endif

            var config = new AdjustConfig(appToken, env)
            {
                LogLevel = logLevel,
                IsSendingInBackgroundEnabled = _sendInBackground
            };

            AdjustSdk.Adjust.InitSdk(config);
        }
    }
}