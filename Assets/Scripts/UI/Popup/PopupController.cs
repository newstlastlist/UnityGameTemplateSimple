using System;
using System.Collections.Generic;
using Infrastructure.Resources;
using UI.Popup.PrivacySettingsPopup;
using UnityEngine;

namespace UI.Popup
{
    public sealed class PopupController : MonoBehaviour, IPopupController
    {
        [Serializable]
        private sealed class PopupEntry
        {
            public PopupId Id;
            public PrefabFakeReference Prefab;
        }

        [SerializeField] private Transform _popupRoot;
        [SerializeField] private List<PopupEntry> _popups = new List<PopupEntry>();

        private readonly Dictionary<PopupId, PrefabFakeReference> _map = new Dictionary<PopupId, PrefabFakeReference>();

        public Transform PopupRoot => _popupRoot;

        private void Awake()
        {
            _map.Clear();
            foreach (var entry in _popups)
            {
                if (entry != null)
                {
                    _map[entry.Id] = entry.Prefab;
                }
            }
        }

        public bool TryGetReference(PopupId popupId, out PrefabFakeReference reference)
        {
            return _map.TryGetValue(popupId, out reference);
        }

        public IDisposable CreatePresenter(PopupId popupId, BasePopupView view)
        {
            // Centralize presenter creation per popup type here.
            // Example switch; replace cases with your real presenters.
            switch (popupId)
            {
                case PopupId.PrivacySettings: return new PrivacySettingsPopupPresenter((PrivacySettingsPopupView)view);
                default:
                    Debug.LogError($"Popup Id {popupId} not supported");
                    return null;
            }
        }
    }
}
