using System;
using Shared;
using App;
using Infrastructure;

namespace UI.Popup.ResetLevelPopup
{
    public class ResetLevelPopupPresenter : IDisposable
    {
        private readonly ResetLevelPopupView _view;

        public ResetLevelPopupPresenter(ResetLevelPopupView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _view.OnConfirmResetClicked += OnConfirmResetClickedHandler;
            _view.OnCancelResetClicked += OnCancelResetClickedHandler;
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnConfirmResetClicked -= OnConfirmResetClickedHandler;
                _view.OnCancelResetClicked -= OnCancelResetClickedHandler;
            }
        }

        private void OnConfirmResetClickedHandler()
        {
            var progress = Services.Get<IProgressService>();
            progress.ResetWinStreak();
            _view.Close();
            Services.Get<IScreenNavigator>().Show(PanelType.Game);
        }

        private void OnCancelResetClickedHandler()
        {
            _view.Close();
        }
    }
}


