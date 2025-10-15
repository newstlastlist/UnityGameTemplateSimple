using System;

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
            // Логика будет добавлена позднее
        }

        private void OnCancelResetClickedHandler()
        {
            // Логика будет добавлена позднее
        }
    }
}






