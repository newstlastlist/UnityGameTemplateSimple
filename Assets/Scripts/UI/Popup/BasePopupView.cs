using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popup
{
    public abstract class BasePopupView : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;

        public event Action OnClosed;

        protected virtual void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClickedHandler);
            }

            Init();
        }

        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClickedHandler);
            }
        }
        
        protected abstract void Init();

        public void Close()
        {
            OnClosed?.Invoke();
        }

        private void OnCloseClickedHandler()
        {
            Close();
        }
    }
}
