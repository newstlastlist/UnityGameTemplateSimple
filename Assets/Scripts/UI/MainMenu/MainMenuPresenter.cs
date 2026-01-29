using System;
using App;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Infrastructure.SceneManagement;
using Shared;

namespace UI.MainMenu
{
    public sealed class MainMenuPresenter
    {
        private readonly MainMenuView _view;
        private readonly ISceneTransitionService _sceneTransitions;

        public MainMenuPresenter(MainMenuView view, ISceneTransitionService sceneTransitions)
        {
            _view = view;
            _sceneTransitions = sceneTransitions;
        }

        public void Initialize()
        {
            int levelNumber = ResolveLevelNumber();
            _view.Initialize(OnPlayClicked, levelNumber);
        }

        private int ResolveLevelNumber()
        {
            // Используем PlayerPrefsProgressService, чтобы показать следующий уровень после последнего пройденного
            PlayerPrefsProgressService progress = new PlayerPrefsProgressService();
            progress.Load();

            int value = progress.LastCompletedLevelIndex + 2; // -1 -> 1, 0 -> 2 и т.д.
            return value < 1 ? 1 : value;
        }

        private void OnPlayClicked()
        {
            // Прячем UI главного меню, чтобы он не перекрывал сплэш
            if (_view != null)
            {
                _view.gameObject.SetActive(false);
            }

            // int levelNumber = ResolveLevelNumber();
            // bool isSecondLevelOrAbove = levelNumber >= 2;
            //
            // if (isSecondLevelOrAbove && Services.TryGet<AppLovinMaxAdService>(out var adService))
            // {
            //     bool shown = adService.TryShowGeneralInterstitial(
            //         onClosed: () => _sceneTransitions?.LoadMainWithSplashAsync().Forget());
            //
            //     if (shown)
            //     {
            //         return;
            //     }
            // }

            // Если интер не показан (кулдаун/нет готового), просто переходим в игру.
            _sceneTransitions?.LoadMainWithSplashAsync().Forget();
        }
    }
}