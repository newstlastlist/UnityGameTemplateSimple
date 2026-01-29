using Domain.Audio;
using Infrastructure;
using Infrastructure.Audio;
using Infrastructure.Resources;
using Infrastructure.Settings;
using Shared;
using Sirenix.OdinInspector;
using UI.Game;
using UI.Popup;
using UI.Settings;
using UI.Tutor;
using UI.Win;
using UnityEngine;
using AudioSettings = Domain.Audio.AudioSettings;
using SettingsPresenter = UI.Settings.SettingsPresenter;

// (removed duplicate using)

namespace App
{
    /// <summary>
    /// Точка входа на игровой сцене.
    /// Настраивает экранную навигацию, попапы и презентеры, используя уже зарегистрированные в BootstrapEntryPoint сервисы.
    /// </summary>
    public sealed class GameplayEntryPoint : MonoBehaviour
    {
        [Header("Core Controllers")]
        [SerializeField] private ScreenController _screenController;
        [SerializeField] private PopupController _popupController;
        
        [Header("Tutorials")]
        [SerializeField] private TutorView _tutorView;

        private GamePresenter _gamePresenter;
        private WinPresenter _winPresenter;
        private SettingsPresenter _settingsPresenter;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            // На этом этапе все базовые сервисы уже зарегистрированы BootstrapEntryPoint.
            // Здесь добавляем только сервисы, зависящие от объектов игровой сцены.

            Services.Register<IScreenNavigator>(new ScreenNavigatorService(_screenController));

            var popupService = new PopupService(_popupController);
            Services.Register<IPopupService>(popupService);

            var gameView = _screenController.GetViewOnPanel<GameView>(PanelType.Game);
            var winView = _screenController.GetViewOnPanel<WinView>(PanelType.Win);
            var settingsView = _screenController.GetViewOnPanel<SettingsView>(PanelType.Settings);

            _gamePresenter = new GamePresenter(gameView);
            _winPresenter = new WinPresenter(winView);
            _settingsPresenter = new SettingsPresenter(settingsView);

            // Tutor service
            if (_tutorView != null)
            {
                var tutorService = new ProjectTutorService(_tutorView);
                Services.Register<ITutorService>(tutorService);
            }
            else
            {
                DebugLogger.LogWarning("[Tutor] TutorView не назначен — туторы отключены");
            }

            _screenController.OnScreenShown += OnScreenShown;
        }

        private void Start()
        {
            // Гарантируем, что хотя бы один раз будет вызван OnScreenShown,
            // чтобы презентеры успели подписаться на события вьюх.
            _screenController.Show(PanelType.Game);
        }

        private void OnDestroy()
        {
            _screenController.OnScreenShown -= OnScreenShown;

            if (Services.TryGet<IProgressService>(out var progress))
            {
                progress.Save();
            }

            if (Services.TryGet<IAudioService>(out var audio))
            {
                var storage = new PlayerPrefsAudioSettingsService();
                var toSave = new AudioSettings
                {
                    IsMuted = audio.IsMuted,
                    MasterVolume = audio.MasterVolume,
                    MusicVolume = audio.MusicVolume,
                    SfxVolume = audio.SfxVolume
                };
                storage.Save(toSave);
            }
        }

        private void OnScreenShown(PanelType id)
        {
            _gamePresenter.Close();
            _winPresenter.Close();
            _settingsPresenter.Close();

            switch (id)
            {
                case PanelType.Game:
                    _gamePresenter.Open();
                    break;

                case PanelType.Win:
                    _winPresenter.Open();
                    break;

                case PanelType.Settings:
                    _settingsPresenter.Open();
                    break;
            }
        }

        #if UNITY_EDITOR
        [Button("Simulate Win")]
        private void SimulateWin()
        {
            if (!Services.TryGet<IProgressService>(out var progress) ||
                !Services.TryGet<ILevelRepository>(out var levelRepository))
            {
                DebugLogger.LogWarning("[GameplayEntryPoint] Cannot simulate win: progress or level repository is not registered.");
                return;
            }

            int levelsCount = levelRepository.Count;
            int currentIndex = progress.ResolveCurrentLevelIndex(levelsCount);

            // Обновляем прогресс так же, как при реальной победе.
            progress.IncrementWinStreak();
            progress.OnLevelCompleted(levelsCount, currentIndex);
            progress.Save();

            // Показываем экран победы через ScreenController.
            if (_screenController != null)
            {
                _screenController.Show(PanelType.Win);
            }
        }
        #endif
    }
}