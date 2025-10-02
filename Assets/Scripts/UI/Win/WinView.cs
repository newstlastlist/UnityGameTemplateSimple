using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Win
{
    public sealed class WinView : MonoBehaviour
    {

        [Header("Buttons")]
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _nextButton;

        public event Action OnMainMenuClicked;
        public event Action OnNextLevelClicked;

        private void Awake()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClickedHandler);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextLevelClickedHandler);
            }
        }

        private void OnDestroy()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClickedHandler);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextLevelClickedHandler);
            }
        }

  
        private void OnMainMenuClickedHandler()
        {
            OnMainMenuClicked?.Invoke();
        }

        private void OnNextLevelClickedHandler()
        {
            OnNextLevelClicked?.Invoke();
        }
    }
}