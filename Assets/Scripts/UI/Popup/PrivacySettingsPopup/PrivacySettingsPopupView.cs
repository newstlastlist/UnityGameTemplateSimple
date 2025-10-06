using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popup.PrivacySettingsPopup
{
    public class PrivacySettingsPopupView : BasePopupView
    {
        [SerializeField] private Button _privatePolicyBtn;
        [SerializeField] private Button _deleteMyDataBtn;
        
        public event Action OnPrivatePolicyClicked;
        public event Action OnDeleteMyDataClicked;

        protected override void Init()
        {
            if (_privatePolicyBtn != null)
            {
                _privatePolicyBtn.onClick.AddListener(OnPrivatePolicyClickedInternalHandler);
            }

            if (_deleteMyDataBtn != null)
            {
                _deleteMyDataBtn.onClick.AddListener(OnDeleteMyDataClickedInternalHandler);
            }
        }

        protected override void OnDestroy()
        {
            if (_privatePolicyBtn != null)
            {
                _privatePolicyBtn.onClick.RemoveListener(OnPrivatePolicyClickedInternalHandler);
            }

            if (_deleteMyDataBtn != null)
            {
                _deleteMyDataBtn.onClick.RemoveListener(OnDeleteMyDataClickedInternalHandler);
            }

            base.OnDestroy();
        }

        private void OnPrivatePolicyClickedInternalHandler()
        {
            OnPrivatePolicyClicked?.Invoke();
        }

        private void OnDeleteMyDataClickedInternalHandler()
        {
            OnDeleteMyDataClicked?.Invoke();
        }
    }
}