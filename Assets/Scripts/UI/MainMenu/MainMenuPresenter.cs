using System;
using App;
using Infrastructure;
using Shared;

namespace UI.MainMenu
{
    public sealed class MainMenuPresenter
    {
        private readonly MainMenuView _view;
        private readonly ILevelRepository _levelRepository;
        private readonly IProgressService _progressService;
        private readonly IScreenNavigator _screenNavigator;

        public MainMenuPresenter(MainMenuView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelRepository = Services.Get<ILevelRepository>();
            _progressService = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
        }

        public void Open()
        {
            _view.OnPlayClicked += OnPlayClickedHandler;
        }

        public void Close()
        {
            _view.OnPlayClicked -= OnPlayClickedHandler;
        }

        private void OnPlayClickedHandler()
        {
            _screenNavigator.Show(ScreenId.Game);
        }
    }
}