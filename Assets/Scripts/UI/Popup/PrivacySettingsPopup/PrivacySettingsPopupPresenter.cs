using System;
using Domain.Audio;
using Infrastructure;
using Shared;
using UnityEngine;

namespace UI.Popup.PrivacySettingsPopup
{
    public class PrivacySettingsPopupPresenter : IDisposable
    {
        private readonly PrivacySettingsPopupView _view;

        public PrivacySettingsPopupPresenter(PrivacySettingsPopupView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _view.OnPrivatePolicyClicked += OnPrivatePolicyClickedHandler;
            _view.OnDeleteMyDataClicked += OnDeleteMyDataClickedHandler;
        }
        
        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnPrivatePolicyClicked -= OnPrivatePolicyClickedHandler;
                _view.OnDeleteMyDataClicked -= OnDeleteMyDataClickedHandler;
            }
        }

        private void OnPrivatePolicyClickedHandler()
        {
            Application.OpenURL("https://multicastgames.com/policy");
        }

        private void OnDeleteMyDataClickedHandler()
        {
            // Очистка сохранённых пользовательских данных
            try
            {
                if (Services.TryGet<IProgressService>(out var progressService))
                {
                    progressService.Reset();
                }

                if (Services.TryGet<IAudioService>(out var audioService))
                {
                    audioService.SetMuted(false);
                    audioService.SetMasterVolume(1f);
                    audioService.SetMusicVolume(1f);
                    audioService.SetSfxVolume(1f);
                }

                if (Services.TryGet<IProgressService>(out var progressTools))
                {
                    progressTools.ClearAllPersistedData();
                }
            }
            catch (Exception)
            {
                // Ничего: намеренно не падаем из-за платформенных ограничений или отсутствующих сервисов
            }

            _view.Close();
        }
    }
}