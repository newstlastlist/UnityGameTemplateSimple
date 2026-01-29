#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using App;
using Infrastructure;
using LunarConsolePlugin;
using Shared;
using UnityEngine;
using WS.Core.SDK.AppMetrica;
using WS.Core.SDK.AppLovin;

namespace Dev
{
    public static class DevCheatsRegistrar
    {
        private static bool _isRegistered;

        public static void Register()
        {
            if (_isRegistered)
            {
                return;
            }

            _isRegistered = true;

            LunarConsole.RegisterAction("Cheats/Level/Next", SkipNextLevelHandler);
            LunarConsole.RegisterAction("Cheats/Level/Prev", SkipPreviousLevelHandler);
            LunarConsole.RegisterAction("Cheats/Level/+10", SkipNextTenLevelsHandler);
            LunarConsole.RegisterAction("Cheats/Level/-10", SkipPreviousTenLevelsHandler);
            LunarConsole.RegisterAction("Cheats/Level/Restart", RestartLevelHandler);
            LunarConsole.RegisterAction("Cheats/Win/Force", ForceWinHandler);

            LunarConsole.RegisterAction("Cheats/Ads/Toggle Remove Ads", ToggleRemoveAdsHandler);
            LunarConsole.RegisterAction("Cheats/Ads/Remove Ads", RemoveAdsHandler);
            LunarConsole.RegisterAction("Cheats/Ads/Return Ads", ReturnAdsHandler);
        }

        private static void ToggleRemoveAdsHandler()
        {
            bool next = !PlayerPrefsProgressService.IsAdsDisabled();
            ApplyAdsDisabledInternal(next);
        }

        private static void RemoveAdsHandler()
        {
            ApplyAdsDisabledInternal(true);
        }

        private static void ReturnAdsHandler()
        {
            ApplyAdsDisabledInternal(false);
        }

        private static void ApplyAdsDisabledInternal(bool isDisabled)
        {
            PlayerPrefsProgressService.SetAdsDisabled(isDisabled);

            // Мгновенный эффект: скрыть баннер.
            if (Services.TryGet<AppLovinMaxAdService>(out var adService) && adService != null)
            {
                if (isDisabled)
                {
                    adService.HideBanner();
                }
            }
        }

        private static void SkipNextLevelHandler()
        {
            TryShiftCurrentLevelIndex(1);
        }

        private static void SkipPreviousLevelHandler()
        {
            TryShiftCurrentLevelIndex(-1);
        }

        private static void SkipNextTenLevelsHandler()
        {
            TryShiftCurrentLevelIndex(10);
        }

        private static void SkipPreviousTenLevelsHandler()
        {
            TryShiftCurrentLevelIndex(-10);
        }

        private static void RestartLevelHandler()
        {
            if (!Services.TryGet<IScreenNavigator>(out var screenNavigator))
            {
                return;
            }

            screenNavigator.Show(PanelType.Game);
        }

        private static void ForceWinHandler()
        {
            if (!Services.TryGet<IProgressService>(out var progressService))
            {
                return;
            }

            if (!Services.TryGet<ILevelRepository>(out var levelRepository))
            {
                return;
            }

            if (!Services.TryGet<IScreenNavigator>(out var screenNavigator))
            {
                return;
            }

            int levelsCount = levelRepository.Count;
            int currentLevelIndex = progressService.ResolveCurrentLevelIndex(levelsCount);

            AppMetricaAnalytics.Instance?.ReportLevelCompleted(currentLevelIndex + 1, currentLevelIndex);

            progressService.IncrementWinStreak();
            progressService.OnLevelCompleted(levelsCount, currentLevelIndex);

            SaveProgress(progressService);

            screenNavigator.Show(PanelType.Main);
        }

        private static void TryShiftCurrentLevelIndex(int levelIndexDelta)
        {
            if (!Services.TryGet<IProgressService>(out var progressService))
            {
                return;
            }

            if (!Services.TryGet<ILevelRepository>(out var levelRepository))
            {
                return;
            }

            if (!Services.TryGet<IScreenNavigator>(out var screenNavigator))
            {
                return;
            }

            int levelsCount = levelRepository.Count;
            int currentLevelIndex = progressService.ResolveCurrentLevelIndex(levelsCount);
            int targetLevelIndex = currentLevelIndex + levelIndexDelta;
            bool changed = TrySetCurrentLevelIndex(progressService, levelsCount, targetLevelIndex);
            if (!changed)
            {
                return;
            }

            SaveProgress(progressService);
            screenNavigator.Show(PanelType.Game);
        }

        private static bool TrySetCurrentLevelIndex(IProgressService progressService, int levelsCount, int targetLevelIndex)
        {
            if (progressService == null)
            {
                return false;
            }

            if (levelsCount <= 0)
            {
                return false;
            }

            int clampedTarget = Mathf.Clamp(targetLevelIndex, 0, levelsCount - 1);
            int lastCompleted = Mathf.Clamp(clampedTarget - 1, -1, levelsCount - 1);

            bool changed = progressService.LastCompletedLevelIndex != lastCompleted;
            progressService.LastCompletedLevelIndex = lastCompleted;

            // На случай, если игрок уже ушёл в "loop"-режим, держим LoopLevelIndex консистентным.
            progressService.LoopLevelIndex = Mathf.Clamp(clampedTarget, 0, levelsCount - 1);

            return changed;
        }

        private static void SaveProgress(IProgressService progressService)
        {
            if (progressService == null)
            {
                return;
            }

            progressService.Save();
            PlayerPrefs.Save();
        }
    }
}
#endif




