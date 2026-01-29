
using Cysharp.Threading.Tasks;
using Domain.Project;
using Infrastructure;
using Infrastructure.Audio;
using Infrastructure.Resources;
using Infrastructure.SceneManagement;
using Infrastructure.Settings;
using Shared;
using UI.Loading;
using UnityEngine;
using AudioSettings = Domain.Audio.AudioSettings;
using _Project.Scripts.Runtime.SDK.AppLovinMax;
using UnityEngine.Serialization;
using WS.Core.SDK.Adjust;
using WS.Core.SDK.AppMetrica;
using Dev;

namespace App
{
    /// <summary>
    /// Точка входа на bootstrap-сцене.
    /// Регистрирует общие сервисы (ресурсы, настройки, прогресс, звук, загрузочный экран, переходы между сценами)
    /// и затем загружает сцену главного меню с использованием общего загрузочного экрана.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class BootstrapEntryPoint : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private AudioDatabase _audioDatabase;
        [SerializeField] private ResourcesRepository _resourcesRepository;
        [FormerlySerializedAs("_projectSettings")]
        [SerializeField] private ProjectConfigs _projectConfigs;

        [Header("Loading Screen")]
        [SerializeField] private Transform _loadingUiRoot;
        [SerializeField] private PrefabFakeReference _loadingScreenPrefab;

        [Header("Scenes")]
        [SerializeField] private SceneLoaderConfig _sceneLoaderConfig;
        [SerializeField] private bool _loadMenuOnStart = true;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            var serviceRegistry = new ServiceRegistry();
            Services.SetProvider(serviceRegistry);

            var resourceService = new UnityResourceService();
            serviceRegistry.Register<IResourceService>(resourceService);

            var projectSettingsService = new ProjectSettingsService(_projectConfigs);
            serviceRegistry.Register<IProjectSettingsService>(projectSettingsService);

            EnsureAppLovinMaxInitializedInternal();
            EnsureAdjustActivatedInternal(projectSettingsService);
            EnsureAppMetricaActivatedInternal(projectSettingsService);

            if (_loadingUiRoot != null)
            {
                DontDestroyOnLoad(_loadingUiRoot.gameObject);
            }

            string loadingPath = _loadingScreenPrefab != null ? _loadingScreenPrefab.GetResourcesRelativePath() : null;
            var loadingService = new LoadingService(resourceService, loadingPath, _loadingUiRoot);
            serviceRegistry.Register<ILoadingService>(loadingService);

            if (_sceneLoaderConfig != null)
            {
                var sceneLoader = new SceneLoader(_sceneLoaderConfig);
                var sceneTransitions = new SceneTransitionService(sceneLoader, loadingService, projectSettingsService);
                serviceRegistry.Register<ISceneTransitionService>(sceneTransitions);
            }
            else
            {
                DebugLogger.LogWarning("BootstrapEntryPoint: SceneLoaderConfig is not assigned, scene transitions will be unavailable");
            }

            var progressService = new PlayerPrefsProgressService();
            progressService.Load();
            serviceRegistry.Register<IProgressService>(progressService);

            var audioSettingsStorage = new PlayerPrefsAudioSettingsService();
            AudioSettings audioSettings = audioSettingsStorage.Load();
            var audioService = new UnityAudioService(_audioDatabase);
            audioService.Initialize(audioSettings, null);
            serviceRegistry.Register<Domain.Audio.IAudioService>(audioService);

            if (_resourcesRepository != null)
            {
                serviceRegistry.Register<ResourcesRepository>(_resourcesRepository);
                serviceRegistry.Register<ILevelRepository>(new JsonLevelRepository(_resourcesRepository));
            }
            else
            {
                DebugLogger.LogError("BootstrapEntryPoint: ResourcesRepository is not assigned");
            }
        }

        private async void Start()
        {
            if (!_loadMenuOnStart)
            {
                return;
            }

            ISceneTransitionService sceneTransitions;
            if (!Services.TryGet(out sceneTransitions) || sceneTransitions == null)
            {
                DebugLogger.LogError("BootstrapEntryPoint: ISceneTransitionService not resolved from Services");
                return;
            }

            await sceneTransitions.LoadMenuWithSplashAsync();
        }

        // private methods (implementation)
        private static void EnsureAppLovinMaxInitializedInternal()
        {
            // Активатор может быть не проставлен в сцене, поэтому инициализируем SDK программно.
            if (Object.FindObjectOfType<AppLovinMaxBootstrapper>(true) != null)
            {
                return;
            }

            var bootstrapperObject = new GameObject(nameof(AppLovinMaxBootstrapper));
            bootstrapperObject.AddComponent<AppLovinMaxBootstrapper>();
            DontDestroyOnLoad(bootstrapperObject);
        }

        private void EnsureAdjustActivatedInternal(IProjectSettingsService projectSettingsService)
        {
            if (FindObjectOfType<AdjustActivator>() != null)
            {
                return;
            }

            if (projectSettingsService == null || string.IsNullOrWhiteSpace(projectSettingsService.AdjustAppToken))
            {
                DebugLogger.LogWarning("[BootstrapEntryPoint] Adjust AppToken is empty in ProjectConfigs — Adjust will not be activated.");
                return;
            }

            var go = new GameObject(nameof(AdjustActivator));
            go.AddComponent<AdjustActivator>();
        }

        private void EnsureAppMetricaActivatedInternal(IProjectSettingsService projectSettingsService)
        {
            if (FindObjectOfType<AppMetricaActivator>() != null)
            {
                return;
            }

            if (projectSettingsService == null || string.IsNullOrWhiteSpace(projectSettingsService.AppMetricaAppId))
            {
                DebugLogger.LogWarning("[BootstrapEntryPoint] AppMetrica AppId is empty in ProjectConfigs — AppMetrica will not be activated.");
                return;
            }

            var go = new GameObject(nameof(AppMetricaActivator));
            go.AddComponent<AppMetricaActivator>();
        }
    }
}

