using System;
using App;
using Infrastructure;
using Infrastructure.SceneManagement;
using Shared;
using Cysharp.Threading.Tasks;

namespace UI.Win
{
    public sealed class WinPresenter
    {
        private readonly WinView _view;
        private readonly ILevelRepository _levelRepository;
        private readonly IProgressService _progressService;
        private readonly IScreenNavigator _screenNavigator;
        private readonly ISceneTransitionService _sceneTransitionService;

        public WinPresenter(WinView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelRepository = Services.Get<ILevelRepository>();
            _progressService = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
            Services.TryGet<ISceneTransitionService>(out _sceneTransitionService);
        }

        public void Open()
        {
            _view.OnMainMenuClicked += OnMainMenuClickedHandler;
            _view.OnNextLevelClicked += OnNextLevelClickedHandler;

            int streak = _progressService.GetWinStreak();
            _view.ShowWinStreak(streak);
        }

        public void Close()
        {
            _view.OnMainMenuClicked -= OnMainMenuClickedHandler;
            _view.OnNextLevelClicked -= OnNextLevelClickedHandler;
        }

        private void OnMainMenuClickedHandler()
        {
            if (_sceneTransitionService != null)
            {
                _sceneTransitionService.LoadMenuWithSplashAsync().Forget();
                return;
            }

            // Фолбэк на старую экранную навигацию, если сервис переходов сцен не зарегистрирован.
            _screenNavigator.Show(PanelType.Main);
        }

        private void OnNextLevelClickedHandler()
        {
            _screenNavigator.Show(PanelType.Game);
        }
    }
}