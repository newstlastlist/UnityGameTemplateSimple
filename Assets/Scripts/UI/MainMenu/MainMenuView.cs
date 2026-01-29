using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Infrastructure;

namespace UI.MainMenu
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private TextMeshProUGUI _levelText;

        public void Initialize(System.Action onPlayClicked, int levelNumber)
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() => onPlayClicked?.Invoke());
            }

            if (_levelText != null)
            {
                _levelText.text = $"Level {levelNumber}";
            }
        }
    }
}