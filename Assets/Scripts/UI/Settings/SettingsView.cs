using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] private Button _termsButton;
        [SerializeField] private Button _privacyButton;

        [SerializeField] private Toggle _audioOnOffToggle;
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Button _volumeButton;

        [SerializeField] private TMP_Text _versionText;
        [SerializeField] private Button _closeButton;

        public event Action OnTermsClicked;
        public event Action OnPrivacyClicked;
        public event Action<bool> OnAudioEnabledChanged;
        public event Action<bool> OnVibrationEnabledChanged;
        public event Action OnVolumeClicked;
        public event Action OnCloseClicked;

        public bool HasAudioToggle => _audioOnOffToggle != null;
        public bool HasVersionText => _versionText != null;

        private void Awake()
        {
            if (_termsButton != null)
            {
                _termsButton.onClick.AddListener(OnTermsClickedInternalHandler);
            }

            if (_privacyButton != null)
            {
                _privacyButton.onClick.AddListener(OnPrivacyClickedInternalHandler);
            }

            if (_audioOnOffToggle != null)
            {
                _audioOnOffToggle.onValueChanged.AddListener(OnAudioToggleChangedInternalHandler);
            }

            if (_vibrationToggle != null)
            {
                _vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChangedInternalHandler);
            }

            if (_volumeButton != null)
            {
                _volumeButton.onClick.AddListener(OnVolumeClickedInternalHandler);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClickedInternalHandler);
            }
        }

        private void OnDestroy()
        {
            if (_termsButton != null)
            {
                _termsButton.onClick.RemoveListener(OnTermsClickedInternalHandler);
            }

            if (_privacyButton != null)
            {
                _privacyButton.onClick.RemoveListener(OnPrivacyClickedInternalHandler);
            }

            if (_audioOnOffToggle != null)
            {
                _audioOnOffToggle.onValueChanged.RemoveListener(OnAudioToggleChangedInternalHandler);
            }

            if (_vibrationToggle != null)
            {
                _vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggleChangedInternalHandler);
            }

            if (_volumeButton != null)
            {
                _volumeButton.onClick.RemoveListener(OnVolumeClickedInternalHandler);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClickedInternalHandler);
            }
        }

        public void SetAudioToggleState(bool isAudioEnabled)
        {
            if (_audioOnOffToggle == null)
            {
                return;
            }

            _audioOnOffToggle.isOn = isAudioEnabled;
        }

        public void SetVersionLabel(string versionText)
        {
            if (_versionText == null)
            {
                return;
            }

            _versionText.text = versionText;
        }

        private void OnTermsClickedInternalHandler()
        {
            OnTermsClicked?.Invoke();
        }

        private void OnPrivacyClickedInternalHandler()
        {
            OnPrivacyClicked?.Invoke();
        }

        private void OnAudioToggleChangedInternalHandler(bool isAudioEnabled)
        {
            OnAudioEnabledChanged?.Invoke(isAudioEnabled);
        }

        private void OnVibrationToggleChangedInternalHandler(bool isVibrationEnabled)
        {
            OnVibrationEnabledChanged?.Invoke(isVibrationEnabled);
        }

        private void OnVolumeClickedInternalHandler()
        {
            OnVolumeClicked?.Invoke();
        }

        private void OnCloseClickedInternalHandler()
        {
            OnCloseClicked?.Invoke();
        }
    }
}
