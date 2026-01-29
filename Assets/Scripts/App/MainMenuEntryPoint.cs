using Infrastructure.SceneManagement;
using Shared;
using UI.MainMenu;
using UnityEngine;

namespace App
{
    public sealed class MainMenuEntryPoint : MonoBehaviour
    {
        [SerializeField] private MainMenuView _view;

        private void Start()
        {
            if (_view == null)
            {
                _view = GetComponent<MainMenuView>();
            }

            if (_view == null)
            {
                DebugLogger.LogError("MainMenuEntryPoint: MainMenuView is not assigned");
                return;
            }

            ISceneTransitionService sceneTransitions;
            if (!Services.TryGet(out sceneTransitions) || sceneTransitions == null)
            {
                DebugLogger.LogError("MainMenuEntryPoint: ISceneTransitionService not resolved from Services");
                return;
            }

            var presenter = new MainMenuPresenter(_view, sceneTransitions);
            presenter.Initialize();
        }
    }
}