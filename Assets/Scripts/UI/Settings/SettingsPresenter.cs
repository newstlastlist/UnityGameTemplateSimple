using System;
using App;
using Domain.Audio;
using Shared;
using UI.Popup;

namespace UI.Settings
{
    public sealed class SettingsPresenter
    {
        private readonly SettingsView _view;
        private readonly IScreenNavigator _screenNavigator;
        private readonly IAudioService _audioService;

        public SettingsPresenter(SettingsView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _screenNavigator = Services.Get<IScreenNavigator>();
            _audioService = Services.Get<IAudioService>();
        }

        public void Open()
        {
            _view.OnTermsClicked += OnTermsClickedHandler;
            _view.OnPrivacyClicked += OnPrivacyClickedHandler;
            _view.OnAudioEnabledChanged += OnAudioEnabledChangedHandler;
            _view.OnVibrationEnabledChanged += OnVibrationEnabledChangedHandler;
            _view.OnVolumeClicked += OnVolumeClickedHandler;
            _view.OnCloseClicked += OnCloseClickedHandler;

            _view.SetAudioToggleState(!_audioService.IsMuted);
            _view.SetVersionLabel($"Version {UnityEngine.Application.version}");
        }

        public void Close()
        {
            _view.OnTermsClicked -= OnTermsClickedHandler;
            _view.OnPrivacyClicked -= OnPrivacyClickedHandler;
            _view.OnAudioEnabledChanged -= OnAudioEnabledChangedHandler;
            _view.OnVibrationEnabledChanged -= OnVibrationEnabledChangedHandler;
            _view.OnVolumeClicked -= OnVolumeClickedHandler;
            _view.OnCloseClicked -= OnCloseClickedHandler;
        }

        private void OnTermsClickedHandler()
        {
            UnityEngine.Application.OpenURL("https://multicastgames.com/termsofuse");
        }

        private void OnPrivacyClickedHandler()
        {
            var popupService = Services.Get<IPopupService>();
            popupService.Show(PopupId.PrivacySettings);
        }

        private void OnAudioEnabledChangedHandler(bool isAudioEnabled)
        {
            _audioService.SetMuted(!isAudioEnabled);
        }

        private void OnVibrationEnabledChangedHandler(bool isVibrationEnabled)
        {
            // TODO: добавить логику вибрации, когда появится система
        }

        private void OnVolumeClickedHandler()
        {
            // TODO: реализовать UX для настройки громкости по решению ГД
        }

        private void OnCloseClickedHandler()
        {
            // Settings — оверлей: просто скрываем панель, не меняя текущий экран (Game/Main).
            _view.Show(false);
            Close();
        }
    }
}
