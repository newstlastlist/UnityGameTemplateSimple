using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace UI.Game
{
    public sealed class GameView : UI.PanelBase
    {
        [SerializeField] private Button _restartBtn;
        [SerializeField] private Button _settingsBtn;
        [SerializeField] private TextMeshProUGUI _levelNumber;

        public event Action OnRestartClicked;
        public event Action OnSettingsClicked;

        private void Awake()
        {
            if (_restartBtn != null)
            {
                _restartBtn.onClick.AddListener(OnRestartClickedInternalHandler);
            }

            if (_settingsBtn != null)
            {
                _settingsBtn.onClick.AddListener(OnSettingsClickedInternalHandler);
            }
        }

        private void OnDestroy()
        {
            if (_restartBtn != null)
            {
                _restartBtn.onClick.RemoveListener(OnRestartClickedInternalHandler);
            }

            if (_settingsBtn != null)
            {
                _settingsBtn.onClick.RemoveListener(OnSettingsClickedInternalHandler);
            }
        }

        public void SetActiveBoosterBtn(bool active)
        {
            // Бустеры в этом проекте пока не используются.
        }

        public void ShowLevelNumber(int humanLevelNumber)
        {
            if (_levelNumber == null)
            {
                return;
            }

            if (humanLevelNumber < 1)
            {
                humanLevelNumber = 1;
            }

            _levelNumber.text = $"LEVEL {humanLevelNumber}";
        }

        private void OnRestartClickedInternalHandler()
        {
            OnRestartClicked?.Invoke();
        }

        private void OnSettingsClickedInternalHandler()
        {
            OnSettingsClicked?.Invoke();
        }

        public override void OnOpenHandler()
        {
            // Пока ничего не требуется при открытии экрана игры
        }
    }
}