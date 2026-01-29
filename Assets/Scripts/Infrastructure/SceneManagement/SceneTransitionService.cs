using Cysharp.Threading.Tasks;
using Infrastructure.Settings;
using UI.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infrastructure.SceneManagement
{
    /// <summary>
    /// Сервис смены сцен с показом нашего стандартного загрузочного экрана.
    /// Использует ILoadingService и минимальное время из ProjectSettings.
    /// </summary>
    public sealed class SceneTransitionService : ISceneTransitionService
    {
        private readonly SceneLoader _sceneLoader;
        private readonly ILoadingService _loadingService;
        private readonly IProjectSettingsService _projectSettingsService;

        public SceneTransitionService(
            SceneLoader sceneLoader,
            ILoadingService loadingService,
            IProjectSettingsService projectSettingsService)
        {
            _sceneLoader = sceneLoader;
            _loadingService = loadingService;
            _projectSettingsService = projectSettingsService;
        }

        public UniTask LoadMainWithSplashAsync()
        {
            return LoadSceneWithSplashAsync(_sceneLoader.MainSceneIndex);
        }

        public UniTask LoadMenuWithSplashAsync()
        {
            return LoadSceneWithSplashAsync(_sceneLoader.MenuSceneIndex);
        }

        private async UniTask LoadSceneWithSplashAsync(int sceneIndex)
        {
            float minSeconds = _projectSettingsService != null
                ? _projectSettingsService.StartupLoadingMinSeconds
                : 2f;

            DebugLogger.Log($"[SceneTransition] Start load sceneIndex={sceneIndex}, minSeconds={minSeconds}");

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
            if (operation == null)
            {
                DebugLogger.LogError($"[SceneTransition] SceneManager.LoadSceneAsync returned null for sceneIndex={sceneIndex}");
                return;
            }

            // Критично: не активируем новую сцену раньше, чем отработает сплэш (minSeconds),
            // иначе игрок увидит "прыжок" в GameView, пока загрузочный экран ещё висит.
            operation.allowSceneActivation = false;

            var splashTask = _loadingService.RunUntil(operation, minSeconds);

            float startTime = Time.realtimeSinceStartup;
            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                bool minTimePassed = elapsed >= minSeconds;
                bool sceneReadyToActivate = operation.progress >= 0.9f; // Unity: 0.9 означает "загружено, ждём активации"

                if (minTimePassed && sceneReadyToActivate)
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            operation.allowSceneActivation = true;
            await splashTask;

            DebugLogger.Log($"[SceneTransition] Completed load sceneIndex={sceneIndex}");
        }
    }
}