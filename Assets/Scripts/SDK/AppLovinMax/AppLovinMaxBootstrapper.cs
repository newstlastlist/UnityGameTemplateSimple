using UnityEngine;
using WS.Core.SDK.AppLovin;

namespace _Project.Scripts.Runtime.SDK.AppLovinMax
{
    public sealed class AppLovinMaxBootstrapper : MonoBehaviour
    {
        private bool _isInitialized;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitializedHandler;
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitializedHandler;

            MaxSdk.InitializeSdk();
        }

        private void OnDestroy()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitializedHandler;
        }

        private void OnSdkInitializedHandler(MaxSdkBase.SdkConfiguration configuration)
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitializedHandler;

            // Создаём сервисы только после успешной инициализации SDK.
            if (FindObjectOfType<AppLovinMaxAdService>(true) != null)
            {
                return;
            }

            var serviceObject = new GameObject(nameof(AppLovinMaxAdService));
            serviceObject.AddComponent<AppLovinMaxAdService>();
            serviceObject.AddComponent<RewardedAdLimitService>();
            DontDestroyOnLoad(serviceObject);
        }
    }
}



