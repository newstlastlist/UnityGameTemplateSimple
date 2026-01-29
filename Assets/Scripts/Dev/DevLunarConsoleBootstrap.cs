#if UNITY_EDITOR || DEVELOPMENT_BUILD
using LunarConsolePlugin;
using System.Reflection;
using UnityEngine;
using WS.Core.SDK.AppLovin;

namespace Dev
{
    public static class DevLunarConsoleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoadHandler()
        {
            EnsureLunarConsoleInstance();
            RegisterBannerVisibilityHandlers();
            DevCheatsRegistrar.Register();
        }

        private static void EnsureLunarConsoleInstance()
        {
            if (LunarConsole.instance != null)
            {
                return;
            }

            var consoleGameObject = new GameObject("LunarConsole");
            var console = consoleGameObject.AddComponent<LunarConsole>();

            // Отключаем дефолтный жест плагина (SwipeDown) и заменяем своим.
            DisableDefaultGesture(console);
            consoleGameObject.AddComponent<DevLunarConsoleTwoFingerTripleTapOpener>();
        }

        private static void RegisterBannerVisibilityHandlers()
        {
            var previousOnConsoleOpened = LunarConsole.onConsoleOpened;
            var previousOnConsoleClosed = LunarConsole.onConsoleClosed;

            LunarConsole.onConsoleOpened = () =>
            {
                previousOnConsoleOpened?.Invoke();
                SetBannerVisibleSafe(false);
            };

            LunarConsole.onConsoleClosed = () =>
            {
                previousOnConsoleClosed?.Invoke();
                SetBannerVisibleSafe(true);
            };
        }

        private static void SetBannerVisibleSafe(bool isVisible)
        {
            var adService = Object.FindObjectOfType<AppLovinMaxAdService>(true);
            if (adService == null)
            {
                return;
            }

            if (isVisible)
            {
                adService.ShowBanner();
                return;
            }

            adService.HideBanner();
        }

        private static void DisableDefaultGesture(LunarConsole console)
        {
            if (console == null)
            {
                return;
            }

            var settingsField = typeof(LunarConsole).GetField("m_settings", BindingFlags.Instance | BindingFlags.NonPublic);
            if (settingsField == null)
            {
                return;
            }

            var settings = settingsField.GetValue(console);
            if (settings == null)
            {
                return;
            }

            var gestureField = settings.GetType().GetField("gesture", BindingFlags.Instance | BindingFlags.Public);
            if (gestureField == null)
            {
                return;
            }

            gestureField.SetValue(settings, Gesture.None);
        }
    }
}
#endif


