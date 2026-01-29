using System;
using App;
using Infrastructure;
using Infrastructure.Settings;
using Shared;
using UI.Tutor;
using WS.Core.SDK.AppLovin;
using WS.Core.SDK.AppMetrica;

namespace UI.Game
{
    public sealed class GamePresenter
    {
        private readonly GameView _view;
        private readonly IScreenNavigator _screenNavigator;
        private readonly IProgressService _progressService;
        private readonly ILevelRepository _levelRepository;
        private readonly IProjectSettingsService _projectSettingsService;

        private int _currentLevelIndex;

        public GamePresenter(GameView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _screenNavigator = Services.Get<IScreenNavigator>();
            _progressService = Services.Get<IProgressService>();
            _levelRepository = Services.Get<ILevelRepository>();
            _projectSettingsService = Services.Get<IProjectSettingsService>();
        }

        public void Open()
        {
            _currentLevelIndex = ResolveCurrentLevelIndexInternal();
            _view.ShowLevelNumber(_currentLevelIndex + 1);

            AppMetricaAnalytics.Instance?.ReportLevelStarted(_currentLevelIndex + 1, _currentLevelIndex);

            _view.OnRestartClicked -= OnRestartClickedInternalHandler;
            _view.OnRestartClicked += OnRestartClickedInternalHandler;

            _view.OnSettingsClicked -= OnSettingsClickedInternalHandler;
            _view.OnSettingsClicked += OnSettingsClickedInternalHandler;
        }

        public void Close()
        {
            _view.OnRestartClicked -= OnRestartClickedInternalHandler;
            _view.OnSettingsClicked -= OnSettingsClickedInternalHandler;
        }

        private void OnRestartClickedInternalHandler()
        {
            if (_screenNavigator == null)
            {
                return;
            }

            _screenNavigator.Show(PanelType.Game);
        }

        private void OnSettingsClickedInternalHandler()
        {
            if (_screenNavigator == null)
            {
                return;
            }

            bool shown = TryShowInterstitialIfAllowedInternal(() => _screenNavigator.Show(PanelType.Settings));
            if (!shown)
            {
                _screenNavigator.Show(PanelType.Settings);
            }
        }

        private int ResolveCurrentLevelIndexInternal()
        {
            int levelsCount = _levelRepository != null ? _levelRepository.Count : 0;
            if (_progressService == null)
            {
                return 0;
            }

            int currentIndex = _progressService.ResolveCurrentLevelIndex(levelsCount);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            return currentIndex;
        }

        private bool TryShowInterstitialIfAllowedInternal(Action onClosed)
        {
            if (_projectSettingsService == null)
            {
                return false;
            }

            if (Services.TryGet<ITutorService>(out var tutorService) && tutorService != null && tutorService.HasScenarioForLevel(_currentLevelIndex))
            {
                return false;
            }

            int currentLevelNumber = _currentLevelIndex + 1;
            if (currentLevelNumber < _projectSettingsService.InterstitialStartLevel)
            {
                return false;
            }

            if (!Services.TryGet<AppLovinMaxAdService>(out var adService) || adService == null)
            {
                return false;
            }

            return adService.TryShowGeneralInterstitial(onClosed);
        }
    }
}
