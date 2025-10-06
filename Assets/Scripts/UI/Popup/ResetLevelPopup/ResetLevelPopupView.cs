using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popup.ResetLevelPopup
{
    public class ResetLevelPopupView : BasePopupView
    {
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _saveButton;

        public event Action OnConfirmResetClicked;
        public event Action OnCancelResetClicked;

        protected override void Init()
        {
            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(OnConfirmResetClickedInternalHandler);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.AddListener(OnCancelResetClickedInternalHandler);
            }
        }

        protected override void OnDestroy()
        {
            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveListener(OnConfirmResetClickedInternalHandler);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(OnCancelResetClickedInternalHandler);
            }

            base.OnDestroy();
        }

        private void OnConfirmResetClickedInternalHandler()
        {
            OnConfirmResetClicked?.Invoke();
        }

        private void OnCancelResetClickedInternalHandler()
        {
            OnCancelResetClicked?.Invoke();
        }
    }
}
