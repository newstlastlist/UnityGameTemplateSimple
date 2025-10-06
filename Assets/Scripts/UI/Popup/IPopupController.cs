using Infrastructure.Resources;
using UnityEngine;

namespace UI.Popup
{
    public interface IPopupController
    {
        bool TryGetReference(PopupId popupId, out PrefabFakeReference reference);
        System.IDisposable CreatePresenter(PopupId popupId, BasePopupView view);
        Transform PopupRoot { get; }
    }
}
