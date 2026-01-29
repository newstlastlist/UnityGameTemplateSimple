using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Win
{
    public sealed class WinView : UI.PanelBase
    {
        [Header("Buttons")]
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _nextButton;
        
        [Header("WinStreak")]
        [SerializeField] private GameObject _winsStreakSection;
        [SerializeField] private TextMeshProUGUI _winStreakText;
        
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

        public void ShowWinStreak(int currentWinStreak)
        {
            if (_winStreakText == null || _winsStreakSection == null)
            {
                return;
            }
            bool hasStreak = currentWinStreak > 0;
            _winsStreakSection.SetActive(hasStreak);
            if (hasStreak)
            {
                _winStreakText.text = currentWinStreak.ToString();
            }
        }

        public override void OnOpenHandler()
        {
            // Пока ничего не требуется при открытии экрана победы
        }
    }
}