using Infrastructure;
using Shared;
using UI.Game;
using UI.MainMenu;
using UI.Win;
using UnityEngine;

namespace App
{
    public sealed class GameApp : MonoBehaviour
    {
        [SerializeField] private ScreenController _screenController;

        private ServiceRegistry _serviceRegistry;

        private MainMenuPresenter _mainMenuPresenter;
        private GamePresenter _gamePresenter;
        private WinPresenter _winPresenter;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            _serviceRegistry = new ServiceRegistry();
            Services.SetProvider(_serviceRegistry);

            var progressService = new PlayerPrefsProgressService();
            progressService.Load();

            _serviceRegistry.Register<IProgressService>(progressService);
            _serviceRegistry.Register<IScreenNavigator>(new ScreenNavigator(_screenController));

            var mainMenuView = _screenController.GetViewOnPanel<MainMenuView>(ScreenId.Main);
            var gameView = _screenController.GetViewOnPanel<GameView>(ScreenId.Game);
            var winView = _screenController.GetViewOnPanel<WinView>(ScreenId.Win);

            _mainMenuPresenter = new MainMenuPresenter(mainMenuView);
            _gamePresenter = new GamePresenter(gameView);
            _winPresenter = new WinPresenter(winView);

            _screenController.OnScreenShown += OnScreenShown;

            _screenController.Show(ScreenId.Main);
        }

        private void OnDestroy()
        {
            _screenController.OnScreenShown -= OnScreenShown;

            if (Services.TryGet<IProgressService>(out var progress))
            {
                progress.Save();
            }
        }

        private void OnScreenShown(ScreenId id)
        {
            _mainMenuPresenter.Close();
            _gamePresenter.Close();
            _winPresenter.Close();

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
            }
        }
    }
}