using Infrastructure;
using System.Collections;
using System.Threading.Tasks;
using Domain;
using Domain.Audio;
using Infrastructure.Audio;
using Infrastructure.Resources;
using UI.Popup;
using UI.Loading;
using Shared;
using UI.Game;
using UI.MainMenu;
using UI.Settings;
using UI.Win;
using UnityEngine;
using AudioSettings = Domain.Audio.AudioSettings;
using Infrastructure.Settings;
using Domain.Project;
using SettingsPresenter = UI.Settings.SettingsPresenter;

namespace App
{
    public sealed class GameApp : MonoBehaviour
    {
        [Header("Core Controllers")]
        [SerializeField] private ScreenController _screenController;
        [SerializeField] private AudioDatabase _audioDatabase;
        [SerializeField] private PopupController _popupController;

        [Header("Loading Screen")] 
        [SerializeField] private Transform _loadingUiRoot;
        [SerializeField] private PrefabFakeReference _loadingScreenPrefab;

		[Header("Project Settings")]
		[SerializeField] private ProjectSettings _projectSettings;

        private ServiceRegistry _serviceRegistry;
        private PlayerPrefsAudioSettingsService _audioSettingsStorage;

        private MainMenuPresenter _mainMenuPresenter;
        private GamePresenter _gamePresenter;
        private WinPresenter _winPresenter;
        private UI.Settings.SettingsPresenter _settingsPresenter;
        private IPopupService _popupService;
        private TaskCompletionSource<bool> _startupGate;
        private IProjectSettingsService _projectSettingsService;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            _serviceRegistry = new ServiceRegistry();
            
            Services.SetProvider(_serviceRegistry);
            var resourceService = new UnityResourceService();
            _serviceRegistry.Register<IResourceService>(resourceService);

			// Project Settings service
			var projectSettingsService = new ProjectSettingsService(_projectSettings);
			_serviceRegistry.Register<IProjectSettingsService>(projectSettingsService);

            // Loading
            string loadingPath = _loadingScreenPrefab != null ? _loadingScreenPrefab.GetResourcesRelativePath() : null;
            var loadingService = new LoadingService(resourceService, loadingPath, _loadingUiRoot);
            _serviceRegistry.Register<ILoadingService>(loadingService);
			// Показать загрузку на старте минимум N секунд (из ProjectSettings) и закрыть только после EndOfFrame
            _startupGate = new TaskCompletionSource<bool>();
            _projectSettingsService = Services.Get<IProjectSettingsService>();
            _ = Services.Get<ILoadingService>().RunUntil(_startupGate.Task, _projectSettingsService.StartupLoadingMinSeconds);
            
            var progressService = new PlayerPrefsProgressService();
            progressService.Load();

            _serviceRegistry.Register<IProgressService>(progressService);
            _serviceRegistry.Register<IScreenNavigator>(new ScreenNavigatorService(_screenController));
            

            _audioSettingsStorage = new PlayerPrefsAudioSettingsService();
            AudioSettings audioSettings = _audioSettingsStorage.Load();

            var audioService = new UnityAudioService(_audioDatabase);
            audioService.Initialize(audioSettings, null);
            _serviceRegistry.Register<IAudioService>(audioService);

            _popupService = new PopupService(_popupController);
            _serviceRegistry.Register<IPopupService>(_popupService);

            var mainMenuView = _screenController.GetViewOnPanel<MainMenuView>(ScreenId.Main);
            var gameView = _screenController.GetViewOnPanel<GameView>(ScreenId.Game);
            var winView = _screenController.GetViewOnPanel<WinView>(ScreenId.Win);
            var settingsView = _screenController.GetViewOnPanel<SettingsView>(ScreenId.Settings);

            _mainMenuPresenter = new MainMenuPresenter(mainMenuView);
            _gamePresenter = new GamePresenter(gameView);
            _winPresenter = new WinPresenter(winView);
            _settingsPresenter = new SettingsPresenter(settingsView);

            _screenController.OnScreenShown += OnScreenShown;

            _screenController.Show(ScreenId.Main);
        }

        private void Start()
        {
            StartCoroutine(CompleteStartupGateAtEndOfFrame());
        }

        private void OnDestroy()
        {
            _screenController.OnScreenShown -= OnScreenShown;

            if (Services.TryGet<IProgressService>(out var progress))
            {
                progress.Save();
            }

            if (_audioSettingsStorage != null && Services.TryGet<IAudioService>(out var audio))
            {
                var toSave = new AudioSettings
                {
                    IsMuted = audio.IsMuted,
                    MasterVolume = audio.MasterVolume,
                    MusicVolume = audio.MusicVolume,
                    SfxVolume = audio.SfxVolume
                };
                _audioSettingsStorage.Save(toSave);
            }
        }

        private void OnScreenShown(ScreenId id)
        {
            _mainMenuPresenter.Close();
            _gamePresenter.Close();
            _winPresenter.Close();
            _settingsPresenter.Close();

            switch (id)
            {
                case ScreenId.Main:
                    _mainMenuPresenter.Open();
                    break;

                case ScreenId.Game:
                    _gamePresenter.Open();
                    break;

                case ScreenId.Win:
                    _winPresenter.Open();
                    break;

                case ScreenId.Settings:
                    _settingsPresenter.Open();
                    break;
            }
        }

        private IEnumerator CompleteStartupGateAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            if (_startupGate != null && !_startupGate.Task.IsCompleted)
            {
                _startupGate.SetResult(true);
            }
        }
    }
}